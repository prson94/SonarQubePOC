using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(d360.core.ObjectTypeInfo.Statistic, "Statistic")]
    public partial class Statistic : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int StatisticTypeID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public string ObjectType { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int ObjectID { get; set; }

        [DataMember, Key, Column(Order = 4)]
        public DateTime DateStart { get; set; }

        [DataMember]
        public DateTime DateEnd { get; set; }

        public int Score { get; set; }

        [IgnoreDataMember]
        public virtual StatisticType StatisticType { get; set; }
    }
}
