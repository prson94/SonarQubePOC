using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class PermissionType : BaseCompanyObject, ICompanyObject, IIntObject
    {
        public string Name { get; set; }

        [ForeignKey("CompanyID, PermissionTypeID")]
        public virtual ICollection<Permission> Permissions { get; set; }
    }
}
