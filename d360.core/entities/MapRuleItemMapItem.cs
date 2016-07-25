using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class MapRuleItemMapItem : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int MapRuleItemID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int MapItemID { get; set; }
    }
}
