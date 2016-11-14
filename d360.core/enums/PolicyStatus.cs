using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum PolicyStatus
    {
        [Name("Draft"), Description("The rule is in draft and not yet active.")]
        Draft = 1,
        [Name("Active"), Description("The rule is active.")]
        Active = 2,
        [Name("Inactive"), Description("The rule is inactive.")]
        Inactive = 3
    }

    public class PolicyStatusInfo
    {
        public PolicyStatus ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public static class PolicyStatusExtensions
    {
        public static string GetStatusDisplayName(this RuleStatus type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetStatusDescription(this RuleStatus type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DescriptionAttribute>().Description;
        }

        public static List<PolicyStatusInfo> GetStatusEnumList(this PolicyStatus type)
        {
            var list = new List<PolicyStatusInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                list.Add(new PolicyStatusInfo
                {
                    Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (PolicyStatus)Enum.Parse(typeof(PolicyStatus), tm.Name)
                });
            }

            return list.OrderBy(i => i.Name).ToList();
        }
    }
}
