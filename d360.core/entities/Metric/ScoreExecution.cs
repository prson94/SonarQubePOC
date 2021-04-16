using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("Execution", Schema = "metrics")]
    public class ScoreExecution : BaseUidObject
    {
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
        public bool Processing { get; set; } = false;

        [DataMember]
        public Guid? TriggeredByExecutionUid { get; set; }

        [DataMember]
        public Guid? TriggeredByMeasureUid { get; set; }

        public void SetPercentageComplete(int processed, int total)
        {
            if (total <= 0)
            {
                PercentComplete = 1;
            }
            else 
            {
                PercentComplete = (float)processed / (float)total;
                if (PercentComplete > 1)
                {
                    PercentComplete = 1;
                }
            }
        }
    }
}
