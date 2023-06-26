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
using DocumentFormat.OpenXml.ExtendedProperties;
using System.Data.SqlClient;

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
        DummyCachingProvider dummyCachingProvider;
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
            dummyCachingProvider = new DummyCachingProvider();

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
                queue,
                dummyCachingProvider,
                constants.COMMUNITY_DATABASE_CONNECTION);

            CommunityContext community = new CommunityContext(
                constants.COMMUNITY_DATABASE_CONNECTION,
                dummyCachingProvider,
                queue,
                new UriSecurityContextProvider
                {
                    CompanyID = Info.CompanyID,
                    ResourceID = Info.ResourceID ?? 0,
                    CompanyPrefix = Info.CompanyDomainPrefix,
                    IsAdministrator = false
                });

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
						string resultsSql = "";

						Action<string> markExecutionAsErred = (err) => {
							dbExecutionItem.ErrorMessage = err;
							dbExecutionItem.CompletedOn = DateTime.UtcNow;
							company.Update(dbExecutionItem);
						};

						Func<AssetType, Task> assetTypeActionLogic = null;

						Func<Guid, Task> assetTypeWrapperAction = async (uid) => {
							var type = company.Filter<AssetType>(i => i.uid == uid).SingleOrDefault();
							if (type != null)
							{
								await assetTypeActionLogic(type);
							}
							else
							{
								markExecutionAsErred($"Asset Type for uid [{uid}] not found.");
							}
						};

						Func<IntersectType, Task> intersectTypeActionLogic = null;

						Func<Guid, Task> intersectTypeWrapperAction = async (uid) => {
							var type = company.Filter<IntersectType>(i => i.uid == uid).SingleOrDefault();
							if (type != null)
							{
								await intersectTypeActionLogic(type);
							}
							else
							{
								markExecutionAsErred($"Intersect Type for uid [{uid}] not found.");
							}
						};

						switch (Info.Action)
                        {
							case ApiExecutionAction.PostAssets:
                                var postAssetsFields = JsonConvert.DeserializeObject<ApiExecutionFields_PostAssets>(dbExecutionItem.Fields);
								assetTypeActionLogic = async (at) =>
								{
									var postAssets = await storage.DeserializeJsonObjectFromBlobAsync<List<AssetInsert>>(Info.StorageFolder, Info.RequestFileName);
									company.ImportAssets(dbExecutionItem, at, postAssets, true, dbExecutionTimeout, Info.SendWorkflowEvents, mergeBlockSize: mergeBlockSize, sendGraphEvents: false, useTempTableForFields: (dbExecutionItem.Method == "BULK" ? false : true));
									resultsSql = @"select [ItemNumber], [uid], [ExecutionItemUid], [Message], [Success], IsNew from	api.ExecutionAsset where ExecutionID = @executionId order by ItemNumber asc";
								};
								await assetTypeWrapperAction(postAssetsFields.AssetTypeUid);
								break;
                            case ApiExecutionAction.PutAssets:
                                var putAssetsFields = JsonConvert.DeserializeObject<ApiExecutionFields_PutAssets>(dbExecutionItem.Fields);
								assetTypeActionLogic = async (at) =>
								{
									var putAssets = await storage.DeserializeJsonObjectFromBlobAsync<List<AssetUpdate>>(Info.StorageFolder, Info.RequestFileName);
									company.ImportAssets(dbExecutionItem, at, putAssets, false, dbExecutionTimeout, Info.SendWorkflowEvents, mergeBlockSize: mergeBlockSize, sendGraphEvents: false, useTempTableForFields: (dbExecutionItem.Method == "BULK" ? false : true));
									resultsSql = @"select [ItemNumber], [uid], [ExecutionItemUid], [Message], [Success], IsNew from	api.ExecutionAsset where ExecutionID = @executionId order by ItemNumber asc";
								};
								await assetTypeWrapperAction(putAssetsFields.AssetTypeUid);
								break;
                            case ApiExecutionAction.DeleteAssets:
                                var deleteAssetsFields = JsonConvert.DeserializeObject<ApiExecutionFields_DeleteAssets>(dbExecutionItem.Fields);
								assetTypeActionLogic = async (at) =>
								{
									var deleteAssets = await storage.DeserializeJsonObjectFromBlobAsync<AssetDeletes>(Info.StorageFolder, Info.RequestFileName);
									company.RemoveAssets(dbExecutionItem, at, deleteAssets, dbExecutionTimeout, Info.SendWorkflowEvents);
									resultsSql = @"select [ItemNumber], [uid], [ExecutionItemUid], [Message], [Success] from api.ExecutionDeletedAsset where ExecutionID = @executionId order by ItemNumber asc";
								};
								await assetTypeWrapperAction(deleteAssetsFields.AssetTypeUid);
								break;
                            case ApiExecutionAction.PostRelationships:
                                var postRelationshipsFields = JsonConvert.DeserializeObject<ApiExecutionFields_PostRelationships>(dbExecutionItem.Fields);
								intersectTypeActionLogic = async (it) =>
								{
									var postRelationships = await storage.DeserializeJsonObjectFromBlobAsync<RelationshipInserts>(Info.StorageFolder, Info.RequestFileName);
									company.ImportRelationships(dbExecutionItem, it, postRelationships, dbExecutionTimeout, Info.SendWorkflowEvents, false, false);
									resultsSql = @"select [ItemNumber], [uid], [ExecutionItemUid], [Message], [Success], IsNew from api.ExecutionRelationship where ExecutionID = @executionId order by ItemNumber asc";
								};
								await intersectTypeWrapperAction(postRelationshipsFields.IntersectTypeUid);
								break;
                            case ApiExecutionAction.PutRelationships:
                                var putRelationshipsFields = JsonConvert.DeserializeObject<ApiExecutionFields_PutRelationships>(dbExecutionItem.Fields);
								intersectTypeActionLogic = async (it) =>
								{
									var putRelationships = await storage.DeserializeJsonObjectFromBlobAsync<RelationshipUpdates>(Info.StorageFolder, Info.RequestFileName);
									company.PutRelationships(dbExecutionItem, it, putRelationships, dbExecutionTimeout, Info.SendWorkflowEvents, false, false);
									resultsSql = @"select [ItemNumber], [uid], [ExecutionItemUid], [Message], [Success], IsNew from api.ExecutionRelationship where ExecutionID = @executionId order by ItemNumber asc";
								};
								await intersectTypeWrapperAction(putRelationshipsFields.IntersectTypeUid);
								break;
                            case ApiExecutionAction.DeleteRelationships:
                                var deleteRelationshipsFields = JsonConvert.DeserializeObject<ApiExecutionFields_DeleteRelationships>(dbExecutionItem.Fields);
								intersectTypeActionLogic = async (it) =>
								{
									var deleteRelationships = await storage.DeserializeJsonObjectFromBlobAsync<RelationshipDeletes>(Info.StorageFolder, Info.RequestFileName);
									company.DeleteRelationships(dbExecutionItem, it, deleteRelationships, dbExecutionTimeout, Info.SendWorkflowEvents, false);
									resultsSql = @"select [ItemNumber], [uid], [ExecutionItemUid], [Message], [Success] from api.ExecutionDeletedRelationship where ExecutionID = @executionId order by ItemNumber asc";
								};
								await intersectTypeWrapperAction(deleteRelationshipsFields.IntersectTypeUid);
								break;
                            case ApiExecutionAction.DeleteAssetTypes:
                                var deleteAssetTypes = await storage.DeserializeJsonObjectFromBlobAsync<AssetTypeDeletes>(Info.StorageFolder, Info.RequestFileName);
                                company.RemoveAssetTypes(dbExecutionItem, deleteAssetTypes, 28800, false); //dbExecutionTimeout = 8 hours
                                company.CreateRollupPathChangedExecution();
								resultsSql = @"select [ItemNumber], [uid], [ExecutionItemUid], [Message], [Success] from api.ExecutionDeletedAssetType where ExecutionID = @executionId order by ItemNumber asc";
								break;
                            case ApiExecutionAction.PostCrossReferences:
                                var postCrossReferences = await storage.DeserializeJsonObjectFromBlobAsync<List<AssetCrossReference>>(Info.StorageFolder, Info.RequestFileName);
                                await company.ImportCrossReferencesAsync(dbExecutionItem, postCrossReferences, dbExecutionTimeout);
								resultsSql = @"select [ItemNumber], [uid], [Message], [Success] from api.ExecutionAssetCrossReference where ExecutionID = @executionId order by ItemNumber asc";
								break;
                            case ApiExecutionAction.PostDataQualityResults:
                                var postDataQualityResultsRequest = await storage.DeserializeJsonObjectFromBlobAsync<List<DataQualityInsertModel>>(Info.StorageFolder, Info.RequestFileName);

                                var postDataQualityResultsResponse = company.UpsertAssetResults(postDataQualityResultsRequest.ToList<IDataQualityUpsert>(), dbExecutionItem, dbExecutionTimeout, Info.SendWorkflowEvents);
                                postDataQualityResultsResponse.FindAll(x => x.Uid == null).ForEach(y => y.Uid = Guid.Empty);
								
								resultsSql = @"select [ItemNumber], [uid], [ExecutionItemUid], [Message], [Success] from api.ExecutionAssetResult where ExecutionID = @executionId order by ItemNumber asc";

								var ruleResultUids = postDataQualityResultsResponse.Where(i => i.Success).Select(i => i.Uid.Value).ToList();
                                if (ruleResultUids.Count > 0)
                                {
                                    var assetMeasures = company.GetAssetMeasuresFromRuleResults(ruleResultUids);
                                    company.CreateMeasureChangedResultExecution(assetMeasures);
                                }

                                break;
                            case ApiExecutionAction.PostDataProfile:
                                var postDataProfile = await storage.DeserializeJsonObjectFromBlobAsync<List<DataProfileUpsertModel>>(Info.StorageFolder, Info.RequestFileName);
								await company.UpsertDataProfilesAsync(postDataProfile, dbExecutionItem, true, dbExecutionTimeout);
								resultsSql = @"select [ItemNumber], AssetUid, [ExecutionItemUid], [Message], [Success] from api.ExecutionAssetDataProfile where ExecutionID = @executionId order by ItemNumber asc";
								break;
                            case ApiExecutionAction.PutDataProfile:
                                var putDataProfile = await storage.DeserializeJsonObjectFromBlobAsync<List<DataProfileUpsertModel>>(Info.StorageFolder, Info.RequestFileName);
								await company.UpsertDataProfilesAsync(putDataProfile, dbExecutionItem, false, dbExecutionTimeout);
								resultsSql = @"select [ItemNumber], AssetUid, [ExecutionItemUid], [Message], [Success] from api.ExecutionAssetDataProfile where ExecutionID = @executionId order by ItemNumber asc";
								break;
                            case ApiExecutionAction.DeleteDataProfile:
                                var deleteDataProfile = await storage.DeserializeJsonObjectFromBlobAsync<List<AssetDataProfileDeleteModel>>(Info.StorageFolder, Info.RequestFileName);
								await company.DeleteDataProfilesAsync(deleteDataProfile, dbExecutionItem, dbExecutionTimeout);
								resultsSql = @"select [ItemNumber], [ExecutionItemUid], AssetUid, StartDate, EndDate, [Cascade], [Message], [Success] from api.ExecutionDeleteAssetDataProfile where ExecutionID = @executionId order by ItemNumber asc";
								break;
                            case ApiExecutionAction.PostResponsibilityOverride:
                                var postResponsibilityOverride = await storage.DeserializeJsonObjectFromBlobAsync<List<BulkResponsibilityOverridePostModel>>(Info.StorageFolder, Info.RequestFileName);
                                await company.BulkInsertResponsibilityOverrideAsync(postResponsibilityOverride, dbExecutionItem, dbExecutionTimeout);
								resultsSql = @"select [ItemNumber], AssetUid, [ExecutionItemUid], [Message], [Success] from api.ExecutionResponsibilityTypeRelationOverrideItem where ExecutionID = @executionId order by ItemNumber asc";
								break;
                            case ApiExecutionAction.DeleteFieldTypes:
                                var deleteFieldtypes = JsonConvert.DeserializeObject<ApiExecutionFields_DeleteFieldtypes>(dbExecutionItem.Fields);

                                company.SetApiExecutionProcessingStartTime(dbExecutionItem.ExecutionID);
                                List<FieldType> currentFieldTypes = fieldsRepository.GetFieldTypes(deleteFieldtypes.TypeIdentifierInfo);
                                var result = fieldsRepository.DeleteFields(currentFieldTypes, deleteFieldtypes.FieldNamesToDelete);
                                dbExecutionItem.Processed = result;
								dbExecutionItem.CompletedOn = DateTime.UtcNow;
								company.Update(dbExecutionItem);
                                break;
                            case ApiExecutionAction.UpsertUsers:
                                UserUpsertModel model = await storage.DeserializeJsonObjectFromBlobAsync<UserUpsertModel>(Info.StorageFolder, Info.RequestFileName);
                                await membershipRepository.ProcessUpsertUsers(dbExecutionItem, model.Users, model.LookupFieldsPassedByValue, model.IsInsert, false).ConfigureAwait(false);
								resultsSql = @"select [ItemNumber], [uid], [ExecutionItemUid], [Message], [Success], IsNew from api.ExecutionUser where ExecutionID = @executionId order by ItemNumber asc";
								break;
							case ApiExecutionAction.PatchCatalog:
								var execRepo = new ExecutionsRepository(company, queue, storage);
								var patchCatalogPayload = await storage.DeserializeJsonObjectFromBlobAsync<PatchBulkCatalogRequestModel>(Info.StorageFolder, Info.RequestFileName);
								await execRepo.PatchCatalog(dbExecutionItem.Id, patchCatalogPayload);
								resultsSql = @"select iif([Type] = 'A', 'Asset', 'Relation') as [Type], TypeSourceId, SourceId, SubjectSourceId, ObjectSourceId, [Message], [Success], cast(iif([Action] = 'A', 1, 0) as bit) as IsNew from api.ExecutionCatalogItem where ExecutionId = @Id order by [Type] asc";
								break;
						}

						if (!string.IsNullOrEmpty(resultsSql))
						{
							var results = await company.Connection.QueryAsync<dynamic>(resultsSql, new { executionId = dbExecutionItem.ExecutionID, dbExecutionItem.Id }, commandTimeout: 540);
							await storage.SerializeJsonObjectToBlobAsync(Info.StorageFolder, Info.ResponseFileName, results);
						}
                    }

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
    }
}
