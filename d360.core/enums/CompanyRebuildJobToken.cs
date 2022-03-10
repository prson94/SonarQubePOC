using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum CompanyRebuildJobToken
    {
        [
            Name("Asset Graph"),
            QueueSettingName("AssetGraphQueue"),
            Description("Rebuild Visualization diagrams for assets. This request may take a significant amount of time to complete, depending on the number of assets contained within your environment. Additionally, this action has a performance impact on your environment.")
        ]
        AssetGraph = 1,
        
        [
            Name("Display Values"),
            QueueSettingName("DisplayValueQueue"),
            Description("Rebuild display values for all assets within your environment.")
        ]
        DisplayValues,
        
        [
            Name("Search Index"),
            QueueSettingName("SearchIndexQueue"),
            Description("Rebuilds the full-text index for all assets within your environment, and can take up to sixty minutes to fully rebuild.")
        ]
        SearchIndex
    }

    public class CompanyRebuildJobTokenInfo
    {
        public CompanyRebuildJobToken ID { get; set; }

        public string Value { get; set; }

        public string Name { get; set; }

        public string QueueSettingName { get; set; }

        public string Description { get; set; }
    }

    public static class CompanyRebuildJobTokenExtensions
    {
        public static string GetDisplayName(this CompanyRebuildJobToken type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetName(this CompanyRebuildJobToken type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetDescription(this CompanyRebuildJobToken type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DescriptionAttribute>().Description;
        }

        public static List<CompanyRebuildJobTokenInfo> GetAsList(this CompanyRebuildJobToken type)
        {
            var list = new List<CompanyRebuildJobTokenInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                if (tm.GetCustomAttribute(typeof(ObsoleteAttribute)) == null)
                {
                    var enumValue = (CompanyRebuildJobToken)Enum.Parse(typeof(CompanyRebuildJobToken), tm.Name);

                    list.Add(new CompanyRebuildJobTokenInfo
                    {
                        Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                        QueueSettingName = ((QueueSettingNameAttribute)tm.GetCustomAttribute(typeof(QueueSettingNameAttribute))).Name,
                        Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                        ID = enumValue,
                        Value = enumValue.ToString()
                    });
                }
            }

            return list.OrderBy(i => i.Name).ToList();
        }

        public static CompanyRebuildJobTokenInfo AsInfoModel(this CompanyRebuildJobToken type)
        {
            var info = new CompanyRebuildJobTokenInfo();

            var member = type.GetType().GetMember(type.ToString()).Single();

            info.Description = member.GetCustomAttribute<DescriptionAttribute>().Description;
            info.Name = member.GetCustomAttribute<NameAttribute>().Name;
            info.QueueSettingName = member.GetCustomAttribute<QueueSettingNameAttribute>().Name;
            info.ID = type;
            info.Value = type.ToString();
            return info;
        }
    }
}
