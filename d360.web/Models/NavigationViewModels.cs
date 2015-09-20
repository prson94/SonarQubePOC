using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace d360.web.Models
{
    public class NavListViewModel : List<NavItemViewModel>
    {

    }

    public class NavItemViewModel
    {
        public NavItemViewModel()
        {
            items = new NavListViewModel();
        }

        public string context { get; set; }
        public int tabIndex { get; set; }
        public string icon { get; set; }
        public string text { get; set; }
        public string url { get; set; }
        public NavListViewModel items { get; set; }
    }
}