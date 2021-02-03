using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace d360.core.enums
{
    [Flags]
    public enum Permission
    {
        [Name("Read asset"), Description("Read an asset and its properties."), Category("R")]
        ReadAsset = 1,
        [Name("Add asset"), Description("Add an asset."), Category("M")]
        AddAsset = 2,
        [Name("Remove asset"), Description("Remove an asset."), Category("D")]
        DeleteAsset = 4,
        [Name("Edit asset"), Description("Update an asset's properties."), Category("M")]
        EditAsset = 8,

        [Name("Modify asset"), Description("Add or update an asset's properties."), Category("M")]
        ModifyAsset = AddAsset | EditAsset,

        [Name("Read responsibilties"), Description("Read an asset's roles and responsibilities."), Category("R")]
        ReadResponsibilities = 32,
        [Name("Add responsibilties"), Description("Add an asset's roles and responsibilities."), Category("M")]
        AddResponsibilities = 64,
        [Name("Remove responsibilties"), Description("Remove an asset's roles and responsibilities."), Category("D")]
        DeleteResponsibilities = 128,
        [Name("Edit responsibilties"), Description("Modify an asset's roles and responsibilities."), Category("M")]
        EditResponsibilities = 256,

        [Name("Modify responsibilties"), Description("Add or modify an asset's roles and responsibilities."), Category("M")]
        ModifyResponsibilities = AddResponsibilities | EditResponsibilities,

        [Name("Read relationships"), Description("Read an asset's relationships."), Category("R")]
        ReadRelationships = 1024,
        [Name("Add relationships"), Description("Add an asset's relationships."), Category("M")]
        AddRelationships = 2048,
        [Name("Remove relationships"), Description("Remove an asset's relationships."), Category("D")]
        DeleteRelationships = 4096,
        [Name("Edit relationships"), Description("Modify an asset's relationships."), Category("M")]
        EditRelationships = 8192,

        [Name("Modify relationships"), Description("Add or modify an asset's relationships."), Category("M")]
        ModifyRelationships = AddRelationships | EditRelationships,
    }

    public class PermissionInfo
    {
        public int Value { get; set; }
        public Permission ID { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public bool Selected { get; set; } = false;
    }

    public static class PermissionExtensions
    {
        public static List<PermissionInfo> GetList(this Permission type)
        {
            var list = new List<PermissionInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var info = new PermissionInfo
                {
                    Value = (int)(Permission)Enum.Parse(typeof(Permission), tm.Name),
                    ID = (Permission)Enum.Parse(typeof(Permission), tm.Name),
                    Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                    Category = ((CategoryAttribute)tm.GetCustomAttribute(typeof(CategoryAttribute))).Category,
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                };
                list.Add(info);
            }

            return list;
        }

        public static PermissionInfo GetPermissionInfo(this Permission type)
        {
            MemberInfo[] tm = typeof(Permission).GetMember(type.ToString(), (BindingFlags.Public | BindingFlags.Static));
            
            return new PermissionInfo
            {
                Value = (int)(Permission)Enum.Parse(typeof(Permission), tm[0].Name),
                ID = (Permission)Enum.Parse(typeof(Permission), tm[0].Name),
                Name = ((NameAttribute)tm[0].GetCustomAttribute(typeof(NameAttribute))).Name,
                Category = ((CategoryAttribute)tm[0].GetCustomAttribute(typeof(CategoryAttribute))).Category,
                Description = ((DescriptionAttribute)tm[0].GetCustomAttribute(typeof(DescriptionAttribute))).Description,
            };
        }
    }
}
