using d360.core.entities.Contracts;
using d360.core.enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Asset : BaseObject, IDisplayValueObject
    {
        [DataMember]
        public long ID { get; set; }

        [DataMember]
        public int AssetTypeID { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string DisplayValue { get; set; }

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public State State { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string KeyHash { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string FieldHash { get; set; }

        [IgnoreDataMember]
        public virtual AssetType AssetType { get; set; }
    }
}
