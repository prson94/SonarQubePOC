using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace d360.core
{
    public enum QuestionDisplayStyle
    {
        Radio = 1,
        Rating = 2,
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
                var enumValue = (QuestionDisplayStyle)Enum.Parse(typeof(QuestionDisplayStyle), tm.Name);

                var info = new QuestionDisplayStyleInfo
                {
                    Description = DescriptionAsDisplayString(enumValue),
                    ID = (QuestionDisplayStyle)Enum.Parse(typeof(QuestionDisplayStyle), tm.Name),
                    Name = tm.Name
                };
                list.Add(info);
            }

            return list;
        }

        public static string GetDescription(this QuestionDisplayStyle type)
        {
            return DescriptionAsDisplayString(type);
        }

        private static string DescriptionAsDisplayString(QuestionDisplayStyle type)
        {
            switch (type)
            {
                case QuestionDisplayStyle.CheckList: return resources.Enums.QuestionDisplayStyle_Check; 
                case QuestionDisplayStyle.Radio: return resources.Enums.QuestionDisplayStyle_Radio; 
                case QuestionDisplayStyle.Rating: return resources.Enums.QuestionDisplayStyle_Rating; 
                default: throw new ArgumentOutOfRangeException("QuestionDisplayStyle");
            }
        }
    }
}
