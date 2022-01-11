using d360.core.entities;
using d360.core.queue;
using d360.extensions.storage;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
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
        const int timeout = 60 * 180; //3 hours
        const int sqlBatchSize = 5000;

        [Disable("DisableGraphApiExecution")]
        public static async Task RunExecutionSubscriber([ServiceBusTrigger("%AssetBusTopicName%", "GraphApiExecution")]Message brokeredMessage, TextWriter log)
        {
            var messageString = Encoding.UTF8.GetString(brokeredMessage.Body);
            var info = JsonConvert.DeserializeObject<AssetEventInfo>(messageString);

            if (info.Type != AssetEventType.Execution)
            {
                return;
            }

            CoreFunction.AITrackJobStart(functionName);

            string triggerMessage = $"GraphApiExecutionSubscriber triggered for ExecutionID [{(info?.execution?.ExecutionID.ToString() ?? "")}] on CompanyID [{info.CompanyID}]";
            log.WriteLine(triggerMessage);
            CoreFunction.AITrackEvent(functionName, triggerMessage, new Dictionary<string, string> { { "ExecutionID", (info?.execution?.ExecutionID.ToString() ?? "") } }, info.CompanyID);

            AzureStorageProvider storage = new AzureStorageProvider();

            using (var company = CompanyConnectionUtils.GetCompanyConnection(info.CompanyID))
            {
                try
                {
                    var assets = new List<AssetUpdate>();
                    var relationships = new List<DatabaseBulkRelationshipResult>();
                    Guid typeUid;
                    
                    company.Open();

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
                            typeUid = putFields.AssetTypeUid;
                            string putAssetsJson = await storage.GetFileContentsAsString(info.execution.StorageFolder, info.execution.RequestFileName, Encoding.UTF8);
                            
                            if (!string.IsNullOrEmpty(putAssetsJson))
                                assets = JsonConvert.DeserializeObject<List<AssetUpdate>>(putAssetsJson);

                            await ProcessAssets(company, assets, typeUid, info, false);

                            break;
                        case ApiExecutionAction.PostAssets:
                            var postFields = JsonConvert.DeserializeObject<ApiExecutionFields_PostAssets>(execution.Fields);
                            typeUid = postFields.AssetTypeUid;
                            //we need the response here since the request doesn't contain uids. 
                            //Since this is a POST we don't need to check which fields were updated, we always populate the path
                            string postAssetsJson = await storage.GetFileContentsAsString(info.execution.StorageFolder, info.execution.ResponseFileName, Encoding.UTF8);
                            
                            if (!string.IsNullOrEmpty(postAssetsJson))
                                assets = JsonConvert.DeserializeObject<List<AssetUpdate>>(postAssetsJson);

                            await ProcessAssets(company, assets, typeUid, info, true);

                            break;
                        case ApiExecutionAction.DeleteAssets:
                            var deleteFields = JsonConvert.DeserializeObject<ApiExecutionFields_DeleteAssets>(execution.Fields);
                            typeUid = deleteFields.AssetTypeUid;

                            //we need to process the non-batch DELETE call here too since we could have thousands of assets when Cascade == true
                            //so we grab the uids from the API table since we may or may not have results in storage
                            //If there are errors this may return an empty list. Only call ProcessAssets, if list is not empty
                            assets = (await company.QueryAsync<AssetUpdate>("select [uid] from api.ExecutionDeletedAsset where Success = 1 and ExecutionID = @ExecutionID", new { info.execution.ExecutionID })).ToList();

                            if (assets.Any())
                            {
                                await ProcessAssets(company, assets, typeUid, info, false);
                            }

                            break;
                        case ApiExecutionAction.PostRelationships:
                            var postRelFields = JsonConvert.DeserializeObject<ApiExecutionFields_PostRelationships>(execution.Fields);
                            typeUid = postRelFields.IntersectTypeUid;
                            string postRelationsJson = await storage.GetFileContentsAsString(info.execution.StorageFolder, info.execution.ResponseFileName, Encoding.UTF8);
                            
                            if (!string.IsNullOrEmpty(postRelationsJson))
                                relationships = JsonConvert.DeserializeObject<List<DatabaseBulkRelationshipResult>>(postRelationsJson);

                            await ProcessRelationships(company, relationships, typeUid, info);

                            break;

                        case ApiExecutionAction.DeleteRelationships:
                            var deleteRelFields = JsonConvert.DeserializeObject<ApiExecutionFields_DeleteRelationships>(execution.Fields);
                            typeUid = deleteRelFields.IntersectTypeUid;
                            string deleteRelationsJson = await storage.GetFileContentsAsString(info.execution.StorageFolder, info.execution.ResponseFileName, Encoding.UTF8);

                            if (!string.IsNullOrEmpty(deleteRelationsJson))
                                relationships = JsonConvert.DeserializeObject<List<DatabaseBulkRelationshipResult>>(deleteRelationsJson);

                            await ProcessRelationships(company, relationships, typeUid, info);
                            break;
                        default:
                            throw new Exception($"Action {info.execution.Action} is not supported");
                    }
                }
                catch (Exception ex)
                {
                    var values = new Dictionary<string, string>();

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

        private static async Task ProcessAssets(SqlConnection company, List<AssetUpdate> assets, Guid assetTypeUid, AssetEventInfo info, bool isInsert)
        {

            if (assets == null || !assets.Any())
            {
                throw new Exception("Asset JSON not found in storage");
            }
                        
            {
                try
                {
                    var keyFieldsList = (await company.QueryAsync<string>(@"
                            select F.[Name] from FieldType F 
                            inner join AssetType A on A.ID = F.AssetTypeID 
                            where A.[uid] = @assetTypeUid and F.IsPartOfKey = 1"
                    , new { assetTypeUid }))
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
                            ");

                    var bulkCopy = new SqlBulkCopy(company)
                    {
                        BatchSize = sqlBatchSize,
                        DestinationTableName = "#GraphAssets",
                        BulkCopyTimeout = timeout
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
                            update  #GraphAssets set AssetExists = 0 where AssetExists is null;"
                    , commandTimeout: timeout);

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
                                    inner join #GraphAssets S on S.Uid = T.Uid;"                    
                    , commandTimeout: timeout);

                    bool shouldUpdateGraphHierarchy = true;
                    
                    bool hasChildAssetType = (await company.QueryAsync<bool>(@"select case when T.ID is null then cast(0 as bit) else cast(1 as bit) end from AssetType A
                        left join IntersectTypeDetail T on T.Subject = A.Object and T.SubjectID = A.ObjectID and T.PredicateType in (3,4)
                        where A.[uid] = @assetTypeUid"
                    , new { assetTypeUid }))
                    .SingleOrDefault();

                    int? parentIntersectTypeId = (await company.QueryAsync<int?>(@"select case when T.ID is null then null else T.ID end from AssetType A
                        left join IntersectTypeDetail T on T.Object = A.Object and T.ObjectID = A.ObjectID and T.PredicateType in (3,4)
                        where A.[uid] = @assetTypeUid"
                    , new { assetTypeUid }))
                    .SingleOrDefault();

                    
                    //check for updated parents and update graph Edge records appropriately
                    if (info.execution.Action != ApiExecutionAction.DeleteAssets && !isInsert && parentIntersectTypeId.HasValue)
                    {
                        await company.ExecuteAsync(@"
                            insert into graph.AssetEdge ($from_id, $to_id, ID, Uid, IntersectTypeID, IntersectTypeUid, PredicateID, PredicateUid, PredicateType, Properties, [State], UpdatedOn) 
                            select  N.$node_id, 
                                    O.$node_id, 
                                    E.ID, E.Uid, 
                                    E.IntersectTypeID, 
                                    E.IntersectTypeUid, 
                                    E.PredicateID, 
                                    E.PredicateUid, 
                                    E.PredicateType, 
                                    E.Properties, 
                                    E.State, 
                                    getutcdate() 
                            from    graph.AssetEdge E
                                    inner join graph.AssetNode S on S.$node_id = E.$from_id
                                    inner join graph.AssetNode O on O.$node_id = E.$to_id
                                    inner join [Intersect] I on I.ID = E.ID
                                    inner join Asset SA on SA.Object = I.Subject and SA.ObjectID = I.SubjectID
                                    inner join Asset OA on OA.Object = I.Object and OA.ObjectID = I.ObjectID
                                    inner join #GraphAssets G on G.Uid = O.Uid  and G.GraphExists = 1 and G.AssetExists = 1
                                    inner join graph.AssetNode N on N.ID = SA.ID 
                            where   E.IntersectTypeID = @parentIntersectTypeId 
                                    and OA.ID = O.ID 
                                    and SA.ID != S.ID;

                            delete  E 
                            from    graph.AssetEdge E
                                    inner join graph.AssetNode S on S.$node_id = E.$from_id
                                    inner join graph.AssetNode O on O.$node_id = E.$to_id
                                    inner join [Intersect] I on I.ID = E.ID
                                    inner join Asset SA on SA.Object = I.Subject and SA.ObjectID = I.SubjectID
                                    inner join Asset OA on OA.Object = I.Object and OA.ObjectID = I.ObjectID
                                    inner join #GraphAssets G on G.Uid = O.Uid  and G.GraphExists = 1 and G.AssetExists = 1
                            where   E.IntersectTypeID = @parentIntersectTypeId and OA.ID = O.ID and SA.ID != S.ID
                        "
                        , new { parentIntersectTypeId }
                        , commandTimeout: timeout);

                    }
                    
                    
                    //skip heirarchy update if we're deleting leaf assets
                    if (info.execution.Action == ApiExecutionAction.DeleteAssets && !hasChildAssetType)
                    {
                        shouldUpdateGraphHierarchy = false;
                    }


                    if (shouldUpdateGraphHierarchy)
                    {
                        // Update paths/segments for applicable assets
                        await company.ExecuteAsync(@"exec graph.UpdateGraphTableHierarchyBy @executionId, null, null"
                        , new { executionId = info.execution.ExecutionID }
                        , commandTimeout: timeout);
                    }

                    // Cleanup 
                    await company.ExecuteAsync(@"drop table if exists #GraphAssets;"
                    , commandTimeout: timeout);

                    #endregion

                }
                catch
                {    
                    // ignore exception
                }

            }
        }

        private static async Task ProcessRelationships(SqlConnection company, List<DatabaseBulkRelationshipResult> relationships, Guid intersectTypeUid, AssetEventInfo info)
        {
            if (relationships == null || !relationships.Any())
            {
                throw new Exception("Intersect JSON not found in storage");
            }

            using (var trans = company.BeginTransaction())
            {
                try
                {

                    #region Bulk Copy

                    var table = new DataTable();
                    table.Columns.Add("IntersectID", typeof(int));


                    foreach (var relationship in relationships)
                    {

                        var row = table.NewRow();
                        row["IntersectID"] = relationship.IntersectID;

                        table.Rows.Add(row);
                    }

                    await company.ExecuteAsync(@"
                            drop table if exists #GraphEdges;
                            create table #GraphEdges ([IntersectID] int not null, SubjectAssetUid uniqueidentifier, ObjectAssetUid uniqueidentifier, Recreate bit);
                            ", transaction: trans);

                    var bulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.Default, trans)
                    {
                        BatchSize = sqlBatchSize,
                        DestinationTableName = "#GraphEdges",
                        BulkCopyTimeout = timeout
                    };

                    bulkCopy.ColumnMappings.Add("IntersectID", "IntersectID");

                    await bulkCopy.WriteToServerAsync(table);

                    #endregion

                    #region Update Tables

                    //remove deleted records
                    await company.ExecuteAsync(@"
                        delete A
                        from graph.AssetEdge A
                        inner join #GraphEdges E on E.IntersectID = A.ID
                        where not exists (select 1 from [Intersect] where ID = E.IntersectID)

                        delete from #GraphEdges where not exists (select 1 from [Intersect] where ID = IntersectID)"
                    , transaction: trans
                    , commandTimeout: timeout);

                    //recreate records with changed subject/object
                    await company.ExecuteAsync(@"
                        update  G
                        set     G.SubjectAssetUid = S.[uid],
                                G.ObjectAssetUid = T.[uid]
		                from	#GraphEdges G,
                                graph.AssetNode S,
				                graph.AssetEdge E,
				                graph.AssetNode T
		                where	MATCH(S-(E)->T)
				                and E.[ID] = G.IntersectID

                        update  G
                        set     G.Recreate = 1
                        from    #GraphEdges G
                        inner join [IntersectDetail] I on I.ID = G.IntersectID
                        where I.SubjectUid <> G.SubjectAssetUid or I.ObjectUid <> G.ObjectAssetUid

                        delete A
                        from    graph.AssetEdge A
                        inner join #GraphEdges E on E.IntersectID = A.ID and coalesce(E.Recreate, 0) = 1

                        insert into graph.AssetEdge ($from_id, $to_id, ID, Uid, IntersectTypeID, IntersectTypeUid, PredicateID, PredicateUid, PredicateType, Properties, [State], UpdatedOn)
			            select  SG.$node_id,
					            OG.$node_id,
					            I.ID,
					            I.[Uid],
					            T.ID as IntersectTypeID,
					            T.[Uid] as IntersectTypeUid,
					            P.ID as PredicateID,
					            P.Uid as PredicateUid,
					            P.Type as PredicateType,
					            '<props/>' as Properties,
					            I.[State],
					            coalesce(I.UpdatedOn, I.CreatedOn, getutcdate()) as UpdatedOn
			            from    [Intersect] I
					            inner join Asset SA on SA.[Object] = I.[Subject] and SA.ObjectID = I.SubjectID
					            inner join graph.AssetNode SG on SG.ID = SA.ID
					            inner join Asset OA on OA.[Object] = I.[Object] and OA.ObjectID = I.ObjectID
					            inner join graph.AssetNode OG on OG.ID = OA.ID
					            inner join IntersectType T on T.ID = I.IntersectTypeID
					            inner join [Predicate] P on P.ID = T.PredicateID
                                inner join #GraphEdges E on E.IntersectID = I.ID and coalesce(E.Recreate, 0) = 1
			            where   I.[ID] = E.IntersectID and not exists (select 1 from graph.AssetEdge where [ID] = E.IntersectID)

                        delete from #GraphEdges where Recreate = 1
                        "
                    , transaction: trans
                    , commandTimeout: timeout);

                    //update existing records
                    await company.ExecuteAsync(@"
				        update  E
				        set     E.UpdatedOn = I.UpdatedOn,
						        E.IntersectTypeID = I.IntersectTypeID,
						        E.IntersectTypeUid = T.[Uid],
						        E.PredicateID = T.PredicateID,
						        E.PredicateUid = P.[Uid],
						        E.PredicateType = P.[Type],
						        E.[State] = I.[State]
				        from    graph.AssetEdge E
						        inner join [Intersect] I on I.ID = E.ID
						        inner join IntersectType T on T.ID = I.IntersectTypeID
						        inner join [Predicate] P on P.ID = T.PredicateID
                                inner join #GraphEdges G on G.IntersectID = I.ID"
                    , transaction: trans
                    , commandTimeout: timeout);


                    //add new records
                    await company.ExecuteAsync(@"
                        insert into graph.AssetEdge ($from_id, $to_id, ID, Uid, IntersectTypeID, IntersectTypeUid, PredicateID, PredicateUid, PredicateType, Properties, [State], UpdatedOn)
                                select  SG.$node_id,
                                        OG.$node_id,
                                        I.ID,
                                        I.[Uid],
                                        T.ID as IntersectTypeID,
                                        T.[Uid] as IntersectTypeUid,
                                        P.ID as PredicateID,
                                        P.Uid as PredicateUid,
                                        P.Type as PredicateType,
		                                '<props/>' as Properties,
		                                I.[State],
		                                coalesce(I.UpdatedOn, I.CreatedOn, getutcdate()) as UpdatedOn
                                from    [Intersect] I
                                        inner join Asset SA on SA.[Object] = I.[Subject] and SA.ObjectID = I.SubjectID
		                                inner join graph.AssetNode SG on SG.ID = SA.ID
		                                inner join Asset OA on OA.[Object] = I.[Object] and OA.ObjectID = I.ObjectID
		                                inner join graph.AssetNode OG on OG.ID = OA.ID
		                                inner join IntersectType T on T.ID = I.IntersectTypeID
		                                inner join [Predicate] P on P.ID = T.PredicateID
                                        inner join #GraphEdges E on E.IntersectID = I.ID
                                where   not exists (select 1 from graph.AssetEdge where ID = E.IntersectID)"
                        , transaction: trans
                        , commandTimeout: timeout);

                    //cleanup
                    await company.ExecuteAsync(@"
				        drop table if exists #GraphEdges"
                        , transaction: trans
                        , commandTimeout: timeout);


                    #endregion

                    trans.Commit();
                }
                catch (Exception ex)
                {
                    try
                    {
                        if (trans != null)
                        {
                            trans.Rollback();
                        }
                        throw ex;
                    }
                    catch { }
                }
            }
        }
    }
}
