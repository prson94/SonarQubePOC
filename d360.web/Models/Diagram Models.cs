using d360.core;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.web.Models
{
    public class DbMapItem
    {
        public string Sub { get; set; }
        public int SubID { get; set; }
        public string SubjectID { get; set; }
        public string Subject { get; set; }
        public string SubjectType { get; set; }
        public string SubjectBackColor { get; set; }
        public string SubjectForeColor { get; set; }

        public string Obj { get; set; }
        public int ObjID { get; set; }
        public string ObjectID { get; set; }
        public string Object { get; set; }
        public string ObjectType { get; set; }
        public string ObjectBackColor { get; set; }
        public string ObjectForeColor { get; set; }

        public int Level { get; set; }

        public string Predicate { get; set; }
        public bool Exclude { get; set; }
        public int IntersectMapID { get; set; }
    }

    public class JsonNodeItem
    {
        public string key { get; set; }
        public string obj { get; set; }
        public int objid { get; set; }
        public string type { get; set; }
        public string objecttype { get; set; }
        public int objecttypeid { get; set; }
        public string name { get; set; }
        public string back { get; set; }
        public string fore { get; set; }
        public int level { get; set; }
        public bool exclude { get; set; }
        public int intersectMapId { get; set; }
        public int intersectId { get; set; }

        public int sourceRuleCount { get; set; }

        public int mappingRuleCount { get; set; }
        public override string ToString()
        {
            return level.ToString();
        }
    }

    public class JsonLinkItem
    {
        public int id { get; set; }
        public string from { get; set; }
        public string frompid { get { return "OUT"; } }
        public string to { get; set; }
        public string text { get; set; }
        public int predicateId { get; set; }
        public int mappingRuleCount { get; set; }
    }

    public class DiagramModel
    {
        public DiagramModel()
        {
            nodes = new List<JsonNodeItem>();
            links = new List<JsonLinkItem>();
        }

        public List<JsonNodeItem> nodes { get; set; }

        public List<JsonLinkItem> links { get; set; }
    }

    public class DiagramNode
    {
        public string ID { get; set; }
        public string Key { get; set; }
        public int ParentID { get; set; }
        public SystemObjects Type { get; set; }
        public string ObjectID { get; set; }
        public string TypeName { get; set; }
        public int IntersectMapID { get; set; }
    }

    public class DiagramLink
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Text { get; set; }
        public int PredicateID { get; set; }
        public int IntersectTypeID { get; set; }
        public int IntersectTypeRoleID { get; set; }
        public DiagramNode FromNode { get; set; }
        public DiagramNode ToNode { get; set; }
    }

    public class DiagramChanges
    {
        public string TargetType { get; set; }
        public string TargetID { get; set; }
        public List<DiagramLink> AddedLinks { get; set; }
        public List<DiagramNode> DeletedNodes { get; set; }
        public List<DiagramNode> AllNodes { get; set; }
        public List<DiagramLink> AllLinks { get; set; }

    }

    [DataContract]
    public class InformationCatalogDiagramDataItem
    {
        [DataMember(Name = "key")]
        public int ID { get; set; }
        //[IgnoreDataMember]
        [DataMember(Name = "parent")]
        public int? ParentID { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "url")]
        public string Url { get; set; }
        [DataMember]
        public bool RelationshipsExist { get; set; }
        [DataMember(Name = "children")]
        public List<InformationCatalogDiagramDataItem> Children { get; set; }
    }

}