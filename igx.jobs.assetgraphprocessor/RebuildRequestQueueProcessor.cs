using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using d360.utils.company;
using Dapper;
using d360.core.queue;
using d360.extensions.info;
using d360.extensions.caching;
using d360.extensions.queue;
using d360.model;
using d360.core.enums;
using Newtonsoft.Json;
using d360.extensions.mail;
using d360.core;
using System.Configuration;

namespace igx.jobs.assetgraphprocessor
{
    public class RebuildRequestQueueProcessor
    {
        const string functionName = "AssetGraphProcessor_RebuildQueueRequest";

#if DEBUG
[Disable]
        public static async Task RunRebuildProcessor([TimerTrigger("0 0 1 * * *", RunOnStartup = true)]TimerInfo myTimer, TextWriter log)
#else
        public static async Task RunRebuildProcessor([QueueTrigger("%AssetGraphQueue%"), StorageAccount("QueueStorageAccount")] string myQueueItem, TextWriter log)
#endif
        {
            RebuildAssetGraphModel queueInfo = null;
#if DEBUG
            queueInfo = new RebuildAssetGraphModel { CompanyID = 1, To = 0 };
#else
            queueInfo = JsonConvert.DeserializeObject<RebuildAssetGraphModel>(myQueueItem);
#endif

            #region Create EF connection

            var _c = CoreFunction.GetCompaniesByCurrentSlot()
                .FirstOrDefault(x => x.CompanyID == queueInfo.CompanyID);

            var companyContext = JobDbContextCreator.CreateCompanyContext(
                new UriSecurityContextProvider
                {
                    CompanyID = _c.CompanyID,
                    CompanyPrefix = _c.UrlPrefix,
                    ResourceID = 0,
                    IsAdministrator = true
                },
                new MandrillMailProvider
                {
                    ApiKey = ConfigurationManager.AppSettings[constants.MAIL_API_KEY],
                    SubAccount = ConfigurationManager.AppSettings[constants.MAIL_SUB_ACCOUNT]
                },
                new AzureQueueSource(),
                new DummyCachingProvider(),
                constants.COMMUNITY_DATABASE_CONNECTION);
            
            #endregion

            CoreFunction.AITrackEvent(functionName, $"RebuildQueueRequest triggered for CompanyID {queueInfo.CompanyID}", null, queueInfo.CompanyID);

            using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(queueInfo.CompanyID))
            {
                const int timeout = 60 * 180;

                companyConnection.Open();

                try
                {
                    await companyConnection.ExecuteAsync("graph.SynchronizeTables @populatePaths", new { populatePaths = true }, commandTimeout: timeout);
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex, queueInfo.CompanyID);
                }
                finally 
                {
                    await companyContext.UpdateRebuildJobStatus(CompanyRebuildJobToken.AssetGraph, CompanyRebuildJobStatusState.Inactive);
                }
            }

            CoreFunction.AITrackJobCompletedNoErrors(functionName);
            CoreFunction.AIFlush();
        }
    }
}
