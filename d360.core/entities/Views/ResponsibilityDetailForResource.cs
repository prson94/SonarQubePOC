using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities.Views
{
    [DataContract(Namespace = NAMESPACE)]
    public class ResponsibilityDetailForResource : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int ResponsibleTypeID { get; set; }

        [DataMember, Key, Column(Order = 2, TypeName = "varchar"), StringLength(50)]
        public string Type { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int TypeID { get; set; }

        [DataMember, Key, Column(Order = 4, TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember, Key, Column(Order = 5)]
        public int ObjectID { get; set; }

        [DataMember, Key, Column(Order = 6)]
        public int ResourceID { get; set; }

        [DataMember]
        public string ObjectName { get; set; }

        [DataMember]
        public string TypeName { get; set; }

        //[DataMember]
        //public string ObjectUrl { get; set; }

        [DataMember]
        public string ResponsibilityTypeName { get; set; }

        [DataMember]
        public bool Via { get; set; }

        [DataMember]
        public string SecurityAsset { get; set; }

        [DataMember]
        public int SecurityAssetID { get; set; }

        [DataMember]
        public string SecurityAssetName { get; set; }
    }
}
