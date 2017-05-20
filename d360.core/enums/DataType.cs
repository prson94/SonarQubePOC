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
        [Description("File"), ReadOnly(true)]
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
        [Description("Password"), ReadOnly(true)]
        Password,
        [Description("Link")]
        Link,
        [Description("UNC/File Link"), ReadOnly(true)]
        UncLink,
        [Description("Color Picker"), ReadOnly(true)]
        Color,
        [Description("Fusion Lookup")]
        FusionLookup,
        [Description("Attribute Hierarchy"), ReadOnly(true)]
        Attribute,
        [Description("Filtered Lookup")]//, ReadOnly(true)]
        FilteredLookup,
        [Description("Relation Lookup")]//, ReadOnly(true)]
        ComplexRelationLookup,
        [Description("Percentage"), ReadOnly(true)]
        Percentage, // used for range of > 0 and < 1
        [Description("DataTableSelect"), ReadOnly(true)]
        DataTableSelect,
        [Description("Ownership Lookup")]//, ReadOnly(true)]
        OwnershipLookup
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
