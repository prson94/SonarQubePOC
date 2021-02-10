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
    public class MeasureChangedProcess: ProcessBase, IScoreProcess
    {
        public async Task Run()
        {
            var measureChangedModel = await Storage.DeserializeJsonObjectFromBlobAsync<MeasureChangedModel>(Info.StorageFolder, Info.StorageFile);

            if (measureChangedModel.EffectiveDate <= DateTime.UtcNow.Date)
            {
                // We can continue processing it.
                var Db = GetCompanyContext();
                var maxScoreItem = Db.Filter<ScoreItem>(i => i.AssetVersionUid == measureChangedModel.MetricAssetVersionUid).OrderByDescending(i => i.UpdatedOn).Select(i => new { i.Uid, i.UpdatedOn }).FirstOrDefault();
                var maxUpdatedOnForThisVersion = DateTime.UtcNow.AddDays(-7);
                if (maxScoreItem != null)
                {
                    maxUpdatedOnForThisVersion = maxScoreItem.UpdatedOn;
                }
                if (DateTime.UtcNow.Subtract(maxUpdatedOnForThisVersion).TotalDays > 0)
                {
                    var version = Db.Filter<MetricAssetVersion>(v => v.Uid == measureChangedModel.MetricAssetVersionUid, v => v.Asset.Allocation).SingleOrDefault();

                    if (version != null)
                    {
                        if (version.State == State.Active && !version.Asset.Allocation.IsExternallyCalculated)
                        {
                            var definition = JsonConvert.DeserializeObject<MetricAssetDefinitionViewModel>(version.Definition ?? "{}");
                            bool shouldProceed = false;
                            if (definition.DataQuality != null)
                            {
                                shouldProceed = true;
                            }
                            else if (definition.Governance != null)
                            {
                                if (definition.Governance.Check != MetricGovernanceCheckType.External)
                                {
                                    shouldProceed = true;
                                }
                            }

                            if (shouldProceed)
                            {
                                var list = Db.Query<Guid>(
                                    "select A.Uid from Asset A inner join AssetType T on T.ID = A.AssetTypeID and T.Uid = @AssetTypeUid", 
                                    new { version.Asset.Allocation.AssetTypeUid }
                                    )
                                    .ToList()
                                    .Select(uid => new AssetMeasureModel { 
                                         AssetUid = uid,
                                         EffectiveDate = DateTime.UtcNow,
                                         Measures = new List<AssetMeasureChildModel>() { 
                                            new AssetMeasureChildModel { MetricAssetUid = measureChangedModel.MetricAssetUid, MetricAssetVersionUid = measureChangedModel.MetricAssetVersionUid, Result = false }
                                         }
                                    })
                                    .ToList();
                                
                                if (list.Count > 0)
                                {
                                    Db.SendContinuingScoreEventWithPayload(ScoreQueueChangeType.AssetMeasures, list, Info.ExecutionUid, Info.StartedOn);
                                }
                            }
                        }
                    }
                }
            }
            else 
            {
                // Set a delay of processing until we reach this effective date.
                var queue = new AzureQueueSource();
                var timespan = measureChangedModel.EffectiveDate.Subtract(DateTime.UtcNow);
                if (timespan.TotalDays > 1)
                {
                    timespan = new TimeSpan(1, 0, 0, 0);
                }
                var minutesToAdd = new Random().Next(5, 25);
                timespan = timespan.Add(new TimeSpan(0, minutesToAdd, 0));
                await queue.CreateMessageAsync(Config.GetValue<string>("ScoringQueue"), Info, timespan);
            }
        }
    }
}
