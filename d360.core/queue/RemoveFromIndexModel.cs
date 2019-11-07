using System.Reflection;

namespace d360.core.queue
{
    public class RemoveFromIndexModel : IndexObjectModel
    {
        public RemoveFromIndexModel()
        {
            To = QueueAction.RemoveFromIndex;
        }
        public RemoveFromIndexModel(IndexObjectModel parent)
        {
            foreach (PropertyInfo prop in parent.GetType().GetProperties())
                prop.SetValue(this, prop.GetValue(parent, null), null);
            To = QueueAction.RemoveFromIndex;
        }
    }
}
