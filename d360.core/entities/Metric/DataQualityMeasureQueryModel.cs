using System;
using System.Collections.Generic;
using System.Data.SqlClient;

using d360.core.enums;

using Newtonsoft.Json;

namespace d360.core.entities.Metric
{
    public class DataQualityMeasureQueryModel
    {
        public Guid AssetVersionRollupPathUid { get; set; }

        public string Sql { get; set; }

        public MetricMatchType FilterMatchType { get; set; }

        public List<DataQualityMeasureQueryFilterModel> Filters { get; set; } = new List<DataQualityMeasureQueryFilterModel>();
    }

    public class DataQualityMeasureQueryFilterModel
    {
        public int AssetTypeID { get; set; }

        public int FieldTypeID { get; set; }

        public string Type { get; set; }

        public Operator Operator { get; set; }

        public string Value { get; set; }

        public string WhereQuery { get; set; }

        public SqlParameter Parameter { get; set; }
    }

    public class DataQualityMeasureQueryResultModel
    {
        public Guid StartingAssetUid { get; set; }

        public Guid TechnicalAssetUid { get; set; }

        public Guid RuleAssetUid { get; set; }

        public string Path { get; set; } //JSON

        public List<DataQualityMeasureQueryResult_PathModel> StructuredPath
        {
            get { return JsonConvert.DeserializeObject<List<DataQualityMeasureQueryResult_PathModel>>(Path); }
        }

        public string Results { get; set; } //JSON

        public List<DataQualityMeasureQueryResult_ResultModel> StructuredResults
        {
            get { return JsonConvert.DeserializeObject<List<DataQualityMeasureQueryResult_ResultModel>>(string.IsNullOrEmpty(Results) ? "[]" : Results); }
        }

        public float ResultScoreValue { get; set; }
    }

    public class DataQualityMeasureQueryResult_PathModel
    {
        public Guid Uid { get; set; }

        public int Position { get; set; }
    }

    public class DataQualityMeasureQueryResult_ResultModel
    {
        public Guid Uid { get; set; }

        public float PassFraction { get; set; }

        public DateTime EffectiveDate { get; set; }
    }

    public class DataQualityEvidenceModel
    {
        public bool IsError { get; set; }

        public string ErrorMessage { get; set; }

        public List<DataQualityMeasureQueryResult_PathModel> RollupPath;

        public List<Guid> ResultResultUids { get; set; }
    }
}
