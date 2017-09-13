using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.queue;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Shortcut : BaseIntObject
    {
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Icon { get; set; }
        [DataMember]
        public string IconUrl { get; set; }
        [DataMember]
        public string Url { get; set; }

        [NotMapped, DataMember]
        public string IconPayload { get; set; }
    }
}
