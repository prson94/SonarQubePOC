using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace d360.model
{
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

            client.TrackEvent($"Loaded {maps.Count()} Markit Map Records");
            
            // parse through the maps 


            // save the results

        }

        private async Task<bool> MarkitRequiresRun()
        {
            // is htere any data? and what is the date its for            
            var dataDate = await (Database.Connection.QueryFirstOrDefaultAsync<DateTime>("select max(updatedon) from [fusion].MarkitLineageData"));

            if (dataDate == DateTime.MinValue)
                return false;

            // compare max last run to date in                
            var lastRun = await Database.Connection.QueryFirstOrDefaultAsync<DateTime>("select max(completedOn) from [fusion].MarkitLineageHistory");

            // has data but never run
            if (lastRun == DateTime.MinValue)
                return true;

            if (dataDate > lastRun) return true;

            return false;
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
