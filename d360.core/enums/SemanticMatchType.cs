using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum SemanticMatchType
    {
        [Name("List of Values"), Icon("fa-"), Color("")]
        List = 1,
        
        [Name("Pattern of Data"), Icon("fa-"), Color("")]
        Pattern = 2,
        
        [Name("Numbers"), Icon("fa-"), Color("")]
        Number = 3,
        
        [Name("Advanced (JSON)"), Icon("fa-"), Color("")]
        Advanced = 4
    }

    public class SemanticMatchTypeInfo
    {
        public SemanticMatchType ID { get; set; }
        
        public string Value { get; set; }
        
        public string Name { get; set; }
        
        public string Icon { get; set; }
        
        public string Color { get; set; }
    }

    public static class SemanticMatchTypeExtensions
    {
        public static List<SemanticMatchTypeInfo> GetAsList(this SemanticMatchType type)
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
                        Value = enumValue.ToString()
                    });
                }
            }

            return list.OrderBy(i => i.Name).ToList();
        }
    }
}
