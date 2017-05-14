using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using d360.core;
using Dapper;
using System.IO;
using d360.core.entities;
using SpreadsheetLight;
using d360.extensions.info;
using d360.extensions.caching;
using d360.extensions.queue;
using d360.model;
using Newtonsoft.Json;
using d360.core.queue;
using System.Threading;
using d360.core.exceptions;
using d360.workflow.models;
using d360.workflow;
using d360.workflow.entities;
using System.Data.SqlClient;

namespace d360.jobs.queue.ProcessBulkLoad
{
    public class Program: FunctionsBase
    {
        static void Main()
        {
            JobHostConfiguration config = new JobHostConfiguration(constants.WEBJOBS_STORAGE_CONNECTION);
#if DEBUG
            config.Queues.BatchSize = 1;
            config.Queues.MaxDequeueCount = 5;
            config.Queues.MaxPollingInterval = TimeSpan.FromSeconds(15);
#else
            config.Queues.BatchSize = 3;
            config.Queues.MaxDequeueCount = 3;
            config.Queues.MaxPollingInterval = TimeSpan.FromSeconds(30);
#endif

            var host = new JobHost(config);
            host.RunAndBlock();
        }

        public static void ProcessBulkLoadPoisonMessage([QueueTrigger("d3s-bulkload-poison")] string queueMessage, TextWriter logger)
        {
            logger.WriteLine("Failed to process load, data=" + queueMessage);
        }

        public static void ProcessBulkLoadMessage([QueueTrigger("d3s-bulkload")] string queueMessage, TextWriter logger, CancellationToken token)
        {
            var loadInfo = JsonConvert.DeserializeObject<BulkLoadInfo>(queueMessage);

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

            var companyConnection = GetCompanyConnection(loadInfo.CompanyID);

            #region Create Load Items from Load file

            var load = company.Loads.Include("LoadColumns").SingleOrDefault(i => i.ID == loadInfo.LoadID); //.Include("LoadItems.LoadItemColumns")

            companyConnection.Open();
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

                //logger.WriteLine($"Created {load.LoadItems.Count} load item(s) for Company: {loadInfo.CompanyID}, Load: {loadInfo.LoadID}.");

                companyConnection.Open();

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

            if (load.Action == "O")         // Ownership/Responsibilities
            {
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
#region Vars

                    var rawType = "";
                    var rawSubjectArea = "";
                    var rawItemPath = "";
                    var rawResponsibility = "";
                    var rawResource = "";

                    LoadItemColumn typeColumn = null;
                    LoadItemColumn subjectAreaColumn = null;
                    LoadItemColumn itemColumn = null;
                    LoadItemColumn responsibilityColumn = null;
                    LoadItemColumn resourceColumn = null;

                    SimpleTypeModel verifiedType = null;
                    SimpleTypeModel verifiedSubjectArea = null;
                    SimpleTypeModel verifiedItem = null;
                    SimpleTypeModel verifiedResponsibility = null;
                    SimpleTypeModel verifiedResource = null;

                    var currentColumnIndex = 1;

#endregion

#region Verify Type

                    typeColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == currentColumnIndex);
                    rawType = typeColumn.Value.Trim().ToLower();
                    verifiedType = types.SingleOrDefault(i => i.Name == rawType);

                    if (verifiedType != null)
                    {
                        typeColumn.LookupObject = load.Object;
                        typeColumn.LookupObjectID = verifiedType.ID;
                    }
                    currentColumnIndex++;

#endregion

#region Verify Subject Area

                    if (load.Object == "ArtifactType")
                    {
                        subjectAreaColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == currentColumnIndex);
                        rawSubjectArea = subjectAreaColumn.Value.Trim().ToLower();
                        verifiedSubjectArea = subjectAreas.SingleOrDefault(i => i.Name == rawSubjectArea);
                        if (verifiedSubjectArea != null)
                        {
                            subjectAreaColumn.LookupObject = "TaxonomyType";
                            subjectAreaColumn.LookupObjectID = verifiedSubjectArea.ID;
                        }
                        currentColumnIndex++;
                    }

#endregion

                    if (verifiedType != null)
                    {
                        #region Verify Item

                        itemColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == currentColumnIndex);
                        rawItemPath = itemColumn.Value.Trim().ToLower();
                        switch (load.Object)
                        {
                            case "ArtifactType":
                                if (verifiedSubjectArea != null)
                                {
                                    verifiedItem = company.Filter<Artifact>(x =>
                                        x.ArtifactTypeID == verifiedType.ID &&
                                        x.TaxonomyTypeID == verifiedSubjectArea.ID &&
                                        x.TextPath.ToLower() == rawItemPath
                                    )
                                    .Select(x => new SimpleTypeModel { Name = "Artifact", ID = x.ID })
                                    .FirstOrDefault();
                                }
                                break;
                            case "ReferenceItemType":
                                verifiedItem = company.Filter<ReferenceItemType>(x =>
                                    x.Name.ToLower() == rawItemPath
                                )
                                .Select(x => new SimpleTypeModel { Name = "ReferenceItemType", ID = x.ID })
                                .FirstOrDefault();
                                break;
                            case "FusionType":
                                verifiedItem = company.Filter<Fusion>(x =>
                                    x.FusionTypeID == verifiedType.ID &&
                                    x.Name.ToLower() == rawItemPath
                                )
                                .Select(x => new SimpleTypeModel { Name = "Fusion", ID = x.ID })
                                .FirstOrDefault();
                                break;
                            case "PolicyType":
                                verifiedItem = company.Filter<Policy>(x =>
                                    x.PolicyTypeID == verifiedType.ID &&
                                    x.TextPath.ToLower() == rawItemPath
                                )
                                .Select(x => new SimpleTypeModel { Name = "Policy", ID = x.ID })
                                .FirstOrDefault();
                                break;
                            case "TaxonomyType":
                                verifiedItem = company.Filter<Taxonomy>(x =>
                                    x.TaxonomyTypeID == verifiedType.ID &&
                                    x.TextPath.ToLower() == rawItemPath
                                )
                                .Select(x => new SimpleTypeModel { Name = "Taxonomy", ID = x.ID })
                                .FirstOrDefault();
                                break;
                        }
                        if (verifiedItem != null)
                        {
                            itemColumn.LookupObject = verifiedItem.Name;
                            itemColumn.LookupObjectID = verifiedItem.ID;
                        }

                        #endregion
                    }
                    currentColumnIndex++;

                    #region Verify Responsibility

                    responsibilityColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == currentColumnIndex);
                    rawResponsibility = string.IsNullOrEmpty(responsibilityColumn.Value) ? "" : responsibilityColumn.Value.Trim().ToLower();
                    verifiedResponsibility = responsibilities.SingleOrDefault(i => i.Name == rawResponsibility);
                    if (verifiedResponsibility != null)
                    {
                        responsibilityColumn.LookupObject = "ResponsibilityType";
                        responsibilityColumn.LookupObjectID = verifiedResponsibility.ID;
                    }
                    currentColumnIndex++;
                    
                    #endregion

                    #region Verify Resource

                    resourceColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == currentColumnIndex);
                    rawResource = resourceColumn.Value.Trim().ToLower();
                    verifiedResource = resources.SingleOrDefault(i => i.Name == rawResource);
                    if (verifiedResource != null)
                    {
                        resourceColumn.LookupObject = rawResource.StartsWith("group:") ? "Group" : "Resource";
                        resourceColumn.LookupObjectID = verifiedResource.ID;
                    }

                    #endregion

                    if (verifiedItem != null && verifiedResource != null && verifiedResponsibility != null)
                    {
                        if (allocations.Any(x => x.ObjectType == load.Object && x.ObjectID == verifiedType.ID && x.ResponsibilityTypeID == verifiedResponsibility.ID))
                        {
                            // OK to proceed with insert.
                            var alreadyPresent = company.ResponsibilityDetails.Any(i =>
                                i.ObjectType == verifiedItem.Name && i.ObjectID == verifiedItem.ID &&
                                i.ResponsibleObjectType == resourceColumn.LookupObject && i.ResponsibleObjectID == resourceColumn.LookupObjectID &&
                                i.ResponsibilityTypeID == verifiedResponsibility.ID &&
                                i.AssigningItemType == verifiedItem.Name && i.AssigningItemID == verifiedItem.ID);

                            if (!alreadyPresent)
                            {
                                var model = new Responsibility
                                {
                                    ObjectID = verifiedItem.ID,
                                    ObjectType = verifiedItem.Name,
                                    ResponsibilityTypeID = verifiedResponsibility.ID,
                                    ResponsibleObjectID = resourceColumn.LookupObjectID.Value,
                                    ResponsibleObjectType = resourceColumn.LookupObject,
                                    Visible = true
                                };

                                try
                                {
                                    company.Add<Responsibility>(model);
                                    loadItem.Object = "Responsibility";
                                    loadItem.ObjectID = model.ID;
                                    loadItem.Status = true;
                                    loadItem.StatusMessage = "Successfully created responsibility.";
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
                                loadItem.Status = true;
                                loadItem.StatusMessage = $" Responsibility already present on item.";
                            }
                        }
                        else
                        {
                            loadItem.Status = false;
                            loadItem.StatusMessage = $" Responsibility {rawResponsibility} not allocated to this type of item.";
                        }
                    }
                    else
                    {
                        // Log errors.
                        loadItem.Status = false;

                        if (verifiedItem == null)
                        {
                            loadItem.StatusMessage += $" Could not find item [{rawItemPath}].";
                        }

                        if (verifiedResource == null)
                        {
                            loadItem.StatusMessage += $" Could not find resource [{rawResource}].";
                        }

                        if (verifiedResponsibility == null)
                        {
                            loadItem.StatusMessage += $" Could not find responsibility [{rawResponsibility}].";
                        }
                    }

                    company.Update(loadItem);
                }

                #endregion

                load.DateCompleted = DateTime.UtcNow;
                company.Update(load);

                #endregion
            }
            else if (load.Action == "P")    // Promotion
            {
                #region Promotions

                try
                {
                    companyConnection.Open();
                    executeWithTry(companyConnection, logger, $@"EXEC bulkload.Promotions {load.ID}", 2400);
                    companyConnection.Close();
                }
                catch (Exception ex)
                {
                    logger.WriteLine("Bulk load procedure completed for Load ID {0}. {1}", loadInfo.LoadID, ex.GetFullExceptionData());
                }

                #endregion
            }
            else if (load.Action == "R")    // Relation
            {
                #region Relationship
                try
                {
                    companyConnection.Open();

                    // Call relationships procedure.
                    executeWithTry(companyConnection, logger, $@"EXEC bulkload.Relationships {load.ID}", 2400);

                    companyConnection.Close();
                }
                catch (Exception ex)
                {
                    logger.WriteLine("Bulk load procedure completed for Load ID {0}. {1}", loadInfo.LoadID, ex.GetFullExceptionData());
                }
                #endregion
            }
            else if (load.Action == "U")    // Unrelate
            {
                #region Unrelate
                try
                {
                    companyConnection.Open();

                    // Call relationships procedure.
                    executeWithTry(companyConnection, logger, $@"EXEC bulkload.Unrelate {load.ID}", 2400);

                    companyConnection.Close();
                }
                catch (Exception ex)
                {
                    logger.WriteLine("Bulk load procedure completed for Load ID {0}. {1}", loadInfo.LoadID, ex.GetFullExceptionData());
                }
                #endregion
            }
            else if (load.Action == "S")    // Synonym
            {
                #region Synonyms

                try
                {
                    companyConnection.Open();
                    executeWithTry(companyConnection, logger, $@"EXEC bulkload.Synonyms {load.ID}", 2400);
                    companyConnection.Close();
                }
                catch (Exception ex)
                {
                    logger.WriteLine("Bulk load procedure completed for Load ID {0}. {1}", loadInfo.LoadID, ex.GetFullExceptionData());
                }

                #endregion
            }
            else if (load.Action == "BL")    // Business Lineage
            {
                #region Business Lineage

                try
                {
                    companyConnection.Open();

                    // Call business lineage procedure.
                    executeWithTry(companyConnection, logger, $@"EXEC bulkload.BusinessLineage {load.ID}", 2400);

                    companyConnection.Close();
                }
                catch (Exception ex)
                {
                    logger.WriteLine("Bulk load procedure completed for Load ID {0}. {1}", loadInfo.LoadID, ex.GetFullExceptionData());
                }

                #endregion
            }
            else if (load.Action == "T" || load.Action == "TL")    // Technical Lineage
            {
                #region Technical Lineage

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

                load.DateCompleted = DateTime.UtcNow;
                company.Update(load);

                #endregion
            }
            else if (load.Action == "W")    // Promotion Propose via Workflow
            {
                #region Propose

                load = company.Loads.Include("LoadColumns").Include("LoadItems.LoadItemColumns").SingleOrDefault(i => i.ID == loadInfo.LoadID);

#region Get data to pre-populate

                var proposalSubjectAreas = company.Table<TaxonomyType>().Select(i => new SimpleTypeModel { ID = i.ID, Name = i.Name.ToLower() }).ToList();

#endregion

#region ForEach

                var artifactType = company.GetById<ArtifactType>(load.ObjectID);
                var processor = new Processor();
                var wtrItems = company.Filter<WorkflowTypeRelation>(i => i.Object == "ArtifactType" && i.ObjectID == load.ObjectID && i.Enabled && (i.WorkflowType == WorkflowType.SuggestNewArtifact || i.WorkflowType == WorkflowType.SuggestNewArtifactMulti)).ToList();

                load = company.GetById<Load>(loadInfo.LoadID, i => i.LoadItems);

                foreach (var loadItem in load.LoadItems)
                {
                    var nameColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 1);
                    var descriptionColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 2);
                    LoadItemColumn parentColumn = null;
                    bool parentRequired = artifactType.ParentID.HasValue;

#region Verify Subject Area

                    var subjectAreaColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 3);
                    var rawSubjectArea = subjectAreaColumn.Value.Trim().ToLower();
                    var verifiedSubjectArea = proposalSubjectAreas.SingleOrDefault(i => i.Name == rawSubjectArea);
                    if (verifiedSubjectArea != null)
                    {
                        subjectAreaColumn.LookupObject = "TaxonomyType";
                        subjectAreaColumn.LookupObjectID = verifiedSubjectArea.ID;
                    }

#endregion

#region Verify Parent

                    if (parentRequired)
                    {
                        parentColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 4);
                        var rawParent = parentColumn.Value.Trim().ToLower();
                        var verifiedParents = company.Filter<Artifact>(i => i.ArtifactTypeID == artifactType.ParentID && i.TextPath.ToLower() == parentColumn.Value.ToLower()).ToList();
                        if (verifiedParents != null)
                        {
                            if (verifiedParents.Count > 1)
                            {
                                var verifiedParent = verifiedParents.SingleOrDefault(i => i.TaxonomyTypeID == verifiedSubjectArea.ID);
                                if (verifiedParent == null)
                                    verifiedParent = verifiedParents.First();

                                parentColumn.LookupObject = "Artifact";
                                parentColumn.LookupObjectID = verifiedParent.ID;
                            }
                        }
                    }

#endregion

                    if (verifiedSubjectArea != null && 
                        (
                        (parentRequired && parentColumn.LookupObjectID.HasValue) ||
                        !parentRequired
                        )
                    )
                    {
                        try
                        {
                            var model = new NewArtifactRequest();
                            // Static fields
                            model.ArtifactTypeID = load.ObjectID;
                            model.TaxonomyTypeID = verifiedSubjectArea.ID;
                            model.Name = nameColumn.Value;
                            model.Description = descriptionColumn.Value;
                            if (parentRequired)
                            {
                                model.ParentID = parentColumn.LookupObjectID.Value;
                            }
                            model.RequestingResourceID = load.UpdatedBy.Value;

                            var fields = company.GetFieldTypesByObject(SystemObjects.ArtifactType, load.ObjectID).OrderBy(i => i.SortOrder).ToList();

                            if (fields.Count > 0)
                            {
                                model.Fields = new Dictionary<string, object>();
                                foreach (var field in fields)
                                {
                                    var fieldColumn = load.LoadColumns.SingleOrDefault(i => i.Name == field.Name);
                                    if (fieldColumn != null)
                                    {
                                        var fieldItemColumn = loadItem.LoadItemColumns.SingleOrDefault(i => i.ColumnIndex == fieldColumn.ColumnIndex);
                                        if (fieldItemColumn != null)
                                        {
                                            var cleanedValue = fieldItemColumn.Value;

                                            switch (field.Type)
                                            {
                                                case "Boolean":
                                                    cleanedValue = fieldItemColumn.Value.In<string>("true", "True", "1", "yes", "Yes", "y", "Y").ToString();
                                                    break;
                                                case "Link":
                                                    if (!fieldItemColumn.Value.Contains("|"))
                                                        cleanedValue = $"{fieldItemColumn.Value}|{fieldItemColumn.Value}";
                                                    break;
                                                case "Lookup":
                                                    var lookup = company.Filter<FieldLookupValue>(i => i.LookupObjectType == field.LookupObjectType && 
                                                        i.LookupObjectID == field.LookupObjectID && 
                                                        i.Text.ToLower() == fieldItemColumn.Value.ToLower()
                                                        ).FirstOrDefault();
                                                    if (lookup != null)
                                                    {
                                                        fieldItemColumn.LookupObject = "Lookup";
                                                        fieldItemColumn.LookupObjectID = lookup.Value;
                                                        cleanedValue = lookup.Value.ToString();
                                                    }
                                                    break;
                                            }
                                            model.Fields.Add($"FieldType_{field.ID}", cleanedValue);
                                        }
                                    }
                                }
                            }

                            var dictionary = new Dictionary<string, object>();
                            dictionary.Add("CompanyID", company.CurrentCompanyID);
                            dictionary.Add("requestInfo", model);


                            Guid workflowID = Guid.Empty;
                            if (wtrItems.Count(i => i.WorkflowType == WorkflowType.SuggestNewArtifactMulti) > 0)
                            {
                                workflowID = processor.CreateNewWorkflowInstance(WorkflowVersionMap.SuggestNewArtifactMultiStepIdentity_vCurrent, dictionary);
                            }
                            else
                            {
                                workflowID = processor.CreateNewWorkflowInstance(WorkflowVersionMap.SuggestNewArtifactIdentity_vCurrent, dictionary);
                            }

                            if (workflowID != Guid.Empty)
                            {
                                loadItem.Status = true;
                                loadItem.StatusMessage = "Successfully created propose workflow.";
                            }
                            else
                            {
                                loadItem.Status = false;
                                loadItem.StatusMessage = "Unable to create propose workflow.";
                            }

                        }
                        catch (BaseException ex)
                        {
                            loadItem.Status = false;
                            loadItem.StatusMessage += " " + ex.StatusDescription;
                        }
                    }
                    else
                    {
                        loadItem.Status = false;
                        loadItem.StatusMessage += $" The subject area you provided is invalid: {rawSubjectArea}].";
                    }

                    company.Update(loadItem);

                }

#endregion

                load.DateCompleted = DateTime.UtcNow;
                company.Update(load);

                #endregion
            }
        }

        static void executeWithTry(SqlConnection companyConnection, TextWriter logger, string lineageSql, int timeout = 1200)
        {
            try
            {
                companyConnection.Execute(lineageSql, null, null, timeout);
            }
            catch (Exception ex)
            {
                logger.WriteLine(lineageSql);
                logger.WriteLine(ex.GetFullExceptionData());
            }
        }
    }
}
