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
using d360.extensions.storage;
using System.Text;
using System.Threading;
using d360.core;

namespace igx.jobs.apiexecutionprocessor
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();
#if DEBUG
            config.UseTimers();
            config.UseDevelopmentSettings();
#endif
            config.Queues.BatchSize = 2;
            config.Queues.VisibilityTimeout = TimeSpan.FromHours(6);

            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    public class ApiExecutionProcessor
    {
//#if DEBUG
        //public static async Task Run([TimerTrigger("0 0 */5 * * *", RunOnStartup = true)]TimerInfo myTimer, CancellationToken token, TextWriter log)
//#else
        public async static Task Run([QueueTrigger("%ApiExecutionQueue%"), StorageAccount("QueueStorageAccount")] string myQueueItem, TextWriter log)
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

        AzureQueueSource queue;
        CommunityContext community;
        CompanyContext company;
        AzureStorageProvider storage;
        ApiExecutionInfo Info;

        public async Task Run(ApiExecutionInfo info, TextWriter log)
        {
            Info = info;

            #region Create EF connection

            var sec = new UriSecurityContextProvider
            {
                CompanyID = Info.CompanyID,
                ResourceID = Info.ResourceID ?? 0,
                CompanyPrefix = Info.CompanyDomainPrefix,
                IsAdministrator = true
            };
            var cache = new DummyCachingProvider();
            queue = new AzureQueueSource();
            community = new CommunityContext(cache, queue, sec);
            company = new CompanyContext(community, cache, queue, sec, true);
            storage = new AzureStorageProvider();

            company.AssetsPartiallyProcessed += Company_AssetsPartiallyProcessed;
            company.RelationshipsPartiallyProcessed += Company_RelationshipsPartiallyProcessed;

            #endregion

            var dbExecutionItem = company.Filter<ApiExecution>(i => i.ExecutionID == Info.ExecutionID).SingleOrDefault();

            try
            {
                //check if this client should / can run an api load
                if (!(await ShouldRunApiJob(company)))
                {
                    int delaySeconds = int.Parse(CoreFunction.GetConfigValueByKey("RunningJobDelay")??"300");

                    TimeSpan delay = new TimeSpan(0, 0, delaySeconds);

                    await queue.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), info, delay);

                    return;
                }


                if (dbExecutionItem != null)
                {
                    AssetType assetType = null;
                    IntersectType intersectType = null;

                    int dbExecutionTimeout = int.Parse(CoreFunction.GetConfigValueByKey("DBExecuteQueryTimeout"));

                    bool fieldJsonPropertyLoadLimitToTopLevel = true;
                    try
                    {
                        fieldJsonPropertyLoadLimitToTopLevel = bool.Parse(community.GetCompanySettings().Single(i => i.Key == "FieldJsonPropertyLoadLimitToTopLevel").Value);
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, Info.CompanyID, new Dictionary<string, string>() {
                            { "ExecutionID", Info.ExecutionID.ToString() },
                            { "StorageFolder", Info.StorageFolder },
                            { "RequestFileName", Info.RequestFileName },
                            { "ResponseFileName", Info.ResponseFileName }
                        });
                    }
                    
                    switch (Info.Action)
                    {
                        case ApiExecutionAction.PostAssets:
                            #region
                            var postAssetsFields = JsonConvert.DeserializeObject<ApiExecutionFields_PostAssets>(dbExecutionItem.Fields);
                            assetType = company.Filter<AssetType>(i => i.uid == postAssetsFields.AssetTypeUid).Single();
                            string postAssetsJson = storage.GetFileContentsAsString(Info.StorageFolder, Info.RequestFileName, Encoding.UTF8);
                            var postAssets = JsonConvert.DeserializeObject<List<AssetInsert>>(postAssetsJson);

                            log.WriteLine($"POST Assets (DB Start): Total raw assets: {postAssets.Count}. Asset Type Uid: {postAssetsFields.AssetTypeUid}.");
                            var postAssetsResults = company.ImportAssets(dbExecutionItem, assetType, postAssets, true, dbExecutionTimeout, fieldJsonPropertyLoadLimitToTopLevel, Info.SendWorkflowEvents);
                            dbExecutionItem.Processed = postAssetsResults.Count(i => i.Success);
                            dbExecutionItem.Error = postAssetsResults.Count(i => !i.Success);
                            log.WriteLine($"POST Assets (DB Complete): Total results: {postAssetsResults.Count}.");

                            log.WriteLine($"POST Assets (Response Storage Start): Storage folder: {Info.StorageFolder}. Response File: {Info.ResponseFileName}.");
                            storage.CreateFile(Info.StorageFolder, Info.ResponseFileName, JsonConvert.SerializeObject(postAssetsResults));
                            log.WriteLine($"POST Assets (Response Storage Complete): Storage folder: {Info.StorageFolder}. Response File: {Info.ResponseFileName}.");
                            break;
                            #endregion
                        case ApiExecutionAction.PutAssets:
                            #region
                            var putAssetsFields = JsonConvert.DeserializeObject<ApiExecutionFields_PutAssets>(dbExecutionItem.Fields);
                            assetType = company.Filter<AssetType>(i => i.uid == putAssetsFields.AssetTypeUid).Single();
                            string putAssetsJson = storage.GetFileContentsAsString(Info.StorageFolder, Info.RequestFileName, Encoding.UTF8);
                            var putAssets = JsonConvert.DeserializeObject<List<AssetUpdate>>(putAssetsJson);

                            log.WriteLine($"PUT Assets (DB Start): Total raw assets: {putAssets.Count}. Asset Type Uid: {putAssetsFields.AssetTypeUid}.");
                            var putAssetsResults = company.ImportAssets(dbExecutionItem, assetType, putAssets, false, dbExecutionTimeout, fieldJsonPropertyLoadLimitToTopLevel, Info.SendWorkflowEvents);
                            dbExecutionItem.Processed = putAssetsResults.Count(i => i.Success);
                            dbExecutionItem.Error = putAssetsResults.Count(i => !i.Success);
                            log.WriteLine($"PUT Assets (DB Complete): Total results: {putAssetsResults.Count}.");

                            log.WriteLine($"PUT Assets (Response Storage Start): Storage folder: {Info.StorageFolder}. Response File: {Info.ResponseFileName}.");
                            storage.CreateFile(Info.StorageFolder, Info.ResponseFileName, JsonConvert.SerializeObject(putAssetsResults));
                            log.WriteLine($"PUT Assets (Response Storage Complete): Storage folder: {Info.StorageFolder}. Response File: {Info.ResponseFileName}.");
                            break;
                            #endregion
                        case ApiExecutionAction.DeleteAssets:
                            #region
                            var deleteAssetsFields = JsonConvert.DeserializeObject<ApiExecutionFields_DeleteAssets>(dbExecutionItem.Fields);
                            assetType = company.Filter<AssetType>(i => i.uid == deleteAssetsFields.AssetTypeUid).Single();

                            string deleteAssetsJson = storage.GetFileContentsAsString(Info.StorageFolder, Info.RequestFileName, Encoding.UTF8);
                            var deleteAssets = JsonConvert.DeserializeObject<AssetDeletes>(deleteAssetsJson);

                            log.WriteLine($"DELETE Assets (DB Start): Total raw assets: {deleteAssets.Count}. Asset Type Uid: {deleteAssetsFields.AssetTypeUid}.");
                            var deleteAssetsResults = company.RemoveAssets(dbExecutionItem, assetType, deleteAssets, dbExecutionTimeout);
                            dbExecutionItem.Processed = deleteAssetsResults.Count(i => i.Success);
                            dbExecutionItem.Error = deleteAssetsResults.Count(i => !i.Success);
                            log.WriteLine($"DELETE Assets (DB Complete): Total results: {deleteAssetsResults.Count}.");

                            log.WriteLine($"DELETE Assets (Response Storage Start): Storage folder: {Info.StorageFolder}. Response File: {Info.ResponseFileName}.");
                            storage.CreateFile(Info.StorageFolder, Info.ResponseFileName, JsonConvert.SerializeObject(deleteAssetsResults));
                            log.WriteLine($"DELETE Assets (Response Storage Complete): Storage folder: {Info.StorageFolder}. Response File: {Info.ResponseFileName}.");
                            break;
                            #endregion
                        case ApiExecutionAction.PostRelationships:
                            #region
                            var postRelationshipsFields = JsonConvert.DeserializeObject<ApiExecutionFields_PostRelationships>(dbExecutionItem.Fields);
                            intersectType = company.Filter<IntersectType>(i => i.uid == postRelationshipsFields.IntersectTypeUid).Single();
                            string postRelationshipsJson = storage.GetFileContentsAsString(Info.StorageFolder, Info.RequestFileName, Encoding.UTF8);
                            var postRelationships = JsonConvert.DeserializeObject<RelationshipInserts>(postRelationshipsJson);

                            log.WriteLine($"POST Relationships (DB Start): Total raw assets: {postRelationships.Count}. Intersect Type Uid: {postRelationshipsFields.IntersectTypeUid}.");
                            var postRelationshipsResults = company.ImportRelationships(dbExecutionItem, intersectType, postRelationships, dbExecutionTimeout, Info.SendWorkflowEvents);
                            dbExecutionItem.Processed = postRelationshipsResults.Count(i => i.Success);
                            dbExecutionItem.Error = postRelationshipsResults.Count(i => !i.Success);
                            log.WriteLine($"POST Relationships (DB Complete): Total results: {postRelationshipsResults.Count}.");

                            log.WriteLine($"POST Relationships (Response Storage Start): Storage folder: {Info.StorageFolder}. Response File: {Info.ResponseFileName}.");
                            storage.CreateFile(Info.StorageFolder, Info.ResponseFileName, JsonConvert.SerializeObject(postRelationshipsResults));
                            log.WriteLine($"POST Relationships (Response Storage Complete): Storage folder: {Info.StorageFolder}. Response File: {Info.ResponseFileName}.");
                            break;
                        #endregion
                        case ApiExecutionAction.DeleteAssetTypes:
                            #region
                            var deleteAssetTypesFields = JsonConvert.DeserializeObject<ApiExecutionFields_DeleteAssetTypes>(dbExecutionItem.Fields);

                            string deleteAssetTypesJson = storage.GetFileContentsAsString(Info.StorageFolder, Info.RequestFileName, Encoding.UTF8);
                            var deleteAssetTypes = JsonConvert.DeserializeObject<AssetTypeDeletes>(deleteAssetTypesJson);

                            log.WriteLine($"DELETE Asset Types (DB Start): Total raw assets: {deleteAssetTypes.Count}.");
                            var deleteAssetTypesResults = company.RemoveAssetTypes(dbExecutionItem, deleteAssetTypes, 28800); //dbExecutionTimeout = 8 hours
                            dbExecutionItem.Processed = deleteAssetTypesResults.Count(i => i.Success);
                            dbExecutionItem.Error = deleteAssetTypesResults.Count(i => !i.Success);
                            log.WriteLine($"DELETE Asset Types (DB Complete): Total results: {deleteAssetTypesResults.Count}.");

                            log.WriteLine($"DELETE Asset Types (Response Storage Start): Storage folder: {Info.StorageFolder}. Response File: {Info.ResponseFileName}.");
                            storage.CreateFile(Info.StorageFolder, Info.ResponseFileName, JsonConvert.SerializeObject(deleteAssetTypesResults));
                            log.WriteLine($"DELETE Asset Types (Response Storage Complete): Storage folder: {Info.StorageFolder}. Response File: {Info.ResponseFileName}.");
                            break;
                        #endregion
                        case ApiExecutionAction.PostCrossReferences:
                            string postCrossReferencesJson = storage.GetFileContentsAsString(Info.StorageFolder, Info.RequestFileName, Encoding.UTF8);
                            var postCrossReferences = JsonConvert.DeserializeObject<List<AssetCrossReference>>(postCrossReferencesJson);

                            log.WriteLine($"POST Cross References (DB Start): Total raw Cross References: {postCrossReferences.Count}");
                            var postCrossReferenceResult = company.ImportCrossRefernces(dbExecutionItem, postCrossReferences, dbExecutionTimeout);
                            dbExecutionItem.Processed = postCrossReferenceResult.Processed;
                            dbExecutionItem.Error = postCrossReferenceResult.Error;
                            log.WriteLine($"POST Cross References (DB Complete): Total Processed: {postCrossReferenceResult.Processed}.");
                            log.WriteLine($"POST Cross References (DB Complete): Total Error: {postCrossReferenceResult.Error}.");
                            break;
                    }

                    dbExecutionItem.CompletedOn = DateTime.UtcNow;
                    company.Update(dbExecutionItem);
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

                dbExecutionItem.ErrorMessage = ex.GetFullExceptionData(false);
                dbExecutionItem.CompletedOn = DateTime.UtcNow;

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
        /// <returns></returns>
        private async Task<bool> ShouldRunApiJob(CompanyContext company)
        {
            // call function in db to see if the api job should run
            return await company.Database.Connection.QueryFirstOrDefaultAsync<bool>("select api.ShouldAllowNewBatchCall()");
        }

        private void Company_AssetsPartiallyProcessed(object sender, AssetsPartiallyProcessedEventArgs e)
        {
            storage.CreateFile(Info.StorageFolder, Info.ResponseFileName, JsonConvert.SerializeObject(e.Results));
        }

        private void Company_RelationshipsPartiallyProcessed(object sender, RelationshipsPartiallyProcessedEventArgs e)
        {
            storage.CreateFile(Info.StorageFolder, Info.ResponseFileName, JsonConvert.SerializeObject(e.Results));
        }
    }
}
