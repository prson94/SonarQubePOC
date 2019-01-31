using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.Data.Common;
using d360.core.entities;
using System.Data;

namespace d360.model
{
    public class MarkitMapRow
    {
        public int SourceFusionAttributeID { get; set; }
        public int TargetFusionAttributeID { get; set; }
        public Tuple<string, int> SourceObject { get; set; }
        public Tuple<string, int> TargetObject { get; set; }
    }

    public class MarkitObject
    {
        public int ObjectID { get; set; }
        public string Object { get; set; }
    }

    public class MarkitObjectComparer : IEqualityComparer<MarkitObject>
    {
        public bool Equals(MarkitObject x, MarkitObject y)
        {
            return x.ObjectID == y.ObjectID && (string.Compare(x.Object, y.Object, true) == 0);
        }

        public int GetHashCode(MarkitObject obj)
        {
            return obj.ObjectID.GetHashCode() + obj.Object.GetHashCode();
        }
    }



    partial class CompanyContext : BaseContext
    {
        
        public async Task GenerateMarkitBusinessLineage()
        {
            TelemetryClient client = new TelemetryClient();

            //check if anything has changed since last run
            if (!(await MarkitRequiresRun()))
            {
                return;
            }

            client.TrackEvent($"Markit data requires processing on company {CurrentCompanyID}");

            var maps = await LoadMarkitMapRawData();
            var objectMaps = await LoadMarkitMapItemMapData();

            var rows = maps.Count();
            client.TrackEvent($"Loaded {rows} Markit Map Records");

            //find source/target mappings by traversing possible paths along the technical lineage
            var leftmostMaps = maps.Where(m => !maps.Any(n => n.TargetFusionAttributeID == m.SourceFusionAttributeID)).ToList();
            var mappings = new List<FusionMarkitSourceTargetMapping>();
            foreach(var map in leftmostMaps)
                UpdateSourceTargetObjectMap(maps.ToList(), map, mappings, new List<int>(), map.SourceAssetID, map, objectMaps.ToList()
                    ,objectMaps.FirstOrDefault(o => o.FusionAttributeID == map.SourceFusionAttributeID), false);

            var asdf = mappings.Where(m => m.ObjectAssetID != 0).ToList();

         
            // start a transaction

            if (Database.Connection.State != ConnectionState.Open)
                Database.Connection.Open();

            using (var transaction = Database.Connection.BeginTransaction())
            {
                // save the results
                await SaveMarkitLineageResults(transaction);

                // save the run information
                await SaveMarkitLineageRunDetails(rows,rows,0,transaction);

                transaction.Commit();
            }

        }


        private void UpdateSourceTargetObjectMap(List<FusionMarkitLineageData> maps, FusionMarkitLineageData currentMap, List<FusionMarkitSourceTargetMapping> mappings, List<int> processedList, long? sourceAssetId, FusionMarkitLineageData root, List<FusionMarkitObjectMapping> objectMaps, FusionMarkitObjectMapping rootObjectMap, bool skipMap = false)
        {
            processedList.Add(currentMap.ID);
            var nextMaps = maps.Where(m => m.SourceFusionAttributeID == currentMap?.TargetFusionAttributeID && !processedList.Contains(m.ID));
            var skipNextMap = false;

            if (rootObjectMap == null)
                rootObjectMap = objectMaps.FirstOrDefault(o => o.FusionAttributeID == currentMap.SourceFusionAttributeID);

            if (sourceAssetId == null)
                sourceAssetId = currentMap.SourceAssetID;
            if (skipMap)
                sourceAssetId = null;
            if (sourceAssetId != null && currentMap.TargetAssetID != null && currentMap.TargetAssetID != sourceAssetId)
            {
                if (!skipMap)
                {
                    if (nextMaps.Count() < 1 && rootObjectMap == null)
                        rootObjectMap = objectMaps.FirstOrDefault(o => o.FusionAttributeID == currentMap.TargetFusionAttributeID);

                    mappings.Add(new FusionMarkitSourceTargetMapping
                    {
                        MapID = currentMap.ID,
                        SourceFusionAttributeID = (int)currentMap.SourceFusionAttributeID,
                        TargetFusionAttributeID = (int)currentMap.TargetFusionAttributeID,
                        SourceAssetID = (long)sourceAssetId,
                        TargetAssetID = (long)currentMap.TargetAssetID,
                        ObjectAssetID = (rootObjectMap == null ? 0 : rootObjectMap.ObjectAssetID),
                        UltimateParent = root
                    });

                    skipNextMap = true;
                }

                sourceAssetId = null;
                rootObjectMap = null;
            }


            foreach(var next in nextMaps)
                UpdateSourceTargetObjectMap(maps, next, mappings, processedList, sourceAssetId, root, objectMaps, rootObjectMap, skipNextMap);



           
        }

        private void GenerateBusinessLineageForObject(KeyValuePair<MarkitObject, List<int>> item, IEnumerable<FusionMarkitLineageData> maps)
        {
            if (item.Value == null || item.Value.Count == 0) return;

            var mapValues = new List<int>(item.Value);

            // look for first map record we can find in maps
            FusionMarkitLineageData initialMap = maps.FirstOrDefault(x => x.ID == mapValues[0]);

            if (initialMap == null) return;

            //remove the first item from mapvalues since we already found it
            mapValues.RemoveAt(0);

            GenerateMarkitBusinessLineageImpl(null, null, initialMap, mapValues, item.Key, maps);
        }

        private void GenerateMarkitBusinessLineageImpl(string obj, int? objectId, FusionMarkitLineageData currentMap, List<int> mapValues, MarkitObject key, IEnumerable<FusionMarkitLineageData> maps)
        {
            Queue<FusionMarkitLineageData> mapQueue = new Queue<FusionMarkitLineageData>();

            mapQueue.Enqueue(currentMap);

            currentMap.Visited = true;

            while(mapQueue.Count != 0)
            {
                var front = mapQueue.Dequeue();
                var parent = mapQueue.Peek();
                // if source or target is null
                if (!front.TargetAssetID.HasValue || !front.SourceAssetID.HasValue)
                {
                    if (string.IsNullOrEmpty(front.Target))
                    {
                        front.Target = front.Source;
                        front.TargetID = front.SourceID;
                    }
                    else
                    {
                        front.Source = front.Target;
                        front.SourceID = front.TargetID;
                    }
                }

                //mark item as visited
                front.Visited = true;

                //look for all adjacent verticies if not visited add toe the queue
                // this needs to be optimized to be binary search / hash table
                //left
                var leftMaps = maps.Where(x => x.TargetFusionAttributeID == front.SourceFusionAttributeID);

                foreach (var map in leftMaps)
                {
                    if(!map.Visited)
                        mapQueue.Enqueue(map);
                }

                //right
                var rightMaps = maps.Where(x => x.SourceFusionAttributeID == front.TargetFusionAttributeID);

                foreach (var map in rightMaps)
                {
                    if (!map.Visited)
                        mapQueue.Enqueue(map);
                }
            }
        }

        private async Task SaveMarkitLineageRunDetails(int rows, int techRows, int businessRows, DbTransaction transaction)
        {
            var sql = $"insert into fusion.MarkitLineageHistory values(@rows,@techLineageRows,@businessLineageRows,@dt);";
            var now = DateTime.UtcNow;
            await Database.Connection.ExecuteAsync(sql, new { rows, techLineageRows = techRows, businessLineageRows = businessRows, dt = now }, transaction);
        }

        private async Task SaveMarkitLineageResults(DbTransaction transaction)
        {
            //create temp tables to connect the records
            // temp table for storing mapruleitem id to map table id
            await Database.Connection.ExecuteAsync(@"IF OBJECT_ID('tempdb..#MapRuleItemIDList') IS NOT NULL
			                                DROP TABLE #MapRuleItemIDList;

		                                create table #MapRuleItemIDList (    
                                            MapRuleItemID int not null,
			                                MapID Int
		                                );
                                ", transaction: transaction);

            await Database.Connection.ExecuteAsync(@"IF OBJECT_ID('tempdb..#MapItemIDList') IS NOT NULL
			                                            DROP TABLE #MapItemIDList;

		                                create table #MapItemIDList (    
                                            MapItemID int, 
                                            sourceintersectid int, 
                                            targetintersectid int
		                                );
                                ", transaction: transaction);


            await SaveMarkitTechLineage(transaction);
            

            /*
             -- output the results into proper tables here
	

	
	update T
	set T.MapItemID = mi.ID
	from #objectmap T
		inner join mapitem mi on(T.sourceintersectid = mi.SourceIntersectID and T.targetintersectid = mi.TargetIntersectID and mi.[Owner] = 'MARKIT LINEAGE'); 

	
	MERGE
	INTO    mapitem mi
	USING   (			
			select distinct sourceintersectid, targetintersectid FROM #objectmap where (sourceintersectid is not null and targetintersectid is not null) and sourceintersectid != targetintersectid and mapitemid is null
			) S
	ON      (1 = 0)
	WHEN NOT MATCHED THEN
	INSERT  (SourceIntersectID, TargetIntersectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
	VALUES  (S.sourceintersectid, S.targetintersectid, 0, getutcdate(), 0, getutcdate(), 'MARKIT LINEAGE')
	OUTPUT  INSERTED.ID, S.sourceintersectid, S.targetintersectid into @MapItemIDList;

	
	update T
	set T.mapitemid = MI.MapItemID
	from #objectmap T
		inner join @MapItemIDList MI on (MI.sourceintersectid = T.sourceintersectid and MI.targetintersectid = T.targetintersectid)
		
	
	delete from mapitem where [owner] = 'MARKIT LINEAGE' and id not in (select mapitemid from #objectmap);
	
	
	update T
	set T.mapruleitemid = S.id
	from #maps T
		inner join [dbo].[mapruleitem] S on (S.[owner] = 'MARKIT LINEAGE' and S.SourceFusionAttributeID = T.SourceFusionAttributeID and S.TargetFusionAttributeID = T.TargetFusionAttributeID);
	
	
	
	
	
	update T
	set T.MapRuleItemID = MI.MapRuleItemID
	from #maps T
		inner join @MapRuleItemIDList MI on (MI.MapID = T.ID);

	
	delete from mapruleitem where [owner] = 'MARKIT LINEAGE' and id not in (select MapRuleItemID from #maps);
			
	
	insert into mapruleitemmapitem 
		(MapRuleItemID, MapItemID, [Owner])
		SELECT distinct M.MapRuleItemID, OM.MapItemID , 'MARKIT LINEAGE'
		FROM #maps M 
		inner join #objectmap OM on(M.ID = OM.MapID)
		where M.MapRuleItemID is not null and OM.MapItemID is not null;	

             */
        }

        private async Task SaveMarkitTechLineage(DbTransaction transaction)
        {
            // remove any prior tech lineage data
            await Database.Connection.ExecuteAsync("delete from mapruleitemmapitem where [owner] = 'MARKIT LINEAGE';", transaction:transaction);

            await Database.Connection.ExecuteAsync(@"
                MERGE
	            INTO    mapruleitem mri
	            USING   (
			            select SourceFusionAttributeID, TargetFusionAttributeID, ID from [fusion].MarkitLineageData where mapruleitemid is null
			            ) S
	            ON      (1 = 0)
	            WHEN NOT MATCHED THEN
	            INSERT  (SourceFusionAttributeID, TargetFusionAttributeID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
	            VALUES  (S.SourceFusionAttributeID, S.TargetFusionAttributeID, 0, getutcdate(), 0, getutcdate(), 'MARKIT LINEAGE')
	            OUTPUT  INSERTED.ID, S.ID into #MapRuleItemIDList;
            ", transaction: transaction);

        }

        private async Task<bool> MarkitRequiresRun()
        {
            // is htere any data? and what is the date its for            
            var dataDate = await (Database.Connection.QueryFirstOrDefaultAsync<DateTime?>("select max(updatedon) from [fusion].MarkitLineageData"));

            if (!dataDate.HasValue || dataDate.Value == DateTime.MinValue)
                return false;

            // compare max last run to date in                
            var lastRun = (await Database.Connection.QueryFirstAsync<DateTime?>("select max(completedOn) from [fusion].MarkitLineageHistory where completedOn is not null"));

            // has data but never run
            if (!lastRun.HasValue || lastRun.Value == DateTime.MinValue)
                return true;

            if (dataDate.Value > lastRun.Value) return true;

            return false;
        }

        private async Task<IEnumerable<core.entities.FusionMarkitObjectMapping>> LoadMarkitMapItemMapData()
        {
            var sql = @"SELECT  
                           FusionAttributeID,
                           ObjectAssetID
                      FROM [fusion].[MarkitLineageMapToBusinessItems]";

            return await Database.Connection.QueryAsync<core.entities.FusionMarkitObjectMapping>(sql);
        }

        private async Task<IEnumerable<core.entities.FusionMarkitLineageData>> LoadMarkitMapRawData()
        {
            var sql = @"SELECT [ID]
                          ,[MapRuleItemID]
                          ,[ParentID]
                          ,[UltimateParentID]
                          ,[Level]
                          ,[SourceFusionAttributeID]
                          ,[SourceFusionAttributeTypeID]
                          ,[SourceObject]
                          ,[SourceParentObject]
                          ,[SourceParentObjectFusionAttributeID]
                          ,[SourceParentObjectFusionAttributeTypeID]
                          ,[TargetFusionAttributeID]
                          ,[TargetFusionAttributeTypeID]
                          ,[TargetObject]
                          ,[TargetParentObject]
                          ,[TargetParentObjectFusionAttributeID]
                          ,[TargetParentObjectFusionAttributeTypeID]
                          ,[SourceAssetID]
                          ,[TargetAssetID]
                          ,[Processed]
                          ,[UpdatedOn]
                      FROM [fusion].[MarkitLineageData]
";

            return await QueryAsync<core.entities.FusionMarkitLineageData>(sql);
        }
    }
}
