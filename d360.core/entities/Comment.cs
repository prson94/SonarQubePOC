using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Comment : BaseIntObject, IIntObject
    {
        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Body { get; set; }

        [DataMember]
        public string OwnerObjectType { get; set; }

        [DataMember]
        public int OwnerObjectID { get; set; }

        [DataMember]
        public CommentType CommentTypeID { get; set; }

        [DataMember]
        public DateTime DateCreated { get; set; }

        [DataMember]
        public int CreatingResourceID { get; set; }

        [DataMember]
        public int? ParentID { get; set; }

        public virtual Comment Parent { get; set; }
        [ForeignKey("CommentID")]
        public virtual ICollection<CommentRelation> Relations { get; set; }
        [ForeignKey("ParentID")]
        public virtual ICollection<Comment> Children { get; set; }
    }
}
