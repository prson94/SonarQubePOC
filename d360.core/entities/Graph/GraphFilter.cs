using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace d360.core.entities.Graph
{
    [DataContract(Namespace = NAMESPACE), Table("Filter", Schema = "graph")]
    [JsonConverter(typeof(GraphFilterConverter))]
    public class GraphFilter : BaseObject, IUpdatedMetadata, ICreatedMetadata
    {
        [DataMember, Key, Column(Order = 1)]
        public Guid Uid { get; set; }

        [DataMember]
        public string Name { get; set; }

        private string _rawSettings = "";

        [IgnoreDataMember]
        public string Settings
        {
            get
            {
                return _rawSettings;
            }
            set
            {
                _rawSettings = value;
            }
        }

        private FilterSettings _structuredSettings;

        [NotMapped, DataMember]
        public FilterSettings StructuredSettings
        {
            get
            {
                if (_structuredSettings == null)
                {
                    _structuredSettings = JsonConvert.DeserializeObject<FilterSettings>(Settings);
                }

                return _structuredSettings;
            }
            set
            {
                _structuredSettings = null;
                Settings = JsonConvert.SerializeObject(value);
            }
        }

        public void SetSettingsFromRaw()
        {
            StructuredSettings = JsonConvert.DeserializeObject<FilterSettings>(Settings);
        }

        public void SetRawFromSettings()
        {
            Settings = JsonConvert.SerializeObject(StructuredSettings);
        }

        [DataMember]
        public bool IsPublic { get; set; }

        [DataMember]
        public bool IsDefault { get; set; }

        [DataMember]
        public int OwnedBy { get; set; }

        [DataMember]
        public DateTime? UpdatedOn { get; set; }

        [DataMember]
        public DateTime? CreatedOn { get; set; }

        [DataMember]
        public int? CreatedBy { get; set; }

        [DataMember]
        public int? UpdatedBy { get; set; }
    }

    [JsonConverter(typeof(GraphFilterConverter))]
    public class FilterSettings
    {
        public int? AncestryMode { get; set; }

        public int? NumberOfHops { get; set; }

        public int? DiagramType { get; set; }

        public List<FilterSetttingAssetType> AssetTypes { get; set; }

        public List<FilterSetttingPredicate> Predicates { get; set; }

        public List<FilterSetttingResponsibilityType> ResponsibilityTypes { get; set; }
    }

    [JsonConverter(typeof(GraphFilterConverter))]
    public class FilterSetttingAssetType
    {
        public string Class { get; set; }

        public Guid? Uid { get; set; }
    }

    [JsonConverter(typeof(GraphFilterConverter))]
    public class FilterSetttingPredicate
    {
        public string Type { get; set; }

        public Guid? Uid { get; set; }
    }

    [JsonConverter(typeof(GraphFilterConverter))]
    public class FilterSetttingResponsibilityType
    {
        public string Type { get; set; }

        public Guid? Uid { get; set; }
    }

    internal class GraphFilterConverter : JsonConverter
    {
        private static string ToCamelCaseString(string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                return char.ToLowerInvariant(str[0]) + str.Substring(1);
            }
            return str;
        }

        private static string ToPascalCaseString(string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                return char.ToUpperInvariant(str[0]) + str.Substring(1);
            }
            return str;
        }

        public override bool CanConvert(Type objectType)
        {
            return (GetType().Namespace == objectType.Namespace);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load(reader);
            object o = Activator.CreateInstance(objectType);

            serializer.Populate(jsonObject.CreateReader(), o);

            if (objectType.Name == "GraphFilter")
            {
                FilterSettings settings = new FilterSettings();
                foreach (var jp in jsonObject.Properties())
                {
                    var sp = settings.GetType().GetProperty(ToPascalCaseString(jp.Name));
                    if (sp != null)
                    {
                        var jv = serializer.Deserialize(jp.Value.CreateReader(), sp.PropertyType);
                        sp.SetValue(settings, jv);
                    }
                }
                ((GraphFilter)o).StructuredSettings = settings;
            }


            return o;
        }

        private bool IsIgnoreDataMemberAttribute(System.Reflection.PropertyInfo p)
        {
            foreach (var attr in p.GetCustomAttributes(true))
            {
                if (attr.GetType().Name == "IgnoreDataMemberAttribute")
                {
                    return true;
                }
            }

            return false;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                serializer.Serialize(writer, null);
                return;
            }

            writer.WriteStartObject();

            foreach (var property in value.GetType().GetProperties())
            {
                if (IsIgnoreDataMemberAttribute(property))
                {
                    continue;
                }

                if (property.Name == "StructuredSettings")
                {
                    var settingValue = property.GetValue(value, null);
                    foreach (var settingPropperty in settingValue.GetType().GetProperties())
                    {
                        writer.WritePropertyName(ToCamelCaseString(settingPropperty.Name));
                        serializer.Serialize(writer, settingPropperty.GetValue(settingValue, null));

                    }
                }
                else
                {
                    var propVal = property.GetValue(value, null);
                    if (propVal != null)
                    {
                        writer.WritePropertyName(ToCamelCaseString(property.Name));
                        serializer.Serialize(writer, propVal);
                    }
                }

            }

            writer.WriteEndObject();
        }
    }
}
