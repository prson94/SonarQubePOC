using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.queue
{
    public class QueueObject
    {
        public QueueAction To { get; set; }

        public int CompanyID { get; set; }
    }
}
