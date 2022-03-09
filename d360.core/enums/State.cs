using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum State
    {
        [Name("Unknown")]
        Unknown = -1,
        
        [Name("Pending Add")]
        PendingAdd = 0,
        
        [Name("Active")]
        Active = 1,
        
        [Name("Pending Delete")]
        PendingDelete = 2,
        
        [Name("Deleted")]
        Deleted = 3,
        
        [Name("InActive")]
        InActive = 4
    }

    public class StateInfo
    {
        public State ID { get; set; }

        public string Name { get; set; }
    }

    public static class StateExtensions
    {
        public static List<StateInfo> GetList(this State type)
        {
            var list = new List<StateInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var info = new StateInfo
                {
                    ID = (State)Enum.Parse(typeof(State), tm.Name),
                    Name = tm.Name,
                };
                list.Add(info);
            }

            return list;
        }

        public static StateInfo AsInfoModel(this State type)
        {
            var t = type.GetType().GetMember(type.ToString()).First();
            return
                new StateInfo
                {
                    Name = ((NameAttribute)t.GetCustomAttribute(typeof(NameAttribute))).Name,
                    ID = type,
                };
        }
    }
}
