using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum ApiUriType
    {
        [
            Name("Collection"), 
            Description("Collection.")
        ]
        Collection = 1,
        [
            Name("Singleton"),
            Description("Single asset.")
        ]
        Singleton = 2
    }

    public class ApiUriTypeInfo
    {
        public ApiUriType ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public static class ApiUriTypeExtensions
    {
        public static string GetDisplayName(this ApiUriType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetName(this ApiUriType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetDescription(this ApiUriType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DescriptionAttribute>().Description;
        }

        public static List<ApiUriTypeInfo> GetAsList(this ApiUriType type)
        {
            var list = new List<ApiUriTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                if (!((ReadOnlyAttribute)tm.GetCustomAttribute(typeof(ReadOnlyAttribute))).IsReadOnly)
                {
                    list.Add(new ApiUriTypeInfo
                    {
                        Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                        Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                        ID = (ApiUriType)Enum.Parse(typeof(ApiUriType), tm.Name)
                    });
                }
            }

            return list.OrderBy(i => i.Name).ToList();
        }
    }
}
