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

namespace igx.jobs.apiexecutionprocessor
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

    public class ApiExecutionProcessor
    {
        const string functionName = "ApiExecution_Process";

        public async static Task Run([QueueTrigger("%ApiExecutionQueue%"), StorageAccount("QueueStorageAccount")] string myQueueItem, TextWriter log)
        {
            var info = JsonConvert.DeserializeObject<ApiExecutionInfo>(myQueueItem);

            try
            {
                #region Create EF connection

                var sec = new UriSecurityContextProvider()
                {
                    CompanyID = info.CompanyID,
                    ResourceID = 0,
                    CompanyPrefix = info.CompanyDomainPrefix,
                    IsAdministrator = true
                };
                var cache = new DummyCachingProvider();
                var queue = new AzureQueueSource();
                var community = new CommunityContext(cache, queue, sec);
                var company = new CompanyContext(community, cache, queue, sec, true);
                var storage = new AzureStorageProvider();

                #endregion

                var companyConnection = CompanyConnectionUtils.GetCompanyConnection(info.CompanyID);
                companyConnection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                var dbExecutionItem = company.Filter<ApiExecution>(i => i.ExecutionID == info.ExecutionID).SingleOrDefault();

                if (dbExecutionItem != null)
                {
                    AssetType assetType = null;
                    IntersectType intersectType = null;

                    int dbExecutionTimeout = int.Parse(CoreFunction.GetConfigValueByKey("DBExecuteQueryTimeout"));

                    switch (info.Action)
                    {
                        case ApiExecutionAction.PostAssets:
                            var postAssetsFields = JsonConvert.DeserializeObject<ApiExecutionFields_PostAssets>(dbExecutionItem.Fields);
                            assetType = company.Filter<AssetType>(i => i.uid == postAssetsFields.AssetTypeUid).Single();
                            string postAssetsJson = storage.GetFileContentsAsString(info.StorageFolder, info.RequestFileName, Encoding.UTF8);
                            var postAssets = JsonConvert.DeserializeObject<AssetInserts>(postAssetsJson);

                            var postAssetsResults = companyConnection.InsertAssets(queue, info.CompanyDomainPrefix, info.CompanyID, dbExecutionItem.ResourceID, assetType, postAssets, dbExecutionTimeout);
                            dbExecutionItem.Processed = postAssetsResults.Count(i => i.Success);
                            dbExecutionItem.Error = postAssetsResults.Count(i => !i.Success);

                            storage.CreateFile(info.StorageFolder, info.ResponseFileName, JsonConvert.SerializeObject(postAssetsResults));
                            break;
                        case ApiExecutionAction.PutAssets:
                            var putAssetsFields = JsonConvert.DeserializeObject<ApiExecutionFields_PutAssets>(dbExecutionItem.Fields);
                            assetType = company.Filter<AssetType>(i => i.uid == putAssetsFields.AssetTypeUid).Single();
                            string putAssetsJson = storage.GetFileContentsAsString(info.StorageFolder, info.RequestFileName, Encoding.UTF8);
                            var putAssets = JsonConvert.DeserializeObject<AssetUpdates>(putAssetsJson);

                            var putAssetsResults = companyConnection.UpdateAssets(queue, info.CompanyDomainPrefix, info.CompanyID, dbExecutionItem.ResourceID, assetType, putAssets, dbExecutionTimeout);
                            dbExecutionItem.Processed = putAssetsResults.Count(i => i.Success);
                            dbExecutionItem.Error = putAssetsResults.Count(i => !i.Success);

                            storage.CreateFile(info.StorageFolder, info.ResponseFileName, JsonConvert.SerializeObject(putAssetsResults));
                            break;
                        case ApiExecutionAction.DeleteAssets:
                            var deleteAssetsFields = JsonConvert.DeserializeObject<ApiExecutionFields_DeleteAssets>(dbExecutionItem.Fields);
                            assetType = company.Filter<AssetType>(i => i.uid == deleteAssetsFields.AssetTypeUid).Single();

                            string deleteAssetsJson = storage.GetFileContentsAsString(info.StorageFolder, info.RequestFileName, Encoding.UTF8);
                            var deleteAssets = JsonConvert.DeserializeObject<AssetDeletes>(deleteAssetsJson);
                            var deleteAssetsResults = companyConnection.DeleteAssets(queue, info.CompanyDomainPrefix, info.CompanyID, dbExecutionItem.ResourceID, assetType, deleteAssets, dbExecutionTimeout);
                            dbExecutionItem.Processed = deleteAssetsResults.Count(i => i.Success);
                            dbExecutionItem.Error = deleteAssetsResults.Count(i => !i.Success);

                            storage.CreateFile(info.StorageFolder, info.ResponseFileName, JsonConvert.SerializeObject(deleteAssetsResults));
                            break;
                        case ApiExecutionAction.PostRelationships:
                            var postRelationshipsFields = JsonConvert.DeserializeObject<ApiExecutionFields_PostRelationships>(dbExecutionItem.Fields);
                            intersectType = company.Filter<IntersectType>(i => i.uid == postRelationshipsFields.IntersectTypeUid).Single();
                            string postRelationshipsJson = storage.GetFileContentsAsString(info.StorageFolder, info.RequestFileName, Encoding.UTF8);
                            var postRelationships = JsonConvert.DeserializeObject<RelationshipInserts>(postRelationshipsJson);

                            var postRelationshipsResults = companyConnection.BulkRelationshipsImport(queue, info.CompanyDomainPrefix, info.CompanyID, dbExecutionItem.ResourceID, intersectType, postRelationships, dbExecutionTimeout);
                            dbExecutionItem.Processed = postRelationshipsResults.Count(i => i.Success);
                            dbExecutionItem.Error = postRelationshipsResults.Count(i => !i.Success);

                            storage.CreateFile(info.StorageFolder, info.ResponseFileName, JsonConvert.SerializeObject(postRelationshipsResults));
                            break;
                    }

                    companyConnection.Close();

                    dbExecutionItem.CompletedOn = DateTime.UtcNow;
                    company.Update(dbExecutionItem);
                }


            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, info.CompanyID);
            }
        }
    }
}
