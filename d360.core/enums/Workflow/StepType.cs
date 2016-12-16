using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace d360.core.enums.Workflow
{
    public enum StepType
    {
        [Description("Start")]
        Start,
        [Description("Task")]
        Task,
        [Description("Terminate")]
        Terminate,
        [Description("Finish")]
        Finish
    }

    public class StepTypeInfo
    {
        public StepType ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public static class StepTypeExtensions
    {
        public static List<StepTypeInfo> GetList(this StepType type)
        {
            var list = new List<StepTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var info = new StepTypeInfo
                {
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (StepType)Enum.Parse(typeof(StepType), tm.Name),
                    Name = tm.Name
                };
                list.Add(info);
            }

            return list;
        }    
    }
}
