using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Views
{
    [DataContract(Namespace = NAMESPACE), Table("ResponsibilityDetail")]
    public class ResponsibilityDetail : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public long AssetID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int AssetTypeID { get; set; }

        [DataMember]
        public long? OverrideID { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int ResponsibilityTypeID { get; set; }

        [DataMember]
        public string ResponsibilityTypeName { get; set; }

        [DataMember]
        public string ResourceName { get; set; }

        [DataMember, Key, Column(Order = 4)]
        public int ResourceID { get; set; }

        [DataMember, StringLength(1), Key, Column(Order = 5)]
        public string SecurityAsset { get; set; }

        [DataMember, Key, Column(Order = 6)]
        public int SecurityAssetID { get; set; }

        [DataMember]
        public string SecurityAssetName { get; set; }

        [DataMember]
        public string Context { get; set; }

        [DataMember]
        public int PermissionsBitMask { get; set; }

        [DataMember]
        public bool ApplyToType { get; set; }

        [DataMember]
        public bool IsVisible { get; set; }

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public int TypeID { get; set; }
    }
}
