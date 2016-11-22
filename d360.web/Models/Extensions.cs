using d360.core.entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace d360.web.Models
{
    public class FieldTypeLookupDefinitionField
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
    }
    public class FieldTypeLookupDefinitionRelation
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
    public class FieldTypeLookupDefinition
    {
        public List<FieldTypeLookupDefinitionField> Fields { get; set; }
        public List<FieldTypeLookupDefinitionRelation> Relations { get; set; }
    }

    public static class Extensions
    {
        public static FieldTypeLookupDefinition ParseDefinition(this FieldTypeLookup lookup)
        {
            return JsonConvert.DeserializeObject<FieldTypeLookupDefinition>(lookup.Definition);
        }
    }
}