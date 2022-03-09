using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IssueTypeRelation : BaseObject
    {
        [Column(Order = 1), DataMember, Key]
        public int IssueTypeID { get; set; }

        [Column(Order = 2), DataMember, Key]
        public int AssetTypeID { get; set; }

        public virtual IssueType IssueType { get; set; }

        [NotMapped, DataMember]
        public AssetTypeClass Class { get; set; }

        [NotMapped, DataMember]
        public string ClassName { get { return Class.ToString(); } }

        [NotMapped, DataMember]
        public string TypeName { get; set; }
    }
}
