using d360.core;
using d360.core.entities.Metric;
using d360.core.queue;
using d360.extensions.queue;
using igx.jobs.scoreprocessor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    public class MeasureRemovedProcess : ProcessBase, IScoreProcess
    {
        public async Task Run()
        {
            var measureChangedModel = await Storage.DeserializeJsonObjectFromBlobAsync<MeasureRemovedModel>(Info.StorageFolder, Info.StorageFile);

            if (measureChangedModel == null)
            {
                throw new Exception("Cannot load score file from storage");
            }

            if (measureChangedModel.EffectiveEndDate.Date <= DateTime.UtcNow.Date)
            {
                // We can continue processing it.
                var Db = GetCompanyContext();

                var itemsToRescoreQuery = await Db.QueryAsync<MeasureRemovedScoreRequeueDataItem>(@"
declare @today date = cast(getutcdate() as date);

select	distinct
		S.AssetUid
		,A.Uid as MetricAssetUid
		,V.Uid as MetricAssetVersionUid
		,coalesce(SI.Result, 0) as Result
from	metrics.ScoreItem I
		inner join metrics.ScoreItemLink L on L.ScoreItemUid = I.Uid and I.AssetVersionUid = @MetricAssetVersionUid
		inner join metrics.Score S on S.Uid = L.ScoreUid
        inner join metrics.AssetVersion LV on LV.Uid = I.AssetVersionUid 
		inner join metrics.Asset LA on LA.Uid = LV.AssetUid
		inner join metrics.Asset A on A.AllocationUid = LA.AllocationUid and A.Uid <> LA.Uid and A.IsGroup = 0 and (A.ParentUid <> LV.AssetUid or A.ParentUid is null)
		cross apply (
			select	max(EffectiveDate) as EffectiveDate
			from	metrics.AssetVersion
			where	AssetUid = A.Uid
					and [State] = 1 
					and EffectiveDate <= @today 
					and (EffectiveEndDate > @today or EffectiveEndDate is null)
		) MV
		inner join metrics.AssetVersion V on A.Uid = V.AssetUid and V.State = 1 and V.EffectiveDate = MV.EffectiveDate
		outer apply (
			select	II.Value as Result
			from	metrics.ScoreItemLink IL
					inner join metrics.ScoreItem II on IL.ScoreItemUid = II.Uid and IL.ScoreUid = S.Uid and II.AssetVersionUid = V.Uid
		) SI", new { measureChangedModel.MetricAssetVersionUid });
                var itemsToRescore = itemsToRescoreQuery.ToList();

                // Delete asset scores where this measure is the only one that was present and was created today (a one-measure score).
                // Also delete score items linked to these scores that we will be deleting, but are NOT linked to any other (i.e. earlier) scores.
                var cleanupQuery = await Db.QueryMultipleAsync(@"
drop table if exists #Scores;
create table #Scores (Uid uniqueidentifier, EffectiveDate date, EndDate date, OtherMeasuresCount int);
declare @today date = cast(getutcdate() as date);

insert into #Scores
	select	distinct
            L.ScoreUid,
			S.EffectiveDate,
			S.EndDate,
			C.[Count] as OtherMeasuresCount
	from	metrics.ScoreItem I
			inner join metrics.ScoreItemLink L on L.ScoreItemUid = I.Uid and I.AssetVersionUid = @MetricAssetVersionUid
			inner join metrics.Score S on S.Uid = L.ScoreUid and S.EffectiveDate = @today and S.EndDate is null
			cross apply (
				select	count(1) as [Count]
				from	metrics.ScoreItemLink IL
						inner join metrics.ScoreItem II on II.Uid = IL.ScoreItemUid and IL.ScoreUid = S.Uid and IL.ScoreItemUid <> L.ScoreItemUid 
						inner join metrics.AssetVersion IV on IV.Uid = II.AssetVersionUid and IV.State = 1
						inner join metrics.Asset IA on IA.State = 1 and IA.Uid = IV.AssetUid and IA.ParentUid is null
			) C;

-- Delete the link between this measure version and today's score. 
delete	L 
from	metrics.ScoreItemLink L 
		inner join metrics.Score S on S.Uid = L.ScoreUid and S.EffectiveDate = @today and S.EndDate is null 
		inner join metrics.ScoreItem I on I.Uid = L.ScoreItemUid and I.AssetVersionUid = @MetricAssetVersionUid; 

-- End-date active scores that are prior to current UTC.
update  T 
set     T.EndDate = dateadd(dd, -1, @today) 
from	metrics.Score T 
		inner join #Scores S on S.Uid = T.Uid and S.EffectiveDate < @today; 

-- Delete asset scores where this measure is the only one that was present and was created today (a one-measure score).
delete  T 
from	metrics.Score T 
		inner join #Scores S on S.Uid = T.Uid and S.OtherMeasuresCount = 0 and S.EffectiveDate = @today;

select Uid from #Scores where EffectiveDate < @today; 
select Uid from #Scores where OtherMeasuresCount = 0 and EffectiveDate = @today;", new { measureChangedModel.MetricAssetVersionUid });

                var ended = cleanupQuery.Read<Guid>().ToList();
                var deleted = cleanupQuery.Read<Guid>().ToList();

                // Log end-dates.
                await Db.SaveScoreProcessingResultsAsync(Info.ExecutionUid, Info.ChangeType, "EndDateScores", ended, Info.StartedOn);

                // Log deletions.
                await Db.SaveScoreProcessingResultsAsync(Info.ExecutionUid, Info.ChangeType, "DeleteSamesDayScores", deleted, Info.StartedOn);

                if (itemsToRescore.Count > 0)
                {
                    var list = itemsToRescore.GroupBy(i => i.AssetUid)
                        .Select(item => new AssetMeasureModel
                        {
                            AssetUid = item.Key,
                            EffectiveDate = DateTime.UtcNow,
                            Measures = item.Select(m => new AssetMeasureChildModel { 
                                    MetricAssetUid = m.MetricAssetUid, MetricAssetVersionUid = m.MetricAssetVersionUid, Result = m.Result }
                                ).ToList()
                        }).ToList();

                    if (list.Count > 0)
                    {
                        var secondsToAdd = new Random().Next(10, 50);
                        var timespan = new TimeSpan(0, 0, secondsToAdd);
                        Db.SendScoreEventWithPayload(ScoreQueueChangeType.AssetMeasures, list, Info.ExecutionUid, timespan);
                    }
                }
            }
            else
            {
                // Set a delay of processing until we reach this effective date.
                var queue = new AzureQueueSource();
                var timespan = measureChangedModel.EffectiveEndDate.Date.Subtract(DateTime.UtcNow.Date.AddDays(1));
                var minutesToAdd = new Random().Next(2, 7);
                timespan = timespan.Add(new TimeSpan(0, minutesToAdd, 0));
                await queue.CreateMessageAsync(Config.GetValue<string>("ScoringQueue"), Info, timespan);
            }
        }
    }
}
