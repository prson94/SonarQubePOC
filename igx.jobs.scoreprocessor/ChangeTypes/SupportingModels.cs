using d360.core.enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    internal class MetConditionsModel
    {
        public bool ConditionMet { get; set; } = false;
        public decimal? SelectedWeight { get; set; }
        public float? SelectedThreshold { get; set; }
        public Guid? SelectedConditionUid
        {
            get
            {
                if (Conditions.Count > 0)
                {
                    return Conditions[0].ConditionUid;
                }
                else
                {
                    return null;
                }
            }
        }
        public List<MetConditionModel> Conditions { get; set; } = new List<MetConditionModel>();
    }

    internal class MetConditionModel
    {
        public int Position { get; set; }
        public Guid ConditionUid { get; set; }
        public decimal? Weight { get; set; }
        public float? Threshold { get; set; }

    }

    internal class AssetAllocationPreviousResult
    {
        public Guid ScoreUid { get; set; }

        public Guid ScoreItemUid { get; set; }

        public Guid AllocationUid { get; set; }
        public Guid AssetUid { get; set; }
        public Guid MetricAssetUid { get; set; }
        public Guid MetricAssetVersionUid { get; set; }
        public Guid? ConditionUid { get; set; }
        public DateTime EffectiveDate { get; set; }
        public bool Value { get; set; }
        public decimal AdjustedWeight { get; set; }
        public decimal AdjustedMaxWeight { get; set; }
    }

    internal class MatchingScoreModel
    {
        public Guid ScoreUid { get; set; }
        public Guid AllocationUid { get; set; }
        public Guid AssetUid { get; set; }
        public DateTime EffectiveDate { get; set; }
    }
    internal class MatchingScoreItemModel
    {
        public Guid ScoreItemUid { get; set; }
        public Guid MetricAssetUid { get; set; }
        public Guid AssetUid { get; set; }
        public DateTime EffectiveDate { get; set; }
    }

    internal class AllocationDataModel
    {
        public Guid AllocationUid { get; set; }
        public DateTime EffectiveDate { get; set; }
        public Guid MetricAssetUid { get; set; }
        public Guid? MetricParentAssetUid { get; set; }
        public bool IsGroup { get; set; }
        public Guid MetricAssetVersionUid { get; set; }
        public decimal Weight { get; set; }
        public float Threshold { get; set; }
        public bool MatchConditionsOnly { get; set; }
        public string Definition { get; set; }
        public string ConditionsJson { get; set; }
        public List<AllocationDataModelCondition> Conditions { get { return JsonConvert.DeserializeObject<List<AllocationDataModelCondition>>(ConditionsJson ?? "[]"); } }
    }

    internal class AllocationDataModelCondition
    {
        public Guid ConditionUid { get; set; }
        public MetricMatchType MatchType { get; set; }
        public int Position { get; set; }
        public decimal Weight { get; set; }
        public float Threshold { get; set; }
        public List<AllocationDataModelConditionItem> Items { get; set; }
    }

    internal class AllocationDataModelConditionItem
    {
        public Guid ItemUid { get; set; }
        public MetricConditionType ConditionType { get; set; }
        public int? ConditionFieldTypeID { get; set; }
        public int? ConditionIntersectTypeID { get; set; }
        public string Operator { get; set; }
        public List<AllocationDataModelConditionItemValue> Values { get; set; }
    }

    internal class AllocationDataModelConditionItemValue
    {
        public string Value { get; set; }
    }

    internal class ScoreItemLink
    {
        public Guid ScoreUid { get; set; }
        public Guid ScoreItemUid { get; set; }
    }

}
