using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace d360.web.Models
{
    public class GroupSearchResultModel
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public int NumberOfMembers { get; set; }

        public bool IsMember { get; set; }
    }
}