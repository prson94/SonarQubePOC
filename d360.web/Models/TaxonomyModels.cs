using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using d360.core.entities;

namespace d360.web.Models
{
    public class TaxonomyEditModel
    {
        public TaxonomyType Type { get; set; }
        public Taxonomy Item { get; set; }
        public Fields Fields { get; set; }
    }
    public class TaxonomyViewModel
    {
        public Taxonomy Item { get; set; }
        public Fields Fields { get; set; }
    }
    public class TaxonomyListViewModel
    {
        public TaxonomyType Type { get; set; }
        public List<Taxonomy> Items { get; set; }
        public bool GovernanceEnabled { get; set; }
    }
    public class TaxonomyAdministrationViewModel
    {
        public List<TaxonomyType> Types { get; set; }
    }
}