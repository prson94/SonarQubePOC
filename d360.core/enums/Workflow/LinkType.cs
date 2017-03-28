using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace d360.core.enums.Workflow
{
    public enum LinkType
    {
        [Description("Always")]
        Always = 1,
        [Description("Conditional")]
        Condition = 2,
        [Description("Timer")]
        Timer = 3
    }

    public class LinkTypeInfo
    {
        public LinkType ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public static class LinkTypeExtensions
    {
        public static List<LinkTypeInfo> GetList(this LinkType type)
        {
            var list = new List<LinkTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var info = new LinkTypeInfo
                {
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (LinkType)Enum.Parse(typeof(LinkType), tm.Name),
                    Name = tm.Name
                };
                list.Add(info);
            }

            return list;
        }    
    }
}
