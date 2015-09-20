using d360.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace d360.web.Models
{
    [DataContract(Namespace = constants.NAMESPACE)]
    public class PageNavigationItem
    {
        public PageNavigationItem()
        {
            LazyLoad = true;
        }

        [DataMember]
        public string Title { get; set; }
        [DataMember]
        public string Icon { get; set; }
        [DataMember]
        public string Uri { get; set; }
        [DataMember]
        public bool LazyLoad { get; set; }
    }
}