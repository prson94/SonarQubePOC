using d360.core;
using d360.core.entities.Metric;
using d360.core.queue;
using d360.extensions.queue;
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
                var assetUidsQuery = await Db.QueryAsync<Guid>(@"
select	S.AssetUid
from	metrics.ScoreItem I
		inner join metrics.ScoreItemLink L on L.ScoreItemUid = I.Uid and I.AssetVersionUid = @MetricAssetVersionUid
		inner join metrics.Score S on S.Uid = L.ScoreUid and S.EndDate is null
group by S.AssetUid", new { measureChangedModel.MetricAssetVersionUid });
                var assetUids = assetUidsQuery.ToList();

                if (assetUids.Count > 0)
                {
                    var list = assetUids
                        .Select(uid => new AssetMeasureModel
                        {
                            AssetUid = uid,
                            EffectiveDate = DateTime.UtcNow,
                            Measures = new List<AssetMeasureChildModel>() {
                                            new AssetMeasureChildModel { MetricAssetUid = measureChangedModel.MetricAssetUid, MetricAssetVersionUid = measureChangedModel.MetricAssetVersionUid, Result = false }
                             }
                        })
                        .ToList();

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
