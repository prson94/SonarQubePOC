using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using d360.core.entities;

namespace d360.web.Models
{
    public class DomainsListViewModel
    {
        public DomainType Type { get; set; }
        public List<DomainGroup> Groups { get; set; }
        public List<Domain> Lists { get; set; }
    }
}