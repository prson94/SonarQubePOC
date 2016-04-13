using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities.Views
{
    [DataContract(Namespace = NAMESPACE)]
    public class StatisticTypeCheckOption : BaseObject
    {
        [DataMember, Key, Column(Order = 1, TypeName = "varchar"), StringLength(18)]
        public string ObjectType { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int ObjectID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(17)]
        public string NamePrefix { get; set; }
    }
}
