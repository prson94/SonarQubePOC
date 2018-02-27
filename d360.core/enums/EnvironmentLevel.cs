using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace d360.core.enums
{
    public enum EnvironmentLevel
    {
        [Name("Nightly")]
        Nightly = 0,
        [Name("Development")]
        Development = 1,
        [Name("UAT")]
        UAT = 2,
        [Name("Production")]
        Production = 3,
        [Name("LEGACYDEV")]
        LegacyDevelopment = 4,
        [Name("ALTERNATE")]
        Alternate = 5
    }

    public class EnvironmentLevelInfo
    {
        public EnvironmentLevel ID { get; set;  }
        public string Name { get; set; }
    }

    public static class EnvironmentLevelExtensions
    {
        public static List<EnvironmentLevelInfo> GetList(this EnvironmentLevel type)
        {
            var list = new List<EnvironmentLevelInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var info = new EnvironmentLevelInfo
                {
                    ID = (EnvironmentLevel)Enum.Parse(typeof(EnvironmentLevel), tm.Name),
                    Name = tm.Name,
                };
                list.Add(info);
            }

            return list;
        }
    }
}
