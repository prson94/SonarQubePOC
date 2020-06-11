using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public class TagApiModelWrapper
    {
        [DataMember]
        public int? pageSize { get; set; }
        [DataMember]
        public int? pageNum { get; set; }
        [DataMember]
        public int total { get; set; } = 0;
        [DataMember]
        public IEnumerable<TagApiModel> items { get; set; }
    }
}
