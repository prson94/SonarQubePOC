using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Permission : BaseCompanyObject, ICompanyObject, IIntObject
    {
        public string Name { get; set; }

        public int PermissionTypeID { get; set; }

        public PermissionType PermissionType { get; set; }
    }
}
