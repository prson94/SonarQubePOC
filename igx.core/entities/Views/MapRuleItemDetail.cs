using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class MapRuleItemDetail : BaseObject
    {
        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public int ID { get; set; }

        [DataMember, Key, Column(Order = 1)]
        public string TextID { get; set; }

        [DataMember]
        public string ParentTextID { get; set; }

        [DataMember]
        public string Transformation { get; set; }

        [DataMember]
        public string SourceFusion { get; set; }

        [DataMember]
        public int? SourceFusionAttributeID { get; set; }

        [DataMember]
        public string SourceFusionAttributeTextPath { get; set; }

        [DataMember]
        public string SourceObjectName { get; set; }

        [DataMember]
        public int? SourceObjectID { get; set; }

        [DataMember]
        public string SourceObject { get; set; }

        [DataMember]
        public string TargetFusion { get; set; }

        [DataMember]
        public int? TargetFusionAttributeID { get; set; }

        [DataMember]
        public string TargetFusionAttributeTextPath { get; set; }

        [DataMember]
        public string TargetObjectName { get; set; }

        [DataMember]
        public int? TargetObjectID { get; set; }

        [DataMember]
        public string TargetObject { get; set; }
    }
}
