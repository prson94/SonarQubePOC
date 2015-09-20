using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace d360.web.Models
{
    public class CommentData
    {
        public string ObjectType { get; set; }
        public int? ObjectID { get; set; }
        public Comment Comment { get; set; }
    }

    public class CommentRequestData
    {
        public string ObjectType { get; set; }

        public int? ObjectID { get; set; }

        public int Skip { get; set; }

        public int Take { get; set; }

        public int DateFilter { get; set; }

        public int TypeFilter { get; set; }
    }
}