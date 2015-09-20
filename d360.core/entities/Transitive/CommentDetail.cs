using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    [DataContract(Name = "Comment", Namespace = NAMESPACE)]
    public class CommentDetail : BaseIntObject
    {
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

        [DataMember, NotMapped]
        public ICollection<CommentDetail> Comments { get; set; }
    }
}
