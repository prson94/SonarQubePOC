using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    public class MicrosoftGraphUserListModel
    {
        [Newtonsoft.Json.JsonProperty("@odata.context")]
        public string context { get; set; }

        [Newtonsoft.Json.JsonProperty("@odata.nextLink")]
        public string next { get; set; }

        public List<Newtonsoft.Json.Linq.JObject> value { get; set; }
    }
}
