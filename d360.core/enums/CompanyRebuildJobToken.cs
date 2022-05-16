using d360.core.resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum CompanyRebuildJobToken
    {
        [
            QueueSettingName("AssetGraphQueue")
        ]
        AssetGraph = 1,

        [
            QueueSettingName("DisplayValueQueue")
        ]
        DisplayValues,

        [
            QueueSettingName("SearchIndexQueue")
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
                        Name = NameAsDisplayString(type),
                        QueueSettingName = ((QueueSettingNameAttribute)tm.GetCustomAttribute(typeof(QueueSettingNameAttribute))).Name,
                        Description = DescriptionAsDisplayString(enumValue),
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

            info.Description = DescriptionAsDisplayString(type);
            info.Name = NameAsDisplayString(type);
            info.QueueSettingName = member.GetCustomAttribute<QueueSettingNameAttribute>().Name;
            info.ID = type;
            info.Value = type.ToString();
            return info;
        }
        
        private static string DescriptionAsDisplayString(CompanyRebuildJobToken type)
        {
            switch (type)
            {
                case CompanyRebuildJobToken.AssetGraph: return Enums.CompanyRebuildJobToken_AssetGraph_Desc;
                case CompanyRebuildJobToken.DisplayValues: return Enums.CompanyRebuildJobToken_DisplayValues_Desc;
                case CompanyRebuildJobToken.SearchIndex: return Enums.CompanyRebuildJobToken_SearchIndex_Desc;
                default: throw new ArgumentOutOfRangeException("CompanyRebuildJobToken");
            }
        }

        private static string NameAsDisplayString(CompanyRebuildJobToken type)
        {
            switch (type)
            {
                case CompanyRebuildJobToken.AssetGraph: return Enums.CompanyRebuildJobToken_AssetGraph;
                case CompanyRebuildJobToken.DisplayValues: return Enums.CompanyRebuildJobToken_DisplayValues;
                case CompanyRebuildJobToken.SearchIndex: return Enums.CompanyRebuildJobToken_SearchIndex;
                default: throw new ArgumentOutOfRangeException("CompanyRebuildJobToken");
            }
        }
    }
}
