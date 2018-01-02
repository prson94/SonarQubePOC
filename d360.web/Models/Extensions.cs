using d360.core.entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace d360.web.Models
{
    public class FieldTypeComplexLookupDefinitionField
    {
        public string Object { get; set; }
        public int ObjectID { get; set; }
        public int FieldTypeID { get; set; }
        public string FieldTypeName { get; set; }
        public string Filter { get; set; }
        public string OverrideDisplayName { get; set; }
        public int DisplayOrder { get; set; }
        public int SortOrder { get; set; }
        public bool Show { get; set; } = true;
        public int? Width { get; set; } = null;
    }
    public class FieldTypeComplexLookupDefinitionRelation
    {
        public int IntersectTypeID { get; set; }
        public string Object { get; set; }
        public int ObjectID { get; set; }
        public core.ComplexLookupRelationType RelationType { get; set; }

        /// <summary>
        /// Generated when it comes time to create a dynamic SQL query.
        /// </summary>
        public string ColumnStatement { get; set; }
        /// <summary>
        /// Generated when it comes time to create a dynamic SQL query.
        /// </summary>
        public string JoinStatement { get; set; }
        /// <summary>
        /// Generated when it comes time to create a dynamic SQL query.
        /// </summary>
        public string OrderByStatement { get; set; }
        /// <summary>
        /// Generated when it comes time to create a dynamic SQL query.
        /// </summary>
        public string WhereStatement { get; set; }
    }
    public class FieldTypeComplexLookupDefinition
    {
        public List<FieldTypeComplexLookupDefinitionField> Fields { get; set; }
        public List<FieldTypeComplexLookupDefinitionRelation> Relations { get; set; }
    }

    public class FieldTypeOwnershipLookupDefinition
    {
        public bool DisplayAssignmentSource { get; set; } = true;

        public bool ExpandGroupMembership { get; set; } = true;
    }

    public static class Extensions
    {
        public static FieldTypeComplexLookupDefinition ParseComplexLookupDefinition(this FieldTypeLookup lookup)
        {
            return JsonConvert.DeserializeObject<FieldTypeComplexLookupDefinition>(lookup.Definition);
        }

        public static FieldTypeOwnershipLookupDefinition ParseOwnershipLookupDefinition(this FieldTypeLookup lookup)
        {
            return JsonConvert.DeserializeObject<FieldTypeOwnershipLookupDefinition>(lookup.Definition);
        }
    }
}