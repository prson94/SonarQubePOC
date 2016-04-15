using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum MapType
    {
        [Name("Lineage"), Description("Allows you to define source paths between objects."), ReadOnly(false)]
        Lineage = 1,
        [Name("Source To Target"), Description("The most common mapping that allows you to set sources and targets across types contained in the system."), ReadOnly(false)]
        SourceToTarget = 2,
        [Name("Type Hierarchy"), Description("This hierarchy allows for creating a tree structure or hierarchy referencing a different artifact types at each level."), ReadOnly(false)]
        TypeHierarchy = 3,
        [Name("Group Hierarchy"), Description("This hierarchy allows for creating a tree structure or hierarchy referencing a different artifact types at each level."), ReadOnly(true)]
        GroupHierarchy = 4,
        [Name("Parent Child Hierarchy"), Description("This hierarchy allows for creating a tree structure or hierarchy referencing a different artifact types at each level."), ReadOnly(true)]
        ParentChildHierarchy = 5,
        [Name("Synonym"), Description("Allows you to establish synonyms between two objects that are synonyms of each other."), ReadOnly(false)]
        Synonym = 6,
        [Name("Simple"), Description(""), ReadOnly(false)]
        Simple = 7
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

        public static string GetName(this MapType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
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
                if (!((ReadOnlyAttribute)tm.GetCustomAttribute(typeof(ReadOnlyAttribute))).IsReadOnly)
                {
                    list.Add(new MapTypeInfo
                    {
                        Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                        Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                        ID = (MapType)Enum.Parse(typeof(MapType), tm.Name)
                    });
                }
            }

            return list.OrderBy(i => i.Name).ToList();
        }
    }
}
