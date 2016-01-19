using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace d360.core
{
    public enum DataType
    {
        [Description("True/False")]
        Boolean,
        [Description("Date")]
        Date,
        [Description("Date With Time")]
        DateTime,
        [Description("File")]
        File,
        [Description("Hidden"), ReadOnly(true)]
        Hidden,
        [Description("Html/Richtext")]
        Html,
        [Description("Number")]
        Number,
        [Description("Decimal Number")]
        Decimal,
        [Description("List")]
        Lookup,
        [Description("Simple Text")]
        Text,
        [Description("Password")]
        Password,
        [Description("Link")]
        Link,
        [Description("UNC/File Link")]
        UncLink,
        [Description("Color Picker")]
        Color,
        [Description("Fusion Lookup")]
        FusionLookup
    }

    public class DataTypeInfo
    {
        public DataType ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool ReadOnly { get; set; }
    }

    public static class DataTypeExtensions
    {
        public static List<DataTypeInfo> GetDataTypeInfoList(this DataType type)
        {
            var list = new List<DataTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var aReadOnly = ((ReadOnlyAttribute)tm.GetCustomAttribute(typeof(ReadOnlyAttribute)));
                var info = new DataTypeInfo
                {
                    ReadOnly = (aReadOnly != null) ? aReadOnly.IsReadOnly : false,
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (DataType)Enum.Parse(typeof(DataType), tm.Name),
                    Name = tm.Name
                };
                list.Add(info);
            }

            return list;
        }    
    }
}
