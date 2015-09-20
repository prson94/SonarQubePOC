using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class RolePermission : BaseObject, ICompanyObject
    {
        [Column(Order = 1), Key]
        public int CompanyID { get; set; }

        [Column(Order = 2), DataMember, Key]
        public int RoleID { get; set; }

        [Column(Order = 3), DataMember, Key]
        public int PermissionID { get; set; }

        public Permission Permission { get; set; }

        public Role Role { get; set; }
    }
}
