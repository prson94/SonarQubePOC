using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using d360.extensions.info;
using d360.extensions.caching;
using d360.extensions.queue;
using d360.model;
using System.Linq;
using d360.utils.company;
using d360.core.queue;
using Dapper;
using System.IO;
using SpreadsheetLight;
using d360.core.entities;
using System.Collections.Generic;
using System.Data.SqlClient;
using d360.core.exceptions;
using d360.core;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using igx.functions.Core;
using System.Configuration;
using System.Threading.Tasks;

namespace igx.functions.Queue
{
    public static class ProcessBulkLoad
    {
        const string functionName = "ProcessBulkLoad";

        [FunctionName(functionName)]
        public static async Task Run([QueueTrigger("%BulkLoadQueue%", Connection = "MainStorageAccount")]string myQueueItem, TraceWriter log) //%BulkLoadQueueName%
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
                        #region Ownership
                        
                        #region
                        /*
                            "Item Type",
                            "Subject Area", //ArtifactType only
                            "Item Path",
                            "Responsibility",
                            "Resource"
                         */
                        #endregion

                        #region Get data to pre-populate

                        var subjectAreas = company.Table<TaxonomyType>().Select(i => new SimpleTypeModel { Name = i.Name.ToLower(), ID = i.ID }).ToList();

                        List<SimpleTypeModel> types = null;
                        switch (load.Object)
                        {
                            case "ArtifactType":
                                types = company.Table<ArtifactType>().Select(i => new SimpleTypeModel { Name = i.Name.ToLower(), ID = i.ID }).ToList();
                                break;
                            case "ReferenceItemType":
                                types = company.Table<ReferenceItemType>().Select(i => new SimpleTypeModel { Name = i.Name.ToLower(), ID = i.ID }).ToList();
                                break;
                            case "FusionType":
                                types = company.Table<FusionType>().Select(i => new SimpleTypeModel { Name = i.Name.ToLower(), ID = i.ID }).ToList();
                                break;
                            case "PolicyType":
                                types = company.Table<PolicyType>().Select(i => new SimpleTypeModel { Name = i.Name.ToLower(), ID = i.ID }).ToList();
                                break;
                            case "TaxonomyType":
                                types = company.Table<TaxonomyType>().Select(i => new SimpleTypeModel { Name = i.Name.ToLower(), ID = i.ID }).ToList();
                                break;
                        }

                        var resources = new List<SimpleTypeModel>();
                        resources.AddRange(
                            company.Table<Group>().OrderBy(x => x.Name).Select(x => new SimpleTypeModel { Name = "group:" + x.Name.Trim().ToLower(), ID = x.ID }).ToList());
                        resources.AddRange(
                            company.Table<GlobalReportingResource>().ToList().Select(x => new SimpleTypeModel { Name = "user:" + x.FullName.Trim().ToLower(), ID = x.ResourceID })
                         );

                        var responsibilities = company.Table<ResponsibilityType>().OrderBy(x => x.Name).Select(x => new SimpleTypeModel { Name = x.Name.ToLower(), ID = x.ID });

                        var allocations = company.Table<ResponsibilityTypeRelation>().ToList();

                        #endregion

                        #region ForEach

                        load = company.Loads.Include("LoadColumns").Include("LoadItems.LoadItemColumns").SingleOrDefault(i => i.ID == loadInfo.LoadID);

                        foreach (var loadItem in load.LoadItems)
                        {
                            //#region Vars

                            //var rawType = "";
                            //var rawSubjectArea = "";
                            //var rawItemPath = "";
                            //var rawResponsibility = "";
                            //var rawResource = "";

                            //LoadItemColumn typeColumn = null;
                            //LoadItemColumn subjectAreaColumn = null;
                            //LoadItemColumn itemColumn = null;
                            //LoadItemColumn responsibilityColumn = null;
                            //LoadItemColumn resourceColumn = null;

                            //SimpleTypeModel verifiedType = null;
                            //SimpleTypeModel verifiedSubjectArea = null;
                            //SimpleTypeModel verifiedItem = null;
                            //SimpleTypeModel verifiedResponsibility = null;
                            //SimpleTypeModel verifiedResource = null;

                            //var currentColumnIndex = 1;

                            //#endregion

                            //#region Verify Type

                            //typeColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == currentColumnIndex);
                            //rawType = typeColumn.Value.Trim().ToLower();
                            //verifiedType = types.SingleOrDefault(i => i.Name == rawType);

                            //if (verifiedType != null)
                            //{
                            //    typeColumn.LookupObject = load.Object;
                            //    typeColumn.LookupObjectID = verifiedType.ID;
                            //}
                            //currentColumnIndex++;

                            //#endregion

                            //if (verifiedType != null)
                            //{
                            //    #region Verify Item

                            //    itemColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == currentColumnIndex);
                            //    rawItemPath = itemColumn.Value.Trim().ToLower();
                            //    switch (load.Object)
                            //    {
                            //        case "ArtifactType":
                            //            if (verifiedSubjectArea != null)
                            //            {
                            //                verifiedItem = company.Filter<Artifact>(x =>
                            //                    x.ArtifactTypeID == verifiedType.ID //&&
                            //                    //x.TaxonomyTypeID == verifiedSubjectArea.ID &&
                            //                    //x.TextPath.ToLower() == rawItemPath
                            //                )
                            //                .Select(x => new SimpleTypeModel { Name = "Artifact", ID = x.ID })
                            //                .FirstOrDefault();
                            //            }
                            //            break;
                            //        case "ReferenceItemType":
                            //            verifiedItem = company.Filter<ReferenceItemType>(x =>
                            //                x.Name.ToLower() == rawItemPath
                            //            )
                            //            .Select(x => new SimpleTypeModel { Name = "ReferenceItemType", ID = x.ID })
                            //            .FirstOrDefault();
                            //            break;
                            //        case "FusionType":
                            //            verifiedItem = company.Filter<Fusion>(x =>
                            //                x.FusionTypeID == verifiedType.ID &&
                            //                x.Name.ToLower() == rawItemPath
                            //            )
                            //            .Select(x => new SimpleTypeModel { Name = "Fusion", ID = x.ID })
                            //            .FirstOrDefault();
                            //            break;
                            //        case "PolicyType":
                            //            verifiedItem = company.Filter<Policy>(x =>
                            //                x.PolicyTypeID == verifiedType.ID &&
                            //                x.TextPath.ToLower() == rawItemPath
                            //            )
                            //            .Select(x => new SimpleTypeModel { Name = "Policy", ID = x.ID })
                            //            .FirstOrDefault();
                            //            break;
                            //        case "TaxonomyType":
                            //            verifiedItem = company.Filter<Taxonomy>(x =>
                            //                x.TaxonomyTypeID == verifiedType.ID &&
                            //                x.TextPath.ToLower() == rawItemPath
                            //            )
                            //            .Select(x => new SimpleTypeModel { Name = "Taxonomy", ID = x.ID })
                            //            .FirstOrDefault();
                            //            break;
                            //    }
                            //    if (verifiedItem != null)
                            //    {
                            //        itemColumn.LookupObject = verifiedItem.Name;
                            //        itemColumn.LookupObjectID = verifiedItem.ID;
                            //    }

                            //    #endregion
                            //}
                            //currentColumnIndex++;

                            //#region Verify Responsibility

                            //responsibilityColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == currentColumnIndex);
                            //rawResponsibility = string.IsNullOrEmpty(responsibilityColumn.Value) ? "" : responsibilityColumn.Value.Trim().ToLower();
                            //verifiedResponsibility = responsibilities.SingleOrDefault(i => i.Name == rawResponsibility);
                            //if (verifiedResponsibility != null)
                            //{
                            //    responsibilityColumn.LookupObject = "ResponsibilityType";
                            //    responsibilityColumn.LookupObjectID = verifiedResponsibility.ID;
                            //}
                            //currentColumnIndex++;

                            //#endregion

                            //#region Verify Resource

                            //resourceColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == currentColumnIndex);
                            //rawResource = resourceColumn.Value.Trim().ToLower();
                            //verifiedResource = resources.SingleOrDefault(i => i.Name == rawResource);
                            //if (verifiedResource != null)
                            //{
                            //    resourceColumn.LookupObject = rawResource.StartsWith("group:") ? "Group" : "Resource";
                            //    resourceColumn.LookupObjectID = verifiedResource.ID;
                            //}

                            //#endregion

                            //if (verifiedItem != null && verifiedResource != null && verifiedResponsibility != null)
                            //{
                            //    if (allocations.Any(x => x.ObjectType == load.Object && x.ObjectID == verifiedType.ID && x.ResponsibilityTypeID == verifiedResponsibility.ID))
                            //    {
                            //        // OK to proceed with insert.
                            //        var alreadyPresent = company.ResponsibilityTypeRelationOverrideItems.Any(i =>
                            //            i.ObjectType == verifiedItem.Name && i.ObjectID == verifiedItem.ID &&
                            //            i.ResponsibleObjectType == resourceColumn.LookupObject && i.ResponsibleObjectID == resourceColumn.LookupObjectID &&
                            //            i.ResponsibilityTypeID == verifiedResponsibility.ID &&
                            //            i.AssigningItemType == verifiedItem.Name && i.AssigningItemID == verifiedItem.ID);

                            //        if (!alreadyPresent)
                            //        {
                            //            var model = new Responsibility
                            //            {
                            //                ObjectID = verifiedItem.ID,
                            //                ObjectType = verifiedItem.Name,
                            //                ResponsibilityTypeID = verifiedResponsibility.ID,
                            //                ResponsibleObjectID = resourceColumn.LookupObjectID.Value,
                            //                ResponsibleObjectType = resourceColumn.LookupObject,
                            //                Visible = true
                            //            };

                            //            try
                            //            {
                            //                company.Add<Responsibility>(model);
                            //                loadItem.Object = "Responsibility";
                            //                loadItem.ObjectID = model.ID;
                            //                loadItem.Status = true;
                            //                loadItem.StatusMessage = "Successfully created responsibility.";
                            //            }
                            //            catch (BaseException ex)
                            //            {
                            //                loadItem.Status = false;
                            //                loadItem.StatusMessage = ex.StatusDescription;
                            //            }
                            //            catch (Exception ex)
                            //            {
                            //                loadItem.Status = false;
                            //                loadItem.StatusMessage = ex.Message;
                            //            }
                            //        }
                            //        else
                            //        {
                            //            loadItem.Status = true;
                            //            loadItem.StatusMessage = $" Responsibility already present on item.";
                            //        }
                            //    }
                            //    else
                            //    {
                            //        loadItem.Status = false;
                            //        loadItem.StatusMessage = $" Responsibility {rawResponsibility} not allocated to this type of item.";
                            //    }
                            //}
                            //else
                            //{
                            //    // Log errors.
                            //    loadItem.Status = false;

                            //    if (verifiedItem == null)
                            //    {
                            //        loadItem.StatusMessage += $" Could not find item [{rawItemPath}].";
                            //    }

                            //    if (verifiedResource == null)
                            //    {
                            //        loadItem.StatusMessage += $" Could not find resource [{rawResource}].";
                            //    }

                            //    if (verifiedResponsibility == null)
                            //    {
                            //        loadItem.StatusMessage += $" Could not find responsibility [{rawResponsibility}].";
                            //    }
                            //}

                            //company.Update(loadItem);
                        }

                        #endregion

                        break;
                        #endregion
                    case "P":   // Promotions
                        executeWithTry(companyConnection, log, $@"EXEC bulkload.Promotions {load.ID}", loadInfo.CompanyID, 2400);
                        break;
                    case "R":   // Relations                                
                        log.Info($"Starting bulk relate job with load ID {load.ID} for Company ID {loadInfo.CompanyID}");
                        await company.PerformBulkRelationshipOperation(load.ID, d360.core.enums.BulkRelationshipOperation.Relate);
                        break;
                    case "U":   // Unrelate
                        log.Info($"Starting bulk unrelate job with load ID {load.ID} for Company ID {loadInfo.CompanyID}");
                        await company.PerformBulkRelationshipOperation(load.ID, d360.core.enums.BulkRelationshipOperation.Unrelate);
                        break;
                    case "B":
                    case "BL":  // Business Lineage
                        executeWithTry(companyConnection, log, $@"EXEC bulkload.BusinessLineage {load.ID}", loadInfo.CompanyID, 2400);
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
                                        company.Add<MapRuleItem>(technicalMapping);
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
                log.Error($"Company [{loadInfo.CompanyID}], Load ID [{loadInfo.LoadID}]: [{ex.GetFullExceptionData()}]");
            }
        }

        static void executeWithTry(SqlConnection companyConnection, TraceWriter logger, string lineageSql, int companyID, int timeout = 1200)
        {
            try
            {
                companyConnection.Execute(lineageSql, null, null, timeout);
            }
            catch (Exception ex)
            {
                logger.Error(lineageSql);
                CoreFunction.AITrackException(functionName, ex, companyID);
                logger.Error(ex.GetFullExceptionData());
            }
        }
    }
}
