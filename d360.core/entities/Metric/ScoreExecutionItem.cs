using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.queue;

using Newtonsoft.Json;

namespace d360.core.entities
{
    public enum ScoreExecutionItemState
    {
        NotProcessed = 0,
        Success = 1,
        Error = 2
    }

    [DataContract(Namespace = NAMESPACE), Table("ExecutionItem", Schema = "metrics")]
    public class ScoreExecutionItem : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public long ExecutionID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public ScoreQueueChangeType ChangeType { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int RowNumber { get; set; }

        [DataMember]
        public string Payload { get; set; }

        [DataMember]
        public ScoreExecutionItemState State { get; set; }

        [DataMember]
        public string Message { get; set; }

        public T GetPayload<T>()
        {
            return JsonConvert.DeserializeObject<T>(Payload ?? "{}");
        }
    }

    public class ScoreExecutionItemViewModel
    {
        public ScoreQueueChangeType ChangeType { get; set; }

        public int RowNumber { get; set; }

        public dynamic Payload { get; set; }

        public ScoreExecutionItemState State { get; set; }

        public string Message { get; set; }

    }
}
