using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum Cardinality
    {
        [Name("One (required)")]
        One = 1,
        
        [Name("Many")]
        Many = 2
    }

    public class CardinalityInfo
    {
        public Cardinality ID { get; set; }
        
        public string Name { get; set; }
    }

    public static class CardinalityExtensions
    {
        public static List<CardinalityInfo> GetList(this Cardinality type)
        {
            var list = new List<CardinalityInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var info = new CardinalityInfo
                {
                    ID = (Cardinality)Enum.Parse(typeof(Cardinality), tm.Name),
                    Name = tm.Name,
                };
                list.Add(info);
            }

            return list;
        }

        public static CardinalityInfo AsInfoModel(this Cardinality type)
        {
            var t = type.GetType().GetMember(type.ToString()).First();
            return
                new CardinalityInfo
                {
                    Name = ((NameAttribute)t.GetCustomAttribute(typeof(NameAttribute))).Name,
                    ID = type,
                };
        }
    }
}
