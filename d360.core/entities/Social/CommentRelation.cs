using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class CommentRelation : BaseObject
    {
        [Column(Order = 1), DataMember, Key]
        public int CommentID { get; set; }

        [Column(Order = 2), DataMember, Key]
        public long AssetID { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class CommentRelationDetail : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public Guid AssetUid { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public string Path { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public string TypeName { get; set; }

        [DataMember]
        public Guid AssetTypeUid { get; set; }

        [DataMember]
        public string Url { get; set; }

        [DataMember]
        public string IconBackColor { get; set; }

        [DataMember]
        public string IconForeColor { get; set; }
    }
}
