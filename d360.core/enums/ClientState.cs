using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace d360.core.enums
{
    public enum ClientState
    {
        [Name("Proof of Concept")]
        PoC = 0,
        [Name("Active")]
        Active = 1,
        [Name("Inactive")]
        Inactive = 2
    }

    public class ClientStateInfo
    {
        public ClientState ID { get; set;  }
        public string Name { get; set; }
    }

    public static class ClientStateExtensions
    {
        public static List<ClientStateInfo> GetList(this ClientState type)
        {
            var list = new List<ClientStateInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var info = new ClientStateInfo
                {
                    ID = (ClientState)Enum.Parse(typeof(ClientState), tm.Name),
                    Name = tm.Name,
                };
                list.Add(info);
            }

            return list;
        }
    }
}
