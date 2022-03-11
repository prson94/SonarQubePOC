using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace d360.core.enums
{
    [JsonConverter(typeof(StringEnumConverter), true)]
    public enum MetricConditionType
    {
        [Name("N/A"), EnumMember(Value = "NotApplicable"), Description("")]
        NotApplicable = 0,

        [Name("And"), EnumMember(Value = "And"), Description("")]
        And = 1,
        
        [Name("Or"), EnumMember(Value = "Or"), Description("")]
        Or = 2
    }

    public class MetricConditionTypeInfo
    {
        public MetricConditionType ID { get; set; }
        
        public string Name { get; set; }
        
        public string Description { get; set; }
    }

    public static class MetricConditionTypeClassExtensions
    {
        public static string GetDisplayName(this MetricConditionType type)
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

        public static List<MetricConditionTypeInfo> GetAsList(this MetricConditionType type)
        {
            var list = new List<MetricConditionTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                if (tm.GetCustomAttribute(typeof(ObsoleteAttribute)) == null)
                {
                    var enumValue = (MetricConditionType)Enum.Parse(typeof(MetricConditionType), tm.Name);

                    list.Add(new MetricConditionTypeInfo
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