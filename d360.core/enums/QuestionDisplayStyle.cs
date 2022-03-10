using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace d360.core
{
    public enum QuestionDisplayStyle
    {
        [Description("Radio List")]
        Radio = 1,
        
        [Description("Rating")]
        Rating = 2,
        
        [Description("Check List")]
        CheckList = 3
    }

    public class QuestionDisplayStyleInfo
    {
        public QuestionDisplayStyle ID { get; set; }
        
        public string Name { get; set; }
        
        public string Description { get; set; }
    }

    public static class ResponseTypeDisplayStyleExtensions
    {
        public static List<QuestionDisplayStyleInfo> GetResponseTypeDisplayStyleInfoList(this QuestionDisplayStyle type)
        {
            var list = new List<QuestionDisplayStyleInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var info = new QuestionDisplayStyleInfo
                {
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (QuestionDisplayStyle)Enum.Parse(typeof(QuestionDisplayStyle), tm.Name),
                    Name = tm.Name
                };
                list.Add(info);
            }

            return list;
        }

        public static string GetDescription(this QuestionDisplayStyle type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DescriptionAttribute>().Description;
        }
    }
}
