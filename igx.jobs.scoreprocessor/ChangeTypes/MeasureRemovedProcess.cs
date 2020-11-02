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

            if (measureChangedModel.EffectiveEndDate.Date < DateTime.UtcNow.Date)
            {
                // We can continue processing it.
                var Db = GetCompanyContext();

                // End-date asset scores where this measure is the only one that was present (a one-measure score).
                var endedScores = await Db.QueryAsync<Guid>(@"
create table #Scores (ScoreUid uniqueidentifier);

insert into #Scores
	select	distinct
			S.Uid
	from	metrics.ScoreItem I
			inner join metrics.ScoreItemLink L on L.ScoreItemUid = I.Uid and I.AssetVersionUid = @MetricAssetVersionUid
			inner join metrics.Score S on S.Uid = L.ScoreUid and S.EndDate is null
			left join metrics.ScoreItemLink L2 on L2.ScoreUid = S.Uid and L2.ScoreItemUid <> I.Uid
			left join metrics.ScoreItem I2 on I2.Uid = L2.ScoreItemUid
			left join metrics.AssetVersion V on V.Uid = I2.AssetVersionUid and V.State = 1
			left join metrics.Asset A on A.State = 1 and A.Uid = V.AssetUid and A.IsGroup = 0
	where	A.Uid is null 
            or V.EffectiveEndDate is not null;

declare @date date = getutcdate();

update	T
set		T.EndDate = @date
from	metrics.Score T
		inner join #Scores S on S.ScoreUid = T.Uid;

select ScoreUid from #Scores;", new { measureChangedModel.MetricAssetVersionUid });
                
                // Log the scores end-dates.
                await Db.SaveScoreProcessingResultsAsync(Info.ExecutionUid, Info.ChangeType, "EndDateScores", endedScores, Info.StartedOn);

                var itemsToRescoreQuery = await Db.QueryAsync<MeasureRemovedScoreRequeueDataItem>(@"
select	distinct
		S.AssetUid,
		A.Uid as MetricAssetUid,
		V.Uid as MetricAssetVersionUid,
		I2.Value as Result
from	metrics.ScoreItem I
		inner join metrics.ScoreItemLink L on L.ScoreItemUid = I.Uid and I.AssetVersionUid = @MetricAssetVersionUid
		inner join metrics.Score S on S.Uid = L.ScoreUid and S.EndDate is null
		inner join metrics.ScoreItemLink L2 on L2.ScoreUid = S.Uid and L2.ScoreItemUid <> I.Uid
		inner join metrics.ScoreItem I2 on I2.Uid = L2.ScoreItemUid
		inner join metrics.AssetVersion V on V.Uid = I2.AssetVersionUid and V.State = 1
		inner join metrics.Asset A on A.State = 1 and A.Uid = V.AssetUid and A.IsGroup = 0", new { measureChangedModel.MetricAssetVersionUid });
                var itemsToRescore = itemsToRescoreQuery.ToList();

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
                        Db.SendScoreEventWithPayload(Info.ExecutionUid, ScoreQueueChangeType.AssetMeasures, list, Info.StartedOn);
                    }
                }
            }
            else
            {
                // Set a delay of processing until we reach this effective date.
                var queue = new AzureQueueSource();
                var timespan = measureChangedModel.EffectiveEndDate.Date.Subtract(DateTime.UtcNow.Date.AddDays(1));
                await queue.CreateMessageAsync(Config.GetValue<string>("ScoringQueue"), Info, timespan);
            }
        }
    }
}
