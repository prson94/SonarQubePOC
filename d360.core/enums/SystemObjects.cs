using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Linq;

namespace d360.core
{
    public enum SystemObjects
    {
        [Description("Artifact"), EnableAudit(true), IsType(false)]
        Artifact = 1,
        [Description("Synonym"), AllowSurvey(false), EnableAudit(false), IsType(false)]
        Synonym,
        [Description("Synonym Type"), AllowSurvey(false), EnableAudit(false), IsType(true)]
        SynonymType,
        [Description("Artifact Type"), AllowSurvey(true), EnableAudit(true), IsType(true)]
        ArtifactType,
        [Description("Attribute"), AllowOwnership(false), EnableAudit(true), IsType(false)]
        Attribute,
        [Description("Attribute Group"), EnableAudit(false), IsType(true)]
        AttributeType,
        [Description("Email Template"), AllowOwnership(false), EnableAudit(true), IsType(false)]
        EmailTemplate,
        [Description("Fusion"), EnableAudit(true), IsType(false)]
        Fusion,
        [Description("Fusion Attribute"), EnableAudit(false), IsType(false)]
        FusionAttribute,
        [Description("Fusion Attribute Type"), EnableAudit(true), IsType(true)]
        FusionAttributeType,
        [Description("Fusion Type"), EnableAudit(true), IsType(true)]
        FusionType,
        [Description("Group"), EnableAudit(true), IsType(false)]
        Group,
        [Description("Intersect"), EnableAudit(false), IsType(false)]
        Intersect,
        [Description("Intersect Type"), EnableAudit(true), IsType(true)]
        IntersectType,
        [Description("Lookup Item"), EnableAudit(false), IsType(false)]
        Lookup,
        [Description("Lookup Type"), AllowSurvey(true), EnableAudit(true), IsType(true)]
        LookupType,
        [Description("Resource"), AllowOwnership(false), EnableAudit(false), IsType(false)]
        Resource,
        [Description("Resource Type"), AllowOwnership(false), AllowSurvey(true), EnableAudit(false), IsType(true)]
        ResourceType,
        [Description("Survey Type"), AllowSurvey(false), EnableAudit(true), IsType(true)]
        SurveyType,
        [Description("Taxonomy"), EnableAudit(true), IsType(false)]
        Taxonomy,
        [Description("Taxonomy Type"), AllowSurvey(true), EnableAudit(true), IsType(true)]
        TaxonomyType,
        [Description("Tooltip  Template"), AllowOwnership(false), EnableAudit(false), IsType(false)]
        TooltipTemplate,
        [Description("Field"), EnableAudit(false), IsType(false)]
        Field,
        [Description("Field Type"), AllowSurvey(false), EnableAudit(true), IsType(true)]
        FieldType,
        [Description("Response Type"), AllowSurvey(false), EnableAudit(false), IsType(true)]
        ResponseType,
        [Description("Score"), AllowSurvey(false), EnableAudit(false), IsType(false)]
        Score,
        [Description("Score Type"), AllowSurvey(false), EnableAudit(true), IsType(false)]
        ScoreType,
        [Description("Responsibility"), AllowSurvey(false), EnableAudit(true), IsType(false)]
        Responsibility,
        [Description("Responsibility Type"), AllowSurvey(false), EnableAudit(true), IsType(true)]
        ResponsibilityType,
        [Description("Responsibility Type Claim"), AllowSurvey(false), EnableAudit(false), IsType(false)]
        ResponsibilityTypeClaim,
        [Description("Claim"), AllowSurvey(false), EnableAudit(false), IsType(false)]
        Claim,
        [Description("Bulk Load"), AllowSurvey(false), EnableAudit(false), IsType(false)]
        Load,
        [Description("Report"), AllowSurvey(false), EnableAudit(true), IsType(false)]
        Report,
        [Description("Attribute Type Category"), AllowSurvey(false), EnableAudit(false), IsType(false)]
        AttributeTypeCategory,
        [Description("Policy"), AllowSurvey(false), EnableAudit(true), IsType(false)]
        Policy,
        [Description("Policy Type"), AllowSurvey(false), EnableAudit(true), IsType(true)]
        PolicyType,
        [Description("Rule"), AllowSurvey(false), EnableAudit(true), IsType(false)]
        Rule,
        [Description("Rule Type"), AllowSurvey(false), EnableAudit(true), IsType(true)]
        RuleType,
        [Description("Fusion Execution"), AllowSurvey(false), EnableAudit(false), IsType(false)]
        FusionExecution,
        [Description("Workflow Relation"), AllowSurvey(false), EnableAudit(false), IsType(false)]
        WorkflowTypeRelation,
        [Description("Predicate"), AllowSurvey(false), EnableAudit(true), IsType(false)]
        Predicate,
        [Description("Group Type"), AllowSurvey(false), EnableAudit(false), IsType(true)]
        GroupType,
        [Description("Rule Dimension"), AllowSurvey(false), EnableAudit(true), IsType(false)]
        RuleDimension,        
        [Description("Map"), AllowSurvey(false), EnableAudit(true), IsType(false)]
        Map,
        [Description("Map Type"), AllowSurvey(false), EnableAudit(true), IsType(true)]
        MapType,
        [Description("Reference Item"), AllowSurvey(false), EnableAudit(true), IsType(false)]
        ReferenceItem,
        [Description("Reference Item Type"), AllowSurvey(false), EnableAudit(true), IsType(true)]
        ReferenceItemType,
        [Description("Fusion Query Attribute"), EnableAudit(false), IsType(false)]
        FusionQueryAttribute,
        [Description("Fusion Query Attribute Type"), EnableAudit(true), IsType(true)]
        FusionQueryAttributeType,
        [Description("Monitor"), AllowSurvey(false), EnableAudit(false), IsType(false)]
        Monitor,
        [Description("Issue Type"), EnableAudit(true), IsType(true)]
        IssueType,
        [Description("Issue"), EnableAudit(false), IsType(false)]
        Issue,
        [Description("Rule Implementation"), EnableAudit(true), IsType(false)]
        RuleImplementation,
        [Description("Score Type Metric"), EnableAudit(true), IsType(false)]
        ScoreTypeMetric,
        [Description("Organization"), EnableAudit(false), IsType(false)]
        Organization,
        [Description("Organization Domain"), EnableAudit(false), IsType(false)]
        OrganizationDomain,
        [Description("Organization Invitation"), EnableAudit(false), IsType(false)]
        OrganizationInvitation,
        [Description("Contract"), EnableAudit(true), IsType(false)]
        Contract,
        [Description("Shopping Cart Type"), EnableAudit(true), IsType(true)]
        ShoppingCartType,
        [Description("Shopping Cart"), EnableAudit(true), IsType(false)]
        ShoppingCart,
        [Description("Rule Implementation Type"), EnableAudit(true), IsType(true)]
        RuleImplementationType,
        [Description("Organization Type"), EnableAudit(true), IsType(true)]
        OrganizationType
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
