using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class CompanyHelpResource : BaseObject
    {
        [Column(Order = 1), Key]
        public int CompanyID { get; set; }

        [Column(Order = 2), Key]
        public int HelpResourceID { get; set; }

        public int SortOrder { get; set; }

        public HelpResource HelpResource { get; set; }
    }
}
