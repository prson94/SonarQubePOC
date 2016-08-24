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
using d360.core.exceptions;
using d360.workflow.models;
using d360.workflow;
using d360.workflow.entities;

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
            var isDev = (company.ObjectContext.Connection.DataSource.Contains("dev"));

            #endregion

            #region Create Load Items from Load file

            var load = company.Loads.Include("LoadColumns").Include("LoadItems.LoadItemColumns").SingleOrDefault(i => i.ID == loadInfo.LoadID);
            //var load = company.GetById<Load>(loadInfo.LoadID, 
            //    i => i.LoadColumns, 
            //    i => i.LoadItems);

            var existingRows = load.LoadItems.Any();

            if (!existingRows)
            {
                var memoryStream = new MemoryStream(load.File);
                var xls = new SLDocument(memoryStream);

                var stats = xls.GetWorksheetStatistics();

                var numberOfRows = stats.NumberOfRows;
                var rowIndex = stats.StartRowIndex + 1;
                var numberOfColumns = load.LoadColumns.Count;
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
                    }
                    rowIndex++;
                }

                company.SaveChanges();  // Save all load items and columns we created.

                logger.WriteLine($"Created {load.LoadItems.Count} load item(s) for Company: {loadInfo.CompanyID}, Load: {loadInfo.LoadID}.");
            }
            #endregion

            List<SimpleTypeModel> subjectAreas = null;

            if (load.Action == "O" || load.Action == "N")   //List loading
            {
                subjectAreas = company.Table<TaxonomyType>().Select(i => new SimpleTypeModel { Name = i.Name.ToLower(), ID = i.ID }).ToList();
            }

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

                List<SimpleTypeModel> types = null;
                switch (load.Object)
                {
                    case "ArtifactType":
                        types = company.Table<ArtifactType>().Select(i => new SimpleTypeModel { Name = i.Name.ToLower(), ID = i.ID }).ToList();
                        break;
                    case "DomainType":
                        types = company.Table<DomainType>().Select(i => new SimpleTypeModel { Name = i.Name.ToLower(), ID = i.ID }).ToList();
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
                    company.Table<Group>().OrderBy(x => x.Name).Select(x => new SimpleTypeModel { Name = "group:" + x.Name.ToLower(), ID = x.ID }).ToList());
                resources.AddRange(
                    company.Table<GlobalReportingResource>().ToList().Select(x => new SimpleTypeModel { Name = "user:" + x.FullName.ToLower(), ID = x.ResourceID })
                 );

                var responsibilities = company.Table<ResponsibilityType>().OrderBy(x => x.Name).Select(x => new SimpleTypeModel { Name = x.Name.ToLower(), ID = x.ID });

                var allocations = company.Table<ResponsibilityTypeRelation>().ToList();

                #endregion

                #region For

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
                                    .SingleOrDefault();
                                }
                                break;
                            case "DomainType":
                                verifiedItem = company.Filter<Domain>(x =>
                                    x.DomainTypeID == verifiedType.ID &&
                                    x.Name.ToLower() == rawItemPath
                                )
                                .Select(x => new SimpleTypeModel { Name = "Domain", ID = x.ID })
                                .SingleOrDefault();
                                break;
                            case "FusionType":
                                verifiedItem = company.Filter<Fusion>(x =>
                                    x.FusionTypeID == verifiedType.ID &&
                                    x.Name.ToLower() == rawItemPath
                                )
                                .Select(x => new SimpleTypeModel { Name = "Fusion", ID = x.ID })
                                .SingleOrDefault();
                                break;
                            case "PolicyType":
                                verifiedItem = company.Filter<Policy>(x =>
                                    x.PolicyTypeID == verifiedType.ID &&
                                    x.TextPath.ToLower() == rawItemPath
                                )
                                .Select(x => new SimpleTypeModel { Name = "Policy", ID = x.ID })
                                .SingleOrDefault();
                                break;
                            case "TaxonomyType":
                                verifiedItem = company.Filter<Taxonomy>(x =>
                                    x.TaxonomyTypeID == verifiedType.ID &&
                                    x.TextPath.ToLower() == rawItemPath
                                )
                                .Select(x => new SimpleTypeModel { Name = "Taxonomy", ID = x.ID })
                                .SingleOrDefault();
                                break;
                        }
                        if (verifiedItem != null)
                        {
                            itemColumn.LookupObject = verifiedItem.Name;
                            itemColumn.LookupObjectID = verifiedItem.ID;
                        }
                        currentColumnIndex++;

                        #endregion
                    }

                    #region Verify Responsibility

                    responsibilityColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == currentColumnIndex);
                    rawResponsibility = responsibilityColumn.Value.Trim().ToLower();
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
            else if (load.Action == "R" && isDev)    // Relation
            {
                #region
                /*
                 * Side 1
                 * Side 2
                 * 
                 * OR
                 * 
                 * Side 1 Subject Area
                 * Side 1
                 * Side 2 Subject Area
                 * Side 2
                 */
                #endregion

                #region Get data to pre-populate

                var relationIntersectTypeDetail = company.Filter<IntersectTypeDetail>(i => i.ID == load.ObjectID).FirstOrDefault();
                var relationSubjectAreas = company.Table<TaxonomyType>().Select(i => new SimpleTypeModel { ID = i.ID, Name = i.Name.ToLower() }).ToList();
                var subjectType = new IntersectTypeOption { ID = relationIntersectTypeDetail.SubjectID, Type = relationIntersectTypeDetail.Subject.Replace("Type", ""), Name = relationIntersectTypeDetail.SubjectName };
                var objectType = new IntersectTypeOption { ID = relationIntersectTypeDetail.ObjectID, Type = relationIntersectTypeDetail.Object.Replace("Type", ""), Name = relationIntersectTypeDetail.ObjectName };
                var cachedItems = new List<BulkLoadCacheEntryModel>();

                cachedItems.AddRange(cacheDataForType(subjectType, company));
                cachedItems.AddRange(cacheDataForType(objectType, company));

                #endregion

                #region SubjectArea column check logic

                var subjectCheckSubjectArea = (subjectType.Type == "Artifact");
                var subjectSubjectAreaColumnIndex = (subjectType.Type == "Artifact") ? 1 : 0;
                var subjectColumnIndex = (subjectType.Type == "Artifact") ? 2 : 1;

                var objectCheckSubjectArea = (objectType.Type == "Artifact");
                var objectSubjectAreaColumnIndex = 0;
                var objectColumnIndex = 0;

                if (subjectType.Type == "Artifact" && objectType.Type == "Artifact")
                {
                    objectSubjectAreaColumnIndex = 3;
                    objectColumnIndex = 4;
                }
                else if (subjectType.Type != "Artifact" && objectType.Type == "Artifact")
                {
                    objectSubjectAreaColumnIndex = 2;
                    objectColumnIndex = 3;
                }
                else if (subjectType.Type == "Artifact" && objectType.Type != "Artifact")
                {
                    objectSubjectAreaColumnIndex = 0;
                    objectColumnIndex = 3;
                }
                else // (subjectType.Type != "Artifact" && objectType.Type != "Artifact")
                {
                    objectSubjectAreaColumnIndex = 0;
                    objectColumnIndex = 2;
                }

                #endregion

                #region ForEach

                foreach (var loadItem in load.LoadItems)
                {
                    var rawSubjectArea = "";
                    var rawItem = "";

                    LoadItemColumn subjectSubjectAreaColumn = null;
                    LoadItemColumn subjectColumn = null;

                    LoadItemColumn objectSubjectAreaColumn = null;
                    LoadItemColumn objectColumn = null;

                    SimpleTypeModel verifiedSubjectArea = null;

                    #region Look up subject

                    #region Verify Subject Area

                    if (subjectCheckSubjectArea)
                    {
                        subjectSubjectAreaColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == subjectSubjectAreaColumnIndex);
                        rawSubjectArea = subjectSubjectAreaColumn.Value.Trim().ToLower();
                        verifiedSubjectArea = relationSubjectAreas.SingleOrDefault(i => i.Name == rawSubjectArea);
                        if (verifiedSubjectArea != null)
                        {
                            subjectSubjectAreaColumn.LookupObject = "TaxonomyType";
                            subjectSubjectAreaColumn.LookupObjectID = verifiedSubjectArea.ID;
                        }
                    }

                    #endregion

                    #region Verify Item

                    subjectColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == subjectColumnIndex);
                    rawItem = subjectColumn.Value.Trim().ToLower();

                    LookupItem(company, subjectColumn, rawItem, subjectType, verifiedSubjectArea);
                    //LookupCacheItem(company, subjectColumn, rawItem, subjectType, verifiedSubjectArea, cachedItems);

                    #endregion

                    #endregion

                    #region Look up object

                    #region Verify Subject Area

                    if (objectCheckSubjectArea)
                    {
                        objectSubjectAreaColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == objectSubjectAreaColumnIndex);
                        rawSubjectArea = objectSubjectAreaColumn.Value.Trim().ToLower();
                        verifiedSubjectArea = relationSubjectAreas.SingleOrDefault(i => i.Name == rawSubjectArea);
                        if (verifiedSubjectArea != null)
                        {
                            objectSubjectAreaColumn.LookupObject = "TaxonomyType";
                            objectSubjectAreaColumn.LookupObjectID = verifiedSubjectArea.ID;
                        }
                    }

                    #endregion

                    #region Verify Item

                    objectColumn = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == objectColumnIndex);
                    rawItem = objectColumn.Value.Trim().ToLower();

                    LookupItem(company, objectColumn, rawItem, objectType, verifiedSubjectArea);
                    //LookupCacheItem(company, objectColumn, rawItem, objectType, verifiedSubjectArea, cachedItems);

                    #endregion

                    #endregion

                    var subject = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == subjectColumnIndex);
                    var @object = loadItem.LoadItemColumns.Single(i => i.ColumnIndex == objectColumnIndex);

                    Intersect model = null;

                    if (!string.IsNullOrEmpty(subject.LookupObject) && subject.LookupObjectID.HasValue &&
                        !string.IsNullOrEmpty(@object.LookupObject) && @object.LookupObjectID.HasValue)
                    {
                        try
                        {
                            model = company.AddIntersect(subject.LookupObject, subject.LookupObjectID.Value, @object.LookupObject, @object.LookupObjectID.Value, IntersectClassification.Normal, null, null);
                            if (model != null)
                            {
                                loadItem.Status = true;
                                loadItem.StatusMessage = "Successfully created/updated relationship.";
                            }
                            else
                            {
                                loadItem.Status = false;
                                loadItem.StatusMessage = "Unable to create relationship.";
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
                        loadItem.StatusMessage += $" One of the sides of this relationships could not be resolved [Subject = {subject.Value}, Object = {@object.Value}].";
                    }

                    company.Update(loadItem);
                }

                #endregion

                load.DateCompleted = DateTime.UtcNow;
                company.Update(load);
            }
            else if (load.Action == "N")    // New Lineage
            {
                #region Lineage

                #region
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
                #endregion

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
                var fusions = company.Table<Fusion>().Select(i => new SimpleTypeModel { Name = i.Name.ToLower(), ID = i.ID }).ToList();

                #endregion

                #region ForEach

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
                        targetFusionConfigurationColumn.LookupObjectID = verifiedTargetFusionConfiguration.ID;
                    }

                    #endregion

                    #region Lookup up target fusion attribute

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

                    Intersect source = null;
                    Intersect target = null;

                    if (!string.IsNullOrEmpty(sourceSubject.LookupObject) && sourceSubject.LookupObjectID.HasValue && !string.IsNullOrEmpty(sourceObject.LookupObject) && sourceObject.LookupObjectID.HasValue)
                    {
                        try
                        {
                            source = company.AddIntersect(sourceSubject.LookupObject, sourceSubject.LookupObjectID.Value, sourceObject.LookupObject, sourceObject.LookupObjectID.Value, IntersectClassification.Normal, null, null);
                        }
                        catch (BaseException ex)
                        {
                            shouldContinue = false;
                            loadItem.Status = false;
                            loadItem.StatusMessage += " " + ex.StatusDescription;
                        }
                    }
                    else
                    {
                        shouldContinue = false;
                        loadItem.Status = false;
                        loadItem.StatusMessage += $" One of the sides of this relationships could not be resolved [Subject = {sourceSubject.Value}, Subject = {sourceObject.Value}].";
                    }
                    if (targetObject != null)
                    {
                        if (!string.IsNullOrEmpty(targetSubject.LookupObject) && targetSubject.LookupObjectID.HasValue && !string.IsNullOrEmpty(targetObject.LookupObject) && targetObject.LookupObjectID.HasValue)
                        {
                            try
                            {
                                target = company.AddIntersect(targetSubject.LookupObject, targetSubject.LookupObjectID.Value, targetObject.LookupObject, targetObject.LookupObjectID.Value, IntersectClassification.Normal, null, null);
                            }
                            catch (BaseException ex)
                            {
                                shouldContinue = false;
                                loadItem.Status = false;
                                loadItem.StatusMessage += " " + ex.StatusDescription;
                            }
                        }
                        else
                        {
                            shouldContinue = false;
                            loadItem.Status = false;
                            loadItem.StatusMessage += $" One of the sides of this relationships could not be resolved [Subject = {targetSubject.Value}, Subject = {targetObject.Value}].";
                        }
                    }

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

                    #region Continue processing map logic

                    if (shouldContinue)
                    {
                        var map = company.Filter<Map>(i =>
                            i.MapItems.Any(mi => mi.SourceIntersectID == source.ID && mi.TargetIntersectID == target.ID),
                            i => i.MapItems
                            ).FirstOrDefault();

                        if (map == null)
                        {
                            map = new Map { Transformation = rawTransformation, MapItems = new List<MapItem>() };
                            if (verifiedRole != null)
                                map.IntersectRoleID = verifiedRole.ID;

                            map.MapItems.Add(new MapItem { SourceIntersectID = source.ID, TargetIntersectID = target.ID });
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
                            // Create child relationships.
                            Intersect childSourceRelation = null;
                            Intersect childTargetRelation = null;
                            MapRule mapRule = null;

                            try
                            {
                                childSourceRelation = company.AddIntersect("Intersect", source.ID, "FusionAttribute", sourceFusionAttribute.LookupObjectID.Value, IntersectClassification.Normal, null, null);
                            }
                            catch (BaseException ex)
                            {
                                loadItem.StatusMessage += " " + ex.StatusDescription;
                            }

                            try
                            {
                                childTargetRelation = company.AddIntersect("Intersect", target.ID, "FusionAttribute", targetFusionAttribute.LookupObjectID.Value, IntersectClassification.Normal, null, null);
                            }
                            catch (BaseException ex)
                            {
                                loadItem.StatusMessage += " " + ex.StatusDescription;
                            }

                            if (childSourceRelation != null && childTargetRelation != null)
                            {
                                mapRule = company.Filter<MapRule>(i =>
                                    i.MapRuleItems.Any(mi => mi.SourceFusionAttributeID == sourceFusionAttribute.LookupObjectID.Value && mi.TargetFusionAttributeID == targetFusionAttribute.LookupObjectID.Value),
                                    i => i.MapRuleItems
                                ).FirstOrDefault();

                                if (mapRule == null)
                                {
                                    mapRule = new MapRule { MapRuleItems = new List<MapRuleItem>() };
                                    mapRule.MapRuleItems.Add(new MapRuleItem { SourceFusionAttributeID = sourceFusionAttribute.LookupObjectID.Value, TargetFusionAttributeID = targetFusionAttribute.LookupObjectID.Value });
                                    company.Add<MapRule>(mapRule);

                                    loadItem.StatusMessage += $" Technical Map created.";
                                }
                                else
                                {
                                    //map.Transformation = rawTransformation;
                                    //company.Update<MapRule>(mapRule);

                                    //loadItem.StatusMessage += $" Technical Map updated.";
                                }
                            }
                            else
                            {
                                loadItem.Status = false;
                                loadItem.StatusMessage += $"Technical relationship could not be created or updated.";
                            }

                            if (map != null && mapRule != null)
                            {
                                var mapItem = map.MapItems.Single(i => i.SourceIntersectID == source.ID && i.TargetIntersectID == target.ID);
                                var mapRuleItem = mapRule.MapRuleItems.Single(i => i.SourceFusionAttributeID == sourceFusionAttribute.LookupObjectID.Value && i.TargetFusionAttributeID == targetFusionAttribute.LookupObjectID.Value);

                                var joinRecord = company.Filter<MapRuleItemMapItem>(i => i.MapItemID == mapItem.ID && i.MapRuleItemID == mapRuleItem.ID).SingleOrDefault();
                                if (joinRecord == null)
                                {
                                    joinRecord = new MapRuleItemMapItem { MapItemID = mapItem.ID, MapRuleItemID = mapRuleItem.ID };
                                    company.Add(joinRecord);
                                }
                            }
                        }
                    }

                    #endregion

                    company.Update(loadItem);
                }

                #endregion

                load.DateCompleted = DateTime.UtcNow;
                company.Update(load);

                #endregion
            }
            else if (load.Action == "T")    // Technical Fusion
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
                #region Get data to pre-populate

                var proposalSubjectAreas = company.Table<TaxonomyType>().Select(i => new SimpleTypeModel { ID = i.ID, Name = i.Name.ToLower() }).ToList();

                #endregion

                #region ForEach

                var artifactType = company.GetById<ArtifactType>(load.ObjectID);
                var processor = new Processor();
                var wtrItems = company.Filter<WorkflowTypeRelation>(i => i.Object == "ArtifactType" && i.ObjectID == load.ObjectID && i.Enabled && (i.WorkflowType == WorkflowType.SuggestNewArtifact || i.WorkflowType == WorkflowType.SuggestNewArtifactMulti)).ToList();

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

                            var fields = company.GetFieldTypeRelationsByObject(SystemObjects.ArtifactType, load.ObjectID).OrderBy(i => i.SortOrder).ToList();

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
                        case "Intersect":
                            var intersect = company.Filter<Intersect>(i => i.IntersectTypeID == verifiedType.ID && i.Name.ToLower() == rawItem).SingleOrDefault();
                            if (intersect != null)
                            {
                                itemColumn.LookupObject = verifiedType.Type;
                                itemColumn.LookupObjectID = intersect.ID;
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

        static void LookupCacheItem(CompanyContext company, LoadItemColumn itemColumn, string rawItem, IntersectTypeOption verifiedType, SimpleTypeModel verifiedSubjectArea, List<BulkLoadCacheEntryModel> cache)
        {
            BulkLoadCacheEntryModel cacheModel = null;

            if (verifiedType != null)
            {
                if (verifiedType.Type == "Artifact")
                {
                    if (verifiedSubjectArea != null)
                    {
                        cacheModel = cache.SingleOrDefault(i => i.TypeID == verifiedType.ID && i.GroupID == verifiedSubjectArea.ID && i.Name == rawItem);
                        if (cacheModel != null)
                        {
                            itemColumn.LookupObject = cacheModel.Object;
                            itemColumn.LookupObjectID = cacheModel.ObjectID;
                        }
                    }
                }
                else
                {
                    switch (verifiedType.Type)
                    {
                        case "Intersect":
                            var intersect = company.Filter<Intersect>(i => i.IntersectTypeID == verifiedType.ID && i.Name.ToLower() == rawItem).SingleOrDefault();
                            if (intersect != null)
                            {
                                itemColumn.LookupObject = verifiedType.Type;
                                itemColumn.LookupObjectID = intersect.ID;
                            }
                            break;
                        default:
                            cacheModel = cache.SingleOrDefault(i => i.TypeID == verifiedType.ID && i.Name == rawItem);
                            if (cacheModel != null)
                            {
                                itemColumn.LookupObject = cacheModel.Object;
                                itemColumn.LookupObjectID = cacheModel.ObjectID;
                            }
                            break;
                    }

                }
            }
        }

        static IQueryable<BulkLoadCacheEntryModel> cacheDataForType(IntersectTypeOption option, CompanyContext company)
        {
            switch (option.Type)
            {
                case "Artifact":
                    return company.Filter<Artifact>(i => i.ArtifactTypeID == option.ID).Select(i => new BulkLoadCacheEntryModel { Object = "Artifact", ObjectID = i.ID, GroupID = i.TaxonomyTypeID, Name = i.TextPath.ToLower(), TypeID = i.ArtifactTypeID });
                case "Domain":
                    return company.Filter<Domain>(i => i.DomainTypeID == option.ID).Select(i => new BulkLoadCacheEntryModel { Object = "Domain", ObjectID = i.ID, Name = i.Name.ToLower(), TypeID = i.DomainTypeID });
                case "Policy":
                    return company.Filter<Policy>(i => i.PolicyTypeID == option.ID).Select(i => new BulkLoadCacheEntryModel { Object = "Policy", ObjectID = i.ID, Name = i.TextPath.ToLower(), TypeID = i.PolicyTypeID });
                case "Rule":
                    return company.Filter<Rule>(i => (int)i.RuleType == option.ID).Select(i => new BulkLoadCacheEntryModel { Object = "Rule", ObjectID = i.ID, Name = i.Name.ToLower(), TypeID = (int)i.RuleType });
                case "Taxonomy":
                    return company.Filter<Taxonomy>(i => i.TaxonomyTypeID == option.ID).Select(i => new BulkLoadCacheEntryModel { Object = "Taxonomy", ObjectID = i.ID, Name = i.TextPath.ToLower(), TypeID = i.TaxonomyTypeID });
                default:
                    return null;
            }
        }
    }
}
