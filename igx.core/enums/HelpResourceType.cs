using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace d360.core
{
    public enum HelpResourceType
    {
        [Description("Video")]
        Video = 1,
        [Description("File")]
        File = 2
    } 

    public class HelpResourceTypeInfo
    {
        public HelpResourceType ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public static class HelpResourceTypeExtensions
    {
        public static List<HelpResourceTypeInfo> GetHelpResourceTypeInfoList(this DataType type)
        {
            var list = new List<HelpResourceTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var info = new HelpResourceTypeInfo
                {
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (HelpResourceType)Enum.Parse(typeof(HelpResourceType), tm.Name),
                    Name = tm.Name
                };
                list.Add(info);
            }

            return list;
        }    
    }
}
