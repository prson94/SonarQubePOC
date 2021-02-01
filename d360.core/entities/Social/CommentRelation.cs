using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class CommentRelation : BaseObject
    {
        [Column(Order = 1), DataMember, Key]
        public int CommentID { get; set; }

        [Column(Order = 2), DataMember, Key]
        public Guid AssetUid { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class CommentRelationDetail : BaseObject
    {
        [DataMember]
        public Guid AssetUid { get; set; }

        [DataMember]
        public string Path { get; set; }

        [DataMember]
        public string TypeName { get; set; }

        [DataMember]
        public string Url { get; set; }

        [DataMember]
        public string IconBackColor { get; set; }

        [DataMember]
        public string IconForeColor { get; set; }
    }
}
