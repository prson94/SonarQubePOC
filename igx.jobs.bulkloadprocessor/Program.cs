using d360.core.entities;
using d360.core.exceptions;
using d360.core.queue;
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
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    public class BulkLoadProcessor
    {
        const string functionName = "BulkLoad_Process";

        public async static Task Run([QueueTrigger("%BulkLoadQueue%"), StorageAccount("MainStorageAccount")] string myQueueItem, TextWriter log)
        {
            var loadInfo = JsonConvert.DeserializeObject<BulkLoadInfo>(myQueueItem);

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

                var companyConnection = CompanyConnectionUtils.GetCompanyConnection(loadInfo.CompanyID);

                #region Create Load Items from Load file

                var load = company.Loads.Include("LoadColumns").SingleOrDefault(i => i.ID == loadInfo.LoadID); //.Include("LoadItems.LoadItemColumns")

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
                    case "O":
                        //log.Info($"Starting bulk responsibilities job with load ID {load.ID} for Company ID {loadInfo.CompanyID}");
                        await BulkLoadOwnership(company, load.ID);
                        break;
                    case "P":   // Promotions
                        executeWithTry(companyConnection, $@"EXEC bulkload.Promotions {load.ID}", loadInfo.CompanyID, 2400);
                        break;
                    case "R":   // Relations                                
                        //log.Info($"Starting bulk relate job with load ID {load.ID} for Company ID {loadInfo.CompanyID}");
                        await company.PerformBulkRelationshipOperation(load.ID, d360.core.enums.BulkRelationshipOperation.Relate);
                        break;
                    case "U":   // Unrelate
                        //log.Info($"Starting bulk unrelate job with load ID {load.ID} for Company ID {loadInfo.CompanyID}");
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
                CoreFunction.AITrackException(functionName, ex, loadInfo.CompanyID);
                //log.Error($"Company [{loadInfo.CompanyID}], Load ID [{loadInfo.LoadID}]: [{ex.GetFullExceptionData()}]");
            }
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
                            var grp = company.Groups.Where(x => string.Compare(x.Name, resourceParts[1], true) == 0).FirstOrDefault();

                            if (grp == null)
                            {
                                msg = $"Bulk load responsibilities group name value {resourceParts[1]} is not a valid group name it cannot be found in the groups table.";
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
                        }
                        else
                        {
                            msg = "Responsibility added sucessfully.";
                            company.ResponsibilityTypeRelationOverrideItems.Add(responsiblityOverride);
                        }
                    }

                    CoreFunction.AITrackTrace(functionName, $"Bulk load responsibilities adding {currentRowIndex} of {rowData.Count} responsibilites.", companyId: company.CurrentCompanyID);

                    // update status for this item
                    var statusSql = "update LoadItem set [Object] = 'Intersect', ObjectID = @objectId, Status = 1, StatusMessage = @msg where LoadID = @loadId and RowIndex = @rowIndex";

                    await company.QueryAsync<int>(statusSql, new { objectId = responsiblityOverride.ID, msg = msg, loadId = loadId, rowIndex = currentRowIndex });
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
