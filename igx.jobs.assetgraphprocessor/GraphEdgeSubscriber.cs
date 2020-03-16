using d360.core.queue;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace igx.jobs.assetgraphprocessor
{
    public class GraphEdgeSubscriber
    {
        const string functionName = "AssetGraphProcessor_GraphEdgeSubscriber";
        const int timeout = 60 * 10;

        public static async Task Run([ServiceBusTrigger("%AssetBusTopicName%", "GraphEdge", AccessRights.Manage)]BrokeredMessage brokeredMessage, TextWriter log)
        {
            var info = brokeredMessage.GetBody<AssetEventInfo>();
            if (info.Type != AssetEventType.Edge)
                return;

#if DEBUG
            CoreFunction.AITrackJobStart(functionName);
            log.WriteLine($"GraphEdgeSubscriber triggered for uid: {info.Uid}");
            CoreFunction.AITrackEvent(functionName, "GraphEdgeSubscriber triggered", new Dictionary<string, string> { { "uid", info.Uid.ToString() } });
#endif

            using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(info.CompanyID))
            {
                try
                {
                    companyConnection.OpenWithRetry(RetryPolicy.DefaultProgressive);
                    await companyConnection.ExecuteAsync(@"graph.UpdateAssetEdge @uid", new { uid = info.Uid }, commandTimeout: timeout);
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex, info.CompanyID, new Dictionary<string, string>() { { "uid", info.Uid.ToString() } });
                }
            }

#if DEBUG
            CoreFunction.AITrackJobCompletedNoErrors(functionName);
            CoreFunction.AIFlush();
#endif
        }
    }
}
