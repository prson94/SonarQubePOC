using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ObjectStyle : BaseObject
    {
        [DataMember, Key, Column(Order = 1, TypeName = "varchar"), StringLength(50)]
        public string ObjectType { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int ObjectID { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(7)]
        public string IconBackColor { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(7)]
        public string IconForeColor { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(25)]
        public string IconText { get; set; }
    }
}
