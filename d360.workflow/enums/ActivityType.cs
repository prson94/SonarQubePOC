using d360.core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace d360.workflow
{
    [DataContract]
    public enum ActivityType
    {
        [Name("Approval by Owner"), Description("The assigned owner must complete this activity in order to continue the approval process."), EnumMember]
        OwnerApproval = 1,
        [Name("Certification by Owner"), Description("The assigned owner must certify that all data on the specified item is correct to complete the certification process."), EnumMember]
        OwnerCertification = 2,
        [Name("Assign Issue To Pool"), Description("The owner is assigned as a potential resource to work on the issue.  They must still choose to work the issue."), EnumMember]
        AssignIssueToPool = 3,
        [Name("Assign Issue To Self"), Description("The owner has chosen to work the issue."), EnumMember]
        AssignIssueToSelf = 4,
        [Name("Final Approval"), Description("The owner has approved this item and it needs to be signed off by another user."), EnumMember]
        FinalApproval = 5,
    }

    public class ActivityTypeInfo
    {
        public ActivityType ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public static class ActivityTypeExtensions
    {
        public static string GetActivityTypeDisplayName(this ActivityType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetReportTileTypeDescription(this ActivityType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DescriptionAttribute>().Description;
        }

        public static List<ActivityTypeInfo> GetReportTileTypeEnumList(this ActivityType type)
        {
            var list = new List<ActivityTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                list.Add(new ActivityTypeInfo
                {
                    Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (ActivityType)Enum.Parse(typeof(ActivityType), tm.Name)
                });
            }

            return list;
        }
    }
}
