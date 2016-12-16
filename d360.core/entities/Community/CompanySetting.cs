using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class CompanySetting: BaseObject
    {
         [DataMember, Key, Column(Order = 1)]
        public int CompanyID { get; set; }
        
        [DataMember, Key, Column(Order = 2)]
        public int SettingID { get; set; }

        [DataMember]
        public string Value { get; set; }

        [IgnoreDataMember, ForeignKey("CompanyID")]
        public virtual Company Company { get; set; }

        [IgnoreDataMember, ForeignKey("SettingID")]
        public virtual Setting Setting { get; set; }
    }
}
