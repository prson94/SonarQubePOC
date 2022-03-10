using System;
using System.Collections.Generic;
using System.Reflection;

namespace d360.core.enums.Workflow
{
    /// <summary>
    /// Corresponds to the type of change sent to the event monitoring system.  Workflows can listen to these types of events.
    /// </summary>
    public enum ChangeType
    {
        [Description("Item Added")]
        Add = 1,

        [Description("Item Changed")]
        Update = 2,
        
        [Description("Item Removed")]
        Delete = 3,
        
        [Description("Schedule")]
        Schedule = 4,
        
        [Description("Score Changed")]
        ScoreUpdate = 5,
        
        [Description("Request Certification")]
        RequestCertification = 8
    }

    public class ChangeTypeInfo
    {
        public ChangeType ID { get; set; }
        
        public string Name { get; set; }
        
        public string Description { get; set; }
    }

    public static class ChangeTypeExtensions
    {
        public static List<ChangeTypeInfo> GetList(this ChangeType type)
        {
            var list = new List<ChangeTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var info = new ChangeTypeInfo
                {
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (ChangeType)Enum.Parse(typeof(ChangeType), tm.Name),
                    Name = tm.Name
                };
                list.Add(info);
            }

            return list;
        }
    }
}
