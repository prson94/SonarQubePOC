using d360.core.entities;
using d360.core.queue;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.queue;
using d360.model;
using Dapper;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using d360.extensions.storage;
using System.Text;
using d360.core;
using d360.core.entities.Metric;

using System.Threading;
using System.Net.Http;
using Microsoft.Extensions.Hosting;
using d360.model.DataAccessLayer;
using d360.core.entities.Membership;
using d360.extensions;
using System.Configuration;
using d360.extensions.mail;

namespace igx.jobs.apiexecutionprocessor
{
    class Program
    {
        static async Task Main()
        {
            var builder = CoreFunction.JobHostConfigBuilder();
            builder.ConfigureWebJobs(c =>
            {
                c.AddAzureStorageCoreServices();
                c.AddAzureStorage(s =>
                {
                    s.VisibilityTimeout = TimeSpan.FromHours(6);
                    s.BatchSize = 2;
                });
            });

            using (var host = builder.Build())
            {
                    await host.RunAsync();
            }
        }
    }

    public class ApiExecutionProcessor
    {
        //#if DEBUG
        //public static async Task Run([TimerTrigger("0 0 */5 * * *", RunOnStartup = true)]TimerInfo myTimer, CancellationToken token, TextWriter log)
        //#else
        public async static Task Run([QueueTrigger("%ApiExecutionQueue%", Connection = "QueueStorageAccount")] string myQueueItem, TextWriter log)
        //#endif
        {
            ApiExecutionInfo info = null;
            /*#if DEBUG
                        info = new ApiExecutionInfo
                        {
                            Action = ApiExecutionAction.PostAssets,
                            CompanyDomainPrefix = "mpappas.eng",
                            CompanyID = 2, 
                            ResourceID = 3,
                            ExecutionID = new Guid("d04067cc-18e4-44d9-a817-c13dfbc6c6a7")
                        };
            #else*/
            info = JsonConvert.DeserializeObject<ApiExecutionInfo>(myQueueItem);
            //#endif

            //Should this job be allowed to run?
            var job = new ApiJobProcessor();
            await job.Run(info, log);
        }
    }


    public class ApiJobProcessor
    {
        const string functionName = "ApiExecution_Process";

        const int DEFAULT_MERGE_BLOCK_SIZE = 500;
        const int DEFAULT_SQL_BULK_COPY_BLOCK_SIZE = 5000;
        const int DEFAULT_SQL_BULK_COPY_TIMEOUT = 0;
        const int DEFAULT_WORKFLOW_BATCH_SIZE = 50;
        AzureQueueSource queue;
        private CompanyContext company;
        AzureStorageProvider storage;
        ApiExecutionInfo Info;

        public async Task Run(ApiExecutionInfo info, TextWriter log)
        {
            CoreFunction.AITrackJobStart(functionName);
            CoreFunction.AITrackEvent(functionName, $"Starting Batch ExecutionID:{(info != null ? info.ExecutionID.ToString() : "unknown execution id")}");

            Info = info;

            #region Create EF connection

            //An instance of this class is a thread safe because it's a wrapper for ServiceBusClient which is thread safe.
            //We don't need to have more than one of these.
            queue = new AzureQueueSource(); 
            storage = new AzureStorageProvider();

            company = JobDbContextCreator.CreateCompanyContext(
                new UriSecurityContextProvider
                {
                    CompanyID = Info.CompanyID,
                    ResourceID = Info.ResourceID ?? 0,
                    CompanyPrefix = Info.CompanyDomainPrefix,
                    IsAdministrator = false
                },
                new MandrillMailProvider
                {
                    ApiKey = ConfigurationManager.AppSettings[constants.MAIL_API_KEY],
                    SubAccount = ConfigurationManager.AppSettings[constants.MAIL_SUB_ACCOUNT]
                },
                queue);
            CommunityContext community = JobDbContextCreator.CreateCommunityContext(
                new UriSecurityContextProvider
                {
                    CompanyID = Info.CompanyID,
                    ResourceID = Info.ResourceID ?? 0,
                    CompanyPrefix = Info.CompanyDomainPrefix,
                    IsAdministrator = false
                },
                queue);

            company.AssetsPartiallyProcessed += Company_AssetsPartiallyProcessed;
            company.RelationshipsPartiallyProcessed += Company_RelationshipsPartiallyProcessed;
            var resource = company.GlobalReportingResources.FirstOrDefault(x => x.ResourceID == company.CurrentResourceID);
            if (resource != null)
            {
                company.CurrentResourceIsAdmin = resource.IsAdministrator;
            }

            FieldsRepository fieldsRepository = new FieldsRepository(company, queue, storage);
            AssetRepository assetRepository = new AssetRepository(company, queue, storage, community);
            MembershipRepository membershipRepository = new MembershipRepository(company, community, assetRepository, queue, storage); 

            #endregion

            var dbExecutionItem = company.Filter<ApiExecution>(i => i.ExecutionID == Info.ExecutionID).SingleOrDefault();


            //wait a moment in case there are multiple queue messages
            Thread.Sleep(new Random().Next(2000));

            try
            {
                bool jobAlreadyRunning = false;

                // jobs with a error message a retrying make them wait in line like the other batch jobs otherwise what happens is > 2 batch jobs start running 
                // at the same time filling all the batch slots causing people to say why is my job stuck in line. 
                if ( ( dbExecutionItem != null) && dbExecutionItem.ProcessingStartedOn.HasValue && string.IsNullOrEmpty(dbExecutionItem.ErrorMessage))
                    jobAlreadyRunning = true;


                //mark this execution for processing
                if (dbExecutionItem != null)
                {
                    dbExecutionItem.MarkedForProcessing = true;
                    company.Update(dbExecutionItem);
                }

                //check if this client should / can run an api load if the job already started and we are resuming it let it through without applying the should run api check
                if (!jobAlreadyRunning && !(await ShouldRunApiJob(company, dbExecutionItem?.ExecutionID)))
                {
                    int delaySeconds = int.Parse(CoreFunction.GetConfigValueByKey("RunningJobDelay") ?? "30");
                    TimeSpan delay = new TimeSpan(0, 0, delaySeconds);


                    if (dbExecutionItem != null)
                    {
                        dbExecutionItem.MarkedForProcessing = false;
                        company.Update(dbExecutionItem);
                    }


                    await queue.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), info, delay);

                    return;
                }


                if (dbExecutionItem != null)
                {

                    AssetType assetType = null;
                    IntersectType intersectType = null;
                    int mergeBlockSize = DEFAULT_MERGE_BLOCK_SIZE;
                    
                    int dbExecutionTimeout = int.Parse(CoreFunction.GetConfigValueByKey("DBExecuteQueryTimeout"));

                    if (int.TryParse(CoreFunction.GetConfigValueByKey("V2ApiBatchMergeBlockSize"), out int tempBlockSize))
                    {
                        mergeBlockSize = tempBlockSize > 0 ? tempBlockSize : DEFAULT_MERGE_BLOCK_SIZE;
                    }

                    if (int.TryParse(CoreFunction.GetConfigValueByKey("V2ApiBatchSqlBatchSize"), out int tempsqlBulkCopyBlockSize))
                    {
                        company.SqlBulkBatchSize = tempsqlBulkCopyBlockSize >= 0 ? tempsqlBulkCopyBlockSize : DEFAULT_SQL_BULK_COPY_BLOCK_SIZE;
                    }

                    if (int.TryParse(CoreFunction.GetConfigValueByKey("V2ApiBatchSqlBulkTimeout"), out int tempsqlBulkCopyTimeout))
                    {
                        company.SqlBulkBatchTimeout = tempsqlBulkCopyTimeout >= 0 ? tempsqlBulkCopyTimeout : DEFAULT_SQL_BULK_COPY_TIMEOUT;
                    }

                    if (int.TryParse(CoreFunction.GetConfigValueByKey("V2ApiBatchWorkflowBatchSize"), out int tempWorkflowBatchSize))
                    {
                        company.WorkflowSendBatchSize = tempWorkflowBatchSize >= 0 ? tempWorkflowBatchSize : DEFAULT_WORKFLOW_BATCH_SIZE;
                    }


                    bool executeJob = true;


                    if (dbExecutionItem.State == d360.core.enums.State.Deleted)
                    {
                        executeJob = false;
                        log.WriteLine($"Execution job with UID {dbExecutionItem.ExecutionID} was canceled by user.");
                    }

                    dbExecutionItem.MarkedForProcessing = executeJob;
                    company.Update(dbExecutionItem);

                    if (executeJob)
                    {
                        switch (Info.Action)
                        {
                            case ApiExecutionAction.PostAssets:
                                #region
                                var postAssetsFields = JsonConvert.DeserializeObject<ApiExecutionFields_PostAssets>(dbExecutionItem.Fields);
                                assetType = company.Filter<AssetType>(i => i.uid == postAssetsFields.AssetTypeUid).SingleOrDefault();
                                
                                if (assetType != null)
                                {
                                    List<AssetInsert> postAssets = await storage.DeserializeJsonObjectFromBlobAsync<List<AssetInsert>>(Info.StorageFolder, Info.RequestFileName);

                                    log.WriteLine($"POST Assets (DB Start): Total raw assets: {postAssets.Count}. Asset Type Uid: {postAssetsFields.AssetTypeUid}. Timeout: {dbExecutionTimeout}. Merge Block Size: {mergeBlockSize}.");
                                    var postAssetsResults = company.ImportAssets(dbExecutionItem, assetType, postAssets, true, dbExecutionTimeout, Info.SendWorkflowEvents, mergeBlockSize: mergeBlockSize, sendGraphEvents: false, useTempTableForFields: (dbExecutionItem.Method == "BULK" ? false : true));
                                    dbExecutionItem.Processed = postAssetsResults.Count(i => i.Success);
                                    dbExecutionItem.Error = postAssetsResults.Count(i => !i.Success);
                                    log.WriteLine($"POST Assets (DB Complete): Total results: {postAssetsResults.Count}.");

                                    await SaveResultsJsonToAzure(postAssetsResults, log, "Assets", HttpMethod.Post);

                                    company.SendApiGraphEvent(Info);
                                }
                                else
                                {
                                    dbExecutionItem.ErrorMessage = $"Asset Type for uid [{postAssetsFields.AssetTypeUid}] not found.";
                                }

                                break;
                            #endregion
                            case ApiExecutionAction.PutAssets:
                                #region
                                var putAssetsFields = JsonConvert.DeserializeObject<ApiExecutionFields_PutAssets>(dbExecutionItem.Fields);
                                assetType = company.Filter<AssetType>(i => i.uid == putAssetsFields.AssetTypeUid).SingleOrDefault();

                                if (assetType != null)
                                {
                                    var putAssets = await storage.DeserializeJsonObjectFromBlobAsync<List<AssetUpdate>>(Info.StorageFolder, Info.RequestFileName);

                                    log.WriteLine($"PUT Assets (DB Start): Total raw assets: {putAssets.Count}. Asset Type Uid: {putAssetsFields.AssetTypeUid}. Timeout: {dbExecutionTimeout}. Merge Block Size: {mergeBlockSize}.");
                                    var putAssetsResults = company.ImportAssets(dbExecutionItem, assetType, putAssets, false, dbExecutionTimeout, Info.SendWorkflowEvents, mergeBlockSize: mergeBlockSize, sendGraphEvents: false, useTempTableForFields: (dbExecutionItem.Method == "BULK" ? false:true));
                                    dbExecutionItem.Processed = putAssetsResults.Count(i => i.Success);
                                    dbExecutionItem.Error = putAssetsResults.Count(i => !i.Success);
                                    log.WriteLine($"PUT Assets (DB Complete): Total results: {putAssetsResults.Count}.");

                                    await SaveResultsJsonToAzure(putAssetsResults, log, "Assets", HttpMethod.Put);

                                    company.SendApiGraphEvent(Info);
                                }
                                else
                                {
                                    dbExecutionItem.ErrorMessage = $"Asset Type for uid [{putAssetsFields.AssetTypeUid}] not found.";
                                }

                                break;
                            #endregion
                            case ApiExecutionAction.DeleteAssets:
                                #region
                                var deleteAssetsFields = JsonConvert.DeserializeObject<ApiExecutionFields_DeleteAssets>(dbExecutionItem.Fields);
                                assetType = company.Filter<AssetType>(i => i.uid == deleteAssetsFields.AssetTypeUid).SingleOrDefault();

                                if (assetType != null)
                                {
                                    var deleteAssets = await storage.DeserializeJsonObjectFromBlobAsync<AssetDeletes>(Info.StorageFolder, Info.RequestFileName);

                                    log.WriteLine($"DELETE Assets (DB Start): Total raw assets: {deleteAssets.Count}. Asset Type Uid: {deleteAssetsFields.AssetTypeUid}.");
                                    var deleteAssetsResults = company.RemoveAssets(dbExecutionItem, assetType, deleteAssets, dbExecutionTimeout, Info.SendWorkflowEvents);
                                    dbExecutionItem.Processed = deleteAssetsResults.Count(i => i.Success);
                                    dbExecutionItem.Error = deleteAssetsResults.Count(i => !i.Success);
                                    log.WriteLine($"DELETE Assets (DB Complete): Total results: {deleteAssetsResults.Count}.");

                                    await SaveResultsJsonToAzure(deleteAssetsResults, log, "Assets", HttpMethod.Delete);

                                    company.SendApiGraphEvent(Info);
                                }
                                else
                                {
                                    dbExecutionItem.ErrorMessage = $"Asset Type for uid [{deleteAssetsFields.AssetTypeUid}] not found.";
                                }

                                break;
                            #endregion
                            case ApiExecutionAction.PostRelationships:
                                #region
                                var postRelationshipsFields = JsonConvert.DeserializeObject<ApiExecutionFields_PostRelationships>(dbExecutionItem.Fields);
                                intersectType = company.Filter<IntersectType>(i => i.uid == postRelationshipsFields.IntersectTypeUid).SingleOrDefault();
                                
                                if (intersectType != null)
                                {
                                    var postRelationships = await storage.DeserializeJsonObjectFromBlobAsync<RelationshipInserts>(Info.StorageFolder, Info.RequestFileName);

                                    log.WriteLine($"POST Relationships (DB Start): Total raw assets: {postRelationships.Count}. Intersect Type Uid: {postRelationshipsFields.IntersectTypeUid}.");
                                    var postRelationshipsResults = company.ImportRelationships(dbExecutionItem, intersectType, postRelationships, dbExecutionTimeout, Info.SendWorkflowEvents, false, false);
                                    dbExecutionItem.Processed = postRelationshipsResults.Count(i => i.Success);
                                    dbExecutionItem.Error = postRelationshipsResults.Count(i => !i.Success);
                                    log.WriteLine($"POST Relationships (DB Complete): Total results: {postRelationshipsResults.Count}.");

                                    await SaveResultsJsonToAzure(postRelationshipsResults, log, "Relationships", HttpMethod.Post);
                                    company.SendApiGraphEvent(Info);
                                }
                                else
                                {
                                    dbExecutionItem.ErrorMessage = $"Intersect Type for uid [{postRelationshipsFields.IntersectTypeUid}] not found.";
                                }

                                break;
                            #endregion
                            case ApiExecutionAction.PutRelationships:
                                #region
                                var putRelationshipsFields = JsonConvert.DeserializeObject<ApiExecutionFields_PutRelationships>(dbExecutionItem.Fields);
                                intersectType = company.Filter<IntersectType>(i => i.uid == putRelationshipsFields.IntersectTypeUid).SingleOrDefault();

                                if (intersectType != null)
                                {
                                    var putRelationships = await storage.DeserializeJsonObjectFromBlobAsync<RelationshipUpdates>(Info.StorageFolder, Info.RequestFileName);

                                    log.WriteLine($"PUT Relationships (DB Start): Total raw assets: {putRelationships.Count}. Intersect Type Uid: {putRelationshipsFields.IntersectTypeUid}.");
                                    var putRelationshipsResults = company.PutRelationships(dbExecutionItem, intersectType, putRelationships, dbExecutionTimeout, Info.SendWorkflowEvents, false, false);
                                    dbExecutionItem.Processed = putRelationshipsResults.Count(i => i.Success);
                                    dbExecutionItem.Error = putRelationshipsResults.Count(i => !i.Success);
                                    log.WriteLine($"PUT Relationships (DB Complete): Total results: {putRelationshipsResults.Count}.");

                                    await SaveResultsJsonToAzure(putRelationshipsResults, log, "Relationships", HttpMethod.Put).ConfigureAwait(false);
                                    company.SendApiGraphEvent(Info);
                                }
                                else
                                {
                                    dbExecutionItem.ErrorMessage = $"Intersect Type for uid [{putRelationshipsFields.IntersectTypeUid}] not found.";
                                }

                                break;
                            #endregion
                            case ApiExecutionAction.DeleteRelationships:
                                #region
                                var deleteRelationshipsFields = JsonConvert.DeserializeObject<ApiExecutionFields_DeleteRelationships>(dbExecutionItem.Fields);
                                intersectType = company.Filter<IntersectType>(i => i.uid == deleteRelationshipsFields.IntersectTypeUid).SingleOrDefault();
                                
                                if (intersectType != null)
                                {
                                    var deleteRelationships = await storage.DeserializeJsonObjectFromBlobAsync<RelationshipDeletes>(Info.StorageFolder, Info.RequestFileName);

                                    log.WriteLine($"DELETE Relationships (DB Start): Total raw assets: {deleteRelationships.Count}. Intersect Type Uid: {deleteRelationshipsFields.IntersectTypeUid}.");
                                    var deleteRelationshipsResults = company.DeleteRelationships(dbExecutionItem, intersectType, deleteRelationships, dbExecutionTimeout, Info.SendWorkflowEvents, false);
                                    dbExecutionItem.Processed = deleteRelationshipsResults.Count(i => i.Success);
                                    dbExecutionItem.Error = deleteRelationshipsResults.Count(i => !i.Success);
                                    log.WriteLine($"DELETE Relationships (DB Complete): Total results: {deleteRelationshipsResults.Count}.");

                                    await SaveResultsJsonToAzure(deleteRelationshipsResults, log, "Relationships", HttpMethod.Delete);
                                    company.SendApiGraphEvent(Info);
                                }
                                else
                                {
                                    dbExecutionItem.ErrorMessage = $"Intersect Type for uid [{deleteRelationshipsFields.IntersectTypeUid}] not found.";
                                }

                                break;
                            #endregion
                            case ApiExecutionAction.DeleteAssetTypes:
                                #region
                                var deleteAssetTypes = await storage.DeserializeJsonObjectFromBlobAsync<AssetTypeDeletes>(Info.StorageFolder, Info.RequestFileName);

                                log.WriteLine($"DELETE Asset Types (DB Start): Total raw assets: {deleteAssetTypes.Count}.");
                                var deleteAssetTypesResults = company.RemoveAssetTypes(dbExecutionItem, deleteAssetTypes, 28800); //dbExecutionTimeout = 8 hours
                                dbExecutionItem.Processed = deleteAssetTypesResults.Count(i => i.Success);
                                dbExecutionItem.Error = deleteAssetTypesResults.Count(i => !i.Success);
                                log.WriteLine($"DELETE Asset Types (DB Complete): Total results: {deleteAssetTypesResults.Count}.");

                                company.CreateRollupPathChangedExecution();

                                await SaveResultsJsonToAzure(deleteAssetTypesResults, log, "Asset Types", HttpMethod.Delete);

                                break;
                            #endregion
                            case ApiExecutionAction.PostCrossReferences:
                                var postCrossReferences = await storage.DeserializeJsonObjectFromBlobAsync<List<AssetCrossReference>>(Info.StorageFolder, Info.RequestFileName);

                                log.WriteLine($"POST Cross References (DB Start): Total raw Cross References: {postCrossReferences.Count}");
                                var postCrossReferenceResult = company.ImportCrossReferences(dbExecutionItem, postCrossReferences, dbExecutionTimeout);
                                dbExecutionItem.Processed = postCrossReferenceResult.Count(i => i.Success);
                                dbExecutionItem.Error = postCrossReferenceResult.Count(i => !i.Success);
                                log.WriteLine($"POST Cross References (DB Complete): Total Processed: {dbExecutionItem.Processed}.");
                                log.WriteLine($"POST Cross References (DB Complete): Total Error: {dbExecutionItem.Error}.");

                                await SaveResultsJsonToAzure(postCrossReferenceResult, log, "Cross References", HttpMethod.Post);
                                                                
                                break;
                            case ApiExecutionAction.PostDataQualityResults:
                                #region Process DataQualityResults

                                var postDataQualityResultsRequest = await storage.DeserializeJsonObjectFromBlobAsync<List<DataQualityInsertModel>>(Info.StorageFolder, Info.RequestFileName);

                                log.WriteLine($"POST DataQualityResults (DB Start): Total raw Data Quality Results: {postDataQualityResultsRequest.Count}. Timeout: {dbExecutionTimeout}. Merge Block Size: {mergeBlockSize}.");
                                var postDataQualityResultsResponse = company.UpsertAssetResults(postDataQualityResultsRequest.ToList<IDataQualityUpsert>(), dbExecutionItem, dbExecutionTimeout, Info.SendWorkflowEvents);
                                postDataQualityResultsResponse.FindAll(x => x.Uid == null).ForEach(y => y.Uid = Guid.Empty);
                                dbExecutionItem.Processed = postDataQualityResultsResponse.Count(i => i.Success);
                                dbExecutionItem.Error = postDataQualityResultsResponse.Count(i => !i.Success);
                                log.WriteLine($"POST DataQualityResults (DB Complete): Total results: {postDataQualityResultsResponse.Count}.");
                                
                                await SaveResultsJsonToAzure(postDataQualityResultsResponse, log, "DataQualityResults", HttpMethod.Post);

                                var ruleResultUids = postDataQualityResultsResponse.Where(i => i.Success).Select(i => i.Uid.Value).ToList();
                                if (ruleResultUids.Count > 0)
                                {
                                    var assetMeasures = company.GetAssetMeasuresFromRuleResults(ruleResultUids);
                                    company.CreateMeasureChangedResultExecution(assetMeasures);
                                }

                                #endregion
                                break;
                            case ApiExecutionAction.PostDataProfile:
                                var postDataProfile = await storage.DeserializeJsonObjectFromBlobAsync<List<DataProfileUpsertModel>>(Info.StorageFolder, Info.RequestFileName);

                                log.WriteLine($"POST Asset Data Profile (DB Start): Total raw Data Profile Records: {postDataProfile.Count}");
                                var postDataProfileResult = company.UpsertDataProfiles(postDataProfile, dbExecutionItem, true, dbExecutionTimeout);
                                dbExecutionItem.Processed = postDataProfileResult.Count(i => i.Success);
                                dbExecutionItem.Error = postDataProfileResult.Count(i => !i.Success);
                                log.WriteLine($"POST Asset Data Profile (DB Complete): Total Processed: {dbExecutionItem.Processed}.");
                                log.WriteLine($"POST Asset Data Profile (DB Complete): Total Error: {dbExecutionItem.Error}.");

                                await SaveResultsJsonToAzure(postDataProfileResult, log, "Asset Data Profile", HttpMethod.Post).ConfigureAwait(false);
                                break;
                            case ApiExecutionAction.PutDataProfile:
                                var putDataProfile = await storage.DeserializeJsonObjectFromBlobAsync<List<DataProfileUpsertModel>>(Info.StorageFolder, Info.RequestFileName);

                                log.WriteLine($"PUT Asset Data Profile (DB Start): Total raw Data Profile Records: {putDataProfile.Count}");
                                var putDataProfileResult = company.UpsertDataProfiles(putDataProfile, dbExecutionItem, false, dbExecutionTimeout);
                                dbExecutionItem.Processed = putDataProfileResult.Count(i => i.Success);
                                dbExecutionItem.Error = putDataProfileResult.Count(i => !i.Success);
                                log.WriteLine($"PUT Asset Data Profile (DB Complete): Total Processed: {dbExecutionItem.Processed}.");
                                log.WriteLine($"PUT Asset Data Profile (DB Complete): Total Error: {dbExecutionItem.Error}.");

                                await SaveResultsJsonToAzure(putDataProfileResult, log, "Asset Data Profile", HttpMethod.Put).ConfigureAwait(false);
                                break;
                            case ApiExecutionAction.DeleteDataProfile:
                                var deleteDataProfile = await storage.DeserializeJsonObjectFromBlobAsync<List<AssetDataProfileDeleteModel>>(Info.StorageFolder, Info.RequestFileName);

                                log.WriteLine($"DELETE Asset Data Profile (DB Start): Total raw Data Profile Records: {deleteDataProfile.Count}");
                                var deleteDataProfileResult = company.DeleteDataProfiles(deleteDataProfile, dbExecutionItem, dbExecutionTimeout);
                                dbExecutionItem.Processed = deleteDataProfileResult.Count(i => i.Success);
                                dbExecutionItem.Error = deleteDataProfileResult.Count(i => !i.Success);
                                log.WriteLine($"DELETE Asset Data Profile (DB Complete): Total Processed: {dbExecutionItem.Processed}.");
                                log.WriteLine($"DELETE Asset Data Profile (DB Complete): Total Error: {dbExecutionItem.Error}.");

                                await SaveResultsJsonToAzure(deleteDataProfileResult, log, "Asset Data Profile", HttpMethod.Delete).ConfigureAwait(false);
                                break;
                            case ApiExecutionAction.PostResponsibilityOverride:
                                var postResponsibilityOverride = await storage.DeserializeJsonObjectFromBlobAsync<List<BulkResponsibilityOverridePostModel>>(Info.StorageFolder, Info.RequestFileName);

                                log.WriteLine($"POST Responsibility Override (DB Start): Total raw Data Profile Records: {postResponsibilityOverride.Count}");
                                var postResponsibilityOverrideResult = company.BulkInsertResponsibilityOverride(postResponsibilityOverride, dbExecutionItem, dbExecutionTimeout);
                                dbExecutionItem.Processed = postResponsibilityOverrideResult.Count(i => i.Success);
                                dbExecutionItem.Error = postResponsibilityOverrideResult.Count(i => !i.Success);
                                log.WriteLine($"POST Responsibility Override (DB Complete): Total Processed: {dbExecutionItem.Processed}.");
                                log.WriteLine($"POST Responsibility Override (DB Complete): Total Error: {dbExecutionItem.Error}.");

                                await SaveResultsJsonToAzure(postResponsibilityOverrideResult, log, "Responsibility Override", HttpMethod.Post).ConfigureAwait(false);
                                break;
                            case ApiExecutionAction.DeleteFieldTypes:
                                var deleteFieldtypes = JsonConvert.DeserializeObject<ApiExecutionFields_DeleteFieldtypes>(dbExecutionItem.Fields);

                                company.SetApiExecutionProcessingStartTime(dbExecutionItem.ExecutionID);
                                log.WriteLine($"DELETE Field Type (DB Start): Total field types: {deleteFieldtypes.FieldNamesToDelete.Count}");
                                List<FieldType> currentFieldTypes = fieldsRepository.GetFieldTypes(deleteFieldtypes.TypeIdentifierInfo);
                                var result = fieldsRepository.DeleteFields(currentFieldTypes, deleteFieldtypes.FieldNamesToDelete);
                                dbExecutionItem.Processed = result;
                                log.WriteLine($"DELETE Field Type (DB Complete): Total Processed: {dbExecutionItem.Processed}.");
                                break;
                            case ApiExecutionAction.UpsertUsers:
                                UserUpsertModel model = await storage.DeserializeJsonObjectFromBlobAsync<UserUpsertModel>(Info.StorageFolder, Info.RequestFileName);
                                string operation = model.IsInsert ? "POST" : "PUT";

                                log.WriteLine($"{operation} Users (DB Start): Total users: {model.Users.Count()}");

                                var userResult = await membershipRepository.ProcessUpsertUsers(dbExecutionItem, model.Users, model.LookupFieldsPassedByValue, model.IsInsert, false).ConfigureAwait(false);
                                dbExecutionItem.Processed = userResult.Count(i => i.Success);
                                dbExecutionItem.Error = userResult.Count(i => !i.Success);
                                log.WriteLine($"{operation} Users (DB Complete): Total Processed: {dbExecutionItem.Processed}.");
                                log.WriteLine($"{operation} Users (DB Complete): Total Error: {dbExecutionItem.Error}.");
                                break;
                        }
                    }
                    dbExecutionItem.CompletedOn = DateTime.UtcNow;
                    company.Update(dbExecutionItem);

                    CoreFunction.AITrackJobCompletedNoErrors(functionName);
                }
                else
                {
                    // this is the case where the batch job has been started however no record can be found in the api execution table for the execution id.  Log it
                    CoreFunction.AITrackEvent(functionName, $"Cannot find [api].[execution] record for batch ExecutionID:{(info != null ? info.ExecutionID.ToString() : "unknown execution id")}");
                }
            }
            catch (Exception ex)
            {
                log.WriteLine($"{ex.GetFullExceptionData()}");
                CoreFunction.AITrackException(functionName, ex, Info.CompanyID, new Dictionary<string, string>() {
                    { "ExecutionID", Info.ExecutionID.ToString() },
                    { "StorageFolder", Info.StorageFolder },
                    { "RequestFileName", Info.RequestFileName },
                    { "ResponseFileName", Info.ResponseFileName }
                });
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                dbExecutionItem.ErrorMessage = message;
                dbExecutionItem.CompletedOn = DateTime.UtcNow;
                dbExecutionItem.MarkedForProcessing = false;

                try
                {
                    company.Update(dbExecutionItem);
                }
                catch (Exception cex)
                {
                    log.WriteLine($"{cex.GetFullExceptionData()}");
                }
            }
        }

        /// <summary>
        /// Saves results to azure storage streams json object to storage as a stream.  Logs name to provided textwritter
        /// </summary>        
        private async Task SaveResultsJsonToAzure(object resultsJson, TextWriter log, string operationName, HttpMethod method)
        {
            log.WriteLine($"{method} {operationName} (Response Storage Start): Storage folder: {Info.StorageFolder}. Response File: {Info.ResponseFileName}.");

            await storage.SerializeJsonObjectToBlobAsync(Info.StorageFolder, Info.ResponseFileName, resultsJson);

            log.WriteLine($"{method} {operationName} (Response Storage Complete): Storage folder: {Info.StorageFolder}. Response File: {Info.ResponseFileName}.");
        }


        /// <summary>
        /// Checks if the current company should permit a new api job to start
        /// </summary>
        /// <param name="company"></param>
        /// <param name="executionID"></param>
        /// <returns></returns>
        private async Task<bool> ShouldRunApiJob(CompanyContext company, Guid? executionID)
        {
            // call function in db to see if the api job should run
            return await company.Database.Connection.QueryFirstOrDefaultAsync<bool>("select api.ShouldAllowNewBatchCall( @executionID)", new { executionID }, commandTimeout:300);
        }

        private async void Company_AssetsPartiallyProcessed(object sender, AssetsPartiallyProcessedEventArgs e)
        {
            await storage.SerializeJsonObjectToBlobAsync(Info.StorageFolder, Info.ResponseFileName, e.Results);
        }

        private async void Company_RelationshipsPartiallyProcessed(object sender, RelationshipsPartiallyProcessedEventArgs e)
        {
            await storage.SerializeJsonObjectToBlobAsync(Info.StorageFolder, Info.ResponseFileName, e.Results);
        }
    }
}
