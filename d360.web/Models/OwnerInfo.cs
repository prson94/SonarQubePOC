using d360.core;
using System.Runtime.Serialization;

namespace d360.web.Models
{
    [DataContract(Namespace = constants.NAMESPACE)]
    public class OwnerInfo
    {
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string ResourceUrl { get; set; }
        [DataMember]
        public int ObjectID { get; set; }
        [DataMember]
        public string ObjectName { get; set; }
        [DataMember]
        public string ObjectTypeName { get; set; }
        [DataMember]
        public string ObjectUrl { get; set; }
        [DataMember]
        public string ObjectType { get; set; }
        [DataMember]
        public int OpenEventCount { get; set; }
        [DataMember]
        public string Role { get; set; }
        public string ObjectHtmlLink { get { return "<a data-type='" + ObjectType + "' data-context='Preview' data-id='" + ObjectID + "' href='" + ObjectUrl + "'>" + ObjectName + "</a>"; } }
        public string ResourceHtmlLink { get { return "<a href='" + ResourceUrl + "'>" + Name + "</a>"; } }
        
        [DataMember]
        public int OwnershipObjectID { get; set; }
        [DataMember]
        public string OwnershipType { get; set; }
    }
}