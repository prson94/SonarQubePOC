using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using d360.core.entities;

namespace d360.web.Models
{
    public class RelationshipsViewModel
    {
        public string ObjectType { get; set; }
        public int ObjectID { get; set; }
        public List<AllowedIntersectionType> Types {get;set;}
    }
}