using System.Collections.Generic;

using Newtonsoft.Json.Serialization;

namespace d360.model.helpers
{
    public class CustomJSONContractResolver : DefaultContractResolver
    {
        private Dictionary<string, string> PropertyMappings { get; set; }

        public CustomJSONContractResolver(Dictionary<string, string> customPropertyMapping)
        {
            PropertyMappings = customPropertyMapping;
        }

        protected override string ResolvePropertyName(string propertyName)
        {
            bool resolved = PropertyMappings.TryGetValue(propertyName, out string resolvedName);
            return resolved ? resolvedName : base.ResolvePropertyName(propertyName);
        }
    }
}
