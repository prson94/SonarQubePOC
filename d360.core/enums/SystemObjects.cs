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
         ExcludeDataType(DataType.JSON | DataType.Path | DataType.Tag | DataType.Counter)]
        Unknown = -1,
        [Description("Artifact"), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Counter)]
        Artifact = 1,
        [Description("Synonym"), AllowSurvey(false), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Synonym,
        [Description("Synonym Type"), AllowSurvey(false), EnableAudit(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        SynonymType,
        [Description("Artifact Type"), AllowSurvey(true), EnableAudit(true), IsType(true)]
        ArtifactType,
        [Description("Email Template"), AllowOwnership(false), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Counter)]
        EmailTemplate,
        [Description("Fusion"), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        Fusion,
        [Description("Fusion Attribute"), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        FusionAttribute,
        [Description("Fusion Attribute Type"), EnableAudit(true), IsType(true), 
        ExcludeDataType(DataType.OwnershipLookup | DataType.Path | DataType.RefListRelationship | DataType.Tag | DataType.Score | DataType.Counter)]
        FusionAttributeType,
        [Description("Fusion Type"), EnableAudit(true), IsType(true), 
            ExcludeDataType(DataType.ComplexRelationLookup | DataType.FieldFromRelationship | DataType.JSON | DataType.JsonElement | 
            DataType.Link | DataType.Lookup | DataType.OwnershipLookup | DataType.Path | DataType.RefListRelationship | DataType.Relationship | DataType.Tag | DataType.Score | DataType.Counter)]
        FusionType,
        [Description("Group"), EnableAudit(true), IsType(false),
         ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Group,
        [Description("Intersect"), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Intersect,
        [Description("Intersect Type"), EnableAudit(true), IsType(true), 
            ExcludeDataType(DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        IntersectType,
        [Description("Resource"), AllowOwnership(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Resource,
        [Description("Resource Type"), AllowOwnership(false), AllowSurvey(true), EnableAudit(false), IsType(true),
            ExcludeDataType(DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        ResourceType,
        [Description("Survey Type"), AllowSurvey(false), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        SurveyType,
        [Description("Tag"), EnableAudit(true), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Counter)]
        Tag,
        [Description("Taxonomy"), EnableAudit(true), IsType(false), ExcludeDataType(DataType.Tag)]
        Taxonomy,
        [Description("Taxonomy Type"), AllowSurvey(true), EnableAudit(true), IsType(true)]
        TaxonomyType,
        [Description("Tooltip  Template"), AllowOwnership(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        TooltipTemplate,
        [Description("Field"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Field,
        [Description("Field Type"), AllowSurvey(false), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        FieldType,
        [Description("Response Type"), AllowSurvey(false), EnableAudit(false), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        ResponseType,
        [Description("Score"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Score,
        [Description("Score Type"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        ScoreType,
        [Description("Responsibility"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        Responsibility,
        [Description("Responsibility Type"), AllowSurvey(false), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        ResponsibilityType,
        [Description("Responsibility Type Claim"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        ResponsibilityTypeClaim,
        [Description("Claim"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Claim,
        [Description("Bulk Load"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        Load,
        [Description("Report"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Report,
        [Description("Attribute Type Category"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        AttributeTypeCategory,
        [Description("Policy"), AllowSurvey(false), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement)]
        Policy,
        [Description("Policy Type"), AllowSurvey(false), EnableAudit(true), IsType(true)]
        PolicyType,
        [Description("Rule"), AllowSurvey(false), EnableAudit(true), IsType(false)]
        Rule,
        [Description("Rule Type"), AllowSurvey(false), EnableAudit(true), IsType(true)]
        RuleType,
        [Description("Fusion Execution"), AllowSurvey(false), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        FusionExecution,
        [Description("Workflow Relation"), AllowSurvey(false), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        WorkflowTypeRelation,
        [Description("Predicate"), AllowSurvey(false), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        Predicate,
        [Description("Group Type"), AllowSurvey(false), EnableAudit(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        GroupType,
        [Description("Rule Dimension"), AllowSurvey(false), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        RuleDimension,        
        [Description("Map"), AllowSurvey(false), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Map,
        [Description("Map Type"), AllowSurvey(false), EnableAudit(true), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        MapType,
        [Description("Reference Item"), AllowSurvey(false), EnableAudit(true), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Score | DataType.Counter)]
        ReferenceItem,
        [Description("Reference Item Type"), AllowSurvey(false), EnableAudit(true), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Score | DataType.Counter)]
        ReferenceItemType,
        [Description("Fusion Query Attribute"), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        FusionQueryAttribute,
        [Description("Fusion Query Attribute Type"), EnableAudit(true), IsType(true),
                    ExcludeDataType(DataType.ComplexRelationLookup | DataType.FieldFromRelationship | DataType.JSON | DataType.JsonElement |
                    DataType.Link | DataType.Lookup | DataType.OwnershipLookup | DataType.Path | DataType.RefListRelationship | DataType.Relationship | DataType.Tag | DataType.Score | DataType.Counter)]
        FusionQueryAttributeType,
        [Description("Monitor"), AllowSurvey(false), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        Monitor,
        [Description("Issue Type"), EnableAudit(true), IsType(true), 
            ExcludeDataType(DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        IssueType,
        [Description("Issue"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        Issue,
        [Description("Score Type Metric"), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        ScoreTypeMetric,
        [Description("Organization"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        Organization,
        [Description("Organization Domain"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        OrganizationDomain,
        [Description("Organization Invitation"), EnableAudit(false), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        OrganizationInvitation,
        [Description("Contract"), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        Contract,
        [Description("Shopping Cart Type"), EnableAudit(true), IsType(true),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        ShoppingCartType,
        [Description("Shopping Cart"), EnableAudit(true), IsType(false),ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        ShoppingCart,
        [Description("Organization Type"), EnableAudit(true), IsType(true), 
            ExcludeDataType(DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        OrganizationType,
        [Description("Export Template"), EnableAudit(true), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter)]
        ExportTemplate,
        [Description("Task Type"), AllowSurvey(false), EnableAudit(false), IsType(true), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter)]
        TaskType,
        [Description("Task"), AllowSurvey(false), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter)]
        Task,
        [Description("Connector Label"), AllowSurvey(false), EnableAudit(false), IsType(false), ExcludeDataType(DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter)]
        ConnectorLabel,
        [Description("Issue Type Relation"), EnableAudit(true), IsType(true),
        ExcludeDataType(DataType.FieldFromRelationship |
        DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter)]
        IssueTypeRelation
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
