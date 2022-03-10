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
    public enum MetricGovernanceCheckType
    {
        [Name("External Check"), EnumMember(Value = "External"), Description("")]
        External = 0,
        
        [Name("Field Check"), EnumMember(Value = "Field"), Description("")]
        Field = 1,
        
        [Name("Owner Check"), EnumMember(Value = "Owner"), Description("")]
        Owner = 2,
        
        [Name("Predicate Check"), EnumMember(Value = "Predicate"), Description("")]
        Predicate = 3,
        
        [Name("Relationship Check"), EnumMember(Value = "Relation"), Description("")]
        Relation = 4
    }

    public class MetricGovernanceCheckTypeInfo
    {
        public MetricGovernanceCheckType ID { get; set; }
        
        public string Name { get; set; }
        
        public string Description { get; set; }
    }

    public static class MetricGovernanceCheckTypeClassExtensions
    {
        public static string GetDisplayName(this MetricGovernanceCheckType type)
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

        public static List<MetricGovernanceCheckTypeInfo> GetAsList(this MetricGovernanceCheckType type)
        {
            var list = new List<MetricGovernanceCheckTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                if (tm.GetCustomAttribute(typeof(ObsoleteAttribute)) == null)
                {
                    var enumValue = (MetricGovernanceCheckType)Enum.Parse(typeof(MetricGovernanceCheckType), tm.Name);

                    list.Add(new MetricGovernanceCheckTypeInfo
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