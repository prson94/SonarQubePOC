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
    public enum MetricUpdateFrequency
    {
        [Name("Not Applicable"), EnumMember(Value = "None"), ReadOnly(false), Description("")]
        None = 0,
        
        [Name("Hourly"), EnumMember(Value = "Hourly"), ReadOnly(false), Description("")]
        Hourly = 1,
        
        [Name("Daily"), EnumMember(Value = "Daily"), ReadOnly(false), Description("")]
        Daily = 2,
        
        [Name("Weekly"), EnumMember(Value = "Weekly"), ReadOnly(false), Description("")]
        Weekly = 3,
        
        [Name("Monthly"), EnumMember(Value = "Monthly"), ReadOnly(false), Description("")]
        Monthly = 4,
        
        [Name("Quarterly"), EnumMember(Value = "Quarterly"), ReadOnly(false), Description("")]
        Quarterly = 5,
        
        [Name("Annually"), EnumMember(Value = "Annually"), ReadOnly(false), Description("")]
        Annually = 6
    }

    public class MetricUpdateFrequencyInfo
    {
        public MetricUpdateFrequency ID { get; set; }
        
        public string Name { get; set; }
        
        public string Description { get; set; }
    }

    public static class MetricUpdateFrequencyClassExtensions
    {
        public static string GetDisplayName(this MetricUpdateFrequency type)
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

        public static List<MetricUpdateFrequencyInfo> GetAsList(this MetricUpdateFrequency type)
        {
            var list = new List<MetricUpdateFrequencyInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                if (tm.GetCustomAttribute(typeof(ObsoleteAttribute)) == null)
                {
                    var enumValue = (MetricUpdateFrequency)Enum.Parse(typeof(MetricUpdateFrequency), tm.Name);

                    list.Add(new MetricUpdateFrequencyInfo
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
