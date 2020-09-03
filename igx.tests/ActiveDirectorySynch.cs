using d360.extensions.azuregraph;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace igx.tests
{
    public class GroupModel
    {
        public int ID { get; set; }
        public int FusionID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class GroupResultModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

    public class UserModel
    {
        public int ID { get; set; }
        public int FusionID { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DisplayName { get; set; }
        public string CountryCode { get; set; }
        public string UserPrincipalName { get; set; }
        public string Description { get; set; }
    }

    public class UserResultModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

    public class MemberModel
    {
        public int UserAttributeID { get; set; }
        public string User { get; set; }
        public int GroupAttributeID { get; set; }
        public string Group { get; set; }
    }


    [TestClass]
    public class ActiveDirectorySynch : BaseTest
    {
        [TestMethod]
        public void SynchGroups()
        {
            var companyID = 100;    // 10
            
            #region Sql

            var groupAttributeTypeID = 878;
            var groupSql = $@"
select	O.ID,
        O.FusionID,
        O.Name,
		F1.FormattedValue as Description
from	FusionAttribute O
		left join Field F1 on F1.ObjectType = 'FusionAttribute' and F1.ObjectID = O.ID and F1.FieldTypeID = 50264
where	FusionAttributeTypeID = {groupAttributeTypeID}";

            var userAttributeTypeID = 879;
            var userSql = $@"
select	O.ID,
        O.FusionID,
        O.Name,
		ltrim(rtrim(replace(SUBSTRING(O.Name, charindex(',', O.Name, 1), len(O.Name)), ',',''))) as FirstName,
		coalesce(ltrim(rtrim(replace(SUBSTRING(O.Name, 1, charindex(',', O.Name, 1)), ',',''))), O.Name) as LastName,
		F1.FormattedValue as DisplayName,
		F2.FormattedValue as CountryCode,
		F3.FormattedValue as UserPrincipalName,
		F4.FormattedValue as Description
from	FusionAttribute O
		left join Field F1 on F1.ObjectType = 'FusionAttribute' and F1.ObjectID = O.ID and F1.FieldTypeID = 50265
		left join Field F2 on F2.ObjectType = 'FusionAttribute' and F2.ObjectID = O.ID and F2.FieldTypeID = 50266
		left join Field F3 on F3.ObjectType = 'FusionAttribute' and F3.ObjectID = O.ID and F3.FieldTypeID = 50267
		left join Field F4 on F4.ObjectType = 'FusionAttribute' and F4.ObjectID = O.ID and F4.FieldTypeID = 50268
where	FusionAttributeTypeID = {userAttributeTypeID}";

            var membershipSql = $@"
select	U.ID as UserAttributeID,
		F3.FormattedValue as [User],
		G.ID as GroupAttributeID,
		G.Name as [Group]
from	FusionAttribute U
		inner join Field F3 on F3.ObjectType = 'FusionAttribute' and F3.ObjectID = U.ID and F3.FieldTypeID = 50267
		inner join [Intersect] I on I.Subject = 'FusionAttribute' and I.SubjectID = U.ID 
		inner join FusionAttribute G on I.Object = 'FusionAttribute' and G.ID = I.ObjectID";

            #endregion

            var community = getCommunityConnection();
            var company = getCompanyConnection(companyID);

            var groups = company.Query<GroupModel>(groupSql).ToList();
            var users = company.Query<UserModel>(userSql).ToList();
            var memberships = company.Query<MemberModel>(membershipSql).ToList();

            var dt = DateTime.UtcNow;
            List<GroupResultModel> groupResults;
            List<UserResultModel> userResults;

            company.Open();
            //company.OpenWithRetry(RetryPolicy.DefaultFixed);
            using (var transG = company.BeginTransaction())
            {
                company.Execute(@"
set nocount on 
create table #Group (
	Name nvarchar(250) not null, 
	Description nvarchar(max) null
)
set nocount off", commandTimeout: 3600, transaction: transG);

                using (var bulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.Default, transG))
                {
                    bulkCopy.BatchSize = groups.Count;
                    bulkCopy.DestinationTableName = "#Group";
                    bulkCopy.BulkCopyTimeout = 3600;

                    var table = new System.Data.DataTable();

                    #region Create column mappings

                    var columnName = "Name";
                    table.Columns.Add(columnName, typeof(string));
                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                    columnName = "Description";
                    table.Columns.Add(columnName, typeof(string));
                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                    #endregion

                    foreach (var item in groups)
                    {
                        var row = table.NewRow();

                        row["Name"] = item.Name;
                        row["Description"] = item.Description;

                        table.Rows.Add(row);
                    }

                    bulkCopy.WriteToServer(table);
                }

                groupResults = company.Query<GroupResultModel>(@"
create table #GroupResult (
    ID int not null,
    Name nvarchar(250) not null
);

merge   [Group] as T 
using   ( 
        select  *
        from    #Group
        ) as S 
        on  (
            T.Name = S.Name
            )
when    matched then
update  set T.Description = S.Description 
when    not matched by target then 
        insert (Name, Description, UpdatedOn, UpdatedBy) 
        values (S.Name, S.Description, getutcdate(), 0)
output  inserted.ID, S.Name into #GroupResult;

select * from #GroupResult;",
commandTimeout: 3600, transaction: transG).ToList();


                transG.Commit();
            }

            community.Open();
            //community.OpenWithRetry(RetryPolicy.DefaultFixed);
            using (var transU = community.BeginTransaction())
            {
                community.Execute(@"
set nocount on 
create table #Resource (
	Username nvarchar(250) not null, 
	FirstName nvarchar(250) not null, 
	LastName nvarchar(250) not null
)
set nocount off", commandTimeout: 3600, transaction: transU);

                using (var bulkCopy = new SqlBulkCopy(community, SqlBulkCopyOptions.Default, transU))
                {
                    bulkCopy.BatchSize = users.Count;
                    bulkCopy.DestinationTableName = "#Resource";
                    bulkCopy.BulkCopyTimeout = 3600;

                    var table = new System.Data.DataTable();

                    #region Create column mappings

                    var columnName = "Username";
                    table.Columns.Add(columnName, typeof(string));
                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                    columnName = "FirstName";
                    table.Columns.Add(columnName, typeof(string));
                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                    columnName = "LastName";
                    table.Columns.Add(columnName, typeof(string));
                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                    #endregion

                    foreach (var item in users)
                    {
                        // Cleanup ----------------------
                        item.UserPrincipalName = item.UserPrincipalName.Trim();
                        if (string.IsNullOrEmpty(item.LastName))
                        {
                            item.FirstName = item.Name;
                            item.LastName = item.Name;
                        }
                        item.FirstName = item.FirstName.Trim();
                        item.LastName = item.LastName.Trim();
                        // ------------------------------

                        var row = table.NewRow();

                        row["Username"] = item.UserPrincipalName;
                        row["FirstName"] = item.FirstName;
                        row["LastName"] = item.LastName;

                        table.Rows.Add(row);
                    }

                    bulkCopy.WriteToServer(table);
                }

                userResults = community.Query<UserResultModel>($@"
create table #UserResult (
    ID int not null,
    Name nvarchar(250) not null
);

merge   [Resource] as T 
using   ( 
        select  *
        from    #Resource
        ) as S 
        on  (
            T.Username = S.Username
            )
when    matched then
update  set T.Status = 'Active'
when    not matched by target then 
        insert (Username, Password, LastName, FirstName, Email, Status) 
        values (S.Username, 'Auto-populated junk', S.LastName, S.FirstName, S.Username, 'Active')
output  inserted.ID, S.Username into #UserResult;

merge   [CompanyResource] as T 
using   ( 
        select  *
        from    #UserResult
        ) as S 
        on  (
            T.ResourceID = S.ID and T.CompanyID = {companyID}
            )
when    not matched by target then 
        insert (ResourceID, CompanyID, IsAdministrator) 
        values (S.ID, {companyID}, 0);

select * from #UserResult;",
commandTimeout: 3600, transaction: transU).ToList();

                transU.Commit();
            }


            using (var transM = company.BeginTransaction())
            {
                company.Execute(@"
set nocount on 
create table #ResourceGroup (
	ResourceID int not null, 
	GroupID int not null
)
set nocount off", commandTimeout: 3600, transaction: transM);

                using (var bulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.Default, transM))
                {
                    bulkCopy.BatchSize = groups.Count;
                    bulkCopy.DestinationTableName = "#ResourceGroup";
                    bulkCopy.BulkCopyTimeout = 3600;

                    var table = new System.Data.DataTable();

                    #region Create column mappings

                    var columnName = "ResourceID";
                    table.Columns.Add(columnName, typeof(int));
                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                    columnName = "GroupID";
                    table.Columns.Add(columnName, typeof(int));
                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                    #endregion

                    foreach (var item in memberships)
                    {
                        var row = table.NewRow();

                        var resourceResult = userResults.FirstOrDefault(i => i.Name == item.User);
                        var groupResult = groupResults.FirstOrDefault(i => i.Name == item.Group);

                        if (resourceResult != null && groupResult != null)
                        {
                            row["ResourceID"] = resourceResult.ID;
                            row["GroupID"] = groupResult.ID;

                            table.Rows.Add(row);
                        }
                    }

                    bulkCopy.WriteToServer(table);
                }

                company.Execute(@"
merge   [ResourceGroup] as T 
using   ( 
        select  *
        from    #ResourceGroup
        ) as S 
        on  (
            T.ResourceID = S.ResourceID and T.GroupID = S.GroupID
            )
when    not matched by target then 
        insert (ResourceID, GroupID, IsOwner) 
        values (S.ResourceID, S.GroupID, 0);

select * from #GroupResult;",
commandTimeout: 3600, transaction: transM);


                transM.Commit();
            }
        }

        [TestMethod]
        public void GetUsersFromAadGraph()
        {
            var users = AzureGraphProvider.GetUsers("02292cae-2fe6-4371-8da1-b03d14808575", "da53c11a-c52a-4f3e-8cde-d284e2c2073d", "KNeGZgbfQWZ1lW0ea1/TCbOzB9BpYvO3U624xM82nAo=");// "OeW4y2bIqwSvUgs2NisrCoXjXfd33D8c5HxGITd5W0U=");

        }
    }
}
