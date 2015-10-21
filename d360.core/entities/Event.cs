using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.ComponentModel;

namespace d360.core
{
    public enum EventCriticality
    {
        [Description("This event is purely information and has no impact on systems or processes."), Name("Negligible")]
        Negligible = 1,
        [Description(""), Name("Low")]
        Low = 2,
        [Description(""), Name("Medium")]
        Medium = 3,
        [Description(""), Name("High")]
        High = 4,
        [Description(""), Name("Critical")]
        Critical = 5
    }

    public class EventCriticalityInfo
    {
        public EventCriticality ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public static class EventCriticalityExtensions
    {
        public static string GetWorkflowTypeDisplayName(this EventCriticality type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetWorkflowTypeDescription(this EventCriticality type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DescriptionAttribute>().Description;
        }

        public static List<EventCriticalityInfo> GetAsList(this EventCriticality type)
        {
            var list = new List<EventCriticalityInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                list.Add(new EventCriticalityInfo
                {
                    Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (EventCriticality)Enum.Parse(typeof(EventCriticality), tm.Name)
                });
            }

            return list;
        }
    }
}

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.Event, "Event")]
    public class Event : BaseIntObject, IIntObject, IFieldsObject
    {
        [DataMember]
        public int EventGroupID { get; set; }

        [DataMember]
        public EventCriticality Criticality { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "SourceID_Name", Description = "SourceID_Description")]
        public string SourceID { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Status_Name", Description = "Status_Description")]
        public string Status { get; set; }

        [DataMember]
        public DateTime Date { get; set; }

        [IgnoreDataMember]
        public virtual EventGroup EventGroup { get; set; }
    }
}
