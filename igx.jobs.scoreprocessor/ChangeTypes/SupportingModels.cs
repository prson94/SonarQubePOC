using d360.core.enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public DateTime? EndDate { get; set; }
        public bool UsedInOtherScores { get; set; }
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
        public string VersionValueHash { get; set; }
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
        public DateTime? EffectiveEndDate { get; set; }
        public ScoreType ScoreType { get; set; }
        public CalculationMethod CalculationMethod { get; set; }
        public bool IsThresholdBased { get; set; }
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
        public string RollupPathJson { get; set; }
        public AllocationDataModelRollupPath RollupPath { get { return JsonConvert.DeserializeObject<AllocationDataModelRollupPath>(RollupPathJson ?? "{}"); } }
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
        public Operator Operator { get; set; }
        public List<AllocationDataModelConditionItemTempValue> ValueItems { get; set; }
        public List<string> Values 
        {
            get 
            {
                if (ValueItems != null)
                {
                    return ValueItems.Select(i => i.Value).ToList();
                }
                else 
                {
                    return new List<string>();
                }
            } 
        }
    }

    internal class AllocationDataModelConditionItemTempValue
    {
        public string Value { get; set; }
    }

    internal class AllocationDataModelRollupPath
    {
        public Guid AssetVersionRollupPathUid { get; set; }
        public string FilterMatchType { get; set; }
        public List<AllocationDataModelRollupPathSegmentLink> SegmentLinks { get; set; }
        public List<AllocationDataModelRollupPathFilter> Filters { get; set; }
    }
    internal class AllocationDataModelRollupPathSegmentLink
    {
        public int IntersectTypeID { get; set; }
        public PredicateType PredicateType { get; set; }
        public int StartPosition { get; set; }
        public int StartAssetTypeID { get; set; }
        public AssetTypeClass StartClass { get; set; }
        public int EndPosition { get; set; }
        public int EndAssetTypeID { get; set; }
        public AssetTypeClass EndClass { get; set; }
    }
    internal class AllocationDataModelRollupPathFilter
    {
        public Guid AssetVersionRollupPathFilterUid { get; set; }
        public int AssetTypeID { get; set; }
        public int FieldTypeID { get; set; }
        public string Operator { get; set; }
        public List<AllocationDataModelRollupPathFilterValue> Values { get; set; }
    }
    internal class AllocationDataModelRollupPathFilterValue
    {
        public string Value { get; set; }
    }

    internal class ScoreItemLink
    {
        public Guid ScoreUid { get; set; }
        public Guid ScoreItemUid { get; set; }
    }

    internal class RollupPathRuleResult
    {
        public Guid Uid { get; set; }
        public float PassFraction { get; set; }
    }

}
