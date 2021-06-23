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
            var Db = GetCompanyContext();
            ExecutionRecord = getExecution(Db.Connection);
            var executionItems = getExecutionItems(Db.Connection, 0);

            if (executionItems.Count > 0)
            {
                var model = executionItems[0].GetPayload<RuleAssetRemovedModel>();
				Db.CreateRuleResultsRemovedExecution(model.AssetUid);
                updateExecutionMarkingItemsAsComplete(Db.Connection, ExecutionRecord);
            }
        }
    }
}
