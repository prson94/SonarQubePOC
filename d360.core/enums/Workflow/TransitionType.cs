using System;
using System.Collections.Generic;
using System.Reflection;

namespace d360.core.enums.Workflow
{
    public enum TransitionType
    {
        [Description("Always")]
        Always = 1,
        
        [Description("Conditional")]
        Condition = 2,
        
        [Description("Timer")]
        Timer = 3
    }

    public class TransitionTypeInfo
    {
        public TransitionType ID { get; set; }
        
        public string Name { get; set; }
        
        public string Description { get; set; }
    }

    public static class TransitionTypeExtensions
    {
        public static List<TransitionTypeInfo> GetList(this TransitionType type)
        {
            var list = new List<TransitionTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var info = new TransitionTypeInfo
                {
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (TransitionType)Enum.Parse(typeof(TransitionType), tm.Name),
                    Name = tm.Name
                };
                list.Add(info);
            }

            return list;
        }
    }
}
