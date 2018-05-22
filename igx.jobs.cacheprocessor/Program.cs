using d360.core;
using d360.core.queue;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace igx.jobs.cacheprocessor
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

    public class CacheProcessor
    {
        const string functionName = "Cache_Process";

        public async static Task Run([QueueTrigger("%CacheQueue%"), StorageAccount("MainStorageAccount")] string myQueueItem, TextWriter log)
        {
            var info = JsonConvert.DeserializeObject<CacheInfo>(myQueueItem);

            try
            {
                var companyConnection = CompanyConnectionUtils.GetCompanyConnection(info.CompanyID);
                companyConnection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                switch (info.CacheObject)
                {
                    case CacheInfoObject.AssetDelete:
                        await companyConnection.ExecuteAsync(
                            "exec cache.SecurityProcessor @CacheObject, @Source, @SourceID", 
                            new { CacheObject = (int)info.CacheObject, Source = (int)info.Source, info.SourceID }, 
                            commandTimeout: 1200);
                        break;
                    case CacheInfoObject.AssetEdit:
                        await companyConnection.ExecuteAsync(
                            "exec cache.SecurityProcessor @CacheObject, @Source, @SourceID", 
                            new { CacheObject = (int)info.CacheObject, Source = (int)info.Source, info.SourceID },
                            commandTimeout: 1200);
                        break;
                    case CacheInfoObject.AssetNoRead:
                        await companyConnection.ExecuteAsync(
                            "exec cache.SecurityProcessor @CacheObject, @Source, @SourceID", 
                            new { CacheObject = (int)info.CacheObject, Source = (int)info.Source, info.SourceID },
                            commandTimeout: 1200);
                        break;
                    case CacheInfoObject.AssetResponsibility:
                        await companyConnection.ExecuteAsync(
                            "exec cache.SecurityProcessor @CacheObject, @Source, @SourceID", 
                            new { CacheObject = (int)info.CacheObject, Source = (int)info.Source, info.SourceID },
                            commandTimeout: 1200);
                        break;
                }

                companyConnection.Close();
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, info.CompanyID);
                log.WriteLine($"Company [{info.CompanyID}], Source [{info.Source}], Source ID [{info.SourceID}]: [{ex.GetFullExceptionData()}]");
            }
        }
    }
}
