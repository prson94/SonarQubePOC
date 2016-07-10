using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace d360.core
{
    public enum SystemObjects
    {
        [Description("Artifact")]
        Artifact = 1,
        [Description("Synonym"), AllowSurvey(false)]
        Synonym = 2,
        [Description("Synonym Type"), AllowSurvey(false)]
        SynonymType = 3,
        [Description("Artifact Type"), AllowSurvey(true)]
        ArtifactType = 4,
        [Description("Attribute"), AllowOwnership(false)]
        Attribute = 5,
        [Description("Attribute Group")]
        AttributeType = 6,
        [Description("Domain List")]
        Domain = 7,
        [Description("Domain List Group"), AllowOwnership(false)]
        DomainGroup = 8,
        [Description("Domain List Item"), AllowOwnership(false)]
        DomainItem = 9,
        [Description("Domain List Type"), AllowSurvey(true)]
        DomainType = 10,
        [Description("Email Template"), AllowOwnership(false)]
        EmailTemplate = 11,
        [Description("Event")]
        Event = 12,
        [Description("Event Type")]
        EventType = 13,
        [Description("Fusion")]
        Fusion = 14,
        [Description("Fusion Attribute")]
        FusionAttribute = 15,
        [Description("Fusion Attribute Type")]
        FusionAttributeType = 16,
        [Description("FusionIntersect")]
        FusionIntersect = 17,
        [Description("Fusion Intersect Type")]
        FusionIntersectType = 18,
        [Description("Fusion Type")]
        FusionType = 19,
        [Description("Group")]
        Group = 20,
        [Description("Intersect")]
        Intersect = 21,
        [Description("Intersect Type")]
        IntersectType = 22,
        [Description("Lookup Item")]
        Lookup = 23,
        [Description("Lookup Type"), AllowSurvey(true)]
        LookupType = 24,
        [Description("Ownership"), AllowOwnership(false)]
        Ownership = 25,
        [Description("Ownership Type"), AllowOwnership(false)]
        OwnershipType = 26,
        [Description("Resource"), AllowOwnership(false)]
        Resource = 27,
        [Description("Resource Type"), AllowOwnership(false), AllowSurvey(true)]
        ResourceType = 28,
        [Description("Role"), AllowOwnership(false), AllowSurvey(true)]
        Role = 29,
        [Description("Survey Type"), AllowSurvey(false)]
        SurveyType = 30,
        [Description("Taxonomy")]
        Taxonomy = 31,
        [Description("Taxonomy Type"), AllowSurvey(true)]
        TaxonomyType = 32,
        [Description("Tooltip  Template"), AllowOwnership(false)]
        TooltipTemplate = 33,
        [Description("Event Group")]
        EventGroup = 34,
        [Description("Field")]
        Field = 35,
        [Description("Field Type"), AllowSurvey(false)]
        FieldType = 36,
        [Description("Resolution"), AllowSurvey(true)]
        Resolution = 37,
        [Description("Response Type"), AllowSurvey(false)]
        ResponseType = 38,
        [Description("Statistic"), AllowSurvey(false)]
        Statistic = 39,
        [Description("Statistic Type"), AllowSurvey(false)]
        StatisticType = 40,
        [Description("Responsibility"), AllowSurvey(false)]
        Responsibility = 41,
        [Description("Responsibility Type"), AllowSurvey(false)]
        ResponsibilityType = 42,
        [Description("Responsibility Type Claim"), AllowSurvey(false)]
        ResponsibilityTypeClaim = 43,
        [Description("Responsibility Type Group"), AllowSurvey(false)]
        ResponsibilityTypeGroup = 44,
        [Description("Claim"), AllowSurvey(false)]
        Claim = 45,
        [Description("Bulk Load"), AllowSurvey(false)]
        Load = 46,
        [Description("Report"), AllowSurvey(false)]
        Report = 47,
        [Description("Attribute Type Category"), AllowSurvey(false)]
        AttributeTypeCategory = 48,
        [Description("Policy"), AllowSurvey(false)]
        Policy = 49,
        [Description("Policy Type"), AllowSurvey(false)]
        PolicyType = 50,
        [Description("Policy Type Class"), AllowSurvey(false)]
        PolicyTypeClass = 51,
        [Description("Rule"), AllowSurvey(false)]
        Rule = 52,
        [Description("Rule Type"), AllowSurvey(false)]
        RuleType = 53,
        [Description("Fusion Execution"), AllowSurvey(false)]
        FusionExecution = 54,
        [Description("Workflow Relation"), AllowSurvey(false)]
        WorkflowTypeRelation = 55,
        [Description("Taxonomy Type Class"), AllowSurvey(false)]
        TaxonomyTypeClass = 56,
        [Description("Predicate"), AllowSurvey(false)]
        Predicate = 57,
        [Description("Group Type"), AllowSurvey(false)]
        GroupType = 58,
        [Description("Relation")]
        Relation = 59,
        [Description("Relation Type")]
        RelationType = 60,
        [Description("Rule Dimension"), AllowSurvey(false)]
        RuleDimension = 61,
        [Description("Monitor"), AllowSurvey(false)]
        Monitor = 61,
        [Description("Map"), AllowSurvey(false)]
        Map= 62
    }

    public class SystemObjectInfo
    {
        public SystemObjects ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        //public bool AllowOwnership { get; set; }
        //public bool AllowSurvey { get; set; }
    }

    public static class SystemObjectExtensions
    {
        public static List<SystemObjectInfo> GetSystemObjectInfoList(this SystemObjects type)
        {
            var list = new List<SystemObjectInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                //var aAttrOwnership = ((AllowOwnershipAttribute)tm.GetCustomAttribute(typeof(AllowOwnershipAttribute)));
                //var aAttrSurvey = ((AllowSurveyAttribute)tm.GetCustomAttribute(typeof(AllowSurveyAttribute)));
                list.Add(new SystemObjectInfo
                {
                    //AllowOwnership = (aAttrOwnership != null) ? aAttrOwnership.Allowed : true,
                    //AllowSurvey = (aAttrSurvey != null) ? aAttrSurvey.Allowed : false,
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (SystemObjects)Enum.Parse(typeof(SystemObjects), tm.Name),
                    Name = tm.Name
                });
            }

            return list;
        }    
    }
}
