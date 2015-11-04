using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("Security", Schema = "utility")]
    public class ObjectSecurity : BaseObject
    {
        #region Properties

        [DataMember, Key, Column(Order = 1)]
        public string ObjectType { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int ObjectID { get; set; }

        [DataMember]
        public int RoleID { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int ResourceID { get; set; }

        #endregion

        [ForeignKey("RoleID")]
        public virtual Role Role { get; set; }

    }
}
