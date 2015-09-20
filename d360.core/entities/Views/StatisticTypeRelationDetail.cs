using d360.core.entities.Contracts;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Views
{
    [DataContract(Namespace = NAMESPACE)]
    public partial class StatisticTypeRelationDetail : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int StatisticTypeID { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public string ObjectName { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public string ObjectType { get; set; }

        [DataMember]
        public int Score { get; set; }
    }
}
