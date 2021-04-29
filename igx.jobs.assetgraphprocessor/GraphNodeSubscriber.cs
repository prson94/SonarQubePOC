using d360.core.queue;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.jobs.assetgraphprocessor
{
    public class GraphNodeSubscriber
    {
        const string functionName = "AssetGraphProcessor_GraphNodeSubscriber";
        const int timeout = 60 * 10;

        [Disable("DisableGraphNode")]
        public static async Task RunNodeSubscriber([ServiceBusTrigger("%AssetBusTopicName%", "GraphNode")]Message brokeredMessage, TextWriter log)
        {
            var messageString = Encoding.UTF8.GetString(brokeredMessage.Body);
            var info = JsonConvert.DeserializeObject<AssetEventInfo>(messageString);
            if (info.Type != AssetEventType.Node)
                return;

            CoreFunction.AITrackJobStart(functionName);

            string triggerMessage = $"GraphNodeSubscriber triggered for uid [{info.Uid}] on CompanyID [{info.CompanyID}]";
            log.WriteLine(triggerMessage);
            CoreFunction.AITrackEvent(functionName, triggerMessage, new Dictionary<string, string> { { "uid", info.Uid.ToString() } }, info.CompanyID);

            using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(info.CompanyID))
            {
                try
                {
                    companyConnection.Open();

                    bool updatePath = false;

                    if (info.ChangedFieldNames?.Any() ?? false)
                    {
                        DynamicParameters dbArgs = new DynamicParameters();
                        string fieldList = "";

                        for (int i = 0; i < info.ChangedFieldNames.Count; i++)
                        {
                            dbArgs.Add($"@field{i}", info.ChangedFieldNames[i]);
                        }

                        fieldList = string.Join(",", dbArgs.ParameterNames.Select(p => $"@{p}"));
                        dbArgs.Add("@uid", info.Uid);

                        int keyFieldCount = (await companyConnection.QueryAsync<int>($@"
                                    select  count(*) 
                                    from    FieldType FT
                                            inner join Asset A on A.[uid] = @uid and FT.AssetTypeID = A.AssetTypeID
                                    where   FT.Name in ({fieldList})", dbArgs, commandTimeout: timeout)).FirstOrDefault();

                        if (keyFieldCount > 0)
                        {
                            updatePath = true;
                        }
                    }

                    await companyConnection.ExecuteAsync(@"graph.UpdateAssetNode @uid, @updatePath", new { uid = info.Uid, updatePath }, commandTimeout: timeout);
                }
                catch (Exception ex)
                {
                    string fieldNameList = info.ChangedFieldNames == null ? "" : string.Join(", ", info.ChangedFieldNames);
                    CoreFunction.AITrackException(functionName, ex, info.CompanyID, new Dictionary<string, string>() { { "uid", info.Uid.ToString() }, { "fields", fieldNameList } });
                }
            }

            CoreFunction.AITrackJobCompletedNoErrors(functionName);
            CoreFunction.AIFlush();
        }
    }
}
