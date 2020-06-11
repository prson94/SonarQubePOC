using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum StatisticCheckType
    {
        [Name("Existence Check"), Description("")]
        Existence = 1,
        [Name("External Metric"), Description("This metric is loaded from an external source.")]
        External = 2,
        [Name("Field Value Check"), Description("")]
        PropertyValueCheck = 3,
        [Name("Field Populated Check"), Description("")]
        PropertyPopulated = 4,
        [Name("Relationship Existence Check"), Description("")]
        Relationship = 5,
        [Name("Fusion Ownership Check"), Description("")]
        FusionOwnership = 6,
        [Name("Relationship Score Rollup Check"), Description("")]
        ScoreRollupViaRelationship = 7,
        [Name("Ownership Score Rollup Check"), Description("")]
        ScoreRollupViaOwnership = 8,
        [Name("Result Metric Check"), Description(""), ReadOnly(true)]
        ResultMetric = 9,
        [Name("Predicate Existence Check"), Description("")]
        PredicateMetric = 10
    }

    public class StatisticCheckTypeInfo
    {
        public StatisticCheckType ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public static class StatisticCheckTypeExtensions
    {
        public static string GetDisplayName(this StatisticCheckType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetDescription(this StatisticCheckType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DescriptionAttribute>().Description;
        }

        public static List<StatisticCheckTypeInfo> GetEnumList(this StatisticCheckType type)
        {
            var list = new List<StatisticCheckTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                list.Add(new StatisticCheckTypeInfo
                {
                    Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (StatisticCheckType)Enum.Parse(typeof(StatisticCheckType), tm.Name)
                });
            }

            return list.OrderBy(i => i.Name).ToList();
        }
    }
}
