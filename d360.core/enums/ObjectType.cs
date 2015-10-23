using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core
{
    /// <summary>
    /// Not sure this class is relevant any more.  May need to remove.  
    /// I was going to use this to automatically determine the class 
    /// level data that I needed when entering ObjectVersion items.
    /// </summary> 
    public static class ObjectTypeInfo
    {
        [Description("Artifact")]
        public const int Artifact = 1;

        [Description("Artifact Type")]
        public const int ArtifactType = 2;

        [Description("Attribute")]
        public const int Attribute = 3;

        [Description("Attribute Group")]
        public const int AttributeType = 4;

        [Description("Claim")]
        public const int Claim = 5;

        [Description("Claim")]
        public const int ClaimObject = 6;

        [Description("Domain")]
        public const int Domain = 7;

        [Description("Domain Group")]
        public const int DomainGroup = 8;

        [Description("Domain Item")]
        public const int DomainItem = 9;

        [Description("Domain Type")]
        public const int DomainType = 10;

        [Description("Email Template")]
        public const int EmailTemplate = 11;

        [Description("Event")]
        public const int Event = 12;

        [Description("Event Group")]
        public const int EventGroup = 13;

        [Description("Event Type")]
        public const int EventType = 14;

        [Description("Field")]
        public const int Field = 15;

        [Description("Field Type")]
        public const int FieldType = 16;

        [Description("Fusion")]
        public const int Fusion = 17;

        [Description("Fusion Attribute")]
        public const int FusionAttribute = 18;

        [Description("Fusion Attribute Type")]
        public const int FusionAttributeType = 19;

        [Description("Fusion Type")]
        public const int FusionType = 20;

        [Description("Group")]
        public const int Group = 21;

        [Description("Intersect")]
        public const int Intersect = 22;

        [Description("Intersect Type")]
        public const int IntersectType = 23;

        [Description("Lookup Item")]
        public const int Lookup = 24;

        [Description("Lookup Type")]
        public const int LookupType = 25;

        [Description("Question")]
        public const int Question = 26;

        [Description("Question Type")]
        public const int QuestionType = 27;

        [Description("Report")]
        public const int Report = 28;

        [Description("Report Tile")]
        public const int ReportTile = 29;

        [Description("Resolution")]
        public const int Resolution = 30;

        [Description("Resource")]
        public const int Resource = 31;

        [Description("Resource Type")]
        public const int ResourceType = 32;

        [Description("Response Type")]
        public const int ResponseType = 33;

        [Description("Responsibility")]
        public const int Responsibility = 34;

        [Description("Responsibility Type")]
        public const int ResponsibilityType = 35;

        [Description("Responsibility Type Claim")]
        public const int ResponsibilityTypeClaim = 36;

        [Description("Responsibility Type Group")]
        public const int ResponsibilityTypeGroup = 37;

        [Description("Statistic")]
        public const int Statistic = 38;

        [Description("Statistic Type")]
        public const int StatisticType = 39;

        [Description("Survey")]
        public const int Survey = 40;

        [Description("Survey Type")]
        public const int SurveyType = 41;

        [Description("Taxonomy")]
        public const int Taxonomy = 42;

        [Description("Taxonomy Type")]
        public const int TaxonomyType = 43;

        [Description("Taxonomy Type Level")]
        public const int TaxonomyTypeLevel = 44;

        [Description("Tooltip  Template")]
        public const int TooltipTemplate = 45;

        [Description("Vocabulary")]
        public const int Vocabulary = 46;

        [Description("Report Tile Type")]
        public const int ReportTileType = 47;

        [Description("Report Layout")]
        public const int ReportLayout = 48;

        [Description("Attribute Type Category")]
        public const int AttributeTypeCategory = 49;

        [Description("Policy")]
        public const int Policy = 50;

        [Description("Rule")]
        public const int Rule = 51;

        [Description("Synonym")]
        public const int Synonym = 52;

        [Description("Workflow")]
        public const int Workflow = 53;
    }
}
