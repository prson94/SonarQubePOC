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
using d360.core.enums;
using Newtonsoft.Json;
using d360.core.queue;
using System.Threading;

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
            config.Queues.BatchSize = 8;
            config.Queues.MaxDequeueCount = 3;
            config.Queues.MaxPollingInterval = TimeSpan.FromSeconds(30);
#endif

            var host = new JobHost(config);
            host.RunAndBlock();

            #region 
//            var mex = new List<Exception>();

//            try
//            {
//                var companies = new List<int>() { 4 };
//                //var companies = GetActiveCompanyIDs();//.Where(i => i == 4).ToList();

//                var domainPrefixes = GetCompanyDomainPrefixes();

//                companies.AsParallel().WithDegreeOfParallelism(4).ForAll(companyID =>
//                {
//                    var sec = new UriSecurityContextProvider() {
//                        CompanyID = companyID,
//                        ResourceID = 0,
//                        CompanyPrefix = domainPrefixes.Single(i => i.Key == companyID).Value,
//                        IsAdministrator = true
//                    };
//                    var cache = new DummyCachingProvider();
//                    var queue = new AzureQueueSource();
//                    var community = new CommunityContext(cache, queue, sec);
//                    var company = new CompanyContext(community, cache, queue, sec);

//                    var queueItems = company.BulkLoadQueues.Where(i => i.MachineAssigned == null && i.NumberOfRetries < 3).OrderBy(i => i.LoadID).Take(2).ToList();

//                    queueItems.ForEach(q =>
//                    {
//                        q.MachineAssigned = System.Environment.MachineName;
//                    });
//                    company.SaveChanges();

//                    queueItems.ForEach(q =>
//                    {
//                        try
//                        {
//                            var load = company.Loads.Include(i => i.LoadColumns).SingleOrDefault(i => i.ID == q.LoadID);

//                            Console.WriteLine("Company: {0}. Processing Load {1}", companyID, load.ID);

//                            var existingRows = company.LoadItems.Any(i => i.LoadID == q.LoadID);

//                            if (!existingRows)
//                            {
//                                var memoryStream = new MemoryStream(load.File);
//                                var xls = new SLDocument(memoryStream);

//                                var stats = xls.GetWorksheetStatistics();

//                                var numberOfRows = stats.NumberOfRows;
//                                var rowIndex = stats.StartRowIndex + 1;
//                                while (rowIndex <= stats.EndRowIndex)
//                                {

//                                    var loadItem = new LoadItem { LoadID = load.ID, RowIndex = rowIndex, LoadItemColumns = new List<LoadItemColumn>(), Status = "Queued" };

//                                    foreach (var c in load.LoadColumns.OrderBy(i => i.ColumnIndex))
//                                    {
//                                        var format = xls.GetCellStyle(rowIndex, c.ColumnIndex).FormatCode;
//                                        var isDate = false;

//                                        if (format.Contains("[$-404]") || format.Contains("m/d") || format.Contains("m-d") || format.Contains("d-m") ||
//                                            format.Contains("[$-F400]") || format.Contains("[$-409]"))
//                                            isDate = true;

//                                        loadItem.LoadItemColumns.Add(new LoadItemColumn { ColumnIndex = c.ColumnIndex, LoadID = load.ID, RowIndex = rowIndex, Value = (isDate ? xls.GetCellValueAsDateTime(rowIndex, c.ColumnIndex).ToShortDateString() : xls.GetCellValueAsString(rowIndex, c.ColumnIndex) )});
//                                    }

//                                    load.LoadItems.Add(loadItem); //company.LoadItems.Add(loadItem);

//                                    rowIndex++;
//                                }

//                                company.SaveChanges();  // Save all load items and columns we created.
//                            }

//                            Console.WriteLine("Company: {0}. Executing ProcessBulkLoad procedure for Load {1}", companyID, load.ID);

//                            if (load.Action == "N")
//                            {
//                                /*
//                                 * Source subject type	
//                                 * Source subject type name	
//                                 * Source subject subject area	
//                                 *      Source subject	
//                                 * 
//                                 * Source object type	
//                                 * Source object type name	
//                                 * Source object subject area	
//                                 *      Source object	
//                                 * 
//                                 * Target subject type	
//                                 * Target subject type name	
//                                 * Target subject subject area	
//                                 *      Target subject	
//                                 * 
//                                 * Target object type	
//                                 * Target object type name	
//                                 * Target object subject area	
//                                 *      Target object	
//                                 * 
//                                 * Role
//                                 */
//                                //"Artifact", "Domain", "Policy", "Rule", "Taxonomy"

//                                #region Get data to pre-populate

//                                var objectTypes = company.Query<IntersectTypeOption>(@"
//select ID, ltrim(rtrim(lcase(Name))) as Name, 'Artifact' from ArtifactType
//union
//select ID, ltrim(rtrim(lcase(Name))) as Name, 'Domain' from DomainType
//union
//select ID, ltrim(rtrim(lcase(Name))) as Name, 'Policy' from PolicyType
//union
//select 1 as ID, 'informational' as Name, 'Rule'
//union
//select 2 as ID, 'quality check' as Name, 'Rule'
//union
//select 3 as ID, 'metric' as Name, 'Rule'
//union
//select 4 as ID, 'profile' as Name, 'Rule'
//union
//select ID, ltrim(rtrim(lcase(Name))) as Name, 'Taxonomy' from TaxonomyType").ToList();
//                                var roles = company.Table<IntersectRole>().Select(i => new SimpleTypeModel { Name = i.Name.ToLower(), ID = i.ID }).ToList();
//                                var subjectAreas = company.Table<IntersectType>().Select(i => new SimpleTypeModel { Name = i.Name.ToLower(), ID = i.ID }).ToList();

//                                #endregion

//                                foreach (var loadItem in load.LoadItems)
//                                {
//                                    var rawType = "";
//                                    var rawTypeName = "";
//                                    var rawSubjectArea = "";
//                                    var rawItem = "";

//                                    LoadItemColumn typeColumn = null;
//                                    LoadItemColumn typeNameColumn = null;
//                                    LoadItemColumn subjectAreaColumn = null;
//                                    LoadItemColumn itemColumn = null;

//                                    IntersectTypeOption verifiedType = null;
//                                    SimpleTypeModel verifiedSubjectArea = null;
//                                    SimpleTypeModel verifiedRole = null;

//                                    #region Look up source subject info

//                                    #region Verify Type

//                                    typeColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 1);
//                                    rawType = typeColumn.Value.Trim().ToLower();
//                                    typeNameColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 2);
//                                    rawTypeName = typeNameColumn.Value.Trim().ToLower();
//                                    verifiedType = objectTypes.SingleOrDefault(i => i.Type == rawType && i.Name == rawTypeName);

//                                    if (verifiedType != null)
//                                    {
//                                        typeNameColumn.LookupObject = verifiedType.Type + "Type";
//                                        typeNameColumn.LookupObjectID = verifiedType.ID;
//                                    }

//                                    #endregion

//                                    #region Verify Subject Area

//                                    subjectAreaColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 3);
//                                    rawSubjectArea = subjectAreaColumn.Value.Trim().ToLower();
//                                    verifiedSubjectArea = subjectAreas.SingleOrDefault(i => i.Name == rawSubjectArea);
//                                    if (subjectAreaColumn != null)
//                                    {
//                                        subjectAreaColumn.LookupObject = "TaxonomyType";
//                                        subjectAreaColumn.LookupObjectID = verifiedSubjectArea.ID;
//                                    }

//                                    #endregion

//                                    #region Verify Item

//                                    itemColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 4);
//                                    rawItem = itemColumn.Value.Trim().ToLower();
//                                    LookupItem(company, itemColumn, rawItem, verifiedType, verifiedSubjectArea);

//                                    #endregion

//                                    #endregion

//                                    #region Look up source object info

//                                    #region Verify Type

//                                    typeColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 5);
//                                    rawType = typeColumn.Value.Trim().ToLower();
//                                    typeNameColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 6);
//                                    rawTypeName = typeNameColumn.Value.Trim().ToLower();
//                                    verifiedType = objectTypes.SingleOrDefault(i => i.Type == rawType && i.Name == rawTypeName);

//                                    if (verifiedType != null)
//                                    {
//                                        typeNameColumn.LookupObject = verifiedType.Type + "Type";
//                                        typeNameColumn.LookupObjectID = verifiedType.ID;
//                                    }

//                                    #endregion

//                                    #region Verify Subject Area

//                                    subjectAreaColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 7);
//                                    rawSubjectArea = subjectAreaColumn.Value.Trim().ToLower();
//                                    verifiedSubjectArea = subjectAreas.SingleOrDefault(i => i.Name == rawSubjectArea);
//                                    if (subjectAreaColumn != null)
//                                    {
//                                        subjectAreaColumn.LookupObject = "TaxonomyType";
//                                        subjectAreaColumn.LookupObjectID = verifiedSubjectArea.ID;
//                                    }

//                                    #endregion

//                                    #region Verify Item

//                                    itemColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 8);
//                                    rawItem = itemColumn.Value.Trim().ToLower();
//                                    LookupItem(company, itemColumn, rawItem, verifiedType, verifiedSubjectArea);

//                                    #endregion

//                                    #endregion

//                                    #region Look up target subject info

//                                    #region Verify Type

//                                    typeColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 9);
//                                    rawType = typeColumn.Value.Trim().ToLower();
//                                    typeNameColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 10);
//                                    rawTypeName = typeNameColumn.Value.Trim().ToLower();
//                                    verifiedType = objectTypes.SingleOrDefault(i => i.Type == rawType && i.Name == rawTypeName);

//                                    if (verifiedType != null)
//                                    {
//                                        typeNameColumn.LookupObject = verifiedType.Type + "Type";
//                                        typeNameColumn.LookupObjectID = verifiedType.ID;
//                                    }

//                                    #endregion

//                                    #region Verify Subject Area

//                                    subjectAreaColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 11);
//                                    rawSubjectArea = subjectAreaColumn.Value.Trim().ToLower();
//                                    verifiedSubjectArea = subjectAreas.SingleOrDefault(i => i.Name == rawSubjectArea);
//                                    if (subjectAreaColumn != null)
//                                    {
//                                        subjectAreaColumn.LookupObject = "TaxonomyType";
//                                        subjectAreaColumn.LookupObjectID = verifiedSubjectArea.ID;
//                                    }

//                                    #endregion

//                                    #region Verify Item

//                                    itemColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 12);
//                                    rawItem = itemColumn.Value.Trim().ToLower();
//                                    LookupItem(company, itemColumn, rawItem, verifiedType, verifiedSubjectArea);

//                                    #endregion

//                                    #endregion

//                                    #region Look up target object info

//                                    #region Verify Type

//                                    typeColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 13);
//                                    rawType = typeColumn.Value.Trim().ToLower();
//                                    typeNameColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 14);
//                                    rawTypeName = typeNameColumn.Value.Trim().ToLower();
//                                    verifiedType = objectTypes.SingleOrDefault(i => i.Type == rawType && i.Name == rawTypeName);

//                                    if (verifiedType != null)
//                                    {
//                                        typeNameColumn.LookupObject = verifiedType.Type + "Type";
//                                        typeNameColumn.LookupObjectID = verifiedType.ID;
//                                    }

//                                    #endregion

//                                    #region Verify Subject Area

//                                    subjectAreaColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 15);
//                                    rawSubjectArea = subjectAreaColumn.Value.Trim().ToLower();
//                                    verifiedSubjectArea = subjectAreas.SingleOrDefault(i => i.Name == rawSubjectArea);
//                                    if (subjectAreaColumn != null)
//                                    {
//                                        subjectAreaColumn.LookupObject = "TaxonomyType";
//                                        subjectAreaColumn.LookupObjectID = verifiedSubjectArea.ID;
//                                    }

//                                    #endregion

//                                    #region Verify Item

//                                    itemColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 16);
//                                    rawItem = itemColumn.Value.Trim().ToLower();
//                                    LookupItem(company, itemColumn, rawItem, verifiedType, verifiedSubjectArea);

//                                    #endregion

//                                    #endregion

//                                    #region Lookup up role

//                                    var roleColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 13);
//                                    var rawRole = roleColumn.Value.Trim().ToLower();
//                                    verifiedRole = roles.SingleOrDefault(i => i.Name == rawRole);

//                                    if (verifiedRole != null)
//                                    {
//                                        roleColumn.LookupObject ="IntersectRole";
//                                        roleColumn.LookupObjectID = verifiedRole.ID;
//                                    }

//                                    #endregion

//                                    var sourceSubject = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 4);
//                                    var sourceObject = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 8);
//                                    var targetSubject = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 12);
//                                    var targetObject = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 16);

//                                    var source = company.AddIntersect(sourceSubject.LookupObject, sourceSubject.LookupObjectID.Value, sourceObject.LookupObject, sourceObject.LookupObjectID.Value, IntersectClassification.Normal, null, null);
//                                    var target = company.AddIntersect(targetSubject.LookupObject, targetSubject.LookupObjectID.Value, targetObject.LookupObject, targetObject.LookupObjectID.Value, IntersectClassification.Normal, null, null);
//                                    var map = company.Filter<Map>(i =>
//                                        i.MapItems.Any(mi => mi.IntersectID == source.ID && mi.IsSource) &&
//                                        i.MapItems.Any(mi => mi.IntersectID == target.ID && !mi.IsSource),
//                                        i => i.MapItems
//                                        ).FirstOrDefault();

//                                    if (map == null)
//                                    {
//                                        map = new Map { IntersectRoleID = verifiedRole.ID, Transformation = "some transform", MapItems = new List<MapItem>() };
//                                        map.MapItems.Add(new MapItem { DiagramKey = "some arbitrary value S", IntersectID = source.ID, IsSource = true });
//                                        map.MapItems.Add(new MapItem { DiagramKey = "some arbitrary value T", IntersectID = target.ID, IsSource = false });
//                                        company.Add<Map>(map);
//                                    }

//                                    if (map != null)
//                                    {
//                                        loadItem.Status = "Success";
//                                    }
//                                }
//                            }
//                            else
//                            {
//                                #region Legacy stored procedure method

//                                bool writeStatus = true;
//                                var task = company.ObjectContext.Connection.ExecuteAsync(
//                                    "exec ProcessBulkLoad @LoadID", 
//                                    new { LoadID = load.ID }, 
//                                    null, 
//                                    10800
//                                );   // 180 minute timeout.

//                                task.ContinueWith(t =>
//                                {
//                                    if (t.IsCompleted)
//                                        Console.WriteLine("Bulk load procedure completed for Load ID {0}", q.LoadID);
//                                    if(t.IsFaulted)
//                                        Console.WriteLine("Bulk load procedure failed for Load ID {0}", q.LoadID);
//                                    if (t.Exception != null)
//                                    {
//                                        if (t.Exception.InnerExceptions != null)
//                                        {
//                                            mex.AddRange(t.Exception.InnerExceptions);
//                                        }
//                                    }
//                                    writeStatus = false;
//                                });

//                                while (writeStatus && (task.Exception == null))
//                                {
//                                    Console.WriteLine(".");
//                                    System.Threading.Thread.Sleep(45000);
//                                }

//                                #endregion
//                            }

//                            Console.WriteLine("Company: {0}. Finished executing ProcessBulkLoad procedure for Load {1}", companyID, load.ID);

//                            company.BulkLoadQueues.Remove(q);
//                            company.SaveChanges();
//                        }
//                        catch (Exception ex)
//                        {
//                            mex.Add(ex);
//                            q.NumberOfRetries++;
//                            q.HasError = true;
//                            q.ErrorMessage = ex.GetFullExceptionData();
//                            company.SaveChanges();
//                        }
//                    });

//                    company.Dispose();
//                });
//            }
//            catch (Exception ex)
//            {
//                var msg = ex.Message + ((ex.InnerException != null) ? "  " + ex.InnerException.Message : "");
//                Console.WriteLine(msg);
//            }

//            if (mex.Count > 0) throw new AggregateException("One or more exceptions occurred", mex);
            #endregion
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

            #endregion

            #region Create Load Items from Load file

            var load = company.GetById<Load>(loadInfo.LoadID, i => i.LoadColumns, i => i.LoadItems);

            var existingRows = load.LoadItems.Any();

            if (!existingRows)
            {
                var memoryStream = new MemoryStream(load.File);
                var xls = new SLDocument(memoryStream);

                var stats = xls.GetWorksheetStatistics();

                var numberOfRows = stats.NumberOfRows;
                var rowIndex = stats.StartRowIndex + 1;
                while (rowIndex <= stats.EndRowIndex)
                {

                    var loadItem = new LoadItem { LoadID = load.ID, RowIndex = rowIndex, LoadItemColumns = new List<LoadItemColumn>() };
                    company.LoadItems.Add(loadItem);
//company.Add<LoadItem>(loadItem);

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

                        company.LoadItemColumns.Add(
                            new LoadItemColumn { ColumnIndex = c.ColumnIndex, LoadID = load.ID, RowIndex = rowIndex, Value = loadValue }
                        );
                        //loadItem.LoadItemColumns.Add(new LoadItemColumn { ColumnIndex = c.ColumnIndex, LoadID = load.ID, RowIndex = rowIndex, Value = (isDate ? xls.GetCellValueAsDateTime(rowIndex, c.ColumnIndex).ToShortDateString() : xls.GetCellValueAsString(rowIndex, c.ColumnIndex)) });
                    }
                    //load.LoadItems.Add(loadItem); //company.LoadItems.Add(loadItem);

                    rowIndex++;
                }

                company.SaveChanges();  // Save all load items and columns we created.

                logger.WriteLine($"Created {load.LoadItems.Count} load item(s) for Company: {loadInfo.CompanyID}, Load: {loadInfo.LoadID}.");
            }
            #endregion

            

            if (load.Action == "N")
            {
                /*
                 * Source subject type	
                 * Source subject type name	
                 * Source subject subject area	
                 * Source subject	
                 * 
                 * Source object type	
                 * Source object type name	
                 * Source object subject area	
                 * Source object
                 * 
                 * Source Fusion Configuration
                 * Source Fusion Path
                 * 
                 * Target subject type	
                 * Target subject type name	
                 * Target subject subject area	
                 * Target subject	
                 * 
                 * Target object type	
                 * Target object type name	
                 * Target object subject area	
                 * Target object
                 * 
                 * Target Fusion Configuration
                 * Target Fusion Path
                 * 
                 * Transformation
                 * Role
                 */
                //"Artifact", "Domain", "Policy", "Rule", "Taxonomy"

                #region Get data to pre-populate

                var objectTypes = company.Query<IntersectTypeOption>(@"
select ID, ltrim(rtrim(lower(Name))) as Name, 'Artifact' as Type from ArtifactType
union
select ID, ltrim(rtrim(lower(Name))) as Name, 'Domain' as Type from DomainType
union
select ID, ltrim(rtrim(lower(Name))) as Name, 'Policy' as Type from PolicyType
union
select 1 as ID, 'informational' as Name, 'Rule' as Type
union
select 2 as ID, 'quality check' as Name, 'Rule'as Type
union
select 3 as ID, 'metric' as Name, 'Rule' as Type
union
select 4 as ID, 'profile' as Name, 'Rule' as Type
union
select ID, ltrim(rtrim(lower(Name))) as Name, 'Taxonomy' as Type from TaxonomyType").ToList();
                var roles = company.Table<IntersectRole>().Select(i => new SimpleTypeModel { Name = i.Name.ToLower(), ID = i.ID }).ToList();
                var subjectAreas = company.Table<TaxonomyType>().Select(i => new SimpleTypeModel { Name = i.Name.ToLower(), ID = i.ID }).ToList();
                var fusions = company.Table<Fusion>().Select(i => new SimpleTypeModel { Name = i.Name.ToLower(), ID = i.ID }).ToList();

                #endregion

                foreach (var loadItem in load.LoadItems)
                {
                    var rawType = "";
                    var rawTypeName = "";
                    var rawSubjectArea = "";
                    var rawItem = "";

                    LoadItemColumn typeColumn = null;
                    LoadItemColumn typeNameColumn = null;
                    LoadItemColumn subjectAreaColumn = null;
                    LoadItemColumn itemColumn = null;

                    IntersectTypeOption verifiedType = null;
                    SimpleTypeModel verifiedSubjectArea = null;
                    SimpleTypeModel verifiedRole = null;

                    SimpleTypeModel verifiedSourceFusionConfiguration = null;

                    SimpleTypeModel verifiedTargetFusionConfiguration = null;

                    #region Look up source subject info

                    #region Verify Type

                    typeColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 1);
                    rawType = typeColumn.Value.Trim().ToLower();
                    typeNameColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 2);
                    rawTypeName = typeNameColumn.Value.Trim().ToLower();
                    verifiedType = objectTypes.SingleOrDefault(i => i.Type.ToLower() == rawType && i.Name == rawTypeName);

                    if (verifiedType != null)
                    {
                        typeNameColumn.LookupObject = verifiedType.Type + "Type";
                        typeNameColumn.LookupObjectID = verifiedType.ID;
                    }

                    #endregion

                    #region Verify Subject Area

                    subjectAreaColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 3);
                    rawSubjectArea = subjectAreaColumn.Value.Trim().ToLower();
                    verifiedSubjectArea = subjectAreas.SingleOrDefault(i => i.Name == rawSubjectArea);
                    if (subjectAreaColumn != null)
                    {
                        subjectAreaColumn.LookupObject = "TaxonomyType";
                        subjectAreaColumn.LookupObjectID = verifiedSubjectArea.ID;
                    }

                    #endregion

                    #region Verify Item

                    itemColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 4);
                    rawItem = itemColumn.Value.Trim().ToLower();
                    LookupItem(company, itemColumn, rawItem, verifiedType, verifiedSubjectArea);

                    #endregion

                    #endregion

                    #region Look up source object info

                    #region Verify Type

                    typeColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 5);
                    rawType = typeColumn.Value.Trim().ToLower();
                    typeNameColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 6);
                    rawTypeName = typeNameColumn.Value.Trim().ToLower();
                    verifiedType = objectTypes.SingleOrDefault(i => i.Type.ToLower() == rawType && i.Name == rawTypeName);

                    if (verifiedType != null)
                    {
                        typeNameColumn.LookupObject = verifiedType.Type + "Type";
                        typeNameColumn.LookupObjectID = verifiedType.ID;
                    }

                    #endregion

                    #region Verify Subject Area

                    subjectAreaColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 7);
                    rawSubjectArea = subjectAreaColumn.Value.Trim().ToLower();
                    verifiedSubjectArea = subjectAreas.SingleOrDefault(i => i.Name == rawSubjectArea);
                    if (subjectAreaColumn != null)
                    {
                        subjectAreaColumn.LookupObject = "TaxonomyType";
                        subjectAreaColumn.LookupObjectID = verifiedSubjectArea.ID;
                    }

                    #endregion

                    #region Verify Item

                    itemColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 8);
                    rawItem = itemColumn.Value.Trim().ToLower();
                    LookupItem(company, itemColumn, rawItem, verifiedType, verifiedSubjectArea);

                    #endregion

                    #endregion

                    #region Lookup up source fusion configuration

                    var sourceFusionConfigurationColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 9);
                    var rawSourceFusionConfigurationColumn = (sourceFusionConfigurationColumn.Value + "").Trim().ToLower();
                    verifiedSourceFusionConfiguration = fusions.SingleOrDefault(i => i.Name == rawSourceFusionConfigurationColumn);

                    if (verifiedSourceFusionConfiguration != null)
                    {
                        sourceFusionConfigurationColumn.LookupObject = "Fusion";
                        sourceFusionConfigurationColumn.LookupObjectID = verifiedSourceFusionConfiguration.ID;
                    }

                    #endregion

                    #region Lookup up source fusion attribute

                    if (verifiedSourceFusionConfiguration != null)
                    {
                        var sourceFusionAttributeColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 10);
                        var rawSourceFusionAttributeColumn = (sourceFusionAttributeColumn.Value + "").Trim().ToLower();
                        var verifiedSourceFusionAttribute = company.Filter<FusionAttribute>(i => i.FusionID == verifiedSourceFusionConfiguration.ID && i.TextPath.ToLower() == rawSourceFusionAttributeColumn).FirstOrDefault();

                        if (verifiedSourceFusionAttribute != null)
                        {
                            sourceFusionAttributeColumn.LookupObject = "FusionAttribute";
                            sourceFusionAttributeColumn.LookupObjectID = verifiedSourceFusionAttribute.ID;
                        }
                    }

                    #endregion

                    #region Look up target subject info

                    #region Verify Type

                    typeColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 11);
                    rawType = typeColumn.Value.Trim().ToLower();
                    typeNameColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 12);
                    rawTypeName = typeNameColumn.Value.Trim().ToLower();
                    verifiedType = objectTypes.SingleOrDefault(i => i.Type.ToLower() == rawType && i.Name == rawTypeName);

                    if (verifiedType != null)
                    {
                        typeNameColumn.LookupObject = verifiedType.Type + "Type";
                        typeNameColumn.LookupObjectID = verifiedType.ID;
                    }

                    #endregion

                    #region Verify Subject Area

                    subjectAreaColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 13);
                    rawSubjectArea = subjectAreaColumn.Value.Trim().ToLower();
                    verifiedSubjectArea = subjectAreas.SingleOrDefault(i => i.Name == rawSubjectArea);
                    if (subjectAreaColumn != null)
                    {
                        subjectAreaColumn.LookupObject = "TaxonomyType";
                        subjectAreaColumn.LookupObjectID = verifiedSubjectArea.ID;
                    }

                    #endregion

                    #region Verify Item

                    itemColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 14);
                    rawItem = itemColumn.Value.Trim().ToLower();
                    LookupItem(company, itemColumn, rawItem, verifiedType, verifiedSubjectArea);

                    #endregion

                    #endregion

                    #region Look up target object info

                    #region Verify Type

                    typeColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 15);
                    rawType = typeColumn.Value.Trim().ToLower();
                    typeNameColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 16);
                    rawTypeName = typeNameColumn.Value.Trim().ToLower();
                    verifiedType = objectTypes.SingleOrDefault(i => i.Type.ToLower() == rawType && i.Name == rawTypeName);

                    if (verifiedType != null)
                    {
                        typeNameColumn.LookupObject = verifiedType.Type + "Type";
                        typeNameColumn.LookupObjectID = verifiedType.ID;
                    }

                    #endregion

                    #region Verify Subject Area

                    subjectAreaColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 17);
                    rawSubjectArea = subjectAreaColumn.Value.Trim().ToLower();
                    verifiedSubjectArea = subjectAreas.SingleOrDefault(i => i.Name == rawSubjectArea);
                    if (subjectAreaColumn != null)
                    {
                        subjectAreaColumn.LookupObject = "TaxonomyType";
                        subjectAreaColumn.LookupObjectID = verifiedSubjectArea.ID;
                    }

                    #endregion

                    #region Verify Item

                    itemColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 18);
                    rawItem = itemColumn.Value.Trim().ToLower();
                    LookupItem(company, itemColumn, rawItem, verifiedType, verifiedSubjectArea);

                    #endregion

                    #endregion

                    #region Lookup up source fusion configuration

                    var targetFusionConfigurationColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 19);
                    var rawTargetFusionConfigurationColumn = (targetFusionConfigurationColumn.Value + "").Trim().ToLower();
                    verifiedTargetFusionConfiguration = fusions.SingleOrDefault(i => i.Name == rawTargetFusionConfigurationColumn);

                    if (verifiedTargetFusionConfiguration != null)
                    {
                        targetFusionConfigurationColumn.LookupObject = "Fusion";
                        targetFusionConfigurationColumn.LookupObjectID = verifiedSourceFusionConfiguration.ID;
                    }

                    #endregion

                    #region Lookup up source fusion attribute

                    if (verifiedTargetFusionConfiguration != null)
                    {
                        var targetFusionAttributeColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 20);
                        var rawTargetFusionAttributeColumn = (targetFusionAttributeColumn.Value + "").Trim().ToLower();
                        var verifiedTargetFusionAttribute = company.Filter<FusionAttribute>(i => i.FusionID == verifiedTargetFusionConfiguration.ID && i.TextPath.ToLower() == rawTargetFusionAttributeColumn).FirstOrDefault();

                        if (verifiedTargetFusionAttribute != null)
                        {
                            targetFusionAttributeColumn.LookupObject = "FusionAttribute";
                            targetFusionAttributeColumn.LookupObjectID = verifiedTargetFusionAttribute.ID;
                        }
                    }

                    #endregion

                    #region Load transformation

                    var transformationColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 21);
                    var rawTransformation = transformationColumn.Value.Trim();

                    #endregion

                    #region Lookup up role

                    var roleColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 22);
                    var rawRole = roleColumn.Value.Trim().ToLower();
                    verifiedRole = roles.SingleOrDefault(i => i.Name == rawRole);

                    if (verifiedRole != null)
                    {
                        roleColumn.LookupObject = "IntersectRole";
                        roleColumn.LookupObjectID = verifiedRole.ID;
                    }

                    #endregion

                    var sourceSubject = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 4);
                    var sourceObject = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 8);
                    var targetSubject = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 14);
                    var targetObject = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 18);

                    var shouldContinue = true;

                    var source = company.AddIntersect(sourceSubject.LookupObject, sourceSubject.LookupObjectID.Value, sourceObject.LookupObject, sourceObject.LookupObjectID.Value, IntersectClassification.Normal, null, null);
                    var target = company.AddIntersect(targetSubject.LookupObject, targetSubject.LookupObjectID.Value, targetObject.LookupObject, targetObject.LookupObjectID.Value, IntersectClassification.Normal, null, null);

                    if (source == null)
                    {
                        shouldContinue = false;
                        loadItem.Status = false;
                        loadItem.StatusMessage += $" Could not create the source relationship.";
                    }

                    if (target == null)
                    {
                        shouldContinue = false;
                        loadItem.Status = false;
                        loadItem.StatusMessage += $" Could not create the target relationship.";
                    }

                    if (shouldContinue)
                    {
                        var map = company.Filter<Map>(i =>
                            i.MapItems.Any(mi => mi.IntersectID == source.ID && mi.IsSource) &&
                            i.MapItems.Any(mi => mi.IntersectID == target.ID && !mi.IsSource),
                            i => i.MapItems
                            ).FirstOrDefault();

                        if (map == null)
                        {
                            map = new Map { IntersectRoleID = verifiedRole.ID, Name = $"Map between {source.ID} and {target.ID}", Transformation = rawTransformation, MapItems = new List<MapItem>() };
                            map.MapItems.Add(new MapItem { DiagramKey = "some arbitrary value S", Object = "Intersect", ObjectID = source.ID, IntersectID = source.ID, IsSource = true });
                            map.MapItems.Add(new MapItem { DiagramKey = "some arbitrary value T", Object = "Intersect", ObjectID = target.ID, IntersectID = target.ID, IsSource = false });
                            company.Add<Map>(map);

                            loadItem.Status = true;
                            loadItem.StatusMessage = $"Map created.";
                        }
                        else
                        {
                            map.Transformation = rawTransformation;
                            company.Update<Map>(map);

                            loadItem.Status = true;
                            loadItem.StatusMessage = $"Map updated.";
                        }

                        //var sourceFusionConfiguration = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 9);
                        var sourceFusionAttribute = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 10);
                        //var targetFusionConfiguration = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 19);
                        var targetFusionAttribute = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == 20);
                        if (
                            !string.IsNullOrEmpty(sourceFusionAttribute.LookupObject) && sourceFusionAttribute.LookupObjectID.HasValue &&
                            !string.IsNullOrEmpty(targetFusionAttribute.LookupObject) && targetFusionAttribute.LookupObjectID.HasValue
                            )
                        {
                            var mapRule = company.Filter<MapRule>(i =>
                                i.MapRuleItems.Any(mi => mi.FusionAttributeID == sourceFusionAttribute.LookupObjectID.Value && mi.IsSource) &&
                                i.MapRuleItems.Any(mi => mi.FusionAttributeID == targetFusionAttribute.LookupObjectID.Value && !mi.IsSource),
                                i => i.MapRuleItems
                                ).FirstOrDefault();

                            if (mapRule == null)
                            {
                                mapRule = new MapRule { Name = $"", MapRuleItems = new List<MapRuleItem>() };
                                mapRule.MapRuleItems.Add(new MapRuleItem { FusionAttributeID = source.ID, IsSource = true });
                                mapRule.MapRuleItems.Add(new MapRuleItem { FusionAttributeID = target.ID, IsSource = false });
                                company.Add<MapRule>(mapRule);

                                loadItem.StatusMessage += $" Technical Map created.";
                            }
                            else
                            {
                                //map.Transformation = rawTransformation;
                                //company.Update<MapRule>(mapRule);

                                //loadItem.StatusMessage += $" Technical Map updated.";
                            }

                            if (map != null && mapRule != null)
                            {
                                var joinRecord = company.Query<dynamic>("select * from MapRuleMap where MapRuleID = @r and MapID = @m", new { r = mapRule.ID, m = map.ID }).FirstOrDefault();
                                if (joinRecord == null)
                                {
                                    company.Execute("insert into MapRuleMap values (@r, @m)", new { r = mapRule.ID, m = map.ID });
                                }
                            }
                        }
                    }
                }

                load.DateCompleted = DateTime.UtcNow;
                company.Update(load);
            }
            else
            {
                #region Legacy stored procedure method

                bool writeStatus = true;

                var connection = GetCompanyConnection(loadInfo.CompanyID);
                connection.Open();

                var task = connection.ExecuteAsync("exec ProcessBulkLoad @LoadID", new { LoadID = load.ID }, null, 10800);   // 180 minute timeout.

                task.ContinueWith(t =>
                {
                    logger.WriteLine("");
                    if (t.IsCompleted)
                    {
                        logger.WriteLine("Bulk load procedure completed for Load ID {0}", loadInfo.LoadID);
                        connection.Close();
                    }
                    if (t.IsFaulted)
                        logger.WriteLine("Bulk load procedure failed for Load ID {0}", loadInfo.LoadID);
                    if (t.Exception != null)
                    {
                        if (t.Exception.InnerExceptions != null)
                        {
                            foreach (var ex in t.Exception.InnerExceptions)
                            {
                                logger.WriteLine(ex.GetFullExceptionData());
                            }
                        }
                    }
                    writeStatus = false;
                });

                while (writeStatus && (task.Exception == null))
                {
                    logger.Write(".");
                    System.Threading.Thread.Sleep(45000);
                }

                #endregion
            }
        }

        static void LookupItem(CompanyContext company, LoadItemColumn itemColumn, string rawItem, IntersectTypeOption verifiedType, SimpleTypeModel verifiedSubjectArea)
        {
            if (verifiedType != null)
            {
                if (verifiedType.Type == "Artifact")
                {
                    if (verifiedSubjectArea != null)
                    {
                        var artifact = company.Filter<Artifact>(i => i.ArtifactTypeID == verifiedType.ID && i.TaxonomyTypeID == verifiedSubjectArea.ID && i.TextPath.ToLower() == rawItem).SingleOrDefault();
                        if (artifact != null)
                        {
                            itemColumn.LookupObject = verifiedType.Type;
                            itemColumn.LookupObjectID = artifact.ID;
                        }
                    }
                }
                else
                {
                    switch (verifiedType.Type)
                    {
                        case "Domain":
                            var domain = company.Filter<Domain>(i => i.DomainTypeID == verifiedType.ID && i.Name.ToLower() == rawItem).SingleOrDefault();
                            if (domain != null)
                            {
                                itemColumn.LookupObject = verifiedType.Type;
                                itemColumn.LookupObjectID = domain.ID;
                            }
                            break;
                        case "Rule":
                            var rule = company.Filter<Rule>(i => i.RuleType == (RuleType)verifiedType.ID && i.Name.ToLower() == rawItem).SingleOrDefault();
                            if (rule != null)
                            {
                                itemColumn.LookupObject = verifiedType.Type;
                                itemColumn.LookupObjectID = rule.ID;
                            }
                            break;
                        case "Policy":
                            var policy = company.Filter<Policy>(i => i.PolicyTypeID == verifiedType.ID && i.TextPath.ToLower() == rawItem).SingleOrDefault();
                            if (policy != null)
                            {
                                itemColumn.LookupObject = verifiedType.Type;
                                itemColumn.LookupObjectID = policy.ID;
                            }
                            break;
                        case "Taxonomy":
                            var taxonomy = company.Filter<Taxonomy>(i => i.TaxonomyTypeID == verifiedType.ID && i.TextPath.ToLower() == rawItem).SingleOrDefault();
                            if (taxonomy != null)
                            {
                                itemColumn.LookupObject = verifiedType.Type;
                                itemColumn.LookupObjectID = taxonomy.ID;
                            }
                            break;
                    }
                }
            }
        }
    }
}
