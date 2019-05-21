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
        [Description("Artifact"), EnableAudit(true), IsType(false),
         ExcludeDataType(DataType.JSON)]
        Artifact = 1,
        [Description("Synonym"), AllowSurvey(false), EnableAudit(false), IsType(false),
         ExcludeDataType(DataType.JSON)]
        Synonym,
        [Description("Synonym Type"), AllowSurvey(false), EnableAudit(false), IsType(true),
         ExcludeDataType(DataType.JSON)]
        SynonymType,
        [Description("Artifact Type"), AllowSurvey(true), EnableAudit(true), IsType(true)]
        ArtifactType,
        [Description("Attribute"), AllowOwnership(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON)]
        Attribute,
        [Description("Attribute Group"), EnableAudit(false), IsType(true), 
         ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup | DataType.FusionLookup | 
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup |DataType.Relationship | DataType.JSON)]
        AttributeType,
        [Description("Email Template"), AllowOwnership(false), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON)]
        EmailTemplate,
        [Description("Fusion"), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON)]
        Fusion,
        [Description("Fusion Attribute"), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON)]
        FusionAttribute,
        [Description("Fusion Attribute Type"), EnableAudit(true), IsType(true), 
        ExcludeDataType(DataType.FilteredLookup | DataType.FusionLookup | DataType.OwnershipLookup | DataType.RefListRelationship)]
        FusionAttributeType,
        [Description("Fusion Type"), EnableAudit(true), IsType(true), 
            ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup |
            DataType.FusionLookup | DataType.Link | DataType.Lookup | DataType.OwnershipLookup |
            DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON)]
        FusionType,
        [Description("Group"), EnableAudit(true), IsType(false),
         ExcludeDataType(DataType.JSON)]
        Group,
        [Description("Intersect"), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON)]
        Intersect,
        [Description("Intersect Type"), EnableAudit(true), IsType(true), 
            ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup | DataType.FusionLookup |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON)]
        IntersectType,
        [Description("Lookup Item"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON)]
        Lookup,
        [Description("Lookup Type"), AllowSurvey(true), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON)]
        LookupType,
        [Description("Resource"), AllowOwnership(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON)]
        Resource,
        [Description("Resource Type"), AllowOwnership(false), AllowSurvey(true), EnableAudit(false), IsType(true),
            ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup | DataType.FusionLookup |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.JSON)]
        ResourceType,
        [Description("Survey Type"), AllowSurvey(false), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON)]
        SurveyType,
        [Description("Taxonomy"), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON)]
        Taxonomy,
        [Description("Taxonomy Type"), AllowSurvey(true), EnableAudit(true), IsType(true), 
            ExcludeDataType(DataType.FilteredLookup | DataType.FusionLookup)]
        TaxonomyType,
        [Description("Tooltip  Template"), AllowOwnership(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON)]
        TooltipTemplate,
        [Description("Field"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON)]
        Field,
        [Description("Field Type"), AllowSurvey(false), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON)]
        FieldType,
        [Description("Response Type"), AllowSurvey(false), EnableAudit(false), IsType(true),ExcludeDataType(DataType.JSON)]
        ResponseType,
        [Description("Score"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON)]
        Score,
        [Description("Score Type"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON)]
        ScoreType,
        [Description("Responsibility"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON)]
        Responsibility,
        [Description("Responsibility Type"), AllowSurvey(false), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON)]
        ResponsibilityType,
        [Description("Responsibility Type Claim"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON)]
        ResponsibilityTypeClaim,
        [Description("Claim"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON)]
        Claim,
        [Description("Bulk Load"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON)]
        Load,
        [Description("Report"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON)]
        Report,
        [Description("Attribute Type Category"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON)]
        AttributeTypeCategory,
        [Description("Policy"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON)]
        Policy,
        [Description("Policy Type"), AllowSurvey(false), EnableAudit(true), IsType(true),
              ExcludeDataType(DataType.FilteredLookup | DataType.FusionLookup)]
        PolicyType,
        [Description("Rule"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON)]
        Rule,
        [Description("Rule Type"), AllowSurvey(false), EnableAudit(true), IsType(true), 
            ExcludeDataType(DataType.FilteredLookup | DataType.FusionLookup | DataType.JSON)]
        RuleType,
        [Description("Fusion Execution"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON)]
        FusionExecution,
        [Description("Workflow Relation"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON)]
        WorkflowTypeRelation,
        [Description("Predicate"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON)]
        Predicate,
        [Description("Group Type"), AllowSurvey(false), EnableAudit(false), IsType(true),ExcludeDataType(DataType.JSON)]
        GroupType,
        [Description("Rule Dimension"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON)]
        RuleDimension,        
        [Description("Map"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON)]
        Map,
        [Description("Map Type"), AllowSurvey(false), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON)]
        MapType,
        [Description("Reference Item"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON)]
        ReferenceItem,
        [Description("Reference Item Type"), AllowSurvey(false), EnableAudit(true), IsType(true),
               ExcludeDataType(DataType.FilteredLookup | DataType.FusionLookup | DataType.JSON)]
        ReferenceItemType,
        [Description("Fusion Query Attribute"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON)]
        FusionQueryAttribute,
        [Description("Fusion Query Attribute Type"), EnableAudit(true), IsType(true),
                    ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup |
                    DataType.FusionLookup | DataType.Link | DataType.Lookup | DataType.OwnershipLookup |
                    DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON)]
        FusionQueryAttributeType,
        [Description("Monitor"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON)]
        Monitor,
        [Description("Issue Type"), EnableAudit(true), IsType(true), 
            ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup | DataType.FusionLookup |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON)]
        IssueType,
        [Description("Issue"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON)]
        Issue,
        [Description("Rule Implementation"), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON)]
        RuleImplementation,
        [Description("Score Type Metric"), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON)]
        ScoreTypeMetric,
        [Description("Organization"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON)]
        Organization,
        [Description("Organization Domain"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON)]
        OrganizationDomain,
        [Description("Organization Invitation"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON)]
        OrganizationInvitation,
        [Description("Contract"), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON)]
        Contract,
        [Description("Shopping Cart Type"), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON)]
        ShoppingCartType,
        [Description("Shopping Cart"), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON)]
        ShoppingCart,
        [Description("Rule Implementation Type"), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON)]
        RuleImplementationType,
        [Description("Organization Type"), EnableAudit(true), IsType(true), 
            ExcludeDataType(DataType.FieldFromRelationship | DataType.FilteredLookup | DataType.FusionLookup |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON)]
        OrganizationType,
        [Description("Export Template"), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON)]
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
