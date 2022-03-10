using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum CalculationMethod
    {
        [Name("Average"), Description("")]
        Average = 1,
        
        [Name("Maximum"), Description("")]
        Maximum = 2,
        
        [Name("Minimum"), Description("")]
        Minimum = 3,
        
        [Name("Weighted"), Description("")]
        Weighted = 4
    }

    public class CalculationMethodInfo
    {
        public CalculationMethod ID { get; set; }
        
        public string Name { get; set; }
        
        public string Description { get; set; }
    }

    public static class CalculationMethodExtensions
    {
        public static List<CalculationMethodInfo> GetList(this CalculationMethod type)
        {
            var list = new List<CalculationMethodInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var info = new CalculationMethodInfo
                {
                    ID = (CalculationMethod)Enum.Parse(typeof(CalculationMethod), tm.Name),
                    Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description
                };
                list.Add(info);
            }

            return list;
        }

        public static CalculationMethodInfo AsInfoModel(this CalculationMethod type)
        {
            var t = type.GetType().GetMember(type.ToString()).First();
            return
                new CalculationMethodInfo
                {
                    Name = ((NameAttribute)t.GetCustomAttribute(typeof(NameAttribute))).Name,
                    Description = ((DescriptionAttribute)t.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = type,
                };
        }
    }
}
