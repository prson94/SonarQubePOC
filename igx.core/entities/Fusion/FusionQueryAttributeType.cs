using System;
using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FusionQueryAttributeType : BaseIntObject, IIntObject, ICreatedMetadata, IUpdatedMetadata
    {
        [DataMember]
        public int FusionID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Query { get; set; }

        [DataMember]
        public string DisplayFormat { get; set; }

        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember]
        public virtual Fusion Fusion { get; set; }

        [IgnoreDataMember, ForeignKey("FusionQueryAttributeTypeID")]
        public virtual ICollection<FusionQueryAttribute> FusionQueryAttributes { get; set; }
    }


    [DataContract(Namespace = NAMESPACE)]
    public class FusionQueryAttributeTypeApiModel: BaseObject
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Query { get; set; }

        [DataMember]
        public List<string> KeyColumns { get; set; }
    }
}
