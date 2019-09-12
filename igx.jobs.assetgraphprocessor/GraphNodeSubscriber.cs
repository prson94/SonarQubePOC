using d360.core.queue;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Microsoft.ServiceBus.Messaging;
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
        const int timeout = 1000 * 60 * 10;

        public static async Task Run([ServiceBusTrigger("%AssetBusTopicName%", "GraphNode", AccessRights.Manage)]BrokeredMessage brokeredMessage, TextWriter log)
        {
            var info = brokeredMessage.GetBody<AssetEventInfo>();
            if (info.Type != AssetEventType.Node)
                return;

            CoreFunction.AITrackJobStart(functionName);
            log.WriteLine($"GraphNodeSubscriber triggered for uid: {info.Uid}");
            CoreFunction.AITrackEvent(functionName, "GraphNodeSubscriber triggered", new Dictionary<string, string> { { "uid", info.Uid.ToString() } });

            using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(info.CompanyID))
            {
                string lineageVersion = CompanyConnectionUtils.GetCompanySettings(info.CompanyID).FirstOrDefault(s => s.SettingID == 68)?.Value ?? "";

                if (lineageVersion == "3")
                {

                    companyConnection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                    try
                    {
                        bool updatePath = false;

                        if (info.ChangedFieldNames?.Any() ?? false)
                        {
                            DynamicParameters dbArgs = new DynamicParameters();
                            string fieldList = "";

                            for (int i = 0; i < info.ChangedFieldNames.Count; i++)
                            {
                                dbArgs.Add($"@field{i}", info.ChangedFieldNames[i]);
                            }

                            fieldList = string.Join(",", dbArgs.ParameterNames.ToList());
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
            }

            CoreFunction.AITrackJobCompletedNoErrors(functionName);
            CoreFunction.AIFlush();
        }
    }
}
