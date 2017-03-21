using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Workflow
{
    public class FieldModel
    {
        [JsonProperty(PropertyName = "label", Order = 3)]
        public string Label { get; set; }

        [JsonProperty(PropertyName = "name", Order = 1)]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "type", Order = 2)]
        public string Type { get; set; }
    }

    public class FieldValueModel
    {
        [JsonProperty(PropertyName = "name", Order = 1)]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "value", Order = 2)]
        public string Value { get; set; }
    }

    public class SettingModel
    {
        [JsonProperty(PropertyName = "name", Order = 1)]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "value", Order = 2)]
        public string Value { get; set; }
    }

    public class Conditions
    {
        [JsonProperty(PropertyName = "conjunction", Order = 1)]
        public string Conjunction { get; set; }

        [JsonProperty(PropertyName = "field", Order = 2)]
        public List<FieldValueModel> Fields { get; set; }

        [JsonProperty(PropertyName = "setting", Order = 3)]
        public List<SettingModel> Settings { get; set; }
    }

    public class WorkflowTypeModel
    {
        public WorkflowType Type { get; set; } = new WorkflowType();
        public WorkflowEventRegistration Event { get; set; } = new WorkflowEventRegistration();
    }
}
