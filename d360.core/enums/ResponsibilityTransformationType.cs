using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum ResponsibilityTransformationType
    {
        [Name("Business Transformation"), Description("A business definition of the transformation taking place.")]
        Business = 1,
        [Name("Technical Transformation"), Description("A technical definition of the transformation taking place, including ETL package names or logic within the package.")]
        Technical = 2
    }

    public class ResponsibilityTransformationTypeInfo
    {
        public ResponsibilityTransformationType ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public static class ResponsibilityTransformationTypeExtensions
    {
        public static string GetDisplayName(this ResponsibilityTransformationType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetDescription(this ResponsibilityTransformationType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DescriptionAttribute>().Description;
        }

        public static List<ResponsibilityTransformationTypeInfo> GetEnumList(this ResponsibilityTransformationType type)
        {
            var list = new List<ResponsibilityTransformationTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                list.Add(new ResponsibilityTransformationTypeInfo
                {
                    Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (ResponsibilityTransformationType)Enum.Parse(typeof(ResponsibilityTransformationType), tm.Name)
                });
            }

            return list;
        }
    }
}
