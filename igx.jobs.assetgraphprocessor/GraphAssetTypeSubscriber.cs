using d360.core.entities;
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.jobs.assetgraphprocessor
{
    public class GraphAssetTypeSubscriber
    {
        const string functionName = "AssetGraphProcessor_GraphAssetTypeSubscriber";
        const int timeout = 60 * 10;

        public static async Task RunAssetTypeSubscriber([ServiceBusTrigger("%AssetBusTopicName%", "GraphAssetType")]Message brokeredMessage, TextWriter log)
        {
            var messageString = Encoding.UTF8.GetString(brokeredMessage.Body);
            var info = JsonConvert.DeserializeObject<AssetEventInfo>(messageString);

            if (info.Type != AssetEventType.AssetType)
                return;

            using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(info.CompanyID))
            {
                try
                {
                    companyConnection.Open();
                    var assetType = companyConnection.Query<AssetType>("select * from AssetType where Uid = @uid", new { info.Uid }).SingleOrDefault();
                    if (assetType != null)
                    {
                        await companyConnection.ExecuteAsync(@"exec graph.UpdateGraphTableHierarchyBy null, @assetTypeId, null"
                            , new { assetTypeId = assetType.ID }
                            , commandTimeout: timeout);

                        companyConnection.Close();
                    }
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
