using d360.core.enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace igx.jobs.scoreprocessor.Models
{
    internal class AllocationDataModel
    {
        public Guid AllocationUid { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime? EffectiveEndDate { get; set; }
        public MetricUpdateFrequency UpdateFrequency { get; set; }
        public ScoreType ScoreType { get; set; }
        public CalculationMethod CalculationMethod { get; set; }
        public Guid MetricAssetUid { get; set; }
        public Guid? MetricParentAssetUid { get; set; }
        public bool IsGroup { get; set; }
        public Guid MetricAssetVersionUid { get; set; }
        public decimal Weight { get; set; }
        public float? Threshold { get; set; }
        public bool MatchConditionsOnly { get; set; }
        public string Definition { get; set; }
        public string ConditionsJson { get; set; }
        public List<AllocationDataModelCondition> Conditions { get { return JsonConvert.DeserializeObject<List<AllocationDataModelCondition>>(ConditionsJson ?? "[]"); } }
        public string RollupPathJson { get; set; }
        public AllocationDataModelRollupPath RollupPath { get { return JsonConvert.DeserializeObject<AllocationDataModelRollupPath>(RollupPathJson ?? "{}"); } }
    }
}
