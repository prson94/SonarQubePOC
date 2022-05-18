using d360.core.resources;
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
        Add = 1,
        Update = 2,
        Delete = 3,
        Schedule = 4,
        ScoreUpdate = 5,
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
                var enumValue = (ChangeType)Enum.Parse(typeof(ChangeType), tm.Name);

                var info = new ChangeTypeInfo
                {
                    Description = DescriptionAsDisplayString(enumValue),
                    ID = (ChangeType)Enum.Parse(typeof(ChangeType), tm.Name),
                    Name = tm.Name
                };
                list.Add(info);
            }

            return list;
        }
        private static string DescriptionAsDisplayString(ChangeType type)
        {
            switch (type)
            {
                case ChangeType.Add: return Enums.WorkflowChangeType_ItemAdded;
                case ChangeType.Delete: return Enums.WorkflowChangeType_ItemRemoved;
                case ChangeType.RequestCertification: return Enums.WorkflowChangeType_RequestCertification;
                case ChangeType.Schedule: return Enums.WorkflowChangeType_Schedule;
                case ChangeType.ScoreUpdate: return Enums.WorkflowChangeType_ScoreChange;
                case ChangeType.Update: return Enums.WorkflowChangeType_ItemChanged;
                default: throw new ArgumentOutOfRangeException("ChangeType");
            }
        }
    }
}
