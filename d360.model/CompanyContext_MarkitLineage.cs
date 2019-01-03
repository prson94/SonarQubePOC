using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.Data.Common;

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

            Dictionary<MarkitObject, List<int>> objectMapDictionary = new Dictionary<MarkitObject, List<int>>(new MarkitObjectComparer());

            // iterate through the object
            int previousObjectId = -1;
            foreach (var obj in objectMaps)
            {
                var markitObject = new MarkitObject { Object = obj.Object, ObjectID = obj.ObjectID };

                if (obj.ObjectID != previousObjectId)
                {
                    objectMapDictionary[markitObject] = new List<int>{ obj.MapID};                    
                }
                else
                {
                    //add to existing.
                    objectMapDictionary[markitObject].Add(obj.MapID);
                }
                previousObjectId = obj.ObjectID;
            }

            var rows = maps.Count();
            client.TrackEvent($"Loaded {rows} Markit Map Records");

            // get all the business terms that we need to generate the business lineage for as these will be the items we need to
            // travers the graphs for

            foreach (var item in objectMapDictionary)
            {
                //find any items that connect to this business term
                foreach (var map in item.Value)
                {
                    var mp = maps.Where(x => x.ID == map);
                }
            }
           

            // create a hash that has the source fusion attribute for easy find
            // create a hash that has the target fusion attribute for easy find
           // Dictionary<int, MarkitMapRowData> sourceFusionDictionary = new Dictionary<int, MarkitMapRowData>();
            //Dictionary<int, MarkitMapRowData> targetFusionDictionary = new Dictionary<int, MarkitMapRowData>();

            // start a transaction

            using (var transaction = Database.Connection.BeginTransaction())
            {
                // save the results
                await SaveMarkitLineageResults(transaction);

                // save the run information
                await SaveMarkitLineageRunDetails(rows,rows,0,transaction);

                transaction.Commit();
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

        private async Task<IEnumerable<core.entities.FusionMarkitLineageMapToBusinessItems>> LoadMarkitMapItemMapData()
        {
            var sql = @"SELECT  
                            MapID,
                            [Object],
                            ObjectID                         
                      FROM [fusion].[MarkitLineageMapToBusinessItems] order by [object], ObjectID";

            return await Database.Connection.QueryAsync<core.entities.FusionMarkitLineageMapToBusinessItems>(sql);
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
                          ,[Source]
                          ,[SourceID]
                          ,[Target]
                          ,[TargetID]
                          ,[UpdatedOn]
                      FROM [fusion].[MarkitLineageData]";

            return await QueryAsync<core.entities.FusionMarkitLineageData>(sql);
        }
    }
}
