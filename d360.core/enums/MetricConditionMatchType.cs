using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace d360.core.enums
{
    [JsonConverter(typeof(StringEnumConverter), true)]
    public enum MetricMatchType
    {
        [Name("Any"), EnumMember(Value = "Any"), ReadOnly(false), Description("")]
        Any = 1,

        [Name("All"), EnumMember(Value = "All"), ReadOnly(false), Description("")]
        All = 2
    }

    public class MetricMatchTypeInfo
    {
        public MetricMatchType ID { get; set; }
        
        public string Name { get; set; }
        
        public string Description { get; set; }
    }

    public static class MetricMatchTypeClassExtensions
    {
        public static string GetDisplayName(this MetricMatchType type)
        {
            try
            {
                return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
            }
            catch
            {
                return type.ToString();
            }
        }

        public static List<MetricMatchTypeInfo> GetAsList(this MetricMatchType type)
        {
            var list = new List<MetricMatchTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                if (tm.GetCustomAttribute(typeof(ObsoleteAttribute)) == null)
                {
                    var enumValue = (MetricMatchType)Enum.Parse(typeof(MetricMatchType), tm.Name);

                    list.Add(new MetricMatchTypeInfo
                    {
                        Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                        Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                        ID = enumValue
                    });
                }
            }

            return list.OrderBy(i => i.Name).ToList();
        }
    }
}
