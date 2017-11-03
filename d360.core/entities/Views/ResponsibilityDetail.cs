using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Views
{
    [DataContract(Namespace = NAMESPACE), Table("ResponsibilityDetails")]
    public class ResponsibilityDetail : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public long AssetID { get; set; }
        
        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public long? OverrideItemID { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public int TypeID { get; set; }

        [DataMember]
        public string RuleName { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int ResponsibilityTypeID { get; set; }

        [DataMember]
        public string ResponsibilityTypeName { get; set; }

        [DataMember]
        public string FirstName { get; set; }

        [DataMember]
        public string LastName { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int ResourceID { get; set; }

        [DataMember, StringLength(1), Key, Column(Order = 4)]
        public string SecurityAsset { get; set; }

        [DataMember, Key, Column(Order = 5)]
        public int SecurityAssetID { get; set; }

        [DataMember]
        public string SecurityAssetName { get; set; }

        //[DataMember]
        //public bool Overriden { get; set; }
    }
}
