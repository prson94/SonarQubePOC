using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace d360.core.enums
{
    public enum RuleType
    {
        [Name("Informational"), Description("An informational rule such as a rule defining a data event.  This rule delivers events that are purely informational, and there is no need to perform any other steps.")]
        Informational = 1,
        [Name("Quality Check"), Description("A quality check rule.")]
        Quality = 2,
        [Name("Metric"), Description("A metric rule.  These rules can be included as part of scoring for a related item.")]
        Metric = 3,
        [Name("Profile"), Description("A profile rule.")]
        Profile = 4
    }

    public class RuleTypeInfo
    {
        public RuleType ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public static class RuleTypeExtensions
    {
        public static string GetRuleTypeDisplayName(this RuleType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetRuleTypeDescription(this RuleType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DescriptionAttribute>().Description;
        }

        public static List<RuleTypeInfo> GetRuleTypeEnumList(this RuleType type)
        {
            var list = new List<RuleTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                list.Add(new RuleTypeInfo
                {
                    Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (RuleType)Enum.Parse(typeof(RuleType), tm.Name)
                });
            }

            return list.OrderBy(i => i.Name).ToList();
        }
    }
}
