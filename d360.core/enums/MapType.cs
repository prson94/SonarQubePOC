using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum MapType
    {
        [Name("Source To Target"), Description("The most common mapping that allows you to set sources and targets across types contained in the system.")]
        SourceToTarget = 1,
        [Name("Simple Hierarchy"), Description("This simple hierarchy allows for creating a tree structure or hierarchy referencing the same underlying artifact type.")]
        SimpleHierarchy = 2,
        [Name("Type Hierarchy")Description("This hierarchy allows for creating a tree structure or hierarchy referencing a different artifact types at each level.")]
        TypeHierarchy = 3,
        [Name("Sourcing Hierarchy")Description("A set of calculations involving a hierarchy or ordering to which source to choose, depending on context.")]
        SourcingHierarchy = 4
    }

    public class MapTypeInfo
    {
        public MapType ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public static class MapTypeExtensions
    {
        public static string GetDisplayName(this MapType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DisplayNameAttribute>().DisplayName;
        }

        public static string GetDescription(this MapType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DescriptionAttribute>().Description;
        }

        public static List<MapTypeInfo> GetAsList(this MapType type)
        {
            var list = new List<MapTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                list.Add(new MapTypeInfo
                {
                    Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (MapType)Enum.Parse(typeof(MapType), tm.Name)
                });
            }

            return list.OrderBy(i => i.Name).ToList();
        }
    }
}
