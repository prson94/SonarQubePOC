using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace d360.web.Models
{
    public class FeatureFlagUser
    {
        public string iPAddress { get; set; }
        public bool anonymous { get; set; }
        public string email { get; set; }
        public string name { get; set; }
        public string lastName { get; set; }
        public string firstName { get; set; }
        public string country { get; set; }
        public string key { get; set; }
        public Dictionary<string, string> custom { get; set; }
    }
}