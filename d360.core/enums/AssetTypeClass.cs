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
            Name("Generic Type"),
            Description("Generic Type.")
        ]
        Generic = 0,
        [
            Name("Business Asset"), 
            Description("Business assets."),
            IsAllowedAutoDisplayParent(true), 
            AllowCommentsOnAsset
        ]
        BusinessAsset = 1,
        [
            Name("Model"),
            Description("Model assets."),
            AllowCommentsOnAsset
        ]
        Model = 2,
        [
            Name("Policy"),
            Description("Policy asset."),
            AllowCommentsOnAsset
        ]
        Policy = 6,
        [
            Name("Rule"),
            Description("Rule asset."),
            AllowCommentsOnAsset
        ]
        Rule = 7,
        [
            Name("Technical Asset"),
            Description("Technical asset that represent items like database columns, schemas, XML attributes, etc."),
            IsAllowedAutoDisplayParent(true),
            AllowCommentsOnAsset
        ]
        TechnicalAsset = 8,
        [
            Name("Reference"),
            Description("Reference asset.")
        ]
        Reference = 9,
        [
            Name("Organization"),
            Description("Organization asset.")
        ]
        Organization = 10,
        [
            Name("User"),
            Description("User asset.")
        ]
        User = 11,
        [
            Name("Group"),
            Description("Group asset.")
        ]
        Group = 12,
        [
            Name("Reference List"),
            Description("Reference Item List.")
        ]
        ReferenceItemType = 14,
        [
            Name("Diagram"),
            Description("Diagram asset.")
        ]
        Diagram = 15,
        [
            Name("MetricAllocation"),
            Description("Metric Allocation.")
        ]
        MetricAllocation = 16,
        [
            Name("Predicate"),
            Description("Predicate.")
        ]
        Predicate = 17,
        [
            Name("SemanticType"),
            Description("Semantic Type.")
        ]
        SemanticType = 18,
        [
            Name("Glossary-Obsolete"),
            Obsolete("Use BusinessAsset instead", false),
            Description("Obsolete - do not use.")
        ]
        Glossary = 100,
    }

    public class AssetTypeClassInfo
    {
        public AssetTypeClass ID { get; set; }
        public string Value { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool AllowCommentsOnAsset { get; set; }
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

        public static bool AllowsAutoDisplayParent(this AssetTypeClass type)
        {
            var attr = type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<IsAllowedAutoDisplayParentAttribute>();
            if (attr == null)
                return false;
            else
                return attr._isAllowedAutoDisplayParent;
        }


        public static List<AssetTypeClassInfo> GetAsList(this AssetTypeClass type)
        {
            var list = new List<AssetTypeClassInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                if (tm.GetCustomAttribute(typeof(ObsoleteAttribute)) == null)
                {
                    var enumValue = (AssetTypeClass)Enum.Parse(typeof(AssetTypeClass), tm.Name);

                    list.Add(new AssetTypeClassInfo
                    {
                        Name = tm.GetCustomAttribute<NameAttribute>().Name,
                        Description = tm.GetCustomAttribute<DescriptionAttribute>().Description,
                        ID = enumValue,
                        Value = enumValue.ToString(),
                        AllowCommentsOnAsset = tm.IsDefined(typeof(AllowCommentsOnAssetAttribute)) ? tm.GetCustomAttribute<AllowCommentsOnAssetAttribute>().Allowed : false
                    });
                }
            }

            return list.OrderBy(i => i.Name).ToList();
        }

        public static AssetTypeClassInfo AsInfoModel(this AssetTypeClass type)
        {
            var info = new AssetTypeClassInfo();

            var member = type.GetType().GetMember(type.ToString()).Single();

            info.Description = member.GetCustomAttribute<DescriptionAttribute>().Description;
            info.Name = member.GetCustomAttribute<NameAttribute>().Name;
            info.AllowCommentsOnAsset = member.IsDefined(typeof(AllowCommentsOnAssetAttribute)) ? member.GetCustomAttribute<AllowCommentsOnAssetAttribute>().Allowed : false;
            info.ID = type;
            info.Value = type.ToString();
            return info;
        }
    }
}
