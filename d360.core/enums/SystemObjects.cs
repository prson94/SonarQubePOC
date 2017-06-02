using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Linq;

namespace d360.core
{
    public enum SystemObjects
    {
        [Description("Artifact"), EnableAudit(true)]
        Artifact = 1,
        [Description("Synonym"), AllowSurvey(false), EnableAudit(false)]
        Synonym,
        [Description("Synonym Type"), AllowSurvey(false), EnableAudit(false)]
        SynonymType,
        [Description("Artifact Type"), AllowSurvey(true), EnableAudit(true)]
        ArtifactType,
        [Description("Attribute"), AllowOwnership(false), EnableAudit(true)]
        Attribute,
        [Description("Attribute Group"), EnableAudit(false)]
        AttributeType,
        [Description("Email Template"), AllowOwnership(false), EnableAudit(true)]
        EmailTemplate,
        [Description("Fusion"), EnableAudit(true)]
        Fusion,
        [Description("Fusion Attribute"), EnableAudit(false)]
        FusionAttribute,
        [Description("Fusion Attribute Type"), EnableAudit(true)]
        FusionAttributeType,
        [Description("Fusion Type"), EnableAudit(true)]
        FusionType,
        [Description("Group"), EnableAudit(true)]
        Group,
        [Description("Intersect"), EnableAudit(false)]
        Intersect,
        [Description("Intersect Type"), EnableAudit(true)]
        IntersectType,
        [Description("Lookup Item"), EnableAudit(false)]
        Lookup,
        [Description("Lookup Type"), AllowSurvey(true), EnableAudit(true)]
        LookupType,
        [Description("Resource"), AllowOwnership(false), EnableAudit(false)]
        Resource,
        [Description("Resource Type"), AllowOwnership(false), AllowSurvey(true), EnableAudit(false)]
        ResourceType,
        [Description("Survey Type"), AllowSurvey(false), EnableAudit(true)]
        SurveyType,
        [Description("Taxonomy"), EnableAudit(true)]
        Taxonomy,
        [Description("Taxonomy Type"), AllowSurvey(true), EnableAudit(true)]
        TaxonomyType,
        [Description("Tooltip  Template"), AllowOwnership(false), EnableAudit(false)]
        TooltipTemplate,
        [Description("Field"), EnableAudit(false)]
        Field,
        [Description("Field Type"), AllowSurvey(false), EnableAudit(true)]
        FieldType,
        [Description("Response Type"), AllowSurvey(false), EnableAudit(false)]
        ResponseType,
        [Description("Score"), AllowSurvey(false), EnableAudit(false)]
        Score,
        [Description("Score Type"), AllowSurvey(false), EnableAudit(true)]
        ScoreType,
        [Description("Responsibility"), AllowSurvey(false), EnableAudit(true)]
        Responsibility,
        [Description("Responsibility Type"), AllowSurvey(false), EnableAudit(true)]
        ResponsibilityType,
        [Description("Responsibility Type Claim"), AllowSurvey(false), EnableAudit(false)]
        ResponsibilityTypeClaim,
        [Description("Claim"), AllowSurvey(false), EnableAudit(false)]
        Claim,
        [Description("Bulk Load"), AllowSurvey(false), EnableAudit(false)]
        Load,
        [Description("Report"), AllowSurvey(false), EnableAudit(true)]
        Report,
        [Description("Attribute Type Category"), AllowSurvey(false), EnableAudit(false)]
        AttributeTypeCategory,
        [Description("Policy"), AllowSurvey(false), EnableAudit(true)]
        Policy,
        [Description("Policy Type"), AllowSurvey(false), EnableAudit(true)]
        PolicyType,
        [Description("Policy Type Class"), AllowSurvey(false), EnableAudit(true)]
        PolicyTypeClass,
        [Description("Rule"), AllowSurvey(false), EnableAudit(true)]
        Rule,
        [Description("Rule Type"), AllowSurvey(false), EnableAudit(true)]
        RuleType,
        [Description("Fusion Execution"), AllowSurvey(false), EnableAudit(false)]
        FusionExecution,
        [Description("Workflow Relation"), AllowSurvey(false), EnableAudit(false)]
        WorkflowTypeRelation,
        [Description("Taxonomy Type Class"), AllowSurvey(false), EnableAudit(true)]
        TaxonomyTypeClass,
        [Description("Predicate"), AllowSurvey(false), EnableAudit(true)]
        Predicate,
        [Description("Group Type"), AllowSurvey(false), EnableAudit(false)]
        GroupType,
        [Description("Rule Dimension"), AllowSurvey(false), EnableAudit(true)]
        RuleDimension,        
        [Description("Map"), AllowSurvey(false), EnableAudit(true)]
        Map,
        [Description("Map Type"), AllowSurvey(false), EnableAudit(true)]
        MapType,
        [Description("Intersect Role"), AllowSurvey(false), EnableAudit(true)]
        IntersectRole,
        [Description("Reference Item"), AllowSurvey(false), EnableAudit(true)]
        ReferenceItem,
        [Description("Reference Item Type"), AllowSurvey(false), EnableAudit(true)]
        ReferenceItemType,
        [Description("Fusion Query Attribute"), EnableAudit(false)]
        FusionQueryAttribute,
        [Description("Fusion Query Attribute Type"), EnableAudit(true)]
        FusionQueryAttributeType,
        [Description("Monitor"), AllowSurvey(false), EnableAudit(false)]
        Monitor,
        [Description("Issue Type"), EnableAudit(true)]
        IssueType,
        [Description("Issue"), EnableAudit(false)]
        Issue,
        [Description("Rule Implementation"), EnableAudit(true)]
        RuleImplementation,
        [Description("Score Type Metric"), EnableAudit(true)]
        ScoreTypeMetric,
        [Description("Organization"), EnableAudit(false)]
        Organization,
        [Description("Organization Domain"), EnableAudit(false)]
        OrganizationDomain,
        [Description("Organization Invitation"), EnableAudit(false)]
        OrganizationInvitation,
        [Description("Contract"), EnableAudit(true)]
        Contract,
        [Description("Shopping Cart Type"), EnableAudit(true)]
        ShoppingCartType,
        [Description("Shopping Cart"), EnableAudit(true)]
        ShoppingCart
    }

    public class SystemObjectInfo
    {
        public SystemObjects ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        //public bool AllowOwnership { get; set; }
        public bool EnableAudit { get; set; }
    }

    public static class SystemObjectExtensions
    {
        public static bool IsAuditEnabled(this SystemObjects type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<EnableAuditAttribute>().Enabled;
        }

        public static List<SystemObjectInfo> GetSystemObjectInfoList(this SystemObjects type)
        {
            var list = new List<SystemObjectInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                //var aAttrOwnership = ((AllowOwnershipAttribute)tm.GetCustomAttribute(typeof(AllowOwnershipAttribute)));
                list.Add(new SystemObjectInfo
                {
                    //AllowOwnership = (aAttrOwnership != null) ? aAttrOwnership.Allowed : true,
                    EnableAudit = ((EnableAuditAttribute)tm.GetCustomAttribute(typeof(EnableAuditAttribute))).Enabled,
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (SystemObjects)Enum.Parse(typeof(SystemObjects), tm.Name),
                    Name = tm.Name
                });
            }

            return list;
        }    
    }
}
