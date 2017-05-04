using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("RuleFilter", Schema = "fusion")]
    public class FusionRuleFilter : BaseIntObject
    {
        public FusionRuleFilter()
        {
            Items = new List<FusionRuleFilterItem>();
        }

        [DataMember]
        public int RuleID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public bool All { get; set; }

        public string Fields { get; set; }

        public string Sql { get; set; }

        [NotMapped]
        public XElement FieldsDocument
        {
            get { return XElement.Parse(string.IsNullOrEmpty(Fields) ? "<fields/>" : Fields); }
            set { Fields = value.ToString(); }
        }

        [NotMapped, DataMember]
        public List<FusionRuleFilterItem> Items { get; set; }

        [ForeignKey("RuleID")]
        public virtual FusionRule FusionRule { get; set; }
    }
}
