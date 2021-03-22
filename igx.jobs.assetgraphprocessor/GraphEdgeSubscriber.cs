using d360.core.queue;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace igx.jobs.assetgraphprocessor
{
    public class GraphEdgeSubscriber
    {
        const string functionName = "AssetGraphProcessor_GraphEdgeSubscriber";
        const int timeout = 60 * 10;

        [Disable("DisableGraphEdge")]
        public static async Task RunEdgeSubscriber([ServiceBusTrigger("%AssetBusTopicName%", "GraphEdge")]Message brokeredMessage, TextWriter log)
        {
            var messageString = Encoding.UTF8.GetString(brokeredMessage.Body);
            var info = JsonConvert.DeserializeObject<AssetEventInfo>(messageString);
            if (info.Type != AssetEventType.Edge)
                return;

            CoreFunction.AITrackJobStart(functionName);

            string triggerMessage = $"GraphEdgeSubscriber triggered for uid [{info.Uid}] on CompanyID [{info.CompanyID}]";
            log.WriteLine(triggerMessage);
            CoreFunction.AITrackEvent(functionName, triggerMessage, new Dictionary<string, string> { { "uid", info.Uid.ToString() } }, info.CompanyID);

            using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(info.CompanyID))
            {
                try
                {
                    companyConnection.Open();
                    await companyConnection.ExecuteAsync(@"graph.UpdateAssetEdge @uid", new { uid = info.Uid }, commandTimeout: timeout);
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex, info.CompanyID, new Dictionary<string, string>() { { "uid", info.Uid.ToString() } });
                }
            }

            CoreFunction.AITrackJobCompletedNoErrors(functionName);
            CoreFunction.AIFlush();
        }
    }
}
