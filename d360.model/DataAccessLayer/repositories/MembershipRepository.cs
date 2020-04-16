using d360.core.entities.Membership;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core.enums;
using System.Net;
using System.Data;
using System.Text.RegularExpressions;
using System.Data.SqlClient;

namespace d360.model.DataAccessLayer
{
    public class MembershipRepository : IMembershipRepository
    {
        internal ICompanyContext CompanyContext;
        internal ICommunityContext CommunityContext;

        public MembershipRepository(ICompanyContext companyContext, ICommunityContext communityContext)
        {
            this.CompanyContext = companyContext;
            this.CommunityContext = communityContext;
        }
        public async Task<GroupApiModels> GetGroups(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var dbArgs = new DynamicParameters();
            List<string> condition = new List<string>();
            if (queryParams != null)
            {
                if (queryParams.ToList().Any(q => q.Key.ToLower() == "uid"))
                {
                    Guid uid;
                    var uidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "uid").Value;
                    if (Guid.TryParse(uidString, out uid))
                    {
                        if (uid != Guid.Empty)
                        {
                            condition.Add("A.Uid = @Uid");
                            dbArgs.Add("uid", uid);
                        }
                        
                    }
                }

                if (queryParams.ToList().Any(q => q.Key.ToLower() == "name"))
                {
                  
                    var name = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "name").Value.Trim();
                    if (!string.IsNullOrEmpty(name))
                    {

                        condition.Add("G.Name like  @name");
                        dbArgs.Add("name", name + '%');
                    }
                }

            }

            var whereStatements = condition.Count != 0 ? $" where  {string.Join(" and ", condition)}" : "";
;                        var sql = $@"Select A.Uid,G.Name,G.Description,gr1.uid as PrimaryOwnerUid,gr2.uid as SecondaryOwnerUid from [Group] G
            inner join Asset A on A.[Object]='Group' and A.ObjectID = G.ID
            left join [reporting].[Global_Resource] gr1 on gr1.ResourceID = G.PrimaryOwnerResourceID
            left join [reporting].[Global_Resource] gr2 on gr2.ResourceID = G.SecondaryOwnerResourceID
                {whereStatements}  order by G.Name  ";

            var countSql = $@"Select count(*) from [Group] G
            inner join Asset A on A.[Object]='Group' and A.ObjectID = G.ID
            left join [reporting].[Global_Resource] gr1 on gr1.ResourceID = G.PrimaryOwnerResourceID
            left join [reporting].[Global_Resource] gr2 on gr2.ResourceID = G.SecondaryOwnerResourceID
                {whereStatements}  ";

            var countResults = await CompanyContext.QueryAsync<int>(countSql, dbArgs);
            var count = countResults.First();

            var results = await this.CompanyContext.QueryAsync<GroupApiModel>(sql, dbArgs);

            return new GroupApiModels() { items = results, Total = count };

        }
        public WorkHttpStatus DeleteResources(IEnumerable<UserApiDeleteModel> resources)
        {
            try
            {
                List<UserApiDeleteModel> models = new List<UserApiDeleteModel>();
                foreach (var model in resources)
                {
                    model.Resource = CompanyContext.GlobalReportingResources.SingleOrDefault(r => r.Uid == model.Uid && r.State != CompanyResourceState.Deleted);

                    if (model.Resource == null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.NotFound, "Not Found", $"User for uid [{model.Uid}] not found.");
                    }

                    if (model.Resource.ResourceID < 1)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Invalid User", $"User for uid [{model.Uid}] is a system user and cannot be deleted.");
                    }

                    model.CompanyResource = CommunityContext.CompanyResources.SingleOrDefault(r => r.CompanyID == CompanyContext.CurrentCompanyID && r.ResourceID == model.Resource.ResourceID && r.State != CompanyResourceState.Deleted);

                    if (model.CompanyResource == null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.NotFound, "Not Found", $"User for uid [{model.Uid}] not found.");
                    }
                }

                foreach(var model in resources)
                {
                    model.Resource.State = CompanyResourceState.Deleted;
                    model.CompanyResource.State = CompanyResourceState.Deleted;

                    CompanyContext.Update(model.Resource);
                    CommunityContext.Update(model.CompanyResource);
                }

            }
            catch
            {
                return new WorkHttpStatus(HttpStatusCode.InternalServerError, "Internal Server Error", $"An internal server error occurred");
            }

            return new WorkHttpStatus(HttpStatusCode.OK, "Success", "Users deleted successfully");
        }
        public async Task<IEnumerable<UserApiUpsertResult>> UpsertUsers(IEnumerable<UserApiUpsertModel> users)
        {
            var executionID = Guid.NewGuid();
            var results = new List<UserApiUpsertResult>();

            var fieldTypes = CompanyContext.FieldTypes.Where(f => f.Object == "ResourceType").ToList();

            #region Data Tables

            var userTable = new DataTable();
            var fieldTable = new DataTable();

            userTable.Columns.Add("ExecutionID", typeof(Guid));
            userTable.Columns.Add("Uid", typeof(Guid));
            userTable.Columns.Add("ExecutionItemUid", typeof(Guid));
            userTable.Columns.Add("ItemNumber", typeof(int));
            userTable.Columns.Add("Username", typeof(string));
            userTable.Columns.Add("FirstName", typeof(string));
            userTable.Columns.Add("LastName", typeof(string));
            userTable.Columns.Add("Password", typeof(string));
            userTable.Columns.Add("State", typeof(int));
            userTable.Columns.Add("IsAdministrator", typeof(bool));
            userTable.Columns.Add("IsNew", typeof(bool));
            userTable.Columns.Add("Success", typeof(bool));
            userTable.Columns.Add("Message", typeof(bool));


            fieldTable.Columns.Add("ExecutionID", typeof(Guid));
            fieldTable.Columns.Add("ItemNumber", typeof(int));
            fieldTable.Columns.Add("FieldName", typeof(string));
            fieldTable.Columns.Add("FieldValue", typeof(string));
            fieldTable.Columns.Add("FieldTypeID", typeof(int));
            fieldTable.Columns.Add("LookupValue", typeof(string));

            #endregion

            int itemNumber = 0;
            foreach (var user in users)
            {
                itemNumber++;
                var row = userTable.NewRow();

                var success = true;
                var messages = new List<string>();


                if (user.IsNew)
                {
                    if (user.Uid.HasValue)
                    {
                        success = false;
                        messages.Add("Cannot provide Uid for a new user");
                    }

                    if (user.State.HasValue)
                    {
                        success = false;
                        messages.Add("Cannot provide State for a new user");
                    }
                }
                else
                {
                    if (!user.Uid.HasValue)
                    {
                        success = false;
                        messages.Add("Must provide Uid for updated user");
                    }
                }

                if (string.IsNullOrEmpty(user.Username) || !Regex.IsMatch(user.Username + "", @"^$|\b([A-Za-z0-9'_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})\b"))
                {
                    success = false;
                    messages.Add("Username is not in a valid email format");
                }


                row["ExecutionID"] = executionID;
                if (user.Uid.HasValue) row["Uid"] = user.Uid;
                if (user.ExecutionItemUid.HasValue) row["ExecutionItemUId"] = user.ExecutionItemUid;
                row["ItemNumber"] = itemNumber;
                row["Username"] = user.Username;
                row["FirstName"] = user.FirstName;
                row["LastName"] = user.LastName;
                row["Password"] = user.Password;
                if (user.State.HasValue) row["State"] = (int)user.State;
                row["IsAdministrator"] = user.IsAdministrator;
                row["IsNew"] = user.IsNew;

                userTable.Rows.Add(row);

                if (user.Fields != null)
                {
                    foreach (var field in user.Fields.Keys)
                    {
                        var fieldType = fieldTypes.FirstOrDefault(f => f.Name == field);

                        if (fieldType == null)
                        {
                            success = false;
                            messages.Add($"Field type for key [{field}] not found on this asset");
                        }

                        var fieldRow = fieldTable.NewRow();
                        fieldRow["ExecutionID"] = executionID;
                        fieldRow["ItemNumber"] = itemNumber;
                        fieldRow["FieldName"] = field;
                        fieldRow["FieldValue"] = user.Fields[field];

                        fieldTable.Rows.Add(fieldRow);
                    }
                }

                row["Success"] = success;
                row["Message"] = string.Join(". ", messages);
            }


            #region Bulk Copy

            using (SqlTransaction trans = CompanyContext.Connection.BeginTransaction())
            {
                try
                {
                    await CompanyContext.Connection.ExecuteAsync(@"
                        drop table if exists #UserAssets;
                        create table #UserAssets
                        (
                            ExecutionID uniqueidentifier not null,
                            ExecutionItemUid uniqueidentifier,
                            ItemNumber int not null,
                            Username nvarchar(500),
                            Uid uniqueidentifier,
                            ResourceID int,
                            FirstName nvarchar(500),
                            LastName nvarchar(500),
                            Password nvarchar(500),
                            [State] int,
                            IsAdministrator bit not null,
                            IsNew bit not null,
                            Success bit not null,
                            Message nvarchar(max)
                        );

                        drop table if exists #UserFields;
                        create table #UserFields
                        (
                            ExecutionID uniqueidentifier not null,
                            ItemNumber int not null,
                            FieldName nvarchar(250),
                            FieldValue nvarchar(max),
                            FieldTypeID int,
                            LookupValue nvarchar(max)
                        );

                        ", transaction: trans);

                    SqlBulkCopy bulkCopy = new SqlBulkCopy(CompanyContext.Connection, SqlBulkCopyOptions.Default, trans);
                    bulkCopy.DestinationTableName = "#UserAssets";

                    bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                    bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                    bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                    bulkCopy.ColumnMappings.Add("Username", "Username");
                    bulkCopy.ColumnMappings.Add("Uid", "Uid");
                    bulkCopy.ColumnMappings.Add("FirstName", "FirstName");
                    bulkCopy.ColumnMappings.Add("LastName", "LastName");
                    bulkCopy.ColumnMappings.Add("Password", "Password");
                    bulkCopy.ColumnMappings.Add("State", "State");
                    bulkCopy.ColumnMappings.Add("IsAdministrator", "IsAdministrator");
                    bulkCopy.ColumnMappings.Add("IsNew", "IsNew");
                    bulkCopy.ColumnMappings.Add("Success", "Success");
                    bulkCopy.ColumnMappings.Add("Message", "Message");

                    await bulkCopy.WriteToServerAsync(userTable);

                    bulkCopy = new SqlBulkCopy(CompanyContext.Connection, SqlBulkCopyOptions.Default, trans);
                    bulkCopy.DestinationTableName = "#UserFields";

                    bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                    bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                    bulkCopy.ColumnMappings.Add("FieldName", "FieldName");
                    bulkCopy.ColumnMappings.Add("FieldValue", "FieldValue");
                    bulkCopy.ColumnMappings.Add("FieldTypeID", "FieldTypeID");
                    bulkCopy.ColumnMappings.Add("LookupValue", "LookupValue");


                    await bulkCopy.WriteToServerAsync(fieldTable);


                    #region Populate table values

                    await CompanyContext.Connection.ExecuteAsync(@"
                        update  U
                        set     U.ResourceID = G.ResourceID
                        from    #UserAssets U
                                inner join reporting.Global_Resources G on G.[uid] = U.[Uid] and G.[State] <> @deleted
                        where   U.Success = 1 and U.IsNew = 0;

                        update  U
                        set     U.FieldTypeID = F.ID
                        from    #UserFields U
                                inner join FieldType F on F.Name = U.FieldName and F.Object = 'ResourceType' and F.ObjectID = 1
                        where   U.ExecutionID = @executionID;
                        ", new { executionID, deleted = (int)CompanyResourceState.Deleted }, transaction: trans);

                    #endregion

                    #region Validation

                    await CompanyContext.Connection.ExecuteAsync(@"
                        update  U
                        set     U.Success = 0,
                                U.Message = U.Message + ', Resource for this uid not found'
                        from    #UserAssets U
                        where   U.Success = 1 and U.IsNew = 0 and U.ResourceID is null and U.ExecutionID = @executionID;

                        update  U
                        set     U.Success = 0,
                                U.Message = U.Message + ', One or more field values supplied is missing a field type'
                        from    #UserAssets U
                                cross apply (
                                    select  count(*) as MissingCount 
                                    from    #UserFields F 
                                    where   F.ItemNumber = U.ItemNumber 
                                            and F.ExecutionID = U.ExecutionID
                                            and F.FieldTypeID is null
                                ) C
                        where   U.Success = 1 and U.ExecutionID = @executionID and C.MissingCount > 0

                        ", new { executionID, deleted = (int)CompanyResourceState.Deleted }, transaction: trans);

                    #endregion


                    trans.Commit();
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                }
            }


            #endregion


            return results;
        }
    }
}
