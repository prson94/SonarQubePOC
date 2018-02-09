using d360.core;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.web.Models
{
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
        public int mapId { get; set; }
        public int intersectId { get; set; }
        public int sourceRuleCount { get; set; }
        public int mappingRuleCount { get; set; }
        public int challengeCount { get; set; }
        public int openEventCount { get; set; }
        public int openIssueCount { get; set; }

        public int transformationCount { get; set; }
        public override string ToString()
        {
            return level.ToString();
        }

        public int intersectMapId { get; set; }
    }

    public class JsonLinkItem
    {
        public int id { get; set; }
        public string from { get; set; }
        public string frompid { get { return "OUT"; } }
        public string to { get; set; }
        public string text { get; set; }
        public int intersectRoleId { get; set; }
        public int mappingRuleCount { get; set; }
        public int transformationCount { get; set; }

        public int intersectTypeId { get; set; }
        public int predicateId { get; set; }
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
    

    [DataContract]
    public class InformationCatalogDiagramDataItem
    {
        [DataMember(Name = "key")]
        public int ID { get; set; }
        [DataMember(Name = "assetId")]
        public long AssetID { get; set;  }
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