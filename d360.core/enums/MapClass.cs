using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum MapClass
    {
        [
            Name("Source To Target"), 
            Description("Allows you to define source paths between objects.")
        ]
        SourceToTarget = 1
    }

    public class MapClassInfo
    {
        public MapClass ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public static class MapClassExtensions
    {
        public static string GetDisplayName(this MapClass type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetName(this MapClass type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetDescription(this MapClass type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DescriptionAttribute>().Description;
        }

        public static List<MapClassInfo> GetAsList(this MapClass type)
        {
            var list = new List<MapClassInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                if (!((ReadOnlyAttribute)tm.GetCustomAttribute(typeof(ReadOnlyAttribute))).IsReadOnly)
                {
                    list.Add(new MapClassInfo
                    {
                        Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                        Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                        ID = (MapClass)Enum.Parse(typeof(MapClass), tm.Name)
                    });
                }
            }

            return list.OrderBy(i => i.Name).ToList();
        }
    }
}
