using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace d360.core
{
    public enum TemplateAction
    {
        [Description("Group Join Request")]
        JoinRequest,
        [Description("None")]
        None,
        [Description("Preview")]
        Preview,
        [Description("View Statistics")]
        Statistics,
        [Description("Lookup Preview")]
        LookupPreview,
        [Description("Assigning Item Preview")]
        AssigningItemPreview
    }

    public class TemplateActionInfo
    {
        public TemplateAction ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public static class TemplateActionExtensions
    {
        public static List<TemplateActionInfo> GetTemplateActionInfoList(this TemplateAction type)
        {
            var list = new List<TemplateActionInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                list.Add(new TemplateActionInfo
                {
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (TemplateAction)Enum.Parse(typeof(TemplateAction), tm.Name),
                    Name = tm.Name
                });
            }

            return list;
        }

    }
}
