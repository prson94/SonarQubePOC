using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectMap : BaseIntObject, IIntObject
    {
        [DataMember]
        public int MapID { get; set; }

        [DataMember]
        public int SubjectIntersectNodeID { get; set; }

        [DataMember]
        public int ObjectIntersectNodeID { get; set; }

        [DataMember]
        public int PredicatePhraseID { get; set; }

        [DataMember]
        public MapType Type { get; set; }
    }
}
