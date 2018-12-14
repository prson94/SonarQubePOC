using d360.core.entities.Contracts;
using System;

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
        public string Source { get; set; }
        public int? SourceID { get; set; }
        public string Target { get; set; }
        public int? TargetID { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
}
