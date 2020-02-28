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

#if DEBUG
            CoreFunction.AITrackJobStart(functionName);
            log.WriteLine($"GraphApiExecutionSubscriber triggered for execution uid: {info.execution.ExecutionID}");
            CoreFunction.AITrackEvent(functionName, "GraphApiExecutionSubscriber triggered", new Dictionary<string, string> { { "executionUid", info.execution.ExecutionID.ToString() } });
#endif

            AzureStorageProvider storage = new AzureStorageProvider();

            using (var company = CompanyConnectionUtils.GetCompanyConnection(info.CompanyID))
            {
                try
                {
                    var assets = new List<AssetUpdate>();
                    var isInsert = false;
                    Guid assetTypeUid;
                    
                    company.OpenWithRetry(RetryPolicy.DefaultProgressive);


                    var execution = (await company.QueryAsync<ApiExecution>(@"select * from api.Execution where ExecutionID = @executionID"
                        , new { info.execution.ExecutionID }))
                        .SingleOrDefault();

                    switch (info.execution.Action)
                    {
                        case ApiExecutionAction.PutAssets:
                            var postFields = JsonConvert.DeserializeObject<ApiExecutionFields_PostAssets>(execution.Fields);
                            assetTypeUid = postFields.AssetTypeUid;
                            string putAssetsJson = storage.GetFileContentsAsString(info.execution.StorageFolder, info.execution.RequestFileName, Encoding.UTF8);
                            assets = JsonConvert.DeserializeObject<List<AssetUpdate>>(putAssetsJson);

                            break;
                        case ApiExecutionAction.PostAssets:
                            isInsert = true;
                            var putFields = JsonConvert.DeserializeObject<ApiExecutionFields_PutAssets>(execution.Fields);
                            assetTypeUid = putFields.AssetTypeUid;

                            string postAssetsJson = storage.GetFileContentsAsString(info.execution.StorageFolder, info.execution.RequestFileName, Encoding.UTF8);
                            assets = JsonConvert.DeserializeObject<List<AssetUpdate>>(postAssetsJson);

                            break;
                        default:
                            return;
                    }

                    if (assets == null || !assets.Any())
                        return;


                    using (var trans = company.BeginTransaction())
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
                        table.Columns.Add("ParentUid", typeof(Guid));
                        table.Columns["ParentUid"].AllowDBNull = true;
                        table.Columns.Add("UpdatePath", typeof(bool));

                        foreach (var asset in assets)
                        {
                            var row = table.NewRow();
                            row["Uid"] = asset.Uid;
                            if (asset.ParentUid.HasValue)
                                row["ParentUid"] = asset.ParentUid;
                            else
                                row["ParentUid"] = DBNull.Value;

                            if (isInsert || asset.Fields.Keys.Any(k => keyFieldsList.Contains(k)))
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
                            create table #GraphAssets ([uid] uniqueidentifier not null, [ParentUid] uniqueidentifier, [UpdatePath] bit not null, [GraphExists] bit, [AssetExists] bit);
                            ", transaction: trans);

                        var bulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.Default, trans)
                        {
                            BatchSize = table.Rows.Count,
                            DestinationTableName = "#GraphAssets",
                            BulkCopyTimeout = 3600
                        };

                        bulkCopy.ColumnMappings.Add("Uid", "Uid");
                        bulkCopy.ColumnMappings.Add("ParentUid", "ParentUid");
                        bulkCopy.ColumnMappings.Add("UpdatePath", "UpdatePath");


                        await bulkCopy.WriteToServerAsync(table);

                        #endregion


                        #region Update Graph Tables

                        //update temp table flags
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
                            update  #GraphAssets set AssetExists = 0 where AssetExists is null;");

                        //update graph records
                        await company.ExecuteAsync(@"
		                    delete  E
		                    from    graph.AssetEdge E
				                    inner join graph.AssetNode N on E.$from_id = N.$node_id or E.$to_id = N.$node_id
                                    inner join #GraphAssets G on G.[uid] = N.[uid] and G.GraphExists = 1 and G.AssetExists = 0
		                    where   N.[uid] = @uid

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

                            ", transaction: trans);


                        //update paths/segments for applicable assets
                        await company.ExecuteAsync(@"
                            	declare @class int = 1,
			                    @predicateType int = 3,
                                @assetTypeId int = 0;

                                select  @class = T.[Class], 
                                        @assetTypeId = T.ID 
                                from    AssetType T 
                                where   T.[uid] = @assetTypeUid;

                                if @class = 2 or @class = 6
	                                begin
		                                declare @hierarchy table (ID bigint, [Level] int, Segments xml, [Path] nvarchar(2500));
		                                set @predicateType = 4;

		                                with p as (
			                                select	A.ID,
					                                A.Object,
					                                A.ObjectID,
					                                cast(null as varchar(50)) as ParentObject,
					                                cast(null as int) as ParentObjectID,
					                                1 as [Level],
					                                cast(graph.GetXmlSegment(cast(null as xml), @assetTypeID, A.ID, 1) as xml) as Segments,
					                                cast(graph.GetPathSegment(A.ID) as nvarchar(2500)) as Segment
			                                from	Asset A
					                                left join [IntersectDetail] I on I.PredicateType = @predicateType and I.Object = A.Object and I.ObjectID = A.ObjectID
                                                    inner join #GraphAssets G on G.[uid] = A.[uid] and G.UpdatePath = 1
			                                where	I.ID is null
			                                union all
			                                select	A.ID,
					                                A.Object,
					                                A.ObjectID,
					                                p.Object as ParentObject,
					                                p.ObjectID as ParentObjectID,
					                                p.[Level]+1 as [Level],
					                                cast(graph.GetXmlSegment(p.Segments, @assetTypeID, A.ID, p.[Level]+1) as xml) as Segments,
					                                cast(p.Segment+'.'+ graph.GetPathSegment(A.ID) as nvarchar(2500)) as Segment
			                                from	Asset A
					                                inner join [IntersectDetail] I on I.PredicateType = @predicateType and I.Object = A.Object and I.ObjectID = A.ObjectID
					                                inner join p on p.Object = I.Subject and p.ObjectID = I.SubjectID
			                                where	A.AssetTypeID = @assetTypeID
					                                and p.[Level] <= 25
		                                )
		                                insert into @hierarchy
			                                select ID, [Level], Segments, Segment from p

		                                delete	T
		                                from	@hierarchy T
				                                left join (
					                                select	ID,
							                                max([Level]) as [Level]
					                                from	@hierarchy
					                                group by	ID
				                                ) S on S.ID = T.ID and S.[Level] = T.[Level]
		                                where	S.ID is null;

		                                update	T 
		                                set		T.[Path] = S.[Path], 
				                                T.[Segments] = S.[Segments] 
		                                from	graph.AssetNode T 
				                                inner join @hierarchy S on S.ID = T.ID;
	                                end
	                                else
	                                begin

		                                declare @h table (ID int, [Level] int)
		                                insert into @h
			                                select ID, [Level] from dbo.GetAssetTypeAncestry(@assetTypeID) order by [Level];

		                                with p2 as (
			                                select	A.AssetTypeID,
					                                A.ID,
					                                A.Object,
					                                A.ObjectID,
					                                cast(null as varchar(50)) as ParentObject,
					                                cast(null as int) as ParentObjectID,
					                                1 as [Level],
					                                cast(graph.GetXmlSegment(cast(null as xml), @assetTypeID, A.ID, 1) as xml) as Segments,
					                                cast(graph.GetPathSegment(A.ID) as nvarchar(2500)) as Segment
			                                from	Asset A
					                                inner join @h H on H.[Level] = 1 and H.ID = A.AssetTypeID
					                                left join PredicateIntersect I on I.PredicateType = @predicateType and I.Object = A.Object and I.ObjectID = A.ObjectID
                                                    inner join #GraphAssets G on G.[uid] = A.[uid] and G.UpdatePath = 1
			                                where	I.IntersectID is null
			                                union all
			                                select	A.AssetTypeID,
					                                A.ID,
					                                A.Object,
					                                A.ObjectID,
					                                p.Object as ParentObject,
					                                p.ObjectID as ParentObjectID,
					                                p.[Level]+1 as [Level],
					                                cast(graph.GetXmlSegment(p.Segments, @assetTypeID, A.ID, p.[Level]+1) as xml) as Segments,
					                                cast(p.Segment+'.'+ graph.GetPathSegment(A.ID) as nvarchar(2500)) as Segment
			                                from	Asset A
					                                inner join PredicateIntersect I on I.PredicateType = @predicateType and I.Object = A.Object and I.ObjectID = A.ObjectID
					                                inner join p2 as p on p.Object = I.Subject and p.ObjectID = I.SubjectID
					                                inner join @h H on H.[Level] = p.[Level]+1 and H.ID = A.AssetTypeID
		                                )

		                                update	T 
		                                set		T.[Path] = S.Segment, 
				                                T.[Segments] = S.[Segments] 
		                                from	graph.AssetNode T 
				                                inner join p2 S on S.AssetTypeID = @assetTypeID and S.ID = T.ID ;
	                                end

                            "
                            , new { assetTypeUid }
                            ,transaction: trans);


                        //cleanup 
                        await company.ExecuteAsync(@"
		                    drop table if exists #GraphAssets;
                            ", transaction: trans);

                        #endregion
                    }

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
