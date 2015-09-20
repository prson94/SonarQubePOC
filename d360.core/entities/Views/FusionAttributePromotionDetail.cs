using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("AttributePromotion", Schema="fusion")]
    public class FusionAttributePromotionDetail: BaseObject
    {
        [DataMember]
        public int FusionID { get; set; }

        [DataMember]
        public int ID { get; set; }


        [DataMember]
        public string ObjectName { get; set; }

        [DataMember]
        public int? ObjectID { get; set; }

        [DataMember]
        public string ObjectType { get; set; }
        
        [DataMember]
        public string PromotionName { get; set; }

        [DataMember]
        public int PromotionObjectID { get; set; }

        [DataMember]
        public string PromotionObjectType { get; set; }


        [DataMember]
        public string PromotionParentName { get; set; }

        [DataMember]
        public int? PromotionParentObjectID { get; set; }

        [DataMember]
        public string PromotionParentObjectType { get; set; }

        [DataMember]
        public bool Enabled { get; set; }
    }
}
