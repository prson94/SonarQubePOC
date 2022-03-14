using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum SemanticStatus
    {
        [Name("Draft"), Icon("fa-"), Color("#BBBBBB"), Description("Stone")]
        Draft = 0,
        [Name("Under Review"), Icon("fa-"), Color("#e2792a"), Description("Orange")]
        InReview = 1,
        [Name("Certified"), Icon("fa-"), Color("#3f9d40"), Description("Emerald")]
        Certified = 2
    }

    public class SemanticStatusInfo
    {
        public SemanticStatus ID { get; set; }

        public string Value { get; set; }

        public string Name { get; set; }

        public string Icon { get; set; }

        public string Color { get; set; }
        public string ColorName { get; set; }
    }

    public static class SemanticStatusExtensions
    {
        public static List<SemanticStatusInfo> GetAsList(this SemanticStatus type)
        {
            var list = new List<SemanticStatusInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                if (tm.GetCustomAttribute(typeof(ObsoleteAttribute)) == null)
                {
                    var enumValue = (SemanticStatus)Enum.Parse(typeof(SemanticStatus), tm.Name);

                    list.Add(new SemanticStatusInfo
                    {
                        Name = tm.GetCustomAttribute<NameAttribute>().Name,
                        Color = tm.GetCustomAttribute<ColorAttribute>().Rgb,
                        Icon = tm.GetCustomAttribute<IconAttribute>().Icon,
                        ID = enumValue,
                        Value = enumValue.ToString(),
                        ColorName = tm.GetCustomAttribute<DescriptionAttribute>().Description
                    });
                }
            }

            return list.OrderBy(i => i.Name).ToList();
        }
    }
}
