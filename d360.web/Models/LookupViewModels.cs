using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using d360.core.entities;

namespace d360.web.Models
{
    public class LookupValueViewModel
    {
        public LookupType Lookup { get; set; }
        public Lookup Item { get; set; }
        public Fields Fields { get; set; }
    }
}