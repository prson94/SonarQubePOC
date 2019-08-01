using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Linq;

namespace d360.core
{
    public enum SystemObjects
    {
        [Description("Unknown"), EnableAudit(false), IsType(false),
         ExcludeDataType(DataType.JSON)]
        Unknown = -1,
        [Description("Artifact"), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Artifact = 1,
        [Description("Synonym"), AllowSurvey(false), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Synonym,
        [Description("Synonym Type"), AllowSurvey(false), EnableAudit(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        SynonymType,
        [Description("Artifact Type"), AllowSurvey(true), EnableAudit(true), IsType(true)]
        ArtifactType,
        [Description("Attribute"), AllowOwnership(false), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Attribute,
        [Description("Attribute Group"), EnableAudit(false), IsType(true), 
         ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup | DataType.FusionLookup | 
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup |DataType.Relationship | DataType.JSON | DataType.JsonElement)]
        AttributeType,
        [Description("Email Template"), AllowOwnership(false), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        EmailTemplate,
        [Description("Fusion"), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Fusion,
        [Description("Fusion Attribute"), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        FusionAttribute,
        [Description("Fusion Attribute Type"), EnableAudit(true), IsType(true), 
        ExcludeDataType(DataType.FilteredLookup | DataType.FusionLookup | DataType.OwnershipLookup | DataType.RefListRelationship)]
        FusionAttributeType,
        [Description("Fusion Type"), EnableAudit(true), IsType(true), 
            ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup |
            DataType.FusionLookup | DataType.Link | DataType.Lookup | DataType.OwnershipLookup |
            DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement)]
        FusionType,
        [Description("Group"), EnableAudit(true), IsType(false),
         ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Group,
        [Description("Intersect"), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Intersect,
        [Description("Intersect Type"), EnableAudit(true), IsType(true), 
            ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup | DataType.FusionLookup |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement)]
        IntersectType,
        [Description("Lookup Item"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Lookup,
        [Description("Lookup Type"), AllowSurvey(true), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        LookupType,
        [Description("Resource"), AllowOwnership(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Resource,
        [Description("Resource Type"), AllowOwnership(false), AllowSurvey(true), EnableAudit(false), IsType(true),
            ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup | DataType.FusionLookup |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.JSON | DataType.JsonElement)]
        ResourceType,
        [Description("Survey Type"), AllowSurvey(false), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        SurveyType,
        [Description("Tag"), AllowSurvey(false), EnableAudit(true), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Tag,
        [Description("Taxonomy"), EnableAudit(true), IsType(false)]
        Taxonomy,
        [Description("Taxonomy Type"), AllowSurvey(true), EnableAudit(true), IsType(true), ExcludeDataType(DataType.FilteredLookup | DataType.FusionLookup)]
        TaxonomyType,
        [Description("Tooltip  Template"), AllowOwnership(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        TooltipTemplate,
        [Description("Field"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Field,
        [Description("Field Type"), AllowSurvey(false), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        FieldType,
        [Description("Response Type"), AllowSurvey(false), EnableAudit(false), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        ResponseType,
        [Description("Score"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Score,
        [Description("Score Type"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        ScoreType,
        [Description("Responsibility"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Responsibility,
        [Description("Responsibility Type"), AllowSurvey(false), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        ResponsibilityType,
        [Description("Responsibility Type Claim"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        ResponsibilityTypeClaim,
        [Description("Claim"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Claim,
        [Description("Bulk Load"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Load,
        [Description("Report"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Report,
        [Description("Attribute Type Category"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        AttributeTypeCategory,
        [Description("Policy"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Policy,
        [Description("Policy Type"), AllowSurvey(false), EnableAudit(true), IsType(true), ExcludeDataType(DataType.FilteredLookup | DataType.FusionLookup)]
        PolicyType,
        [Description("Rule"), AllowSurvey(false), EnableAudit(true), IsType(false)]
        Rule,
        [Description("Rule Type"), AllowSurvey(false), EnableAudit(true), IsType(true), ExcludeDataType(DataType.FilteredLookup | DataType.FusionLookup)]
        RuleType,
        [Description("Fusion Execution"), AllowSurvey(false), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        FusionExecution,
        [Description("Workflow Relation"), AllowSurvey(false), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        WorkflowTypeRelation,
        [Description("Predicate"), AllowSurvey(false), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Predicate,
        [Description("Group Type"), AllowSurvey(false), EnableAudit(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        GroupType,
        [Description("Rule Dimension"), AllowSurvey(false), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        RuleDimension,        
        [Description("Map"), AllowSurvey(false), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Map,
        [Description("Map Type"), AllowSurvey(false), EnableAudit(true), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        MapType,
        [Description("Reference Item"), AllowSurvey(false), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        ReferenceItem,
        [Description("Reference Item Type"), AllowSurvey(false), EnableAudit(true), IsType(true), ExcludeDataType(DataType.FilteredLookup | DataType.FusionLookup | DataType.JSON | DataType.JsonElement)]
        ReferenceItemType,
        [Description("Fusion Query Attribute"), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        FusionQueryAttribute,
        [Description("Fusion Query Attribute Type"), EnableAudit(true), IsType(true),
                    ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup |
                    DataType.FusionLookup | DataType.Link | DataType.Lookup | DataType.OwnershipLookup |
                    DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement)]
        FusionQueryAttributeType,
        [Description("Monitor"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Monitor,
        [Description("Issue Type"), EnableAudit(true), IsType(true), 
            ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup | DataType.FusionLookup |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement)]
        IssueType,
        [Description("Issue"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Issue,
        [Description("Rule Implementation"), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        RuleImplementation,
        [Description("Score Type Metric"), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        ScoreTypeMetric,
        [Description("Organization"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Organization,
        [Description("Organization Domain"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        OrganizationDomain,
        [Description("Organization Invitation"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        OrganizationInvitation,
        [Description("Contract"), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Contract,
        [Description("Shopping Cart Type"), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        ShoppingCartType,
        [Description("Shopping Cart"), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        ShoppingCart,
        [Description("Rule Implementation Type"), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        RuleImplementationType,
        [Description("Organization Type"), EnableAudit(true), IsType(true), 
            ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup | DataType.FusionLookup |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement)]
        OrganizationType,
        [Description("Export Template"), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        ExportTemplate
    }

    public class SystemObjectInfo
    {
        public SystemObjects ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool EnableAudit { get; set; }
        public bool IsType { get; set; }
    }

    public static class SystemObjectExtensions
    {
        public static bool IsAuditEnabled(this SystemObjects type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<EnableAuditAttribute>().Enabled;
        }

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

        public static List<SystemObjectInfo> GetSystemObjectInfoList(this SystemObjects type)
        {
            var list = new List<SystemObjectInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {                
                list.Add(new SystemObjectInfo
                {             
                    EnableAudit = ((EnableAuditAttribute)tm.GetCustomAttribute(typeof(EnableAuditAttribute))).Enabled,
                    IsType = ((IsTypeAttribute)tm.GetCustomAttribute(typeof(IsTypeAttribute))).IsType,
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (SystemObjects)Enum.Parse(typeof(SystemObjects), tm.Name),
                    Name = tm.Name
                });
            }

            return list;
        }    
    }
}
