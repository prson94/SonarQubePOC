using System.Reflection;

namespace d360.core.queue
{
    public class UpdateInIndexModel : IndexObjectModel
    {
        public UpdateInIndexModel()
        {
            To = QueueAction.UpdateInIndex;
        }
        public UpdateInIndexModel(IndexObjectModel parent)
        {
            foreach (PropertyInfo prop in parent.GetType().GetProperties())
                prop.SetValue(this, prop.GetValue(parent, null), null);
            To = QueueAction.UpdateInIndex;
        }
    }
}
