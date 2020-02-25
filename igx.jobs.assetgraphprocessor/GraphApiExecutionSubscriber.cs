using d360.core.entities;
using d360.core.queue;
using d360.extensions.storage;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
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
    public class GraphApiExecutionSubscriber
    {
        const string functionName = "AssetGraphProcessor_GraphApiExecutionSubscriber";
        const int timeout = 60 * 180;

        public static async Task Run([ServiceBusTrigger("%AssetBusTopicName%", "GraphApiExecution", AccessRights.Manage)]BrokeredMessage brokeredMessage, TextWriter log)
        {
            var info = brokeredMessage.GetBody<AssetEventInfo>();
            if (info.Type != AssetEventType.Execution)
                return;

#if DEBUG
            CoreFunction.AITrackJobStart(functionName);
            log.WriteLine($"GraphApiExecutionSubscriber triggered for execution uid: {info.execution.ExecutionID}");
            CoreFunction.AITrackEvent(functionName, "GraphApiExecutionSubscriber triggered", new Dictionary<string, string> { { "executionUid", info.execution.ExecutionID.ToString() } });
#endif

            AzureStorageProvider storage = new AzureStorageProvider();

            using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(info.CompanyID))
            {
                try
                {
                    companyConnection.OpenWithRetry(RetryPolicy.DefaultProgressive);


                    switch (info.execution.Action)
                    {
                        case ApiExecutionAction.PutAssets:
                            #region
                            string putAssetsJson = storage.GetFileContentsAsString(info.execution.StorageFolder, info.execution.RequestFileName, Encoding.UTF8);
                            var putAssets = JsonConvert.DeserializeObject<List<AssetUpdate>>(putAssetsJson);

                            break;
                    }






                    //bool updatePath = false;

                    //if (info.ChangedFieldNames?.Any() ?? false)
                    //{
                    //    DynamicParameters dbArgs = new DynamicParameters();
                    //    string fieldList = "";

                    //    for (int i = 0; i < info.ChangedFieldNames.Count; i++)
                    //    {
                    //        dbArgs.Add($"@field{i}", info.ChangedFieldNames[i]);
                    //    }

                    //    fieldList = string.Join(",", dbArgs.ParameterNames.Select(p => $"@{p}"));
                    //    dbArgs.Add("@uid", info.Uid);

                    //    int keyFieldCount = (await companyConnection.QueryAsync<int>($@"
                    //                select  count(*) 
                    //                from    FieldType FT
                    //                        inner join Asset A on A.[uid] = @uid and FT.AssetTypeID = A.AssetTypeID
                    //                where   FT.Name in ({fieldList})", dbArgs, commandTimeout: timeout)).FirstOrDefault();

                    //    if (keyFieldCount > 0)
                    //    {
                    //        updatePath = true;
                    //    }
                    //}

                    await companyConnection.ExecuteAsync(@"graph.UpdateAssetNode @uid, @updatePath", new { uid = info.Uid, updatePath }, commandTimeout: timeout);
                }
                catch (Exception ex)
                {
                    string fieldNameList = info.ChangedFieldNames == null ? "" : string.Join(", ", info.ChangedFieldNames);
                    CoreFunction.AITrackException(functionName, ex, info.CompanyID, new Dictionary<string, string>() { { "uid", info.Uid.ToString() }, { "fields", fieldNameList } });
                }
            }

#if DEBUG
            CoreFunction.AITrackJobCompletedNoErrors(functionName);
            CoreFunction.AIFlush();
#endif

        }
    }
}
