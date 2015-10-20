using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Relationship : BaseObject
    {
        [DataMember]
        public int IntersectTypeID { get; set; }

        [DataMember, Key, Column(Order = 1)]
        public int IntersectID { get; set; }

        [DataMember]
        public IntersectClassification? Classification { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Role { get; set; }

        [DataMember]
        public int SourceIntersectTypeNodeID { get; set; }
        
        [DataMember, Key, Column(Order = 2)]
        public string SourceObjectType { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int SourceObjectID { get; set; }

        [DataMember]
        public string SourceName { get; set; }

        [DataMember]
        public string SourceParent { get; set; }

        [DataMember]
        public int? SourceParentID { get; set; }

        [DataMember]
        public string SourceParentName { get; set; }

        [DataMember]
        public int SourceTypeID { get; set; }

        [DataMember]
        public string SourceType { get; set; }

        [DataMember]
        public string SourceTypeName { get; set; }

        [DataMember]
        public string SourceUrl { get; set; }

        [DataMember]
        public int TargetIntersectTypeNodeID { get; set; }

        [DataMember, Key, Column(Order = 4)]
        public string TargetObjectType { get; set; }

        [DataMember, Key, Column(Order = 5)]
        public int TargetObjectID { get; set; }

        [DataMember]
        public string TargetName { get; set; }

        [DataMember]
        public string TargetParent { get; set; }

        [DataMember]
        public int? TargetParentID { get; set; }

        [DataMember]
        public string TargetParentName { get; set; }

        [DataMember]
        public int TargetTypeID { get; set; }

        [DataMember]
        public string TargetType { get; set; }

        [DataMember]
        public string TargetTypeName { get; set; }

        [DataMember]
        public string TargetUrl { get; set; }

        [DataMember]
        public bool HasTechnicalRelationships { get; set; }
    }
}
