using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class CompanyFeature : BaseObject
    {
        [Column(Order = 1), Key]
        public int CompanyID { get; set; }

        [Column(Order = 2), Key]
        public Feature Feature { get; set; }
    }
}
