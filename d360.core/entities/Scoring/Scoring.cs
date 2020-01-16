using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Scoring
{
    [DataContract(Namespace = NAMESPACE), Table("Allocation", Schema = "metrics")]
    public class Allocation : BaseCreatedAndUpdatedObject
    {
        [DataMember, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Uid { get; set; }

        [DataMember]
        public ScoreType ScoreType { get; set; } = ScoreType.Governance;

        [DataMember]
        public Guid AssetTypeUid { get; set; }

        [DataMember]
        public string OverrideName { get; set; }

        [DataMember]
        public State State { get; set; } = State.Active;

    }
}
