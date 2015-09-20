using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FusionAttributePromotion : BaseObject
    {
        [Key, Column(Order = 1)]
        public int FusionAttributeID { get; set; }

        [Key, Column(Order = 2)]
        public string ObjectType { get; set; }

        [Key, Column(Order = 3)]
        public int ObjectID { get; set; }

        public int FusionAttributePromotionRuleID { get; set; }
    }
}
