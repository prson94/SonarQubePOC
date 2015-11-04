namespace d360.core.queue
{
    public class RemoveFromIndexModel : IndexObjectModel
    {
        public RemoveFromIndexModel()
        {
            To = QueueAction.RemoveFromIndex;
        }
    }
}
