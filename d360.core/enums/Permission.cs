using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace d360.core.enums
{
    public enum Permission
    {
        [Name("Read asset"), Description("Read an asset and its properties."), Category("R")]
        ReadAsset = 1,
        [Name("Modify asset"), Description("Add or update an asset's properties."), Category("M")]
        ModifyAsset = 2,
        [Name("Remove asset"), Description("Remove an asset."), Category("D")]
        DeleteAsset = 4,

        [Name("Read attributes"), Description("Read an asset's complex attributes."), Category("R")]
        ReadAttributes = 8,
        [Name("Modify attributes"), Description("Add or modify an asset's complex attributes."), Category("M")]
        ModifyAttributes = 16,
        [Name("Remove attributes"), Description("Remove an asset's complex attributes."), Category("D")]
        DeleteAttributes = 32,

        [Name("Read responsibilties"), Description("Read an asset's roles and responsibilities."), Category("R")]
        ReadResponsibilities = 64,
        [Name("Modify responsibilties"), Description("Add or modify an asset's roles and responsibilities."), Category("M")]
        ModifyResponsibilities = 128,
        [Name("Remove responsibilties"), Description("Remove an asset's roles and responsibilities."), Category("D")]
        DeleteResponsibilities = 256,

        [Name("Read relationships"), Description("Read an asset's relationships."), Category("R")]
        ReadRelationships = 512,
        [Name("Modify relationships"), Description("Add or modify an asset's relationships."), Category("M")]
        ModifyRelationships = 1024,
        [Name("Remove relationships"), Description("Remove an asset's relationships."), Category("D")]
        DeleteRelationships = 2048
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
    }
}
