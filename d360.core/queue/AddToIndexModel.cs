using System.Reflection;

namespace d360.core.queue
{
    public class AddToIndexModel : IndexObjectModel
    {
        public AddToIndexModel()
        {
            To = QueueAction.AddToIndex;
        }
        public AddToIndexModel(IndexObjectModel parent)
        {
            foreach (PropertyInfo prop in parent.GetType().GetProperties())
                prop.SetValue(this, prop.GetValue(parent, null), null);
            To = QueueAction.AddToIndex;
        }
    }
}
