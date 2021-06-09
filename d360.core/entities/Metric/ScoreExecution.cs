using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("Execution", Schema = "metrics")]
    public class ScoreExecution : BaseObject
    {
        [DataMember, Key]
        public Guid Uid { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }

        [DataMember]
        public double PercentComplete { get; set; }

        [DataMember]
        public int Failures { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public DateTime StartedOn { get; set; }

        [DataMember]
        public DateTime? CompletedOn { get; set; }

        [DataMember]
        public DateTime? ProcessingStartedOn { get; set; }

        [DataMember]
        public bool Processing { get; set; }

        [DataMember]
        public int? LoopSecondsElapsed { get; set; }

        [DataMember]
        public DateTime? UpdatedOn { get; set; }

        [DataMember]
        public Guid? TriggeredByExecutionUid { get; set; }

        [DataMember]
        public Guid? TriggeredByMeasureUid { get; set; }
    }
}
