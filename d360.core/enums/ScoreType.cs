using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum ScoreType
    {
        [Name("Governance Score"), ReadOnly(false), Description("")]
        Governance = 1,
        [Name("Data Quality Score"), ReadOnly(true), Description("")]
        DataQuality = 2,
        [Name("Perceptional Score"), ReadOnly(true), Description("")]
        Perceptional = 3
    }
    public class ScoreTypeInfo
    {
        public ScoreType ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
    public static class ScoreTypeClassExtensions
    {
        public static string GetDisplayName(this ScoreType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static List<ScoreTypeInfo> GetAsList(this ScoreType type)
        {
            var list = new List<ScoreTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                if (tm.GetCustomAttribute(typeof(ObsoleteAttribute)) == null)
                {
                    var enumValue = (ScoreType)Enum.Parse(typeof(ScoreType), tm.Name);

                    list.Add(new ScoreTypeInfo
                    {
                        Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                        Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                        ID = enumValue
                    });
                }
            }

            return list.OrderBy(i => i.Name).ToList();
        }

    }
}