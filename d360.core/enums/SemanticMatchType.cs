using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum SemanticMatchType
    {
        [Name("List of Values"), Icon("fa-"), Color(""), Description("Classify data based on a finite list of strings that are present in the underlying data. Optionally words can be provided to look in the header of the column.")]
        List = 1,
        
        [Name("Pattern in Data"), Icon("fa-"), Color(""), Description("Classify data based on a pattern presented in the data. Optionally words can be provided to look in the header of the column.")]
        Pattern = 2,
        
        [Name("Numbers"), Icon("fa-"), Color(""), Description("Classify a number based on a header name, min, max values in the data, or a specific number of digits/decimal places. Words should be provided to look in the header of the column.")]
        Number = 3,
        
        [Name("Advanced (JSON)"), Icon("fa-"), Color(""), Description("")]
        Advanced = 4
    }

    public class SemanticMatchTypeInfo
    {
        public SemanticMatchType ID { get; set; }
        
        public string Value { get; set; }
        
        public string Name { get; set; }
        
        public string Icon { get; set; }
        
        public string Color { get; set; }

        public string Description { get; set; }
    }

    public static class SemanticMatchTypeExtensions
    {
        public static List<SemanticMatchTypeInfo> GetAsList(this SemanticMatchType type, string orderBy = "name")
        {
            var list = new List<SemanticMatchTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                if (tm.GetCustomAttribute(typeof(ObsoleteAttribute)) == null)
                {
                    var enumValue = (SemanticMatchType)Enum.Parse(typeof(SemanticMatchType), tm.Name);

                    list.Add(new SemanticMatchTypeInfo
                    {
                        Name = tm.GetCustomAttribute<NameAttribute>().Name,
                        Color = tm.GetCustomAttribute<ColorAttribute>().Rgb,
                        Icon = tm.GetCustomAttribute<IconAttribute>().Icon,
                        ID = enumValue,
                        Value = enumValue.ToString(),
                        Description = tm.GetCustomAttribute<DescriptionAttribute>().Description
                    });
                }
            }

            if (orderBy == "none")
            {
                return list;
            }

            return list.OrderBy(i => i.Name).ToList();
        }
    }
}
