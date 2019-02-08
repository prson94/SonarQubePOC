using d360.core.entities.Contracts;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    public class FusionMarkitLineageData : BaseIntObject, IIntObject
    {
        public int? MapRuleItemID { get; set; }
        public int? ParentID { get; set; }
        public int? UltimateParentID { get; set; }
        public int? Level { get; set; }
        public int? SourceFusionAttributeID { get; set; }
        public int? SourceFusionAttributeTypeID { get; set; }
        public string SourceObject { get; set; }
        public string SourceParentObject { get; set; }
        public int? SourceParentObjectFusionAttributeID { get; set; }
        public int? SourceParentObjectFusionAttributeTypeID { get; set; }
        public int? TargetFusionAttributeID { get; set; }
        public int? TargetFusionAttributeTypeID { get; set; }
        public string TargetObject { get; set; }
        public string TargetParentObject { get; set; }
        public int? TargetParentObjectFusionAttributeID { get; set; }
        public int? TargetParentObjectFusionAttributeTypeID { get; set; }
        public long? SourceAssetID { get; set; }
        public long? TargetAssetID { get; set; }
        public DateTime UpdatedOn { get; set; }
    }

    public class FusionMarkitObjectMapping
    {
        public int FusionAttributeID { get; set; }
        public long ObjectAssetID { get; set; }
    }

    public class FusionMarkitSourceTargetMapping
    {
        public int MapID { get; set; }
        public int SourceFusionAttributeID { get; set; }
        public int TargetFusionAttributeID { get; set; }
        public long SourceAssetID { get; set; }
        public long TargetAssetID { get; set; }
        public int MapItemID { get; set; }
        public long ObjectAssetID { get; set; }
    }
}
