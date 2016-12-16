using d360.core.entities.Contracts;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class RuleResultQualifierType : BaseIntObject, IIntObject
    {        
        [DataMember]
        public int RuleID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int Order { get; set; }

        [DataMember]
        public string ResolutionObject { get; set; }

        [DataMember]
        public int? ResolutionObjectID { get; set; }

        [DataMember]
        public int? ResolutionFieldTypeID { get; set; }

        [DataMember]
        public string ResolutionFieldTypeName { get; set; }

        [IgnoreDataMember]
        public virtual Rule Rule { get; set; }
    }
}
