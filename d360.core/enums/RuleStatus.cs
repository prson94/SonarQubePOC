using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum RuleStatus
    {
        [Name("Draft"), Description("The rule is in draft and not yet active.")]
        Draft = 1,
        [Name("Active"), Description("The rule is active.")]
        Active = 2,
        [Name("Inactive"), Description("The rule is inactive.")]
        Inactive = 3
    }

    public class RuleStatusInfo
    {
        public RuleStatus ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public static class RuleStatusExtensions
    {
        public static string GetRuleStatusDisplayName(this RuleStatus type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetRuleTypeDescription(this RuleStatus type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DescriptionAttribute>().Description;
        }

        public static List<RuleStatusInfo> GetRuleTypeEnumList(this RuleStatus type)
        {
            var list = new List<RuleStatusInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                list.Add(new RuleStatusInfo
                {
                    Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (RuleStatus)Enum.Parse(typeof(RuleStatus), tm.Name)
                });
            }

            return list.OrderBy(i => i.Name).ToList();
        }
    }
}
