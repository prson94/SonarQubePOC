using System;
using System.Collections.Generic;
using System.Reflection;

namespace d360.core.enums
{
    public enum Emoji
    {
        [EmojiValue(1), EmojiGroup("vote")]
        ThumbsUp = 1,
        
        [EmojiValue(-1), EmojiGroup("vote")]
        ThumbsDown = 2
    }

    public class EmojiInfo
    {
        public int ID { get; set; }
        
        public string Name { get; set; }
        
        public string Group { get; set; }
        
        public int? Value { get; set; }
    }

    public static class EmojiExtensions
    {
        public static List<EmojiInfo> GetEmojiInfoList(this Emoji type)
        {
            var list = new List<EmojiInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var info = new EmojiInfo
                {
                    ID = (int)Enum.Parse(typeof(Emoji), tm.Name),
                    Name = tm.Name,
                    Group = ((EmojiGroupAttribute)tm.GetCustomAttribute(typeof(EmojiGroupAttribute)))?.Name,
                    Value = ((EmojiValueAttribute)tm.GetCustomAttribute(typeof(EmojiValueAttribute)))?.Value,
                };
                list.Add(info);
            }

            return list;
        }

        public static string GetGroupName(this Emoji type)
        {
            FieldInfo info = type.GetType().GetField(type.ToString());
            return ((EmojiGroupAttribute)info?.GetCustomAttribute(typeof(EmojiGroupAttribute)))?.Name;
        }
    }
}
