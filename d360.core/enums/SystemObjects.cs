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
        
        [Description("Synonym"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        Synonym = 2,
        
        [Description("Synonym Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        SynonymType = 3,
        
        [Description("Artifact Type"), AllowSurvey(true), IsType(true), ExcludeDataType(DataType.System)]
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
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        IntersectType = 12,
        
        [Description("Resource"), AllowOwnership(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        Resource = 13,
        
        [Description("Resource Type"), AllowOwnership(false), AllowSurvey(true), IsType(true),
            ExcludeDataType(DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        ResourceType = 14,
        
        [Description("Survey Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        SurveyType = 15,
        
        [Description("Tag"), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Counter | DataType.System)]
        Tag = 16,
        
        [Description("Taxonomy"), IsType(false), ExcludeDataType(DataType.Tag | DataType.System)]
        Taxonomy = 17,
        
        [Description("Taxonomy Type"), AllowSurvey(true), IsType(true), ExcludeDataType(DataType.System)]
        TaxonomyType = 18,
        
        [Description("Tooltip  Template"), AllowOwnership(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        TooltipTemplate = 19,
        
        [Description("Field"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        Field = 20,
        
        [Description("Field Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        FieldType = 21,
        
        [Description("Response Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        ResponseType = 22,
        
        [Description("Score"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        Score = 23,
        
        [Description("Score Type"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        ScoreType = 24,
        
        [Description("Responsibility"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        Responsibility = 25,
        
        [Description("Responsibility Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        ResponsibilityType = 26,
        
        [Description("Responsibility Type Claim"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        ResponsibilityTypeClaim = 27,
        
        [Description("Claim"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        Claim = 28,
        
        [Description("Bulk Load"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        Load = 29,
        
        [Description("Report"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        Report = 30,
        
        [Description("Attribute Type Category"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        AttributeTypeCategory = 31,
        
        [Description("Policy"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.System)]
        Policy = 32,
        
        [Description("Policy Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.System)]
        PolicyType = 33,
        
        [Description("Rule"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.System)]
        Rule = 34,
        
        [Description("Metric Allocation"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.System)]
        MetricAllocation = 35,
        
        [Description("Rule Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.System)]
        RuleType = 36,
        
        [Description("Workflow Relation"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        WorkflowTypeRelation = 38,
        
        [Description("Predicate"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        Predicate = 39,
        
        [Description("Group Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.Score | DataType.Html | DataType.Link | DataType.System)]
        GroupType = 40,
        
        [Description("Rule Dimension"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        RuleDimension = 41,
        
        [Description("Map"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        Map = 42,
        
        [Description("Map Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        MapType = 43,
        
        [Description("Reference Item"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        ReferenceItem = 44,
        
        [Description("Reference Item Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Score | DataType.Counter)]
        ReferenceItemType = 45,
        
        [Description("Monitor"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        Monitor = 48,
        
        [Description("Issue Type"), IsType(true),
            ExcludeDataType(DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        IssueType = 49,
        
        [Description("Issue"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        Issue = 50,
        
        [Description("Score Type Metric"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        ScoreTypeMetric = 51,

        [Description("Shopping Cart Type"), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        ShoppingCartType = 56,
        
        [Description("Shopping Cart"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
        ShoppingCart = 57,
        
        [Description("Export Template"), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System)]
        ExportTemplate = 59,
        
        [Description("Task Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter | DataType.System)]
        TaskType = 60,
        
        [Description("Task"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter | DataType.System)]
        Task = 61,
        
        [Description("Connector Label"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter | DataType.System)]
        ConnectorLabel = 62,
        
        [Description("Issue Type Relation"), IsType(true),
        ExcludeDataType(DataType.FieldFromRelationship |
        DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System)]
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
