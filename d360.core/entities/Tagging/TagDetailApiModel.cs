using System.Collections.Generic;

namespace d360.core.entities
{
    public class TagDetailApiModel
    {
        public int pageSize { get; set; }

        public int pageNum { get; set; }

        public int? total { get; set; }

        public List<TagDetail> items { get; set; } = new List<TagDetail>();
    }
}
