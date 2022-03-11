using System.Collections.Generic;

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
