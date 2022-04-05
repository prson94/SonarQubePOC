using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.web.Models
{
    [DataContract]
    public class InformationCatalogDiagramDataItem
    {
        [DataMember(Name = "key")]
        public int ID { get; set; }
        
        [DataMember(Name = "assetId")]
        public long AssetID { get; set; }
        
        [DataMember(Name = "parent")]
        public int? ParentID { get; set; }
        
        [DataMember(Name = "name")]
        public string Name { get; set; }
        
        [DataMember(Name = "objectId")]
        public int? ObjectID { get; set; }
        
        [DataMember(Name = "object")]
        public string Object { get; set; }
        
        [DataMember(Name = "uid")]
        public Guid uid { get; set; }
        
        [DataMember(Name = "url")]
        public string Url { get; set; }
        
        [DataMember]
        public bool RelationshipsExist { get; set; }
        
        [DataMember(Name = "children")]
        public List<InformationCatalogDiagramDataItem> Children { get; set; }
    }
}
