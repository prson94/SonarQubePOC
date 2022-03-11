using System;
using System.Collections.Generic;
using System.Reflection;

namespace d360.core
{
    public enum ComplexLookupRelationType
    {
        [Description("Standard Relationship")]
        StandardRelationship = 1,
        
        [Description("Child Relationship")]
        ChildRelationship = 2,
        
        [Description("Child Item")]
        ChildItem = 3,
        
        [Description("Parent Item")]
        ParentItem = 4
    }

    public class ComplexLookupRelationTypeInfo
    {
        public ComplexLookupRelationType ID { get; set; }
        
        public string Name { get; set; }
       
        public string DisplayName { get; set; }
    }

    public static class ComplexLookupRelationTypeExtensions
    {
        public static List<ComplexLookupRelationTypeInfo> GetComplexLookupRelationTypeInfoList(this ComplexLookupRelationType type)
        {
            var list = new List<ComplexLookupRelationTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var info = new ComplexLookupRelationTypeInfo
                {
                    ID = (ComplexLookupRelationType)Enum.Parse(typeof(ComplexLookupRelationType), tm.Name),
                    Name = tm.Name,
                    DisplayName = ((DescriptionAttribute)(tm.GetCustomAttributes(typeof(DescriptionAttribute), false)[0]))?.Description ?? tm.Name
                };
                list.Add(info);
            }

            return list;
        }
    }
}
