using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.queue
{
    public class GraphInfo : QueueObject
    {
        public GraphInfoType Type { get; set; }
        public long ID { get; set; }
    }

    public enum GraphInfoType
    {
        Node,
        Edge,
        Path
    }
}
