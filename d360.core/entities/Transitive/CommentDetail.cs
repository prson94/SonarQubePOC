using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
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

        [DataMember]
        public string IconBackColor { get; set; }
        [DataMember]
        public string IconForeColor { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class CommentDetail : BaseIntObject
    {
        public CommentDetail()
        {
            Tags = new List<CommentDetailTag>();
            Votes = new List<CommentVote>();


        }

        [DataMember]
        public string Body { get; set; }

        [DataMember]
        public CommentType CommentTypeID { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public DateTime DateCreated { get; set; }

        [DataMember, NotMapped]
        public string DateCreatedUTCString { get { return DateCreated.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"); } }

        [DataMember]
        public DateTime? DateEdited { get; set; }

        [DataMember, NotMapped]
        public string DateEditedUTCString { get { return ((DateEdited == null) ? null : ((DateTime)DateEdited).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'")); } }

        [DataMember]
        public string ObjectType { get; set; }

        [DataMember]
        public int? ParentID { get; set; }

        [DataMember]
        public int CreatingResourceID { get; set; }

        [DataMember]
        public bool IsDeleted { get; set; }

        [DataMember, NotMapped]
        public string ResourceName { get; set; }

        [DataMember, NotMapped]
        public string ResourceEmail { get; set; }

        [DataMember, NotMapped]
        public bool? IsEditable { get; set; }

        [DataMember, NotMapped]
        public bool? IsDeletable { get; set; }

        [DataMember, NotMapped]
        public bool CreatorIsOwner { get; set; }

        [DataMember]
        public string ObjectName { get; set; }

        [DataMember]
        public string ObjectUrl { get; set; }

        [IgnoreDataMember]
        public string TagsXml { get; set; }

        [IgnoreDataMember]
        public string VotesXml { get; set; }

        [DataMember]
        public List<CommentDetailTag> Tags { get; set; }

        [DataMember]
        public List<CommentVote> Votes { get; set; }

        [DataMember, NotMapped]
        public ICollection<CommentDetail> Comments { get; set; }

        public void ParseTagXml(bool isNg = false)
        {
            if (!string.IsNullOrEmpty(TagsXml))
            {
                try
                {
                    Tags.AddRange(
                        XElement.Parse(TagsXml).Elements("tag").Select(i => new CommentDetailTag { Object = i.Element("Object").Value, ObjectID = int.Parse(i.Element("ObjectID").Value), ObjectTypeName = i.Element("ObjectTypeName").Value, TextPath = i.Element("TextPath").Value, Url = i.Element(isNg ? "NgUrl" : "Url").Value, IconBackColor = i.Element("IconBackColor").Value, IconForeColor = i.Element("IconForeColor").Value })
                    );
                }
                catch (Exception ex)
                {

                }
            }
        }

        public void ParseVoteXml()
        {
            if (!string.IsNullOrEmpty(VotesXml))
            {
                Votes.AddRange(
                    XElement.Parse(VotesXml).Elements("vote").Select(i => new CommentVote { CommentID = int.Parse(i.Element("CommentID").Value), ResourceID = int.Parse(i.Element("ResourceID").Value), Vote = int.Parse(i.Element("VoteValue").Value) })
                );
            }
        }
    }
}
