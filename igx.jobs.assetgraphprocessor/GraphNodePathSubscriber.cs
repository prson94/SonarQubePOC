using d360.core.queue;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace igx.jobs.assetgraphprocessor
{
    public class GraphNodePathSubscriber
    {
        const string functionName = "AssetGraphProcessor_GraphNodePathSubscriber";
        const int timeout = 60 * 10;

        public static async Task Run([ServiceBusTrigger("%AssetBusTopicName%", "GraphNodePath")]BrokeredMessage brokeredMessage, TextWriter log)
        {
            var info = brokeredMessage.GetBody<AssetEventInfo>();
            if (info.Type != AssetEventType.Node)
            {
                return;
            }

            CoreFunction.AITrackJobStart(functionName);

            string triggerMessage = $"GraphNodePathSubscriber triggered for uid [{info.Uid}] on CompanyID [{info.CompanyID}]";
            log.WriteLine(triggerMessage);
            CoreFunction.AITrackEvent(functionName, triggerMessage, new Dictionary<string, string> { { "uid", info.Uid.ToString() } }, info.CompanyID);

            using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(info.CompanyID))
            {
                try
                {
                    companyConnection.Open();
                    await companyConnection.ExecuteAsync(@"begin
                        declare @assetId bigint;

                        select  @assetId = id
                        from    Asset
                        where   [uid] = @uid;

                        exec graph.UpdateGraphTableHierarchyBy null, null, @assetId 
                    end", new { uid = info.Uid }, commandTimeout: timeout);
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
