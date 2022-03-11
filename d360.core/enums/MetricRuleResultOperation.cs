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
    public enum MetricRuleResultOperation
    {
        [Name("Average"), EnumMember(Value = "Average"), Description("")]
        Average = 1,
        
        [Name("Minimum"), EnumMember(Value = "Minimum"), Description("")]
        Minimum = 2,
        
        [Name("Maximum"), EnumMember(Value = "Maximum"), Description("")]
        Maximum = 3
    }

    public class MetricRuleResultOperationInfo
    {
        public MetricRuleResultOperation ID { get; set; }
        
        public string Name { get; set; }
        
        public string Description { get; set; }
    }

    public static class MetricRuleResultOperationClassExtensions
    {
        public static string GetDisplayName(this MetricRuleResultOperation type)
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

        public static List<MetricRuleResultOperationInfo> GetAsList(this MetricRuleResultOperation type)
        {
            var list = new List<MetricRuleResultOperationInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                if (tm.GetCustomAttribute(typeof(ObsoleteAttribute)) == null)
                {
                    var enumValue = (MetricRuleResultOperation)Enum.Parse(typeof(MetricRuleResultOperation), tm.Name);

                    list.Add(new MetricRuleResultOperationInfo
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
