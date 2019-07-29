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
         ExcludeDataType(DataType.JSON | DataType.Tag)]
        Unknown = -1,
        [Description("Artifact"), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        Artifact = 1,
        [Description("Synonym"), AllowSurvey(false), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        Synonym,
        [Description("Synonym Type"), AllowSurvey(false), EnableAudit(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        SynonymType,
        [Description("Artifact Type"), AllowSurvey(true), EnableAudit(true), IsType(true)]
        ArtifactType,
        [Description("Attribute"), AllowOwnership(false), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        Attribute,
        [Description("Attribute Group"), EnableAudit(false), IsType(true), 
         ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup | DataType.FusionLookup | 
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup |DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Tag)]
        AttributeType,
        [Description("Email Template"), AllowOwnership(false), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        EmailTemplate,
        [Description("Fusion"), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        Fusion,
        [Description("Fusion Attribute"), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        FusionAttribute,
        [Description("Fusion Attribute Type"), EnableAudit(true), IsType(true), 
        ExcludeDataType(DataType.FilteredLookup | DataType.FusionLookup | DataType.OwnershipLookup | DataType.RefListRelationship | DataType.Tag)]
        FusionAttributeType,
        [Description("Fusion Type"), EnableAudit(true), IsType(true), 
            ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup |
            DataType.FusionLookup | DataType.Link | DataType.Lookup | DataType.OwnershipLookup |
            DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Tag)]
        FusionType,
        [Description("Group"), EnableAudit(true), IsType(false),
         ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        Group,
        [Description("Intersect"), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        Intersect,
        [Description("Intersect Type"), EnableAudit(true), IsType(true), 
            ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup | DataType.FusionLookup |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Tag)]
        IntersectType,
        [Description("Lookup Item"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        Lookup,
        [Description("Lookup Type"), AllowSurvey(true), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        LookupType,
        [Description("Resource"), AllowOwnership(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        Resource,
        [Description("Resource Type"), AllowOwnership(false), AllowSurvey(true), EnableAudit(false), IsType(true),
            ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup | DataType.FusionLookup |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.JSON | DataType.JsonElement | DataType.Tag)]
        ResourceType,
        [Description("Survey Type"), AllowSurvey(false), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        SurveyType,
        [Description("Taxonomy"), EnableAudit(true), IsType(false), ExcludeDataType(DataType.Tag)]
        Taxonomy,
        [Description("Taxonomy Type"), AllowSurvey(true), EnableAudit(true), IsType(true), ExcludeDataType(DataType.FilteredLookup | DataType.FusionLookup)]
        TaxonomyType,
        [Description("Tooltip  Template"), AllowOwnership(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        TooltipTemplate,
        [Description("Field"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        Field,
        [Description("Field Type"), AllowSurvey(false), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        FieldType,
        [Description("Response Type"), AllowSurvey(false), EnableAudit(false), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        ResponseType,
        [Description("Score"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        Score,
        [Description("Score Type"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        ScoreType,
        [Description("Responsibility"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        Responsibility,
        [Description("Responsibility Type"), AllowSurvey(false), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        ResponsibilityType,
        [Description("Responsibility Type Claim"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        ResponsibilityTypeClaim,
        [Description("Claim"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        Claim,
        [Description("Bulk Load"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        Load,
        [Description("Report"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        Report,
        [Description("Attribute Type Category"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        AttributeTypeCategory,
        [Description("Policy"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Policy,
        [Description("Policy Type"), AllowSurvey(false), EnableAudit(true), IsType(true), ExcludeDataType(DataType.FilteredLookup | DataType.FusionLookup)]
        PolicyType,
        [Description("Rule"), AllowSurvey(false), EnableAudit(true), IsType(false)]
        Rule,
        [Description("Rule Type"), AllowSurvey(false), EnableAudit(true), IsType(true), ExcludeDataType(DataType.FilteredLookup | DataType.FusionLookup)]
        RuleType,
        [Description("Fusion Execution"), AllowSurvey(false), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        FusionExecution,
        [Description("Workflow Relation"), AllowSurvey(false), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        WorkflowTypeRelation,
        [Description("Predicate"), AllowSurvey(false), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        Predicate,
        [Description("Group Type"), AllowSurvey(false), EnableAudit(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        GroupType,
        [Description("Rule Dimension"), AllowSurvey(false), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        RuleDimension,        
        [Description("Map"), AllowSurvey(false), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        Map,
        [Description("Map Type"), AllowSurvey(false), EnableAudit(true), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        MapType,
        [Description("Reference Item"), AllowSurvey(false), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        ReferenceItem,
        [Description("Reference Item Type"), AllowSurvey(false), EnableAudit(true), IsType(true), ExcludeDataType(DataType.FilteredLookup | DataType.FusionLookup | DataType.JSON | DataType.JsonElement | DataType.Tag)]
        ReferenceItemType,
        [Description("Fusion Query Attribute"), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        FusionQueryAttribute,
        [Description("Fusion Query Attribute Type"), EnableAudit(true), IsType(true),
                    ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup |
                    DataType.FusionLookup | DataType.Link | DataType.Lookup | DataType.OwnershipLookup |
                    DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Tag)]
        FusionQueryAttributeType,
        [Description("Monitor"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        Monitor,
        [Description("Issue Type"), EnableAudit(true), IsType(true), 
            ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup | DataType.FusionLookup |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Tag)]
        IssueType,
        [Description("Issue"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        Issue,
        [Description("Rule Implementation"), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        RuleImplementation,
        [Description("Score Type Metric"), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        ScoreTypeMetric,
        [Description("Organization"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        Organization,
        [Description("Organization Domain"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        OrganizationDomain,
        [Description("Organization Invitation"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        OrganizationInvitation,
        [Description("Contract"), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        Contract,
        [Description("Shopping Cart Type"), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        ShoppingCartType,
        [Description("Shopping Cart"), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        ShoppingCart,
        [Description("Rule Implementation Type"), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
        RuleImplementationType,
        [Description("Organization Type"), EnableAudit(true), IsType(true), 
            ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup | DataType.FusionLookup |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Tag)]
        OrganizationType,
        [Description("Export Template"), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag)]
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
