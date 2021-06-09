using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace d360.model.helpers
{
    public class CustomJSONContractResolver : DefaultContractResolver
    {
        private Dictionary<string, string> PropertyMappings { get; set; }

        public CustomJSONContractResolver(Dictionary<string, string> customPropertyMapping)
        {
            this.PropertyMappings = customPropertyMapping;
        }

        protected override string ResolvePropertyName(string propertyName)
        {
            string resolvedName = null;
            var resolved = this.PropertyMappings.TryGetValue(propertyName, out resolvedName);
            return (resolved) ? resolvedName : base.ResolvePropertyName(propertyName);
        }
    }
}