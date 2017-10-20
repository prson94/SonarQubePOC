using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum AssetTypeClass
    {
        [
            Name("Glossary"), 
            Description("Glossary assets.")
        ]
        Glossary = 1,
        [
            Name("Model"),
            Description("Model assets.")
        ]
        Model = 2,
        [
            Name("Fusion"),
            Description("Fusion assets.")
        ]
        Fusion = 3,
        [
            Name("Fusion Attribute"),
            Description("Fusion attribute assets.")
        ]
        FusionAttribute = 4,
        [
            Name("Fusion Query"),
            Description("Fusion query assets.")
        ]
        FusionQuery = 4,
        [
            Name("Attribute Group"),
            Description("Attribute group data.")
        ]
        AttributeGroup = 5,
        [
            Name("Policy"),
            Description("Policy asset.")
        ]
        Policy = 6,
        [
            Name("Rule"),
            Description("Rule asset.")
        ]
        Rule = 7,
        [
            Name("Map"),
            Description("Map asset.")
        ]
        Map = 8,
        [
            Name("Reference"),
            Description("Reference asset.")
        ]
        Reference = 9
    }

    public class AssetTypeClassInfo
    {
        public AssetTypeClass ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public static class AssetTypeClassExtensions
    {
        public static string GetDisplayName(this AssetTypeClass type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetName(this AssetTypeClass type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetDescription(this AssetTypeClass type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DescriptionAttribute>().Description;
        }

        public static List<AssetTypeClassInfo> GetAsList(this AssetTypeClass type)
        {
            var list = new List<AssetTypeClassInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                if (!((ReadOnlyAttribute)tm.GetCustomAttribute(typeof(ReadOnlyAttribute))).IsReadOnly)
                {
                    list.Add(new AssetTypeClassInfo
                    {
                        Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                        Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                        ID = (AssetTypeClass)Enum.Parse(typeof(AssetTypeClass), tm.Name)
                    });
                }
            }

            return list.OrderBy(i => i.Name).ToList();
        }
    }
}
