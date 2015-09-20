using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class LoadTypeRuleDetail : BaseObject
    {
        [Key, Column(Order = 1), DataMember]
        public int ID { get; set; }

        [DataMember]
        public int LoadTypeID { get; set; }

        [DataMember]
        public LoadTypeRuleGroup LoadTypeRuleGroup { get; set; }

        [DataMember, NotMapped]
        public string LoadTypeRuleGroupName { get { return LoadTypeRuleGroup.ToString(); } }

        [DataMember]
        public int SortOrder { get; set; }

        [DataMember]
        public string ObjectType { get; set; }

        [DataMember]
        public string ObjectName { get; set; }

        [DataMember]
        public string UniqueLoadTypeField { get; set; }

        [DataMember]
        public int RuleItemCount { get; set; }
    }
}
