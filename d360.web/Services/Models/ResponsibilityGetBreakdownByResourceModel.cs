using System;
using Newtonsoft.Json;

namespace d360.web.Services
{
    public class ResponsibilityGetBreakdownByResourceModel
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Class")]
        public string Class { get; set; }

        [JsonProperty("AssetTypeUid")]
        public Guid AssetTypeUid { get; set; }

        [JsonProperty("Count")]
        public int AssetCount { get; set; }
    }
}