using System;
using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class RuleType : BaseCreatedAndUpdatedIntObject, IIntObject, ISearchable, ICreatedMetadata, IUpdatedMetadata
    {
        [DataMember, NotMapped]
        public int? AssetTypeID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string DisplayFormat { get; set; }

        [DataMember]
        public string Description { get; set; }

        [IgnoreDataMember, ForeignKey("RuleTypeID")]
        public virtual ICollection<Rule> Rules { get; set; }
    }
}
