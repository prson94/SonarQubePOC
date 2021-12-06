using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum SemanticBaseType
    {
        [Name("Boolean"), Icon("fa-"), Color("")]
        Boolean = 1,
        [Name("Number (Double)"), Icon("fa-"), Color("")]
        Double = 2,
        [Name("Number (Long)"), Icon("fa-"), Color("")]
        Long = 3,
        [Name("String"), Icon("fa-"), Color("")]
        String = 4,
        [Name("Local Date"), Icon("fa-"), Color("")]
        LocalDate = 5,
        [Name("Local Time"), Icon("fa-"), Color("")]
        LocalTime = 6,
        [Name("Local DateTime"), Icon("fa-"), Color("")]
        LocalDateTime = 5,
        [Name("Offset DateTime"), Icon("fa-"), Color("")]
        OffsetDateTime = 6,
        [Name("Zoned DateTime"), Icon("fa-"), Color("")]
        ZonedDateTime = 7
    }

    public class SemanticBaseTypeInfo
    {
        public SemanticBaseType ID { get; set; }
        public string Value { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
    }

    public static class SemanticBaseTypeExtensions
    {
        public static List<SemanticBaseTypeInfo> GetAsList(this SemanticBaseType type)
        {
            var list = new List<SemanticBaseTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                if (tm.GetCustomAttribute(typeof(ObsoleteAttribute)) == null)
                {
                    var enumValue = Enum.Parse<SemanticBaseType>(tm.Name);

                    list.Add(new SemanticBaseTypeInfo
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
