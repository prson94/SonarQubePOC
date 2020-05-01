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
using System.Data;
using System.Data.SqlClient;
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

            CoreFunction.AITrackJobStart(functionName);
            log.WriteLine($"GraphApiExecutionSubscriber triggered for execution uid: {(info?.execution?.ExecutionID.ToString() ?? "")}");
            CoreFunction.AITrackEvent(functionName, "GraphApiExecutionSubscriber triggered", new Dictionary<string, string> { { "executionUid", (info?.execution?.ExecutionID.ToString() ?? "") } });

            AzureStorageProvider storage = new AzureStorageProvider();

            using (var company = CompanyConnectionUtils.GetCompanyConnection(info.CompanyID))
            {
                try
                {
                    var assets = new List<AssetUpdate>();
                    var isInsert = false;
                    Guid assetTypeUid;
                    
                    company.OpenWithRetry(RetryPolicy.DefaultProgressive);

                    var execution = (
                        await company.QueryAsync<ApiExecution>(@"select * from api.Execution where ExecutionID = @executionID", new { info.execution.ExecutionID })
                        ).SingleOrDefault();

                    if (info.execution == null)
                        throw new Exception("Event execution info is null");

                    if (execution == null)
                        throw new Exception("Execution record not found");

                    switch (info.execution.Action)
                    {
                        case ApiExecutionAction.PutAssets:
                            var putFields = JsonConvert.DeserializeObject<ApiExecutionFields_PostAssets>(execution.Fields);
                            assetTypeUid = putFields.AssetTypeUid;
                            string putAssetsJson = storage.GetFileContentsAsString(info.execution.StorageFolder, info.execution.RequestFileName, Encoding.UTF8);
                            if (!string.IsNullOrEmpty(putAssetsJson))
                                assets = JsonConvert.DeserializeObject<List<AssetUpdate>>(putAssetsJson);

                            break;
                        case ApiExecutionAction.PostAssets:
                            isInsert = true;
                            var postFields = JsonConvert.DeserializeObject<ApiExecutionFields_PostAssets>(execution.Fields);
                            assetTypeUid = postFields.AssetTypeUid;
                            //we need the response here since the request doesn't contain uids. 
                            //Since this is a POST we don't need to check which fields were updated, we always populate the path
                            string postAssetsJson = storage.GetFileContentsAsString(info.execution.StorageFolder, info.execution.ResponseFileName, Encoding.UTF8);
                            if (!string.IsNullOrEmpty(postAssetsJson))
                                assets = JsonConvert.DeserializeObject<List<AssetUpdate>>(postAssetsJson);

                            break;
                        default:
                            throw new Exception($"Action {info.execution.Action} is not supported");
                    }

                    if (assets == null || !assets.Any())
                    {
                        throw new Exception("Asset JSON not found in storage");
                    }


                    using (var trans = company.BeginTransaction())
                    {
                        try
                        {
                            var keyFieldsList = (await company.QueryAsync<string>(@"
                            select F.[Name] from FieldType F 
                            inner join AssetType A on A.ID = F.AssetTypeID 
                            where A.[uid] = @assetTypeUid and F.IsPartOfKey = 1"
                            , new { assetTypeUid }
                            , transaction: trans))
                            .ToList();

                            #region Bulk Copy

                            var table = new DataTable();
                            table.Columns.Add("Uid", typeof(Guid));
                            table.Columns.Add("UpdatePath", typeof(bool));

                            foreach (var asset in assets)
                            {

                                var row = table.NewRow();
                                row["Uid"] = asset.Uid;

                                if (isInsert || (asset.Fields != null && asset.Fields.Keys.Any(k => keyFieldsList.Contains(k))))
                                {
                                    row["UpdatePath"] = true;
                                }
                                else
                                {
                                    row["UpdatePath"] = false;
                                }

                                table.Rows.Add(row);
                            }

                            await company.ExecuteAsync(@"
                            drop table if exists #GraphAssets;
                            create table #GraphAssets ([Uid] uniqueidentifier not null, [UpdatePath] bit not null, [GraphExists] bit, [AssetExists] bit);
                            ", transaction: trans);

                            var bulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.Default, trans)
                            {
                                BatchSize = table.Rows.Count,
                                DestinationTableName = "#GraphAssets",
                                BulkCopyTimeout = 3600
                            };

                            bulkCopy.ColumnMappings.Add("Uid", "Uid");
                            bulkCopy.ColumnMappings.Add("UpdatePath", "UpdatePath");


                            await bulkCopy.WriteToServerAsync(table);
                            
                            #endregion

                            #region Update Graph Tables

                            // Update temp table flags
                            await company.ExecuteAsync(@"
                            update  G
                            set     AssetExists = 1
                            from    #GraphAssets G
                            where   exists (select 1 from Asset where [uid] = G.[uid]);

                            update  G
                            set     GraphExists = 1
                            from    #GraphAssets G
                            where   exists (select 1 from graph.AssetNode where [uid] = G.[uid]);

                            update  #GraphAssets set GraphExists = 0 where GraphExists is null;
                            update  #GraphAssets set AssetExists = 0 where AssetExists is null;", transaction: trans);

                            // Update graph records
                            await company.ExecuteAsync(@"
		                    delete  E
		                    from    graph.AssetEdge E
				                    inner join graph.AssetNode N on E.$from_id = N.$node_id or E.$to_id = N.$node_id
                                    inner join #GraphAssets G on G.[uid] = N.[uid] and G.GraphExists = 1 and G.AssetExists = 0
		                    where   N.[uid] = G.[uid]

		                    delete  N
                            from    graph.AssetNode N
                                    inner join #GraphAssets G on G.[uid] = N.[uid] and G.GraphExists = 1 and G.AssetExists = 0;

                            update  N
		                    set     N.UpdatedOn = A.UpdatedOn,
				                    N.[State] = A.[State],
				                    N.AssetTypeID = A.AssetTypeID,
				                    N.AssetTypeUid = T.[Uid],
				                    N.Class = T.Class
		                    from    graph.AssetNode N
				                    inner join Asset A on A.ID = N.ID
				                    inner join AssetType T on T.ID = A.AssetTypeId
                                    inner join #GraphAssets G on G.[uid] = A.[uid] and G.GraphExists = 1 and G.AssetExists = 1;

		                    insert into graph.AssetNode (ID, [Uid], AssetTypeID, AssetTypeUid, [State], UpdatedOn, Class)
		                    select  A.ID,
				                    A.Uid,
				                    A.AssetTypeID,
				                    T.[Uid] as AssetTypeUid,
				                    A.[State],
				                    A.UpdatedOn,
				                    T.Class
		                    from    Asset A
				                    inner join AssetType T on T.ID = A.AssetTypeID
                                    inner join #GraphAssets G on G.uid = A.uid and G.GraphExists = 0;

                            update  T
                            set     T.UpdateGraph = S.UpdatePath
                            from    api.ExecutionAsset T
                                    inner join #GraphAssets S on S.Uid = T.Uid;", transaction: trans);


                            // Update paths/segments for applicable assets
                            await company.ExecuteAsync(@"exec graph.UpdateGraphTableHierarchyBy @executionId, null, null", new { executionId = info.execution.ExecutionID }, transaction: trans);

                            // Cleanup 
                            await company.ExecuteAsync(@"drop table if exists #GraphAssets;", transaction: trans);

                            #endregion

                            trans.Commit();
                        }
                        catch( Exception ex)
                        {
                            trans.Rollback();
                            throw ex;
                        }
                        
                    }
                }
                catch (Exception ex)
                {
                    var values = new Dictionary<string, string>
                    {
                        { "uid", info.Uid.ToString() }
                    };

                    if (info.execution != null)
                    {
                        values.Add("ExecutionID", info.execution.ExecutionID.ToString());
                        values.Add("ResourceID", info.execution.ResourceID.ToString());
                        values.Add("Action", info.execution.Action.ToString());
                    }

;                    CoreFunction.AITrackException(functionName, ex, info.CompanyID, values);
                }
            }

            CoreFunction.AITrackJobCompletedNoErrors(functionName);
            CoreFunction.AIFlush();

        }
    }
}
