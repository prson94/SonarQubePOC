using System;
using System.Collections.Generic;
using System.Reflection;

namespace d360.core.enums
{
    public enum CompanyResourceState
    {
        [Name("Active")]
        Active = 1,
        
        [Name("Inactive")]
        Inactive = 2,
        
        [Name("Deleted")]
        Deleted = 3
    }

    public class CompanyResourceStateInfo
    {
        public CompanyResourceState ID { get; set; }
        
        public string Name { get; set; }
    }

    public static class CompanyResourceStateExtensions
    {
        public static List<CompanyResourceStateInfo> GetList(this CompanyResourceState type)
        {
            var list = new List<CompanyResourceStateInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var info = new CompanyResourceStateInfo
                {
                    ID = (CompanyResourceState)Enum.Parse(typeof(CompanyResourceState), tm.Name),
                    Name = tm.Name,
                };
                list.Add(info);
            }

            return list;
        }
    }
}
