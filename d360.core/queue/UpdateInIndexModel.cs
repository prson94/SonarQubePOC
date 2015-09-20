using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.queue
{
    public class UpdateInIndexModel : IndexObjectModel
    {
        public UpdateInIndexModel()
        {
            To = QueueAction.UpdateInIndex;
        }
    }
}
