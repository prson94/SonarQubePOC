using System;
using System.ComponentModel.DataAnnotations;

namespace d360.core.entities.Metric
{
    public interface IDataQualityUpsert
    {
        Guid? ExecutionItemUid { get; set; }

        Guid? EvaluatedAssetUid { get; set; }

        string RunDate { get; set; }

        long? PassCount { get; set; }

        long? FailCount { get; set; }
    }
    public class DataQualityInsertModel : IDataQualityUpsert
    {
        public Guid? ExecutionItemUid { get; set; }

        public Guid OwningAssetUid { get; set; }

        public Guid? EvaluatedAssetUid { get; set; }

        public string EffectiveDate { get; set; }

        public string RunDate { get; set; }

        [Range(0, 9223372036854775807,
            ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public long? PassCount { get; set; }

        [Range(0, 9223372036854775807,
            ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public long? FailCount { get; set; }
    }

    public class DataQualityDeleteModel
    {
        public Guid? ExecutionItemUid { get; set; }

        public Guid? Uid { get; set; }

        public Guid? OwningAssetUid { get; set; }

        public Guid? EvaluatedAssetUid { get; set; }

        public string EffectiveDateStart { get; set; }

        public string EffectiveDateEnd { get; set; }

        public string RunDateStart { get; set; }

        public string RunDateEnd { get; set; }
    }

    public class DataQualityAssetResultModel
    {
        public Guid ResultUid { get; set; }

        public Guid AssetUid { get; set; }

        public int Class { get; set; }

        public DateTime EffectiveDate { get; set; }

        public DateTime RunDate { get; set; }
    }

    public class DataQualityUpdateModel : IDataQualityUpsert
    {
        public Guid Uid { get; set; }

        public Guid? ExecutionItemUid { get; set; }

        public Guid? EvaluatedAssetUid { get; set; }

        public string RunDate { get; set; }

        [Range(0, 9223372036854775807,
            ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public long? PassCount { get; set; }

        [Range(0, 9223372036854775807,
            ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public long? FailCount { get; set; }
    }
}
