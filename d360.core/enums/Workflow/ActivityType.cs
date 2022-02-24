using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace d360.core.enums.Workflow
{
    /// <summary>
    /// Corresponds to the type of change sent to the event monitoring system.  Workflows can listen to these types of events.
    /// </summary>
    public enum WorkflowActivityType
    {
        [Name("None"), Description("None"), BackColor("#000"), ForeColor("#fff"), Icon("\uf128")]
        None = 0,
        
        [Name("Email Notification"), Description("Email Notification"), BackColor("#1d9d74"), ForeColor("#fff"), Icon("\uf0e0")]
        EmailNotification = 1,
        
        [Name("Status Change"), Description("Status Change"), BackColor("#1d339d"), ForeColor("#fff"), Icon("\uf024")]
        StatusChange = 2,
        
        [Name("Form"), Description("Form"), BackColor("#aa2a83"), ForeColor("#fff"), Icon("\uf1de")]
        Form = 3,
        
        [Name("Procedure"), Description("Sql Procedure"), BackColor("#4cde77"), ForeColor("#fff"), Icon("\uf1c0")]
        Procedure = 4,
        
        [Name("Field Change"), Description("Field Change"), BackColor("#ff6600"), ForeColor("#fff"), Icon("\uf2c2")]
        FieldChange = 5,
        
        [Name("Relationship Change"), Description("Relationship Change"), BackColor("#0066CC"), ForeColor("#fff"), Icon("\uf0c0")]
        RelationshipChange = 6,
        
        [Name("State Change"), Description("State Change"), BackColor("#ae335f"), ForeColor("#fff"), Icon("\uf0c5")]
        StateChange = 7,
        
        [Name("Delete"), Description("Delete"), BackColor("#b99f39"), ForeColor("#fff"), Icon("\uf014")]
        Delete = 8,
        
        [Name("HTTP Request"), Description("HTTP Request"), BackColor("#597897"), ForeColor("#fff"), Icon("\uf0ac")]
        HTTPRequest = 9,
        
        [Name("HTTP Response"), Description("HTTP Response"), BackColor("#d11947"), ForeColor("#fff"), Icon("\uf085")]
        HTTPResponse = 10,
    }

    public class ActivityTypeInfo
    {
        public WorkflowActivityType ID { get; set; }
        
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        public string BackColor { get; set; }
        
        public string ForeColor { get; set; }
        
        public string Icon { get; set; }
        
        public bool IsShow { get; set; }
    }

    public static class ActivityTypeExtensions
    {
        public static string GetName(this WorkflowActivityType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static List<ActivityTypeInfo> GetList(this WorkflowActivityType type)
        {
            var list = new List<ActivityTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var info = new ActivityTypeInfo
                {
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (WorkflowActivityType)Enum.Parse(typeof(WorkflowActivityType), tm.Name),
                    Name = tm.Name,
                    BackColor = ((BackColorAttribute)tm.GetCustomAttribute(typeof(BackColorAttribute))).Color,
                    ForeColor = ((ForeColorAttribute)tm.GetCustomAttribute(typeof(ForeColorAttribute))).Color,
                    Icon = ((IconAttribute)tm.GetCustomAttribute(typeof(IconAttribute))).Icon,
                    IsShow = true
                };
                list.Add(info);
            }
            return list;
        }
    }
}
