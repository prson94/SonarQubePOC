using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.queue;
using igx.jobs.scoreprocessor.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    public class CheckTypeDependencyRemovedProcess : ProcessBase, IScoreProcess
    {
        public async Task Run()
        {
            var model = await Storage.DeserializeJsonObjectFromBlobAsync<CheckTypeDependencyRemovedModel>(Info.StorageFolder, Info.StorageFile);

            if (model == null)
            {
                throw new ArgumentNullException("model","Cannot load score file from storage");
            }

            // We can continue processing it.
            var Db = GetCompanyContext();

            string endScoreSql = @"
create table #Scores (ScoreUid uniqueidentifier);

insert into #Scores
	select	distinct
			S.Uid
	from	metrics.AssetVersion V
			inner join metrics.ScoreItem I on  I.AssetVersionUid = V.Uid
			inner join metrics.ScoreItemLink L on L.ScoreItemUid = I.Uid 
			inner join metrics.Score S on S.Uid = L.ScoreUid and S.EndDate is null
			cross apply (
				select	count(1) as [Count]
				from	metrics.ScoreItemLink IL
				where	IL.ScoreUid = S.Uid and IL.ScoreItemUid <> L.ScoreItemUid
			) Cnt
    where   Cnt.[Count] = 0
            and V.Uid in @VersionUids;

declare @date date = getutcdate();

update	T
set		T.EndDate = @date
from	metrics.Score T
		inner join #Scores S on S.ScoreUid = T.Uid;

select ScoreUid from #Scores;";

            var impactedAssetMeasureSql = @"
select	distinct
	    S.AssetUid,
	    A2.Uid as MetricAssetUid,
	    V2.Uid as MetricAssetVersionUid,
	    I2.Value as Result
from	metrics.AssetVersion V
		inner join metrics.ScoreItem I on  I.AssetVersionUid = V.Uid
		inner join metrics.ScoreItemLink L on L.ScoreItemUid = I.Uid 
		inner join metrics.Score S on S.Uid = L.ScoreUid and S.EndDate is null
		inner join metrics.ScoreItemLink L2 on L2.ScoreUid = S.Uid and L2.ScoreItemUid <> I.Uid
		inner join metrics.ScoreItem I2 on I2.Uid = L2.ScoreItemUid
		inner join metrics.AssetVersion V2 on V2.Uid = I2.AssetVersionUid and V2.State = 1
		inner join metrics.Asset A2 on A2.State = 1 and A2.Uid = V2.AssetUid and A2.IsGroup = 0
where   V.Uid in @VersionUids;";

            // End-date asset scores where this measure is the only one that was present (a one-measure score).
            var endedScores = await Db.QueryAsync<Guid>(endScoreSql, new { model.VersionUids });
                
            // Log the scores end-dates.
            await Db.SaveScoreProcessingResultsAsync(Info.ExecutionUid, Info.ChangeType, "EndDateScores", endedScores, Info.StartedOn);

            var itemsToRescoreQuery = await Db.QueryAsync<MeasureRemovedScoreRequeueDataItem>(impactedAssetMeasureSql, new { model.VersionUids });
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
                    Db.SendScoreEventWithPayload(ScoreQueueChangeType.AssetMeasures, list, Info.ExecutionUid);
                }
            }
        }
    }
}
