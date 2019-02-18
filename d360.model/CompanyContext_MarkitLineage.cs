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
using System.Data.SqlClient;
using Newtonsoft.Json;

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
        private object processLock = new object();
        private object mappingLock = new object();
        private const int  numThreads = 8;
        private const int maxComplexity = 50;

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

            var mapCount = maps.Count();

            client.TrackEvent($"Loaded {mapCount} Markit Map Records");

            //find source records to start from
            var leftmostMaps = maps.Where(m => m.SourceAssetID != null).ToList();
            var mappings = new List<FusionMarkitSourceTargetMapping>();

            //build map dictionary
            Dictionary<int, List<FusionMarkitLineageData>> sourceMap = new Dictionary<int, List<FusionMarkitLineageData>>();
            foreach(var map in maps)
            {
                if (map.SourceFusionAttributeID == null)
                    continue;
                if (!sourceMap.ContainsKey((int)map.SourceFusionAttributeID))
                {
                    sourceMap.Add((int)map.SourceFusionAttributeID, new List<FusionMarkitLineageData>() { map });
                }
                else
                {
                    var list = sourceMap[(int)map.SourceFusionAttributeID];
                    list.Add(map);
                }
            }

            Parallel.ForEach(leftmostMaps, new ParallelOptions { MaxDegreeOfParallelism = numThreads }, currentMap =>
             {
                 UpdateSourceTargetObjectMap(
                    sourceMap,
                    maps,
                    currentMap,
                    mappings,
                    new HashSet<int>(),
                    new Stack<FusionMarkitParentMapping>(),
                    currentMap,
                    objectMaps,
                    objectMaps.Where(o => o.FusionAttributeID == currentMap.SourceFusionAttributeID).ToList());
             });


            if (Database.Connection.State != ConnectionState.Open)
                Database.Connection.Open();

            await Database.ExecuteSqlCommandAsync("truncate table [fusion].[MarkitLineageMapping]");

            //save mappings to a table
            await SaveMarkitLineageResults(mappings);

            client.TrackEvent($"Saved mapping {mappings.Count()} records to table for lineage processing");

            await Database.ExecuteSqlCommandAsync("[fusion].[GenerateMarkitMapLineage]");

            client.TrackEvent($"Completed lineage generation for company id {CurrentCompanyID}");


            await SaveMarkitLineageRunDetails(maps.Count(), mappings.Count(), objectMaps.Count());

        }


        private void UpdateSourceTargetObjectMap(
            Dictionary<int, List<FusionMarkitLineageData>> sourceMap,
            IEnumerable<FusionMarkitLineageData> maps,
            FusionMarkitLineageData currentMap,
            List<FusionMarkitSourceTargetMapping> mappings,
            HashSet<int> processedList,
            Stack<FusionMarkitParentMapping> sourceAssets,
            FusionMarkitLineageData root,
            IEnumerable<FusionMarkitObjectMapping> objectMaps,
            List<FusionMarkitObjectMapping> businessMaps,
            int complexity = 0)
        {
            if (complexity > maxComplexity)
                return;

            List<FusionMarkitLineageData> nextMaps = new List<FusionMarkitLineageData>();

            lock (processLock)
            {
                
                if (processedList.Any(p => currentMap.ID == p)
                    || maps.Any(m => processedList.Contains(m.ID)
                    && m.TargetFusionAttributeID == currentMap.SourceFusionAttributeID
                    && m.SourceFusionAttributeID == currentMap.TargetFusionAttributeID))
                    return;

                //add current item to list of processed nodes
                processedList.Add(currentMap.ID);
                
                //get next items to process
                if (currentMap != null && currentMap.TargetFusionAttributeID != null && sourceMap.ContainsKey((int)currentMap.TargetFusionAttributeID))
                    nextMaps = sourceMap[(int)currentMap.TargetFusionAttributeID].Where(m => !processedList.Contains(m.ID)).ToList();
            }

            //find a business object mapping if we don't already have one
            var newBussinesMap = objectMaps.FirstOrDefault(o => o.FusionAttributeID == currentMap.SourceFusionAttributeID && !businessMaps.Any(b => b.FusionAttributeID == o.FusionAttributeID && b.ObjectAssetID == o.ObjectAssetID));

            if (newBussinesMap != null)
                businessMaps.Add(newBussinesMap);

            if (currentMap.SourceAssetID != null && (sourceAssets.Any() ? sourceAssets.Peek().AssetID != currentMap.SourceAssetID : true))
                sourceAssets.Push(new FusionMarkitParentMapping { MapID = currentMap.ID, FusionAttributeID = (int)currentMap.SourceFusionAttributeID, AssetID = (long)currentMap.SourceAssetID });

            //if we have a source/target pair we might have a valid mapping
            if (sourceAssets.Any() && currentMap.TargetAssetID != null)
            {
                //need to check final target too
                var targetBusinessMap = objectMaps.FirstOrDefault(o => o.FusionAttributeID == currentMap.TargetFusionAttributeID && !businessMaps.Any(b => b.FusionAttributeID == o.FusionAttributeID && b.ObjectAssetID == o.ObjectAssetID));

                if (targetBusinessMap != null)
                    businessMaps.Add(targetBusinessMap);

                if (businessMaps.Any())
                {
                    var current = new FusionMarkitParentMapping
                    {
                        MapID = currentMap.ID,
                        FusionAttributeID = (int)currentMap.TargetFusionAttributeID,
                        AssetID = (long)currentMap.TargetAssetID
                    };

                    //create mappings for the accumulated sources
                    while(sourceAssets.Any())
                    {
                        var previous = sourceAssets.Pop();

                        if (previous.FusionAttributeID == current.FusionAttributeID || previous.AssetID == current.AssetID)
                        {
                            current = previous;
                            continue;
                        }

                        if (sourceAssets.Any())
                        {
                            lock (mappingLock)
                            {
                                var businessMap = businessMaps.First();
                                if (!mappings.Any(m => m.MapID == current.MapID && m.SourceFusionAttributeID == previous.FusionAttributeID
                                   && m.TargetFusionAttributeID == current.FusionAttributeID && m.SourceAssetID == previous.AssetID && m.TargetAssetID == current.AssetID
                                   && m.ObjectAssetID == businessMap.ObjectAssetID))
                                {
                                    mappings.Add(new FusionMarkitSourceTargetMapping
                                    {
                                        MapID = current.MapID,
                                        SourceFusionAttributeID = (int)previous.FusionAttributeID,
                                        TargetFusionAttributeID = (int)current.FusionAttributeID,
                                        SourceAssetID = (long)previous.AssetID,
                                        TargetAssetID = (long)current.AssetID,
                                        ObjectAssetID = businessMap.ObjectAssetID
                                    });
                                }
                            }
                        }
                        else
                        {
                            foreach (var businessMap in businessMaps)
                            {
                                lock(mappingLock)
                                {
                                    //if the mapping doesn't exist create it
                                    if (!mappings.Any(m => m.MapID == current.MapID && m.SourceFusionAttributeID == previous.FusionAttributeID
                                    && m.TargetFusionAttributeID == current.FusionAttributeID && m.SourceAssetID == previous.AssetID && m.TargetAssetID == current.AssetID
                                    && m.ObjectAssetID == businessMap.ObjectAssetID))
                                    {
                                        mappings.Add(new FusionMarkitSourceTargetMapping
                                        {
                                            MapID = current.MapID,
                                            SourceFusionAttributeID = (int)previous.FusionAttributeID,
                                            TargetFusionAttributeID = (int)current.FusionAttributeID,
                                            SourceAssetID = (long)previous.AssetID,
                                            TargetAssetID = (long)current.AssetID,
                                            ObjectAssetID = businessMap.ObjectAssetID
                                        });
                                    }
                                }
                            }
                        }                  
                        current = previous;                         
                    }
                }
            }

            //process the next nodes in the lineage
            if (nextMaps.Count() == 1)
                UpdateSourceTargetObjectMap(sourceMap, maps, nextMaps.First(), mappings, processedList, sourceAssets, root, objectMaps, businessMaps, ++complexity);
            else
                foreach (var next in nextMaps)
                    UpdateSourceTargetObjectMap(sourceMap, maps, next, mappings, new HashSet<int>(processedList), new Stack<FusionMarkitParentMapping>(sourceAssets), root, objectMaps, new List<FusionMarkitObjectMapping>(businessMaps), complexity + nextMaps.Count());
        }

        private async Task SaveMarkitLineageRunDetails(int rows, int techRows, int businessRows)
        {
            var sql = $"insert into fusion.MarkitLineageHistory values(@rows,@techLineageRows,@businessLineageRows,@dt);";
            var now = DateTime.UtcNow;
            await Database.Connection.ExecuteAsync(sql, new { rows, techLineageRows = techRows, businessLineageRows = businessRows, dt = now });
        }

        private async Task SaveMarkitLineageResults(List<FusionMarkitSourceTargetMapping> mappings)
        {
            using (var conn = new SqlConnection(CompanyConnectionString))
            {
                conn.Open();
                
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                {
                    var columnList = new List<string>()
                    {
                        "MapID",
                        "SourceFusionAttributeID",
                        "TargetFusionAttributeID",
                        "SourceAssetID",
                        "TargetAssetID",
                        "ObjectAssetID",
                        "SourceIntersectID",
                        "TargetIntersectID",
                        "MapItemID",
                    };

                    DataTable dt = new DataTable();
                    columnList.ForEach(c => dt.Columns.Add(c));

                    mappings.ForEach(m =>
                    {
                        var json = JsonConvert.SerializeObject(m);
                        var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                        var row = dt.NewRow();
                        foreach(var c in columnList)
                        {
                            if (dictionary.ContainsKey(c))
                                row[c] = dictionary[c];
                            else
                                row[c] = null;
                        }

                        dt.Rows.Add(row);

                    });

                    bulkCopy.DestinationTableName = "[fusion].[MarkitLineageMapping]";
                    await bulkCopy.WriteToServerAsync(dt);
                    
                }
            }

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
                          ,[UpdatedOn]
                      FROM [fusion].[MarkitLineageData]
";

            return await QueryAsync<core.entities.FusionMarkitLineageData>(sql);
        }
    }
}
