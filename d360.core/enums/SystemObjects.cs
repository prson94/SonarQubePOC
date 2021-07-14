using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Linq;

namespace d360.core
{
    public enum SystemObjects
    {
        [Description("Unknown"), IsType(false),ExcludeDataType(DataType.JSON | DataType.Path | DataType.Tag | DataType.Counter)]
        Unknown = -1,
        [Description("Artifact"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Counter)]
        Artifact = 1,
        [Description("Synonym"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Synonym,
        [Description("Synonym Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        SynonymType,
        [Description("Artifact Type"), AllowSurvey(true), IsType(true)]
        ArtifactType,
        [Description("Email Template"), AllowOwnership(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Counter)]
        EmailTemplate,
        [Description("Fusion"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        Fusion,
        [Description("Fusion Attribute"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        FusionAttribute,
        [Description("Fusion Attribute Type"), IsType(true), 
        ExcludeDataType(DataType.OwnershipLookup | DataType.Path | DataType.RefListRelationship | DataType.Tag | DataType.Score | DataType.Counter)]
        FusionAttributeType,
        [Description("Fusion Type"), IsType(true), 
            ExcludeDataType(DataType.ComplexRelationLookup | DataType.FieldFromRelationship | DataType.JSON | DataType.JsonElement | 
            DataType.Link | DataType.Lookup | DataType.OwnershipLookup | DataType.Path | DataType.RefListRelationship | DataType.Relationship | DataType.Tag | DataType.Score | DataType.Counter)]
        FusionType,
        [Description("Group"), IsType(false),
         ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Group,
        [Description("Intersect"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Intersect,
        [Description("Intersect Type"), IsType(true), 
            ExcludeDataType(DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        IntersectType,
        [Description("Resource"), AllowOwnership(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Resource,
        [Description("Resource Type"), AllowOwnership(false), AllowSurvey(true), IsType(true),
            ExcludeDataType(DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        ResourceType,
        [Description("Survey Type"), AllowSurvey(false), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        SurveyType,
        [Description("Tag"), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Counter)]
        Tag,
        [Description("Taxonomy"), IsType(false), ExcludeDataType(DataType.Tag)]
        Taxonomy,
        [Description("Taxonomy Type"), AllowSurvey(true), IsType(true)]
        TaxonomyType,
        [Description("Tooltip  Template"), AllowOwnership(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        TooltipTemplate,
        [Description("Field"), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Field,
        [Description("Field Type"), AllowSurvey(false), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        FieldType,
        [Description("Response Type"), AllowSurvey(false), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        ResponseType,
        [Description("Score"), AllowSurvey(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Score,
        [Description("Score Type"), AllowSurvey(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        ScoreType,
        [Description("Responsibility"), AllowSurvey(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        Responsibility,
        [Description("Responsibility Type"), AllowSurvey(false), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        ResponsibilityType,
        [Description("Responsibility Type Claim"), AllowSurvey(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        ResponsibilityTypeClaim,
        [Description("Claim"), AllowSurvey(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Claim,
        [Description("Bulk Load"), AllowSurvey(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        Load,
        [Description("Report"), AllowSurvey(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Report,
        [Description("Attribute Type Category"), AllowSurvey(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        AttributeTypeCategory,
        [Description("Policy"), AllowSurvey(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Policy,
        [Description("Policy Type"), AllowSurvey(false), IsType(true)]
        PolicyType,
        [Description("Rule"), AllowSurvey(false), IsType(false)]
        Rule,
        [Description("Rule Type"), AllowSurvey(false), IsType(true)]
        RuleType,
        [Description("Fusion Execution"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        FusionExecution,
        [Description("Workflow Relation"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        WorkflowTypeRelation,
        [Description("Predicate"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        Predicate,
        [Description("Group Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        GroupType,
        [Description("Rule Dimension"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        RuleDimension,        
        [Description("Map"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Map,
        [Description("Map Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        MapType,
        [Description("Reference Item"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Score | DataType.Counter)]
        ReferenceItem,
        [Description("Reference Item Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Score | DataType.Counter)]
        ReferenceItemType,
        [Description("Fusion Query Attribute"), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        FusionQueryAttribute,
        [Description("Fusion Query Attribute Type"), IsType(true),
                    ExcludeDataType(DataType.ComplexRelationLookup | DataType.FieldFromRelationship | DataType.JSON | DataType.JsonElement |
                    DataType.Link | DataType.Lookup | DataType.OwnershipLookup | DataType.Path | DataType.RefListRelationship | DataType.Relationship | DataType.Tag | DataType.Score | DataType.Counter)]
        FusionQueryAttributeType,
        [Description("Monitor"), AllowSurvey(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Monitor,
        [Description("Issue Type"), IsType(true), 
            ExcludeDataType(DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        IssueType,
        [Description("Issue"), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        Issue,
        [Description("Score Type Metric"), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        ScoreTypeMetric,
        [Description("Organization"), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        Organization,
        [Description("Organization Domain"), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        OrganizationDomain,
        [Description("Organization Invitation"), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        OrganizationInvitation,
        [Description("Contract"), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        Contract,
        [Description("Shopping Cart Type"), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        ShoppingCartType,
        [Description("Shopping Cart"), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        ShoppingCart,
        [Description("Organization Type"), IsType(true), 
            ExcludeDataType(DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        OrganizationType,
        [Description("Export Template"), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        ExportTemplate,
        [Description("Task Type"), AllowSurvey(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter)]
        TaskType,
        [Description("Task"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter)]
        Task,
        [Description("Connector Label"), AllowSurvey(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter)]
        ConnectorLabel,
        [Description("Issue Type Relation"), IsType(true),
        ExcludeDataType(DataType.FieldFromRelationship |
        DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        IssueTypeRelation
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
                return DataType.None;
            else
                return etype.Excluded;
        }

    }
}
