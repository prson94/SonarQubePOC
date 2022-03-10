using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum ContractType
    {
        [Name("Organization Terms Of Use"), Description("A terms of use contract for an organization to sign when first logging into the environment, signed by an organizational representative.")]
        OrganizationTermsOfUse = 1,
        
        [Name("User Terms Of Use"), Description("A terms of use contract for users to sign when first logging into the environment.")]
        ResourceTermsOfUse = 2
    }

    public class ContractTypeInfo
    {
        public ContractType ID { get; set; }
        
        public string Name { get; set; }
        
        public string Description { get; set; }
    }

    public static class ContractTypeExtensions
    {
        public static string GetDisplayName(this ContractType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetDescription(this ContractType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DescriptionAttribute>().Description;
        }

        public static List<ContractTypeInfo> GetEnumList(this ContractType type)
        {
            var list = new List<ContractTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                list.Add(new ContractTypeInfo
                {
                    Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (ContractType)Enum.Parse(typeof(ContractType), tm.Name)
                });
            }

            return list.OrderBy(i => i.Name).ToList();
        }
    }
}
