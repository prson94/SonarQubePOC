using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.queue
{
    public enum QueueAction
    {
        AddToIndex = 1,
        UpdateInIndex = 2,
        RemoveFromIndex = 3,
        AddVersion = 4,
        BulkLoad = 5,
        Cache = 6
    }
}
