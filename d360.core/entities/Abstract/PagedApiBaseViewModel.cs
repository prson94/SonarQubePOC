using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public abstract class PagedApiBaseRequestModel : BaseObject
    {
        [DataMember]
        public int pageSize { get; set; } = 200;

        [DataMember]
        public int pageNum { get; set; } = 1;
    }
    public abstract class PagedApiBaseViewModel : BaseObject
    {
        [DataMember]
        public int pageSize { get; set; } = 200;

        [DataMember]
        public int pageNum { get; set; } = 1;

        [DataMember]
        public int? total { get; set; } = 0;
    }

    public class PagedApiBaseViewModel<T> : PagedApiBaseViewModel
    {
        [DataMember]
        public IReadOnlyList<T> items { get; set; }
    }
}
