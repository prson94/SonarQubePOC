using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum IntersectFlowConfiguration
    {
        [Name("Simple Hierarchy"), Description("This simple hierarchy allows for creating a tree structure or hierarchy referencing the same underlying artifact type.")]
        SimpleHierarchy = 1,
        [Name("Type Hierarchy")Description("This hierarchy allows for creating a tree structure or hierarchy referencing a different artifact types at each level.")]
        TypeHierarchy = 2,
        [Name("Name/Value Mapping")Description("Name-value mapping.")]
        NameValue = 3,
        [Name("Calculation Mapping")Description("A calculation.")]
        Calculation = 4,
        [Name("Sourcing Hierarchy Mapping")Description("A set of calculations involving a hierarchy or ordering to which source to choose, depending on context.")]
        SourcingHierarchy = 5
    }

    public class IntersectFlowConfigurationInfo
    {
        public IntersectFlowConfiguration ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public static class IntersectFlowTypeExtensions
    {
        public static string GetDisplayName(this IntersectFlowConfiguration type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DisplayNameAttribute>().DisplayName;
        }

        public static string GetDescription(this IntersectFlowConfiguration type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DescriptionAttribute>().Description;
        }

        public static List<IntersectFlowConfigurationInfo> GetAsList(this IntersectFlowConfiguration type)
        {
            var list = new List<IntersectFlowConfigurationInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                list.Add(new IntersectFlowConfigurationInfo
                {
                    Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (IntersectFlowConfiguration)Enum.Parse(typeof(IntersectFlowConfiguration), tm.Name)
                });
            }

            return list.OrderBy(i => i.Name).ToList();
        }
    }
}
