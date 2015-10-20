using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class CommentDetailTag : BaseObject
    {
        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public string TextPath { get; set; }
        [DataMember]
        public string ObjectTypeName { get; set; }
        [DataMember]
        public string Url { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class CommentDetail : BaseIntObject
    {
        public CommentDetail()
        {
            Tags = new List<CommentDetailTag>();
        }

        [DataMember]
        public string Body { get; set; }

        [DataMember]
        public CommentType CommentTypeID { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public DateTime DateCreated { get; set; }

        [DataMember]
        public string ObjectType { get; set; }

        [DataMember]
        public int? ParentID { get; set; }

        [DataMember]
        public int CreatingResourceID { get; set; }

        [DataMember, NotMapped]
        public string ResourceName { get; set; }

        [DataMember, NotMapped]
        public string ResourceEmail { get; set; }

        [DataMember]
        public string ObjectName { get; set; }

        [DataMember]
        public string ObjectUrl { get; set; }

        [IgnoreDataMember]
        public string TagsXml { get; set; }

        [DataMember]
        public List<CommentDetailTag> Tags { get; set; }

        [DataMember, NotMapped]
        public ICollection<CommentDetail> Comments { get; set; }

        public void ParseTagXml()
        {
            if (!string.IsNullOrEmpty(TagsXml))
            {
                Tags.AddRange(
                    XElement.Parse(TagsXml).Elements("tag").Select(i => new CommentDetailTag { Object = i.Element("Object").Value, ObjectID = int.Parse(i.Element("ObjectID").Value), ObjectTypeName = i.Element("ObjectTypeName").Value, TextPath = i.Element("TextPath").Value, Url = i.Element("Url").Value })
                );
            }
        }
    }
}
