using System.Linq;
using System.Reflection;

namespace d360.core
{
    public enum SystemObjects
    {
        [Description("Unknown"), IsType(false), ExcludeDataType(DataType.JSON | DataType.Path | DataType.Tag | DataType.Counter)]
        Unknown = -1,
        
        [Description("Artifact"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Counter)]
        Artifact = 1,
        
        [Description("Synonym"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Synonym = 2,
        
        [Description("Synonym Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        SynonymType = 3,
        
        [Description("Artifact Type"), AllowSurvey(true), IsType(true)]
        ArtifactType = 4,
        
        [Description("Email Template"), AllowOwnership(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Counter)]
        EmailTemplate = 5,
        
        [Description("Group"), IsType(false),
         ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Group = 10,
        
        [Description("Intersect"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Intersect = 11,
        
        [Description("Intersect Type"), IsType(true),
            ExcludeDataType(DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        IntersectType = 12,
        
        [Description("Resource"), AllowOwnership(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Resource = 13,
        
        [Description("Resource Type"), AllowOwnership(false), AllowSurvey(true), IsType(true),
            ExcludeDataType(DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        ResourceType = 14,
        
        [Description("Survey Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        SurveyType = 15,
        
        [Description("Tag"), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Counter)]
        Tag = 16,
        
        [Description("Taxonomy"), IsType(false), ExcludeDataType(DataType.Tag)]
        Taxonomy = 17,
        
        [Description("Taxonomy Type"), AllowSurvey(true), IsType(true)]
        TaxonomyType = 18,
        
        [Description("Tooltip  Template"), AllowOwnership(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        TooltipTemplate = 19,
        
        [Description("Field"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Field = 20,
        
        [Description("Field Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        FieldType = 21,
        
        [Description("Response Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        ResponseType = 22,
        
        [Description("Score"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Score = 23,
        
        [Description("Score Type"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        ScoreType = 24,
        
        [Description("Responsibility"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        Responsibility = 25,
        
        [Description("Responsibility Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        ResponsibilityType = 26,
        
        [Description("Responsibility Type Claim"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        ResponsibilityTypeClaim = 27,
        
        [Description("Claim"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Claim = 28,
        
        [Description("Bulk Load"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        Load = 29,
        
        [Description("Report"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Report = 30,
        
        [Description("Attribute Type Category"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        AttributeTypeCategory = 31,
        
        [Description("Policy"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Policy = 32,
        
        [Description("Policy Type"), AllowSurvey(false), IsType(true)]
        PolicyType = 33,
        
        [Description("Rule"), AllowSurvey(false), IsType(false)]
        Rule = 34,
        
        [Description("Metric Allocation"), AllowSurvey(false), IsType(false)]
        MetricAllocation = 35,
        
        [Description("Rule Type"), AllowSurvey(false), IsType(true)]
        RuleType = 36,
        
        [Description("Workflow Relation"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        WorkflowTypeRelation = 38,
        
        [Description("Predicate"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        Predicate = 39,
        
        [Description("Group Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.Score | DataType.Html | DataType.Link)]
        GroupType = 40,
        
        [Description("Rule Dimension"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        RuleDimension = 41,
        
        [Description("Map"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Map = 42,
        
        [Description("Map Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        MapType = 43,
        
        [Description("Reference Item"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Score | DataType.Counter)]
        ReferenceItem = 44,
        
        [Description("Reference Item Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Score | DataType.Counter)]
        ReferenceItemType = 45,
        
        [Description("Monitor"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Monitor = 48,
        
        [Description("Issue Type"), IsType(true),
            ExcludeDataType(DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        IssueType = 49,
        
        [Description("Issue"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        Issue = 50,
        
        [Description("Score Type Metric"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        ScoreTypeMetric = 51,
        
        [Description("Organization"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        Organization = 52,
        
        [Description("Organization Domain"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        OrganizationDomain = 53,
        
        [Description("Organization Invitation"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        OrganizationInvitation = 54,
        
        [Description("Contract"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        Contract = 55,
        
        [Description("Shopping Cart Type"), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        ShoppingCartType = 56,
        
        [Description("Shopping Cart"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        ShoppingCart = 57,
        
        [Description("Organization Type"), IsType(true),
            ExcludeDataType(DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        OrganizationType = 58,
        
        [Description("Export Template"), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        ExportTemplate = 59,
        
        [Description("Task Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter)]
        TaskType = 60,
        
        [Description("Task"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter)]
        Task = 61,
        
        [Description("Connector Label"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter)]
        ConnectorLabel = 62,
        
        [Description("Issue Type Relation"), IsType(true),
        ExcludeDataType(DataType.FieldFromRelationship |
        DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        IssueTypeRelation = 63,
        
        [Description("Semantic Type"), IsType(true)]
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
