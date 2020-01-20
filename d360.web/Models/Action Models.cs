using d360.core.entities;
using System;
using System.Collections.Generic;


namespace d360.web.Models
{
    public class ToolbarItemNg
    {
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Action { get; set; }
        public dynamic Params { get; set; }

        public List<ToolbarItemNg> Items { get; set; } = new List<ToolbarItemNg>();
    }

    public class ResourceGroupInfo
    {
        public ResourceGroup[] ResourceGroups { get; set; }
        public Guid GroupGuid { get; set; }
    }
}


