using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public partial class StatisticTypeRelation : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int StatisticTypeID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public string ObjectType { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int ObjectID { get; set; }

        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Score_Name", Description = "Score_Description")]
        public int Score { get; set; }

        [IgnoreDataMember]
        public virtual StatisticType StatisticType { get; set; }
    }
}
