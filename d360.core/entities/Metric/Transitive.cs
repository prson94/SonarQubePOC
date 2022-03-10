using System;
using System.Collections.Generic;

using d360.core.enums;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace d360.core.entities.Metric
{
    public class RuleResultChangedRawModel
    {
        public Guid AssetUid { get; set; }

        public DateTime EffectiveDate { get; set; }

        public Guid AllocationUid { get; set; }

        public Guid MetricAssetUid { get; set; }

        public Guid MetricAssetVersionUid { get; set; }
    }

    public class AssetMeasureModel
    {
        public Guid AssetUid { get; set; }

        public DateTime EffectiveDate { get; set; }

        public List<AssetMeasureChildModel> Measures { get; set; } = new List<AssetMeasureChildModel>();
    }

    public class AssetMeasureChildModel
    {
        public Guid AllocationUid { get; set; }

        public Guid MetricAssetUid { get; set; }

        public Guid? MetricAssetVersionUid { get; set; }

        public bool? Result { get; set; }
    }

    public class MeasureChangedModel
    {
        public Guid MetricAssetUid { get; set; }

        public Guid MetricAssetVersionUid { get; set; }

        public DateTime EffectiveDate { get; set; }
    }

    public class MeasureRemovedModel
    {
        public Guid MetricAssetUid { get; set; }

        public Guid MetricAssetVersionUid { get; set; }

        public DateTime EffectiveEndDate { get; set; }
    }

    public class MetricScoreApiModel
    {
        public int pageSize { get; set; } = 250;

        public int pageNum { get; set; } = 1;

        public int total { get; set; }

        public List<MetricAssetScoreModel> items { get; set; } = new List<MetricAssetScoreModel>();
    }

    public class MetricAssetScoreModel
    {
        public Guid AssetUid { get; set; }

        public List<MetricScoreModel> Scores { get; set; } = new List<MetricScoreModel>();
    }

    public class MetricScoreModel
    {
        public DateTime EffectiveDate { get; set; }

        public float Score { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ScoreType ScoreType { get; set; }
    }

    public class ScoreCreatedModel
    {
        public Guid AllocationUid { get; set; }

        public Guid AssetUid { get; set; }

        public DateTime EffectiveDate { get; set; }
    }
}
