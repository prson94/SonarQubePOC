using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace d360.core.enums.Workflow
{
    /// <summary>
    /// Corresponds to the type of change sent to the event monitoring system.  Workflows can listen to these types of events.
    /// </summary>
    public enum ActivityType
    {
        [Name("Email Notification"), Description("Email Notification"), BackColor("#1d9d74"), ForeColor("#fff"), Icon("\uf0e0")]
        EmailNotification = 1,
        [Name("Status Change"), Description("Status Change"), BackColor("#1d339d"), ForeColor("#fff"), Icon("\uf024")]
        StatusChange = 2
    }

    public class ActivityTypeInfo
    {
        public ActivityType ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string BackColor { get; set; }
        public string ForeColor { get; set; }
        public string Icon { get; set; }
    }

    public static class ActivityTypeExtensions
    {
        public static List<ActivityTypeInfo> GetList(this ActivityType type)
        {
            var list = new List<ActivityTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var info = new ActivityTypeInfo
                {
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (ActivityType)Enum.Parse(typeof(ActivityType), tm.Name),
                    Name = tm.Name,
                    BackColor = ((BackColorAttribute)tm.GetCustomAttribute(typeof(BackColorAttribute))).Color,
                    ForeColor = ((ForeColorAttribute)tm.GetCustomAttribute(typeof(ForeColorAttribute))).Color,
                    Icon = ((IconAttribute)tm.GetCustomAttribute(typeof(IconAttribute))).Icon
                };
                list.Add(info);
            }

            return list;
        }    
    }
}
