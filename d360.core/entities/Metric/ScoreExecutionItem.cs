using d360.core.queue;

namespace d360.core.entities
{
	public enum ScoreExecutionItemState
    {
        NotProcessed = 0,
        Success = 1,
        Error = 2
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
