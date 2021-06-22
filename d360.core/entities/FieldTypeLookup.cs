using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FieldTypeLookup : BaseObject
    {
        [DataMember, Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int FieldTypeID { get; set; }

        [DataMember]
        public bool HideHeader { get; set; }

        [DataMember]
        public bool HideFooter { get; set; }

        [DataMember]
        public bool HideFilter { get; set; }

        [DataMember]
        public int LookupType { get; set; }

        [DataMember]
        public string Definition { get; set; }

        [IgnoreDataMember, ForeignKey("FieldTypeID")]
        public virtual FieldType FieldType { get; set; }
    }

    public enum FieldTypeComplexLookupRelationDirection
    {
        Back = 1,
        Forward = 2,
        Both = 3
    }
    public class FieldTypeComplexLookupDefinitionField
    {
        public Guid AssetTypeUid { get; set; }
        public int FieldTypeID { get; set; }
        public string FieldTypeName { get; set; }
        public string Filter { get; set; }
        public string OverrideDisplayName { get; set; }
        public int DisplayOrder { get; set; }
        public int SortOrder { get; set; }
        public bool Show { get; set; }
        public int? Width { get; set; } = null;
        public int? RelationIndex { get; set; }
    }
    public class FieldTypeComplexLookupDefinitionRelation
    {
        public ComplexLookupRelationType RelationType { get; set; }
        public FieldTypeComplexLookupRelationDirection Direction { get; set; } = 0;
        public Guid? AssetTypeUid { get; set; }
        public Guid? IntersectTypeUid { get; set; }
    }
    public class FieldTypeComplexLookupDefinition
    {
        public List<FieldTypeComplexLookupDefinitionField> Fields { get; set; }
        public List<FieldTypeComplexLookupDefinitionRelation> Relations { get; set; }
    }

    public class FieldTypeComplexLookupDefinitionApiViewModel
    {
        public List<FieldTypeComplexLookupDefinitionFieldApiViewModel> Fields { get; set; }
        public List<FieldTypeComplexLookupDefinitionRelationApiViewModel> Relations { get; set; }
    }

    public class FieldTypeComplexLookupDefinitionFieldApiViewModel
    {
        public Guid AssetTypeUid { get; set; }
        public string FieldTypeName { get; set; }
        public string Filter { get; set; }
        public string OverrideDisplayName { get; set; }
        public int DisplayOrder { get; set; }
        public int SortOrder { get; set; }
        public bool Show { get; set; } = true;
        public int? Width { get; set; } = null;
        public int? RelationIndex { get; set; }
    }
    public class FieldTypeComplexLookupDefinitionRelationApiViewModel
    {
        public Guid IntersectTypeUid { get; set; }
        public Guid AssetTypeUid { get; set; }
        public core.ComplexLookupRelationType? RelationType { get; set; }
        public FieldTypeComplexLookupRelationDirection? Direction { get; set; }
    }

    public class FieldTypeOwnershipLookupDefinition
    {
        public bool DisplayAsList { get; set; }
        public bool DisplayAssignmentSource { get; set; } = true;
        public bool ExpandGroupMembership { get; set; } = true;
        public int? ResponsibilityType { get; set; }
        public Guid? ResponsibilityTypeUid { get; set; }
    }
}
