using d360.core;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.queue;
using d360.extensions.queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    internal class RuleAssetRemovedDbModel
    {
        public Guid AssetUid { get; set; }
        public DateTime EffectiveDate { get; set; }
        public Guid MetricAssetVersionUid { get; set; }
        public Guid MetricAssetUid { get; set; }
    }

    public class RuleAssetRemovedProcess: ProcessBase, IScoreProcess
    {
        public async Task Run()
        {
            var model = await Storage.DeserializeJsonObjectFromBlobAsync<RuleAssetRemovedModel>(Info.StorageFolder, Info.StorageFile);

            if (model == null)
            {
                throw new Exception("Cannot load score file from storage");
            }

            var Db = GetCompanyContext();
            var list = Db.Query<RuleAssetRemovedDbModel>(
                @"
select	S.AssetUid,
		S.EffectiveDate,
		I.AssetVersionUid as MetricAssetVersionUid,
		V.AssetUid as MetricAssetUid
from	metrics.ScoreItem I
		inner join metrics.AssetVersion V on V.Uid = I.AssetVersionUid
		inner join metrics.Asset A on A.Uid = V.AssetUid
		inner join metrics.Allocation Al on Al.Uid = A.AllocationUid and Al.ScoreType = 2
		cross apply openjson(I.Evidence) Ev
		cross apply openjson(Ev.value) Rp 
		cross apply openjson(Rp.value)  with (Uid nvarchar(max) '$.Uid') as P
		inner join metrics.ScoreItemLink SIL on SIL.ScoreItemUid = I.Uid
		inner join metrics.Score S on S.Uid = SIL.ScoreUid
where	Evidence <> '{}' 
		and Evidence is not null
		and ISNUMERIC(Ev.[key]) = 1
		and Rp.[key] = 'RollupPath' 
		and P.Uid = @AssetUid", new { model.AssetUid }
                )
                .GroupBy(i => new { i.AssetUid, i.EffectiveDate })
                .Select(i => new AssetMeasureModel
                {
                    AssetUid = i.Key.AssetUid,
                    EffectiveDate = i.Key.EffectiveDate,
                    Measures =  i.Select(m => new AssetMeasureChildModel { 
                        MetricAssetUid = m.MetricAssetUid, 
                        MetricAssetVersionUid = m.MetricAssetVersionUid, 
                        Result = false 
                    }).ToList()
                })
                .ToList();

            if (list.Count > 0)
            {
                await Db.SendContinuingScoreEventWithPayload(ScoreQueueChangeType.AssetMeasures, list, Info.ExecutionUid, Info.StartedOn);
            }
        }
    }
}
