using System.Xml.Linq;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace d360.core
{
    public static partial class Enums
    {
        public static Dictionary<string, string> AsDictionary(this EventStatus s)
        {
            var list = new Dictionary<string, string>();

            foreach (MemberInfo tm in s.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                list.Add(tm.Name, ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description);
            }

            return list;
        }

        public static string ToDescriptionString(this EventStatus s)
        {
            DescriptionAttribute[] attributes = (DescriptionAttribute[])s.GetType().GetField(s.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : string.Empty;
        }
    }

    public enum EventStatus
    {
        [Description("Open")]
        Open,
        [Description("In Process")]
        InProcess,
        [Description("Assigned")]
        Assigned,
        [Description("Closed")]
        Closed
    }

    public enum EventCriticality
    {
        Negligible = 1,
        Low = 2,
        Medium = 3,
        High = 4,
        Critical = 5
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
