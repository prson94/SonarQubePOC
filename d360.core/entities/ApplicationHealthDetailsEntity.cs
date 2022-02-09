namespace d360.core.entities
{
    public class ApplicationHealthDetailsEntity
    {
        public int QueueTaskCount { get; set; }

        public int ApiExecutionPendingCount { get; set; }

        public int WorkflowItemPendingCount { get; set; }
    }
}
