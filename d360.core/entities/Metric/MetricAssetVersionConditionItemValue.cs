using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("AssetVersionConditionItemValue", Schema = "metrics")]
    public class MetricAssetVersionConditionItemValue: BaseObject 
    {
        [Key, Column(Order = 1)]
        public Guid Uid { get; set; }
        
        [DataMember, Key, Column(Order = 2)]
        public string Value { get; set; }

        [IgnoreDataMember, ForeignKey("Uid")]
        public virtual MetricAssetVersionConditionItem Item { get; set; }
    }
}
