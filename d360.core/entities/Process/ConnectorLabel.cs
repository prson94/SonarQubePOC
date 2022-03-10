using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ConnectorLabel : BaseCreatedAndUpdatedIntObject, ICreatedMetadata, IUpdatedMetadata
    {
        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid uid { get; set; }

        [DataMember, StringLength(250)]
        public string Value { get; set; }

        [DataMember]
        public State State { get; set; } = State.Active;
    }
}
