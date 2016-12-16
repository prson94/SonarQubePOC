using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class CompanyResource : BaseObject
    {
        [Column(Order = 1), Key]
        public int CompanyID { get; set; }

        [Column(Order = 2), Key]
        public int ResourceID { get; set; }

        public bool IsAdministrator { get; set; }

        public Resource Resource { get; set; }
    }
}
