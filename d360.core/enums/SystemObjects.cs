using System.Linq;
using System.Reflection;

namespace d360.core
{
    public enum SystemObjects
    {
        [Description("Unknown"), IsType(false), ExcludeDataType(DataType.JSON | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        Unknown = -1,
        
        [Description("Artifact"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Counter | DataType.System)]
        Artifact = 1,
        
        [Description("Synonym"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        Synonym = 2,
        
        [Description("Synonym Type"), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        SynonymType = 3,
        
        [Description("Artifact Type"), IsType(true), ExcludeDataType(DataType.System)]
        ArtifactType = 4,
        
        [Description("Email Template"), AllowOwnership(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Counter | DataType.System)]
        EmailTemplate = 5,
        
        [Description("Group"), IsType(false),
         ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        Group = 10,
        
        [Description("Intersect"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        Intersect = 11,
        
        [Description("Intersect Type"), IsType(true),
            ExcludeDataType(DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.ReferenceList | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        IntersectType = 12,
        
        [Description("Resource"), AllowOwnership(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        Resource = 13,
        
        [Description("Resource Type"), AllowOwnership(false), IsType(true),
            ExcludeDataType(DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.ReferenceList | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        ResourceType = 14,
               
        [Description("Tag"), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Counter | DataType.System)]
        Tag = 16,
        
        [Description("Taxonomy"), IsType(false), ExcludeDataType(DataType.Tag | DataType.System)]
        Taxonomy = 17,
        
        [Description("Taxonomy Type"), IsType(true), ExcludeDataType(DataType.System)]
        TaxonomyType = 18,
        
        [Description("Field"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        Field = 20,
        
        [Description("Field Type"), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        FieldType = 21,
        
        [Description("Response Type"), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        ResponseType = 22,
        
        [Description("Score"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        Score = 23,
        
        [Description("Score Type"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        ScoreType = 24,
        
        [Description("Responsibility"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        Responsibility = 25,
        
        [Description("Responsibility Type"), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        ResponsibilityType = 26,
        
        [Description("Responsibility Type Claim"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        ResponsibilityTypeClaim = 27,
        
        [Description("Claim"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        Claim = 28,
        
        [Description("Bulk Load"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        Load = 29,
        
        [Description("Report"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        Report = 30,
        
        [Description("Attribute Type Category"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        AttributeTypeCategory = 31,
        
        [Description("Policy"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.System)]
        Policy = 32,
        
        [Description("Policy Type"), IsType(true), ExcludeDataType(DataType.System)]
        PolicyType = 33,
        
        [Description("Rule"), IsType(false), ExcludeDataType(DataType.System)]
        Rule = 34,
        
        [Description("Metric Allocation"), IsType(false), ExcludeDataType(DataType.System)]
        MetricAllocation = 35,
        
        [Description("Rule Type"), IsType(true), ExcludeDataType(DataType.System)]
        RuleType = 36,
        
        [Description("Workflow Relation"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        WorkflowTypeRelation = 38,
        
        [Description("Predicate"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        Predicate = 39,
        
        [Description("Group Type"), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.ComplexRelationLookup | DataType.ReferenceList | DataType.Score | DataType.Html | DataType.Link | DataType.System)]
        GroupType = 40,
        
        [Description("Reference Item"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        ReferenceItem = 44,
        
        [Description("Reference Item Type"), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Score | DataType.Counter)]
        ReferenceItemType = 45,
		[Description("Monitor"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
		Monitor = 48,
		[Description("Issue Type"), IsType(true),
            ExcludeDataType(DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.ReferenceList | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        IssueType = 49,
        
        [Description("Issue"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        Issue = 50,
        
        [Description("Score Type Metric"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        ScoreTypeMetric = 51,
        
        [Description("Export Template"), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        ExportTemplate = 59,
        
        [Description("Task Type"), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.ReferenceList | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter | DataType.System)]
        TaskType = 60,
        
        [Description("Task"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.ReferenceList | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter | DataType.System)]
        Task = 61,
        
        [Description("Connector Label"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.ReferenceList | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter | DataType.System)]
        ConnectorLabel = 62,
        
        [Description("Issue Type Relation"), IsType(true),
        ExcludeDataType(DataType.FieldFromRelationship |
        DataType.OwnershipLookup | DataType.ReferenceList | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        IssueTypeRelation = 63,

		[Description("Semantic Type"), IsType(true), ExcludeDataType(DataType.System)]
        SemanticType = 64
    }

    public static class SystemObjectExtensions
    {
        public static bool IsType(this SystemObjects type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<IsTypeAttribute>().IsType;
        }

        public static DataType ExcludeDataType(this SystemObjects type)
        {
            var etype = type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<ExcludeDataTypeAttribute>();
            if (etype == null)
            {
                return DataType.None;
            }
            else
            {
                return etype.Excluded;
            }
        }
    }
}
