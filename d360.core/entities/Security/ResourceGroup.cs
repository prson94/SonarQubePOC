using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Name = "resourceGroup", Namespace = NAMESPACE)]
    public class ResourceGroup : BaseObject
    {
        #region Properties

        [Key, Column(Order = 1)]
        public int GroupID { get; set; }

        [Key, Column(Order = 2)]
        public int ResourceID { get; set; }

        #endregion

        [ForeignKey("GroupID")]
        public virtual Group Group { get; set; }
    }
}
