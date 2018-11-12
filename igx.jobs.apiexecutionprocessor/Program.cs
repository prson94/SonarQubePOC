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
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    public class BulkLoadProcessor
    {
        const string functionName = "ApiExecution_Process";

        public async static Task Run([QueueTrigger("%ApiExecutionQueue%"), StorageAccount("MainStorageAccount")] string myQueueItem, TextWriter log)
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

                    switch (info.Action)
                    {
                        case ApiExecutionAction.PostAssets:
                            var postFields = JsonConvert.DeserializeObject<ApiExecutionFields_PostAssets>(dbExecutionItem.Fields);
                            assetType = company.Filter<AssetType>(i => i.uid == postFields.AssetTypeUid).Single();
                            string postAssetsJson = storage.GetFileContentsAsString(info.StorageFolder, info.RequestFileName, Encoding.UTF8);
                            var postAssets = JsonConvert.DeserializeObject<AssetInserts>(postAssetsJson);

                            var postResults = companyConnection.InsertAssets(queue, info.CompanyDomainPrefix, info.CompanyID, dbExecutionItem.ResourceID, assetType, postAssets);
                            dbExecutionItem.Processed = postResults.Count(i => i.Success);
                            dbExecutionItem.Error = postResults.Count(i => !i.Success);

                            storage.CreateFile(info.StorageFolder, info.ResponseFileName, JsonConvert.SerializeObject(postResults));
                            break;
                        case ApiExecutionAction.PutAssets:
                            var putFields = JsonConvert.DeserializeObject<ApiExecutionFields_PutAssets>(dbExecutionItem.Fields);
                            assetType = company.Filter<AssetType>(i => i.uid == putFields.AssetTypeUid).Single();
                            string putAssetsJson = storage.GetFileContentsAsString(info.StorageFolder, info.RequestFileName, Encoding.UTF8);
                            var putAssets = JsonConvert.DeserializeObject<AssetUpdates>(putAssetsJson);

                            var putResults = companyConnection.UpdateAssets(queue, info.CompanyDomainPrefix, info.CompanyID, dbExecutionItem.ResourceID, assetType, putAssets);
                            dbExecutionItem.Processed = putResults.Count(i => i.Success);
                            dbExecutionItem.Error = putResults.Count(i => !i.Success);

                            storage.CreateFile(info.StorageFolder, info.ResponseFileName, JsonConvert.SerializeObject(putResults));
                            break;
                        case ApiExecutionAction.DeleteAssets:
                            var deleteFields = JsonConvert.DeserializeObject<ApiExecutionFields_DeleteAssets>(dbExecutionItem.Fields);
                            assetType = company.Filter<AssetType>(i => i.uid == deleteFields.AssetTypeUid).Single();
                            string deleteAssetsJson = storage.GetFileContentsAsString(info.StorageFolder, info.RequestFileName, Encoding.UTF8);
                            var deleteAssets = JsonConvert.DeserializeObject<AssetDeletes>(deleteAssetsJson);

                            var deleteResults = companyConnection.DeleteAssets(queue, info.CompanyDomainPrefix, info.CompanyID, dbExecutionItem.ResourceID, assetType, deleteAssets);
                            dbExecutionItem.Processed = deleteResults.Count(i => i.Success);
                            dbExecutionItem.Error = deleteResults.Count(i => !i.Success);

                            storage.CreateFile(info.StorageFolder, info.ResponseFileName, JsonConvert.SerializeObject(deleteResults));
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
