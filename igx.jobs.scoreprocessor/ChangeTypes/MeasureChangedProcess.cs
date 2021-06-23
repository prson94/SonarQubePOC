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
using Dapper;
using d360.core.exceptions;
using d360.core.entities;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    public class MeasureChangedProcess: ProcessBase, IScoreProcess
    {
        public async Task Run()
        {           
            var Db = GetCompanyContext();
            ExecutionRecord = getExecution(Db.Connection);
            var executionItems = getExecutionItems(Db.Connection, 0);

            if (executionItems.Count > 0)
            {
                var measureChangedModel = executionItems[0].GetPayload<MeasureChangedModel>();

                if (measureChangedModel.EffectiveDate <= DateTime.UtcNow.Date)
                {
                    var maxScoreItem = Db.Filter<ScoreItem>(i => i.AssetVersionUid == measureChangedModel.MetricAssetVersionUid).OrderByDescending(i => i.UpdatedOn).Select(i => new { i.Uid, i.UpdatedOn }).FirstOrDefault();
                    var maxUpdatedOnForThisVersion = DateTime.UtcNow.AddDays(-7);
                    if (maxScoreItem != null)
                    {
                        maxUpdatedOnForThisVersion = maxScoreItem.UpdatedOn;
                    }
                    if (DateTime.UtcNow.Subtract(maxUpdatedOnForThisVersion).TotalDays > 0)
                    {
                        var version = Db.Filter<MetricAssetVersion>(v => v.Uid == measureChangedModel.MetricAssetVersionUid, v => v.Asset.Allocation, v => v.RollupPaths).SingleOrDefault();

                        if (version != null)
                        {
                            if (version.State == State.Active && !version.Asset.Allocation.IsExternallyCalculated)
                            {
                                var definition = JsonConvert.DeserializeObject<MetricAssetDefinitionViewModel>(version.Definition ?? "{}");
                                var list = new List<AssetMeasureModel>();

                                if (definition.DataQuality != null)
                                {
                                    var rollupPath = version.RollupPaths.FirstOrDefault();
                                    if (rollupPath != null)
                                    {
                                        var dqQueryDetail = Db.BuildDataQualityMeasureQueryModel(MetricDataQualityQueryType.ImpactedAssets_EffectiveDates_By_ProvidedUid, rollupPath.Uid);
                                        try
                                        {
                                            list = Db.GetDataQualityAssetEffectiveDateResultModels(dqQueryDetail, version.Asset.AllocationUid, measureChangedModel.MetricAssetUid, measureChangedModel.MetricAssetVersionUid, version.EffectiveDate);
                                        }
                                        catch
                                        {
                                            throw new InvalidScoreMeasure($"Measure with Version Uid {version.Uid} ({version.Name}) references a non-existent / invalid rollup path.");
                                        }
                                    }
                                    else
                                    {
                                        throw new InvalidScoreMeasure($"Measure with Version Uid {version.Uid} ({version.Name}) references a non-existent / invalid rollup path.");
                                    }
                                }
                                else if (definition.Governance != null)
                                {
                                    if (definition.Governance.Check != MetricGovernanceCheckType.External)
                                    {
                                        list = Db.Query<Guid>(
                                            "select A.Uid from Asset A inner join AssetType T on T.ID = A.AssetTypeID and T.Uid = @AssetTypeUid",
                                            new { version.Asset.Allocation.AssetTypeUid }
                                            )
                                            .ToList()
                                            .Select(uid => new AssetMeasureModel
                                            {
                                                AssetUid = uid,
                                                EffectiveDate = DateTime.UtcNow.Date,
                                                Measures = new List<AssetMeasureChildModel>() {
                                                    new AssetMeasureChildModel { 
                                                        AllocationUid = version.Asset.AllocationUid,  
                                                        MetricAssetUid = measureChangedModel.MetricAssetUid, 
                                                        MetricAssetVersionUid = measureChangedModel.MetricAssetVersionUid, 
                                                        Result = false 
                                                    }
                                                }
                                            })
                                            .ToList();
                                    }
                                }

                                Db.CreateMeasureChangedResultExecution(list, ExecutionRecord.Uid);

                                updateExecutionMarkingItemsAsComplete(Db.Connection, ExecutionRecord);
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
}
