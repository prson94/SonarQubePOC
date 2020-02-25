using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace d360.core.entities.Scoring
{
    [DataContract(Namespace = NAMESPACE), Table("Allocation", Schema = "metrics")]
    public class ScoreTypeAllocation : BaseCreatedAndUpdatedObject
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

        [DataMember]
        public bool IsExternallyCalculated { get; set; }

    }

    [DataContract(Namespace = NAMESPACE), Table("Score", Schema = "metrics")]
    public class Score : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public Guid AssetUid { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public DateTime EffectiveDate { get; set; }

        [DataMember]
        public decimal Value { get; set; }

        [DataMember]
        public DateTime? RunDate { get; set; }

        [DataMember]
        public DateTime? EndDate { get; set; }

        [DataMember, Key, Column(Order = 2), JsonConverter(typeof(StringEnumConverter))]   
        public ScoreType ScoreType { get; set; } = ScoreType.Governance;
    }

}
