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
    public class FusionAttributeOwnerRule : BaseIntObject, IUpdatedMetadata
    {
        public int FusionID { get; set; }

        public string ObjectType { get; set; }

        public int ObjectID { get; set; }

        public string RelationshipOwnerObjectType { get; set; }

        public int RelationshipOwnerObjectID { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember]
        public virtual Fusion Fusion { get; set; }

        [IgnoreDataMember]
        public virtual ICollection<FusionAttributeOwnerRuleItem> FusionAttributeOwnerRuleItems { get; set; }
    }
}
