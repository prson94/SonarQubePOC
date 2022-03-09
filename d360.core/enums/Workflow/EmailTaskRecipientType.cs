using System;
using System.Collections.Generic;
using System.Reflection;

namespace d360.core.enums.Workflow
{
    public enum EmailTaskRecipientType
    {
        [Name("None")]
        None = 0,
        
        [Name("Initiator")]
        Initiator,
        
        [Name("Responsibility")]
        Responsibility,
        
        [Name("Specific User")]
        SpecificUser,
        
        [Name("Followers")]
        Followers,
        
        [Name("Group")]
        Group
    }


    public class EmailTaskRecipientTypeInfo
    {
        public EmailTaskRecipientType ID { get; set; }
        
        public string Name { get; set; }
    }

    public static class EmailTaskRecipientTypeExtensions
    {
        public static List<EmailTaskRecipientTypeInfo> GetList(this EmailTaskRecipientType type)
        {
            var list = new List<EmailTaskRecipientTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var info = new EmailTaskRecipientTypeInfo
                {
                    ID = (EmailTaskRecipientType)Enum.Parse(typeof(EmailTaskRecipientType), tm.Name),
                    Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                };
                list.Add(info);
            }

            return list;
        }
    }
}
