using d360.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace d360.web.Models
{
    public class ContextToolbarItem
    {
        public ContextToolbarItem()
        {
            Items = new List<ContextToolbarItem>();
        }

        public string Context { get; set; }
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Uri { get; set; }
        public string Type { get; set; }
        public string Method { get; set; }

        public List<ContextToolbarItem> Items { get; set; }
    }

    [DataContract(Namespace = constants.NAMESPACE)]
    public class PageActionItem
    {
        public PageActionItem()
        {
            CustomData = new List<PageActionItemData>();
            Enabled = true;
            Items = new List<PageActionItem>();
        }

        [DataMember]
        public string Context { get; set; }

        [DataMember]
        public string CommandName { get; set; }

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Warning { get; set; }

        [DataMember]
        public string Icon { get; set; }

        [DataMember]
        public string Uri { get; set; }

        [DataMember]
        public bool Enabled { get; set; }

        [DataMember]
        public List<PageActionItemData> CustomData { get; set; }

        [DataMember]
        public List<PageActionItem> Items { get; set; }
    }

    [DataContract(Namespace = constants.NAMESPACE)]
    public class PageActionItemData
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Value { get; set; }
    }

    public class ToolbarItem
    {
        public ToolbarItem()
        {
            Items = new List<ToolbarItem>();
        }

        public string Context { get; set; }
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public string Uri { get; set; }

        public List<ToolbarItem> Items { get; set; }
    }

    public class ToolbarItemNg
    {
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Action { get; set; }
        public dynamic Params { get; set; }

        public List<ToolbarItemNg> Items { get; set; } = new List<ToolbarItemNg>();
    }
}