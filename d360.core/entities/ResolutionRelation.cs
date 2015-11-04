using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ResolutionRelation : BaseObject
    {
        [Key, Column(Order = 3)]
        public int ObjectID { get; set; }

        [Key, Column(Order = 2)]
        public string ObjectType { get; set; }

        [Key, Column(Order = 1)]
        public int ResolutionID { get; set; }


        public virtual Resolution Resolution { get; set; }
    }
}
