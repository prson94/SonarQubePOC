using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Setting : BaseIntObject, IIntObject
    {
        [DataMember]
        public string FieldName { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string DefaultValue { get; set; }

        [IgnoreDataMember, ForeignKey("SettingID")]
        public virtual ICollection<CompanySetting> CompanySettings { get; set; }
    }
}
