using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public class TagApiModelWrapper
    {
        [DataMember]
        public int? pageSize { get; set; }

        [DataMember]
        public int? pageNum { get; set; }

        [DataMember]
        public int? total { get; set; } = 0;

        [DataMember]
        public IEnumerable<TagApiModel> items { get; set; }
    }
}
