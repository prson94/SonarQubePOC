using d360.core.entities;
using d360.core.exceptions;
using d360.core.queue;
using d360.core.enums;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.queue;
using d360.model;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Newtonsoft.Json;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ganss.XSS;
using System.Text.RegularExpressions;

namespace igx.jobs.bulkloadprocessor
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();
#if DEBUG
            config.UseDevelopmentSettings();
#endif
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    public class BulkLoadProcessor
    {
        const string functionName = "BulkLoad_Process";

        public async static Task Run([QueueTrigger("%BulkLoadQueue%"), StorageAccount("QueueStorageAccount")] string myQueueItem, TextWriter log)
        {
            var loadInfo = JsonConvert.DeserializeObject<BulkLoadInfo>(myQueueItem);
            Load load = null;

            try
            {
                #region Create EF connection

                var sec = new UriSecurityContextProvider()
                {
                    CompanyID = loadInfo.CompanyID,
                    ResourceID = 0,
                    CompanyPrefix = "demo.dev",
                    IsAdministrator = true
                };
                var cache = new DummyCachingProvider();
                var queue = new AzureQueueSource();
                var community = new CommunityContext(cache, queue, sec);
                var company = new CompanyContext(community, cache, queue, sec, true);
                var isDev = (company.ObjectContext.Connection.DataSource.Contains("dev")) || (loadInfo.CompanyID == 8);

                #endregion

                try
                {
                    var companyConnection = CompanyConnectionUtils.GetCompanyConnection(loadInfo.CompanyID);

                    #region Create Load Items from Load file

                    load = company.Loads.Include("LoadColumns").SingleOrDefault(i => i.ID == loadInfo.LoadID); //.Include("LoadItems.LoadItemColumns")

                    companyConnection.OpenWithRetry(RetryPolicy.DefaultProgressive);
                    var loadItemRowCount = companyConnection.Query<int>("select count(1) from LoadItem where LoadID = @id", new { id = load.ID }).Single();
                    companyConnection.Close();

                    if (loadItemRowCount <= 0)
                    {
                        var memoryStream = new MemoryStream(load.File);
                        var xls = new SLDocument(memoryStream);

                        var stats = xls.GetWorksheetStatistics();

                        var numberOfRows = stats.NumberOfRows;
                        var rowIndex = stats.StartRowIndex + 1;
                        var numberOfColumns = load.LoadColumns.Count;

                        var loadItems = new List<LoadItem>();
                        var loadItemColumns = new List<LoadItemColumn>();

                        while (rowIndex <= stats.EndRowIndex)
                        {
                            // Empty row validation.
                            var numberOfEmptyColumns = 0;
                            foreach (var c in load.LoadColumns.OrderBy(i => i.ColumnIndex))
                            {
                                var testValue = (xls.GetCellValueAsString(rowIndex, c.ColumnIndex) ?? "").TrimEnd();
                                if (string.IsNullOrEmpty(testValue))
                                    numberOfEmptyColumns++;
                            }

                            // Empty row check.
                            if (numberOfEmptyColumns < numberOfColumns)
                            {
                                var loadItem = new LoadItem { LoadID = load.ID, RowIndex = rowIndex };
                                loadItems.Add(loadItem);

                                foreach (var c in load.LoadColumns.OrderBy(i => i.ColumnIndex))
                                {
                                    var format = xls.GetCellStyle(rowIndex, c.ColumnIndex).FormatCode;
                                    var isDate = false;

                                    if (format.Contains("[$-404]") || format.Contains("m/d") || format.Contains("m-d") || format.Contains("d-m") ||
                                        format.Contains("[$-F400]") || format.Contains("[$-409]"))
                                        isDate = true;

                                    var loadValue = string.Empty;

                                    if (isDate)
                                    {
                                        loadValue = xls.GetCellValueAsDateTime(rowIndex, c.ColumnIndex).ToShortDateString();
                                    }
                                    else
                                    {
                                        loadValue = (xls.GetCellValueAsString(rowIndex, c.ColumnIndex) ?? "").TrimEnd();

                                        Regex tagRegex = new Regex(@"<[^>]+>");
                                        if (tagRegex.IsMatch(loadValue))
                                        {
                                            var sanitizer = new HtmlSanitizer();
                                            loadValue = sanitizer.Sanitize(loadValue);
                                        }
                                    }


                                    loadItemColumns.Add(new LoadItemColumn { ColumnIndex = c.ColumnIndex, LoadID = load.ID, RowIndex = rowIndex, Value = loadValue });
                                }
                            }
                            rowIndex++;
                        }

                        companyConnection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                        #region Bulk LoadItems

                        using (var trans = companyConnection.BeginTransaction())
                        {
                            using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.Default, trans))
                            {
                                bulkCopy.BatchSize = loadItems.Count;
                                bulkCopy.DestinationTableName = "dbo.LoadItem";
                                bulkCopy.BulkCopyTimeout = 3600;

                                var table = new System.Data.DataTable();
                                var columnName = "LoadID";
                                table.Columns.Add(columnName, typeof(int));
                                bulkCopy.ColumnMappings.Add(columnName, columnName);

                                columnName = "RowIndex";
                                table.Columns.Add(columnName, typeof(int));
                                bulkCopy.ColumnMappings.Add(columnName, columnName);

                                foreach (var item in loadItems)
                                {
                                    var row = table.NewRow();

                                    row["LoadID"] = item.LoadID;
                                    row["RowIndex"] = item.RowIndex;

                                    table.Rows.Add(row);
                                }

                                bulkCopy.WriteToServer(table);
                            }
                            trans.Commit();
                        }

                        #endregion

                        #region Bulk LoadItemColumns

                        using (var trans = companyConnection.BeginTransaction())
                        {
                            using (var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.Default, trans))
                            {
                                bulkCopy.BatchSize = loadItemColumns.Count;
                                bulkCopy.DestinationTableName = "dbo.LoadItemColumn";
                                bulkCopy.BulkCopyTimeout = 3600;

                                var table = new System.Data.DataTable();
                                var columnName = "LoadID";
                                table.Columns.Add(columnName, typeof(int));
                                bulkCopy.ColumnMappings.Add(columnName, columnName);

                                columnName = "RowIndex";
                                table.Columns.Add(columnName, typeof(int));
                                bulkCopy.ColumnMappings.Add(columnName, columnName);

                                columnName = "ColumnIndex";
                                table.Columns.Add(columnName, typeof(int));
                                bulkCopy.ColumnMappings.Add(columnName, columnName);

                                columnName = "Value";
                                table.Columns.Add(columnName, typeof(string));
                                bulkCopy.ColumnMappings.Add(columnName, columnName);

                                foreach (var item in loadItemColumns)
                                {
                                    var row = table.NewRow();

                                    row["LoadID"] = item.LoadID;
                                    row["RowIndex"] = item.RowIndex;
                                    row["ColumnIndex"] = item.ColumnIndex;
                                    if (string.IsNullOrEmpty(item.Value))
                                        row["Value"] = DBNull.Value;
                                    else
                                        row["Value"] = item.Value;

                                    table.Rows.Add(row);
                                }

                                bulkCopy.WriteToServer(table);
                            }
                            trans.Commit();
                        }

                        #endregion

                        companyConnection.Close();
                    }

                    #endregion

                    companyConnection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                    switch (load.Action)
                    {
                        case "M":
                            if (load.ObjectID == 0)
                                BulkLoadMembership(companyConnection, load.ID);
                            else
                                BulkLoadUsers(companyConnection, loadInfo.CompanyID, load.ID);
                            break;
                        case "O":
                            await BulkLoadOwnership(company, load.ID);
                            break;
                        case "P":   // Promotions
                            executeWithTry(companyConnection, $@"EXEC bulkload.Promotions {load.ID}", loadInfo.CompanyID, 2400);
                            company.CreateOrUpdateTypeDisplayValuesAsync(load.ObjectID, load.Object);
                            break;
                        case "R":   // Relations                                
                            await company.PerformBulkRelationshipOperation(load.ID, d360.core.enums.BulkRelationshipOperation.Relate);
                            break;
                        case "U":   // Unrelate
                            await company.PerformBulkRelationshipOperation(load.ID, d360.core.enums.BulkRelationshipOperation.Unrelate);
                            break;
                        case "B":
                        case "BL":  // Business Lineage
                            executeWithTry(companyConnection, $@"EXEC bulkload.BusinessLineage {load.ID}", loadInfo.CompanyID, 2400);
                            break;
                        case "T":
                        case "TL":  // Technical Lineage
                            #region 

                            #region
                            /*
                                Source Fusion Configuration,
                                Source Fusion Path,
                                Target Fusion Configuration,
                                Target Fusion Path,
                                Group
                             */
                            #endregion

                            #region Get data to pre-populate

                            var fusions = company.Table<Fusion>().OrderBy(x => x.Name).Select(x => new SimpleTypeModel { Name = x.Name.ToLower(), ID = x.ID });

                            #endregion

                            var mappingList = new List<SimpleTypeModel>();

                            load = company.Loads.Include("LoadColumns").Include("LoadItems.LoadItemColumns").SingleOrDefault(i => i.ID == loadInfo.LoadID);

                            foreach (var loadItem in load.LoadItems)
                            {
                                #region Vars

                                var rawSourceFusion = "";
                                var rawSourceFusionPath = "";
                                var rawTargetFusion = "";
                                var rawTargetFusionPath = "";
                                var rawGroup = "";

                                LoadItemColumn sourceFusionColumn = null;
                                LoadItemColumn sourceFusionPathColumn = null;
                                LoadItemColumn targetFusionColumn = null;
                                LoadItemColumn targetFusionPathColumn = null;
                                LoadItemColumn groupColumn = null;

                                SimpleTypeModel verifiedSourceFusion = null;
                                SimpleTypeModel verifiedSourceFusionPath = null;
                                SimpleTypeModel verifiedTargetFusion = null;
                                SimpleTypeModel verifiedTargetFusionPath = null;

                                var currentColumnIndex = 1;

                                #endregion

                                #region Verify source fusion

                                sourceFusionColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == currentColumnIndex);
                                rawSourceFusion = (sourceFusionColumn.Value + "").Trim().ToLower();
                                verifiedSourceFusion = fusions.SingleOrDefault(i => i.Name == rawSourceFusion);
                                currentColumnIndex++;

                                if (verifiedSourceFusion != null)
                                {
                                    sourceFusionColumn.LookupObject = "Fusion";
                                    sourceFusionColumn.LookupObjectID = verifiedSourceFusion.ID;
                                }

                                #endregion

                                #region Verify source fusion attribute

                                if (verifiedSourceFusion != null)
                                {
                                    sourceFusionPathColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == currentColumnIndex);
                                    rawSourceFusionPath = (sourceFusionPathColumn.Value + "").Trim().ToLower();
                                    verifiedSourceFusionPath = company.Filter<FusionAttribute>(i => i.FusionID == verifiedSourceFusion.ID && i.TextPath.ToLower() == rawSourceFusionPath).Select(i => new SimpleTypeModel { Name = "FusionAttribute", ID = i.ID }).FirstOrDefault();

                                    if (verifiedSourceFusionPath != null)
                                    {
                                        sourceFusionPathColumn.LookupObject = verifiedSourceFusionPath.Name;
                                        sourceFusionPathColumn.LookupObjectID = verifiedSourceFusionPath.ID;
                                    }
                                }
                                currentColumnIndex++;

                                #endregion

                                #region Verify target fusion

                                targetFusionColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == currentColumnIndex);
                                rawTargetFusion = (targetFusionColumn.Value + "").Trim().ToLower();
                                verifiedTargetFusion = fusions.SingleOrDefault(i => i.Name == rawTargetFusion);
                                currentColumnIndex++;

                                if (verifiedTargetFusion != null)
                                {
                                    targetFusionColumn.LookupObject = "Fusion";
                                    targetFusionColumn.LookupObjectID = verifiedTargetFusion.ID;
                                }

                                #endregion

                                #region Verify target fusion attribute

                                if (verifiedTargetFusion != null)
                                {
                                    targetFusionPathColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == currentColumnIndex);
                                    rawTargetFusionPath = (targetFusionPathColumn.Value + "").Trim().ToLower();
                                    verifiedTargetFusionPath = company.Filter<FusionAttribute>(i => i.FusionID == verifiedTargetFusion.ID && i.TextPath.ToLower() == rawTargetFusionPath).Select(i => new SimpleTypeModel { Name = "FusionAttribute", ID = i.ID }).FirstOrDefault();

                                    if (verifiedTargetFusionPath != null)
                                    {
                                        targetFusionPathColumn.LookupObject = verifiedTargetFusionPath.Name;
                                        targetFusionPathColumn.LookupObjectID = verifiedTargetFusionPath.ID;
                                    }
                                }
                                currentColumnIndex++;

                                #endregion

                                #region Get group

                                groupColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == currentColumnIndex);
                                rawGroup = (groupColumn.Value + "").Trim().ToLower();

                                #endregion

                                #region Validated data.  Decide if we should insert the record.

                                if (verifiedSourceFusion != null && verifiedSourceFusionPath != null &&
                                    verifiedTargetFusion != null && verifiedTargetFusionPath != null)
                                {
                                    // OK to proceed with insert.
                                    var technicalMapping = company.Filter<MapRuleItem>(i =>
                                        i.SourceFusionAttributeID == verifiedSourceFusionPath.ID &&
                                        i.TargetFusionAttributeID == verifiedTargetFusionPath.ID)
                                        .SingleOrDefault();

                                    if (technicalMapping == null)
                                    {
                                        technicalMapping = new MapRuleItem
                                        {
                                            SourceFusionAttributeID = verifiedSourceFusionPath.ID,
                                            TargetFusionAttributeID = verifiedTargetFusionPath.ID
                                        };

                                        try
                                        {
                                            company.Add(technicalMapping);
                                            loadItem.Object = "MapRuleItem";
                                            loadItem.ObjectID = technicalMapping.ID;
                                            loadItem.Status = true;
                                            loadItem.StatusMessage = "Successfully created technical mapping.";

                                            mappingList.Add(new SimpleTypeModel { Name = rawGroup, ID = technicalMapping.ID }); //This is used for post processing.
                                        }
                                        catch (BaseException ex)
                                        {
                                            loadItem.Status = false;
                                            loadItem.StatusMessage = ex.StatusDescription;
                                        }
                                        catch (Exception ex)
                                        {
                                            loadItem.Status = false;
                                            loadItem.StatusMessage = ex.Message;
                                        }
                                    }
                                    else
                                    {
                                        loadItem.Object = "MapRuleItem";
                                        loadItem.ObjectID = technicalMapping.ID;
                                        loadItem.Status = true;
                                        loadItem.StatusMessage = "Technical mapping already exists.";

                                        mappingList.Add(new SimpleTypeModel { Name = rawGroup, ID = technicalMapping.ID }); //This is used for post processing.
                                    }
                                }
                                else
                                {
                                    // Log errors.
                                    loadItem.Status = false;

                                    if (verifiedSourceFusion == null)
                                    {
                                        loadItem.StatusMessage += $" Could not find source fusion configuration [{rawSourceFusion}].";
                                    }

                                    if (verifiedSourceFusionPath == null)
                                    {
                                        loadItem.StatusMessage += $" Could not find source fusion path [{rawSourceFusionPath}].";
                                    }

                                    if (verifiedTargetFusion == null)
                                    {
                                        loadItem.StatusMessage += $" Could not find target fusion [{rawTargetFusion}].";
                                    }

                                    if (verifiedTargetFusionPath == null)
                                    {
                                        loadItem.StatusMessage += $" Could not find target fusion path [{rawTargetFusionPath}].";
                                    }
                                }

                                #endregion

                                company.Update(loadItem);
                            }

                            #region Now process maprules based on groups.

                            try
                            {
                                var groups = mappingList.Select(i => i.Name).Distinct().ToList();
                                foreach (var group in groups)
                                {
                                    if (group != "")
                                    {
                                        var groupedMapRuleItems = mappingList.Where(i => i.Name == group).Select(i => i.ID).ToList();

                                        if (groupedMapRuleItems.Count > 0)
                                        {
                                            var mapRuleExistenceSql = "select M1.MapRuleID from MapRuleItemMapRule M1";
                                            var firstID = groupedMapRuleItems[0];

                                            groupedMapRuleItems.RemoveAt(0);

                                            var tableIndex = 2;
                                            groupedMapRuleItems.ForEach(i =>
                                            {
                                                mapRuleExistenceSql += $" inner join MapRuleItemMapRule M{tableIndex} on M{tableIndex}.MapRuleID = M{tableIndex - 1}.MapRuleID and M{tableIndex}.MapRuleItemID = {i}";
                                                tableIndex++;
                                            });

                                            mapRuleExistenceSql += $" where M1.MapRuleItemID = {firstID}";


                                            //Make sure you add the rmeoved ID back into the ID list.
                                            groupedMapRuleItems.Add(firstID);

                                            var mapRuleCheck = company.Query<dynamic>(mapRuleExistenceSql).FirstOrDefault();

                                            if (mapRuleCheck == null)
                                            {
                                                var mapRule = new MapRule { MapRuleItems = new List<MapRuleItem>() };
                                                var mapRuleItems = company.Filter<MapRuleItem>(i => groupedMapRuleItems.Contains(i.ID));
                                                foreach (var mri in mapRuleItems)
                                                {
                                                    mapRule.MapRuleItems.Add(mri);
                                                }
                                                company.Add(mapRule);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                load.Notes += $" {ex.Message}";
                            }

                            #endregion

                            break;
                            #endregion
                    }

                    companyConnection.Close();

                    load.DateCompleted = DateTime.UtcNow;
                    company.Update(load);
                }
                catch (Exception ex)
                {                    
                    if (load != null)
                    {
                        load.DateCompleted = DateTime.UtcNow;
                        company.Update(load);
                    }

                    CoreFunction.AITrackException(functionName, ex, loadInfo.CompanyID);
                }
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, loadInfo.CompanyID);
            }
        }

        private static void BulkLoadMembership(SqlConnection company, int loadId)
        {
            var load = company.Query<Load>("select * from [Load] where ID = @loadId", new { loadId }).SingleOrDefault();
            if (load == null)
            {                
                throw new Exception($"Bulk load membership cannot find the load job to run [{loadId}].");
            }
            load = null;

            // get the load columns
            var columns = company.Query<LoadColumn>("select * from LoadColumn where LoadID = @loadId", new { loadId });
            if (columns == null)
            {
                throw new Exception($"Bulk load data does not contain any columns in LoadColumn table.  Load ID [{loadId}]");
            }
            columns = null;

            using (var trans = company.BeginTransaction())
            {
                try
                {
                    company.Execute(@"
create table #GroupLoadItems (LoadID int, RowIndex int, 
    StatusMessage nvarchar(500), Status bit, 
    [Action] nvarchar(max),
    [Group] nvarchar(max),
    [GroupID] int null,
    [User] nvarchar(max),
    [UserID] int null
);
create table #GroupInsertResult (ID int);
create table #ResourceGroupInsertResult (ID int);
create table #ResourceGroupDeleteResult (ID int);", transaction: trans);

                    company.Execute(@"
insert into #GroupLoadItems
    select	I.LoadID,
		    I.RowIndex,
		    I.StatusMessage,
		    I.Status,
		    C1.Value as [Action],
		    C2.Value as [Group],
		    cast(null as int) as GroupID,
		    C3.Value as [User],
		    cast(null as int) as UserID
    from	LoadItem I
		    inner join LoadItemColumn C1 on C1.LoadID = I.LoadID and C1.RowIndex = I.RowIndex and C1.ColumnIndex = 1
		    inner join LoadItemColumn C2 on C2.LoadID = I.LoadID and C2.RowIndex = I.RowIndex and C2.ColumnIndex = 2
		    inner join LoadItemColumn C3 on C3.LoadID = I.LoadID and C3.RowIndex = I.RowIndex and C3.ColumnIndex = 3
    where	I.LoadID = @id", new { id = loadId }, transaction: trans);

                    company.Execute(@"
merge into	[Group] as T
using		(
			select	distinct
					ltrim(rtrim([Group])) as Name
			from	#GroupLoadItems
			) S
on			(T.Name  = S.Name)
when not matched by target then
	insert (Name, UpdatedOn, UpdatedBy)
	values (S.Name, getutcdate(), 0)
output inserted.ID into #GroupInsertResult;", transaction: trans);

                    company.Execute(@"
update	T
set		T.GroupID = S.ID,
		T.StatusMessage = case
							when I.ID is not null then 'Group created. '
							else T.StatusMessage
						  end
from	#GroupLoadItems T
		inner join [Group] S on S.Name= T.[Group]
		left join #GroupInsertResult I on I.ID = S.ID;

update	T
set		T.UserID = S.ResourceID
from	#GroupLoadItems T
		inner join reporting.Global_Resource S on S.Email = T.[User];

update	#GroupLoadItems
set		Status = 0,
		StatusMessage = 'No user found with this email address. '
where	UserID is null;", transaction: trans);

                    company.Execute(@"
merge into	[ResourceGroup] as T
using		(
			select	distinct
					[UserID],
					[GroupID]
			from	#GroupLoadItems
			where	UserID is not null and GroupID is not null and [Action] = 'Add'
			) S
on			(T.ResourceID = S.UserID and T.GroupID = S.GroupID)
when not matched by target then
	insert (ResourceID, GroupID, IsOwner)
	values (S.UserID, S.GroupID, 0)
output inserted.ResourceID into #ResourceGroupInsertResult;", transaction: trans);

                    company.Execute(@"
update	T
set		T.Status = 1,
		T.StatusMessage = coalesce(T.StatusMessage, '') + case
							when I.ID is not null then 'Membership created. '
							else 'Membership already exists.'
						  end
from	#GroupLoadItems T
		left join #ResourceGroupInsertResult I on I.ID = T.UserID
where	T.UserID is not null and T.GroupID is not null and T.[Action] = 'Add';", transaction: trans);

                    company.Execute(@"
merge into	[ResourceGroup] as T
using		(
			select	distinct
					[UserID],
					[GroupID]
			from	#GroupLoadItems
			where	UserID is not null and GroupID is not null and [Action] = 'Remove'
			) S
on			(T.ResourceID = S.UserID and T.GroupID = S.GroupID)
when matched and T.ResourceID = S.UserID and T.GroupID = S.GroupID then
	delete
output deleted.ResourceID into #ResourceGroupDeleteResult;", new { id = loadId }, transaction: trans);

                    company.Execute(@"
update	T
set		T.Status =	case
						when I.ID is not null then 1
						else 0
					end,
		T.StatusMessage = coalesce(T.StatusMessage, '') + case
							when I.ID is not null then 'Membership removed. '
							else 'Membership does not exist.'
						  end
from	#GroupLoadItems T
		left join #ResourceGroupDeleteResult I on I.ID = T.UserID
where	T.UserID is not null and T.GroupID is not null and T.[Action] = 'Remove';

update	T
set		T.Status = S.Status,
		T.StatusMessage = S.StatusMessage
from	LoadItem T
		inner join #GroupLoadItems S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex;", new { id = loadId }, transaction: trans);

                    trans.Commit();
                }
                catch 
                {
                    trans.Rollback();
                    throw;
                }
            }
        }
               
        private static void BulkLoadUsers(SqlConnection company, int companyID, int loadId)
        {
            var load = company.Query<Load>("select * from [Load] where ID = @loadId", new { loadId }).SingleOrDefault();
            if (load == null)
            {                
                throw new Exception($"Bulk load users cannot find the load job to run [{loadId}].");
            }
            load = null;

            // get the load columns
            var columns = company.Query<LoadColumn>("select * from LoadColumn where LoadID = @loadId", new { loadId });
            if (columns == null)
            {
                throw new Exception($"Bulk load data does not contain any columns in LoadColumn table.  Load ID [{loadId}]");
            }
            if (columns.Count() < 4)
            {
                throw new Exception($"Bulk load data does not contain the correct number of columns in LoadColumn table.  Load ID [{loadId}]");
            }
            
            var usersToLoad = company.Query<CommunityUserAddModel>(@"
select	I.LoadID,
		I.RowIndex,
		rtrim(ltrim(C1.Value)) as [UserStatus],
		rtrim(ltrim(C2.Value)) as [Email],
		rtrim(ltrim(C3.Value)) as [FirstName],
		rtrim(ltrim(C4.Value)) as [LastName]
from	LoadItem I
		inner join LoadItemColumn C1 on C1.LoadID = I.LoadID and C1.RowIndex = I.RowIndex and C1.ColumnIndex = 1
		inner join LoadItemColumn C2 on C2.LoadID = I.LoadID and C2.RowIndex = I.RowIndex and C2.ColumnIndex = 2
		inner join LoadItemColumn C3 on C3.LoadID = I.LoadID and C3.RowIndex = I.RowIndex and C3.ColumnIndex = 3
		inner join LoadItemColumn C4 on C4.LoadID = I.LoadID and C4.RowIndex = I.RowIndex and C4.ColumnIndex = 4
where	I.LoadID = @loadId", new { loadId }, commandTimeout: 1200).ToList();

            #region Generate data sets

            var tbl = new System.Data.DataTable();

            tbl.Columns.Add("LoadID", typeof(int));
            tbl.Columns.Add("RowIndex", typeof(int));
            tbl.Columns.Add("UserStatus", typeof(string));
            tbl.Columns.Add("Email", typeof(string));
            tbl.Columns.Add("FirstName", typeof(string));
            tbl.Columns.Add("LastName", typeof(string));
            tbl.Columns.Add("EnvironmentID", typeof(int));
            tbl.Columns.Add("Success", typeof(bool));
            tbl.Columns.Add("Message", typeof(string));

            foreach (var userToLoad in usersToLoad)
            {
                var row = tbl.NewRow();

                row["LoadID"] = userToLoad.LoadID;
                row["RowIndex"] = userToLoad.RowIndex;
                row["UserStatus"] = userToLoad.UserStatus;
                row["Message"] = "";
                if (string.IsNullOrEmpty(userToLoad.Email) || !Regex.IsMatch(userToLoad.Email+"", @"^$|\b([A-Za-z0-9'_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})\b"))
                {
                    row["Success"] = false;
                    row["Message"] = "Email is not in a valid format; ";
                }
                row["Email"] = userToLoad.Email + "";
                row["FirstName"] = userToLoad.FirstName;
                row["LastName"] = userToLoad.LastName;
                row["EnvironmentID"] = companyID;

                tbl.Rows.Add(row);
            }

            #endregion

            List<CommunityUserAddResultModel> userResults = null;

            #region Process in Community database.

            var community = new SqlConnection(d360.core.constants.COMMUNITY_DATABASE_CONNECTION);
            community.OpenWithRetry(RetryPolicy.DefaultProgressive);
            using (var trans = community.BeginTransaction())
            {
                try
                {
                    community.Execute(@"
DROP TABLE IF EXISTS #Users;
DROP TABLE IF EXISTS #UsersResult;
DROP TABLE IF EXISTS #UserMembershipsResult;", transaction: trans);

                    community.Execute(@"
create table #Users (
    LoadID int not null,
    RowIndex int not null,
    UserStatus nvarchar(50) null,
    Email nvarchar(500) null,
    FirstName nvarchar(250) null,
    LastName nvarchar(250) null,
	EnvironmentID int not null, 
	ClientID int null,
	ResourceID int null,
    [uid] uniqueidentifier null,
    Success bit null,
    Message nvarchar(2500) null
);
create table #UsersResult (LoadID int, RowIndex int, ResourceID int, [uid] uniqueidentifier, [Action] varchar(25) not null);
create table #UserMembershipsResult (ResourceID int, [Action] varchar(25) not null);
CREATE NONCLUSTERED INDEX IX_TempUsers ON #Users ( Email ASC );
CREATE NONCLUSTERED INDEX IX_TempUsers_LoadID_Email ON #Users ( LoadID ASC, Email ASC );
CREATE NONCLUSTERED INDEX IX_TempUsers_LoadID_RowIndex_Email ON #Users ( LoadID ASC, RowIndex ASC, Email ASC );
", transaction: trans);

                    var usersBulkCopy = new SqlBulkCopy(community, SqlBulkCopyOptions.Default, trans)
                    {
                        BatchSize = tbl.Rows.Count,
                        DestinationTableName = "#Users",
                        BulkCopyTimeout = 3600
                    };

                    usersBulkCopy.ColumnMappings.Add("LoadID", "LoadID");
                    usersBulkCopy.ColumnMappings.Add("RowIndex", "RowIndex");
                    usersBulkCopy.ColumnMappings.Add("UserStatus", "UserStatus");
                    usersBulkCopy.ColumnMappings.Add("Email", "Email");
                    usersBulkCopy.ColumnMappings.Add("FirstName", "FirstName");
                    usersBulkCopy.ColumnMappings.Add("LastName", "LastName");
                    usersBulkCopy.ColumnMappings.Add("EnvironmentID", "EnvironmentID");
                    usersBulkCopy.ColumnMappings.Add("Success", "Success");
                    usersBulkCopy.ColumnMappings.Add("Message", "Message");

                    usersBulkCopy.WriteToServer(tbl);

                    community.Execute(@"update	T
set		T.ClientID = S.ClientID
from	#Users T
		inner join Company S on S.ID = T.EnvironmentID;", transaction: trans);

                    // Check for duplicate email addresses and invalidate the ones with higher row indices.
                    community.Execute(@"update	T
set		T.Success = 0,
		T.Message = 'User email address already used in bulk load file'
from	#Users T
		inner join	(
					select LoadID, min(RowIndex) as MinRowIndex, Email from #Users group by LoadID, Email
					) S on S.LoadID = T.LoadID and S.Email = T.Email and S.MinRowIndex <> T.RowIndex;", transaction: trans);

                    community.Execute(@"update	#Users
set		Success = 0,
        Message = Message + 'User does not have a valid email address; '
where   [Email] is null or [Email] = '';", transaction: trans);

                    community.Execute(@"update	#Users
set		Success = 0,
        Message = Message + 'User does not have a valid first name; '
where   [FirstName] is null or [FirstName] = '';", transaction: trans);

                    community.Execute(@"update	#Users
set		Success = 0,
        Message = Message + 'User does not have a valid last name; '
where   [LastName] is null or [LastName] = '';", transaction: trans);

                    string inclause = String.Join(",", CompanyResourceState.Active.GetList().Select(s => "'" + s.Name + "'"));
                    community.Execute(@"update	#Users
set		Success = 0,
        Message = Message + 'User does not have a valid status; '
where   [UserStatus] IS NULL OR [UserStatus] NOT IN (" + inclause + ");", transaction: trans);

                    community.Execute(@"update	T
set		T.ResourceID = S.ID
from	#Users T
		inner join [Resource] S on S.Email = T.Email;", transaction: trans);

                    community.Execute(@"update	T
set		T.Success = case
						when S.[Count] > 0 then cast(0 as bit)
						else null
					end,
		T.Message = case
						when S.[Count] > 0 then 'User is a member of another account and may not be modified; '
						else null
					end
from	#Users T
		cross apply (
			select	count(1) as [Count]
			from	CompanyResource CR
					inner join Company C on C.ID = CR.CompanyID and C.ClientID <> T.ClientID and CR.ResourceID = T.ResourceID
		) S
where   T.Success is null;", transaction: trans);
                    
                    community.Execute(@"
merge into  [Resource] T
using       (
            select  *
            from    #Users
			where	Success is null
            ) S
on          (
                T.ID = S.ResourceID
            )
when matched then
	update
	set	T.FirstName = S.FirstName,
		T.LastName = S.LastName,
		T.Status = S.UserStatus
when not matched by target then
    insert  (ResourceTypeID, Username, [Password], LastName, FirstName, Email, [Status])
    values  (1, S.Email, 'not set', S.LastName, S.FirstName, S.Email, S.UserStatus)
output S.LoadID, S.RowIndex, inserted.ID, inserted.[uid], $action into #UsersResult;", transaction: trans);

                    community.Execute(@"
update	T
set		T.Success = 1,
		T.ResourceID = S.ResourceID,
        T.[uid] = S.[uid],
		Message = case S.[Action]
					when 'INSERT' then 'User created. '
					else 'User updated. '
				  end
from	#Users T
		inner join #UsersResult S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex;", transaction: trans);

                    community.Execute(@"
merge into  [CompanyResource] T
using       (
            select  distinct
					EnvironmentID as CompanyID,
					ResourceID,
                    case UserStatus when 'Active' then 1 else 2 end as [State]
            from    #Users
			where	Success = 1
            ) S
on          (
                T.CompanyID = S.CompanyID and T.ResourceID = S.ResourceID
            )
when matched then
	update 
		set T.[State] = S.[State]
when not matched by target then
    insert  (CompanyID, ResourceID, IsAdministrator, [State])
    values  (S.CompanyID, S.ResourceID, 0, S.[State])
output inserted.ResourceID, $action into #UserMembershipsResult;", transaction: trans);

                    community.Execute(@"
update	T
set		T.Message = T.Message + 
					case S.[Action]
						when 'INSERT' then 'User added to environment. '
						else 'User already assigned to environment. '
					end
from	#Users T
		left join #UserMembershipsResult S on S.ResourceID = T.ResourceID
where	T.Success = 1", transaction: trans);

                    userResults = community.Query<CommunityUserAddResultModel>("select * from #Users", transaction: trans).ToList();

                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }

            #endregion

            #region Process in Environment database.

            tbl = new System.Data.DataTable();

            tbl.Columns.Add("LoadID", typeof(int));
            tbl.Columns.Add("RowIndex", typeof(int));
            tbl.Columns.Add("UserStatus", typeof(string));
            tbl.Columns.Add("Email", typeof(string));
            tbl.Columns.Add("FirstName", typeof(string));
            tbl.Columns.Add("LastName", typeof(string));
            tbl.Columns.Add("ResourceID", typeof(int));
            tbl.Columns.Add("uid", typeof(Guid));
            tbl.Columns.Add("Success", typeof(bool));
            tbl.Columns.Add("Message", typeof(string));

            foreach (var userResult in userResults)
            {
                var row = tbl.NewRow();

                row["LoadID"] = userResult.LoadID;
                row["RowIndex"] = userResult.RowIndex;
                row["UserStatus"] = userResult.UserStatus;
                row["Email"] = userResult.Email+"";
                row["FirstName"] = userResult.FirstName;
                row["LastName"] = userResult.LastName;

                if (userResult.ResourceID.HasValue)
                    row["ResourceID"] = userResult.ResourceID.Value;

                row["uid"] = userResult.Uid;
                row["Success"] = userResult.Success;
                row["Message"] = userResult.Message;

                tbl.Rows.Add(row);
            }

            using (var trans = company.BeginTransaction())
            {
                try
                {
                    company.Execute(@"DROP TABLE IF EXISTS #Users;", transaction: trans);

                    company.Execute(@"
create table #Users (
    LoadID int not null,
    RowIndex int not null,
    UserStatus nvarchar(50) null,
    Email nvarchar(500) null,
    FirstName nvarchar(250) null,
    LastName nvarchar(250) null,
	ResourceID int null,
    [uid] uniqueidentifier null,
    Success bit null,
    Message nvarchar(2500) null
)
CREATE NONCLUSTERED INDEX IX_TempUsers_Load ON #Users ( LoadID ASC, RowIndex ASC );
CREATE NONCLUSTERED INDEX IX_TempUsers_ResourceID ON #Users ( ResourceID ASC );
", transaction: trans);

                    var usersBulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.Default, trans)
                    {
                        BatchSize = tbl.Rows.Count,
                        DestinationTableName = "#Users",
                        BulkCopyTimeout = 3600
                    };

                    usersBulkCopy.ColumnMappings.Add("LoadID", "LoadID");
                    usersBulkCopy.ColumnMappings.Add("RowIndex", "RowIndex");
                    usersBulkCopy.ColumnMappings.Add("UserStatus", "UserStatus");
                    usersBulkCopy.ColumnMappings.Add("Email", "Email");
                    usersBulkCopy.ColumnMappings.Add("FirstName", "FirstName");
                    usersBulkCopy.ColumnMappings.Add("LastName", "LastName");
                    usersBulkCopy.ColumnMappings.Add("ResourceID", "ResourceID");
                    usersBulkCopy.ColumnMappings.Add("uid", "uid");
                    usersBulkCopy.ColumnMappings.Add("Success", "Success");
                    usersBulkCopy.ColumnMappings.Add("Message", "Message");

                    usersBulkCopy.WriteToServer(tbl);

                    company.Execute(@"
merge into  reporting.Global_Resource T
using       (
            select  ResourceID, 
                    [uid],
                    LastName, 
                    FirstName, 
                    Email, 
                    case UserStatus when 'Active' then 1 else 2 end as [State]
            from    #Users
			where	Success = 1
            ) S
on          (
                T.ResourceID = S.ResourceID
            )
when matched then
	update
	set	T.FirstName = S.FirstName,
		T.LastName = S.LastName,
		T.[State] = S.[State]
when not matched by target then
    insert  ([uid], ResourceID, LastName, FirstName, Email, [State], IsAdministrator)
    values  (S.[uid], S.ResourceID, S.LastName, S.FirstName, S.Email, S.[State], 0);", transaction: trans);

                    company.Execute(@"exec [bulkload].[UpdateDynamicLookupFieldColumns] @loadId", new { loadId }, transaction: trans);

                    company.Execute(@"
merge into  Field T
using       (
			select	A.ID as AssetID,
					A.Object,
					A.ObjectID,
					FT.ID as FieldTypeID,
					case 
						when FT.[Type] = 'Boolean' and LOWER(CI.Value) in ('y', 'yes', 'true', 't', '1') then 'true'
						when FT.[Type] = 'Boolean' and LOWER(CI.Value) not in ('y', 'yes', 'true', 't', '1') then 'false'
						when FT.[Type] = 'Lookup' then cast(CI.LookupObjectID as nvarchar(250))
						else CI.Value
					end as Value,
					0 as UpdatedBy
			from	LoadItem I
					inner join #Users U on U.LoadID = I.LoadID and U.RowIndex = I.RowIndex and U.Success = 1
					inner join Asset A on A.Object = 'Resource' and A.ObjectID = U.ResourceID
					inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex > 4
					inner join LoadItemColumn CI on CI.LoadID = I.LoadID and CI.RowIndex = I.RowIndex and CI.ColumnIndex = C.ColumnIndex
					inner join FieldType FT on FT.Object = 'ResourceType' and FT.ObjectID = 1 and FT.Name = C.Name
            ) S
on          (
                T.AssetID = S.AssetID and T.FieldTypeID = S.FieldTypeID
            )
when matched then
	update
	set	T.Value = S.Value,
		T.UpdatedBy = S.UpdatedBy
when not matched by target then
    insert  (FieldTypeID, ObjectType, ObjectID, Value, UpdatedBy)
    values  (S.FieldTypeID, S.Object, S.ObjectID, S.Value, S.UpdatedBy);", transaction: trans);

                    company.Execute(@"
update	T
set		T.Status = S.Success,
		T.StatusMessage = S.Message
from	LoadItem T
		inner join #Users S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex;
update	[Load]
set		DateCompleted = getutcdate()
where	ID = @loadId", new { loadId }, transaction: trans);

                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }

            #endregion
        }

        private static async Task BulkLoadOwnership(CompanyContext company, int loadId)
        {
            var load = company.Loads.Where(x => x.ID == loadId).FirstOrDefault();

            if (load == null)
            {
                //log.Error($"Bulk load relate cannot find the load job to run [{loadId}].");
                throw new Exception($"Bulk load relate cannot find the load job to run [{loadId}].");
            }


            // get the load columns
            var columns = company.LoadColumns.Where(x => x.LoadID == loadId).ToList();

            if (columns == null)
            {
                throw new Exception($"Bulk load data doesnt contain any columns in LoadColumn table.  Load ID [{loadId}]");
            }

            var loaddata = company.LoadItemColumns.Where(x => x.LoadID == loadId);

            //loop throw rows until there are no more indexes start at 2
            int currentRowIndex = 2;
            var rowData = loaddata.Where(x => x.RowIndex == currentRowIndex).ToList();
            int assetIdIndex = -1;
            int responsibilityIndex = -1;
            int resourceIndex = -1;

            foreach (var column in columns)
            {
                if (string.Compare(column.Name, "Asset ID") == 0)
                {
                    assetIdIndex = column.ColumnIndex;
                }
                else if (string.Compare(column.Name, "Resource") == 0)
                {
                    resourceIndex = column.ColumnIndex;
                }
                else if (string.Compare(column.Name, "Responsibility") == 0)
                {
                    responsibilityIndex = column.ColumnIndex;
                }
            }

            //log.Info($"Bulk load responsibilities will add {rowData.Count} responsibilites.");

            while (rowData != null && rowData.Count > 0)
            {
                //add a row to [ResponsibilityTypeRelationOverrideItem] table for the responsibility
                var responsibilityCol = rowData.Where(x => x.RowIndex == currentRowIndex && x.ColumnIndex == responsibilityIndex).FirstOrDefault();
                var resourceCol = rowData.Where(x => x.RowIndex == currentRowIndex && x.ColumnIndex == resourceIndex).FirstOrDefault();
                var assetCol = rowData.Where(x => x.RowIndex == currentRowIndex && x.ColumnIndex == assetIdIndex).FirstOrDefault();
                var msg = "";
                var status = 0;
                if ((responsibilityCol == null) || (resourceCol == null) || (assetCol == null))
                {
                    if (responsibilityCol == null)
                        CoreFunction.AITrackTrace(functionName, $"Bulk load responsibilities cannot find the responsibility column in row {currentRowIndex}", companyId: company.CurrentCompanyID);
                    if (resourceCol == null)
                        CoreFunction.AITrackTrace(functionName, $"Bulk load responsibilities cannot find the resource column in row {currentRowIndex}", companyId: company.CurrentCompanyID);
                    if (assetCol == null)
                        CoreFunction.AITrackTrace(functionName, $"Bulk load responsibilities cannot find the asset column in row {currentRowIndex}", companyId: company.CurrentCompanyID);
                }
                else
                {
                    var responsiblityOverride = new ResponsibilityTypeRelationOverrideItem();
                    //company.ResponsibilityTypeRelationOverrideItems
                    if (!int.TryParse(assetCol.Value, out int assetId))
                    {
                        msg = $"Bulk load responsibilities asset ID value {assetCol.Value} is not a valid asset id.  Asset ID values must be an integer.";
                        CoreFunction.AITrackTrace(functionName, msg, companyId: company.CurrentCompanyID);
                    }
                    else
                    {
                        responsiblityOverride.AssetID = assetId;
                    }

                    var resource = resourceCol.Value;
                    var responsiblity = responsibilityCol.Value;

                    // lookup the resource
                    var resourceParts = resource.Split(':');

                    if (resourceParts.Length != 2)
                    {
                        msg = $"Bulk load responsibilities resource value {resource} is not a valid resource it must be formatted [type]:[id].";
                        CoreFunction.AITrackTrace(functionName, msg, companyId: company.CurrentCompanyID);
                    }
                    else
                    {
                        if (string.Compare(resourceParts[0], "USER", true) == 0)
                        {
                            responsiblityOverride.SecurityAsset = "R";

                            var email = resourceParts[1];
                            //lookup the resource
                            var res = company.GlobalReportingResources.Where(x => string.Compare(x.Email, email, true) == 0).FirstOrDefault();

                            if (res == null)
                            {
                                msg = $"Bulk load responsibilities user value {resourceParts[1]} is not a valid resource and the email cannot be found in the resources table.";
                                CoreFunction.AITrackTrace(functionName, msg, companyId: company.CurrentCompanyID);
                            }
                            else
                            {
                                responsiblityOverride.SecurityAssetID = res.ResourceID;
                            }
                        }
                        else
                        {
                            responsiblityOverride.SecurityAsset = "G";

                            //lookup the group
                            var resourcePart = resourceParts[1];
                            var grp = company.Groups.Where(x => string.Compare(x.Name, resourcePart, true) == 0).FirstOrDefault();

                            if (grp == null)
                            {
                                msg = $"Bulk load responsibilities group name value {resourcePart} is not a valid group name it cannot be found in the groups table.";
                                CoreFunction.AITrackTrace(functionName, msg, companyId: company.CurrentCompanyID);
                            }
                            else
                            {
                                responsiblityOverride.SecurityAssetID = grp.ID;
                            }
                        }
                    }

                    // lookup the responsibility

                    var resp = company.ResponsibilityTypes.Where(x => string.Compare(x.Name, responsiblity, true) == 0).FirstOrDefault();

                    if (resp == null)
                    {
                        msg = $"Bulk load responsibilities responsibility value {responsiblity} is not a valid responsibility type it cannot be found in the responsibility type table.";
                        CoreFunction.AITrackTrace(functionName, msg, companyId: company.CurrentCompanyID);
                    }
                    else
                    {
                        responsiblityOverride.ResponsibilityTypeID = resp.ID;
                    }

                    if (string.IsNullOrEmpty(msg))
                    {
                        if (company.ResponsibilityTypeRelationOverrideItems.Any(x => x.ResponsibilityTypeID == responsiblityOverride.ResponsibilityTypeID && x.SecurityAsset == responsiblityOverride.SecurityAsset && x.SecurityAssetID == responsiblityOverride.SecurityAssetID && responsiblityOverride.AssetID == x.AssetID))
                        {
                            msg = "Responsibility already exists.";
                            status = 1;
                        }
                        else
                        {
                            msg = "Responsibility added sucessfully.";
                            status = 1;
                            company.ResponsibilityTypeRelationOverrideItems.Add(responsiblityOverride);
                        }
                    }

                    CoreFunction.AITrackTrace(functionName, $"Bulk load responsibilities adding {currentRowIndex} of {rowData.Count} responsibilites.", companyId: company.CurrentCompanyID);

                    // update status for this item
                    var statusSql = "update LoadItem set [Object] = 'Intersect', ObjectID = @objectId, Status = @status, StatusMessage = @msg where LoadID = @loadId and RowIndex = @rowIndex";

                    await company.QueryAsync<int>(statusSql, new { objectId = responsiblityOverride.ID, status=status, msg = msg, loadId = loadId, rowIndex = currentRowIndex });
                }

                //next row
                currentRowIndex++;

                rowData = loaddata.Where(x => x.RowIndex == currentRowIndex).ToList();
            }

            if (currentRowIndex > 2) company.SaveChanges();
        }

        static void executeWithTry(SqlConnection companyConnection, string lineageSql, int companyID, int timeout = 1200)
        {
            try
            {
                companyConnection.Execute(lineageSql, null, null, timeout);
            }
            catch (Exception ex)
            {
                //logger.Error(lineageSql);
                CoreFunction.AITrackException(functionName, ex, companyID);
                //logger.Error(ex.GetFullExceptionData());
            }
        }
    }
}
