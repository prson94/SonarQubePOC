using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using d360.core.entities;

namespace d360.core
{

    public enum DataType
    {
        [Description("None")]
        None = 0, // Not used as a type ;used for logical computing
        [Description("True/False")]
        Boolean = 1 << 0,
        [Description("Date")]
        Date = 1 << 1,
        [Description("Date With Time")]
        DateTime = 1 << 2,
        [Description("File"), ReadOnly(true)]
        File = 1 << 3,
        [Description("Hidden"), ReadOnly(true)]
        Hidden = 1 << 4,
        [Description("Html/Richtext")]
        Html = 1 << 5,
        [Description("Number")]
        Number = 1 << 6,
        [Description("Decimal Number")]
        Decimal = 1 << 7,
        [Description("List")]
        Lookup = 1 << 8,
        [Description("Simple Text")]
        Text = 1 << 9,
        [Description("Password"), ReadOnly(true)]
        Password = 1 << 10,
        [Description("Link")]
        Link = 1 << 11,
        [Description("UNC/File Link"), ReadOnly(true)]
        UncLink = 1 << 12,
        [Description("Color Picker"), ReadOnly(true)]
        Color= 1 << 13,
        [Description("Asset Path")]
        Path = 1 << 15,
        [Description("Relation Lookup")]
        ComplexRelationLookup = 1 << 17,
        [Description("Percentage"), ReadOnly(true)]
        Percentage = 1 << 18, // used for range of > 0 and < 1
        [Description("DataTableSelect"), ReadOnly(true)]
        DataTableSelect = 1 << 19,
        [Description("Ownership Lookup")]
        OwnershipLookup = 1 << 20,
        [Description("Relationship")]
        Relationship = 1 << 21,
        [Description("Field from Relationship")]
        FieldFromRelationship = 1 << 22,
        [Description("Reference Item List from Relationship")]
        RefListRelationship = 1 << 23,
        [Description("JSON")]
        JSON = 1 << 24,
        [Description("JSON Attribute")]
        JsonElement = 1 << 25,
        [Description("Tag")]
        Tag = 1 << 26,
        [Description("Score")]
        Score = 1 << 27,
        [Description("Counter")]
        Counter = 1 << 28
    }

    public class DataTypeInfo
    {
        public DataType ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool ReadOnly { get; set; }
    }

    public class AllowedConversionOption
    {
        public string FromType { get; set; }
        public string ToType { get; set; }
    }

    public static class DataTypeExtensions
    {
        public static List<DataTypeInfo> GetDataTypeInfoList(this DataType type)
        {
            var list = new List<DataTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var aReadOnly = ((ReadOnlyAttribute)tm.GetCustomAttribute(typeof(ReadOnlyAttribute)));
                if ((DataType)Enum.Parse(typeof(DataType), tm.Name) == DataType.None)
                {
                    continue;
                }
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

        public static List<DataTypeInfo> GetDataTypeInfoList(this DataType type, SystemObjects sysObj)
        {
            var list = new List<DataTypeInfo>();

            var excludes = sysObj.ExcludeDataType();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                var aReadOnly = ((ReadOnlyAttribute)tm.GetCustomAttribute(typeof(ReadOnlyAttribute)));
                if (((excludes & (DataType)Enum.Parse(typeof(DataType), tm.Name)) != DataType.None) || ((DataType)Enum.Parse(typeof(DataType), tm.Name) == DataType.None))
                    continue;

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

        public static List<AllowedConversionOption> GetAllowedConversionOptions(this DataType type)
        {
            return new List<AllowedConversionOption>() {
                new AllowedConversionOption { FromType = "Boolean", ToType = "Text" },
                new AllowedConversionOption { FromType = "Date", ToType = "DateWithTime" },
                new AllowedConversionOption { FromType = "Decimal", ToType = "Percentage" },
                new AllowedConversionOption { FromType = "Number", ToType = "Decimal" },
                new AllowedConversionOption { FromType = "Number", ToType = "Percentage" },
                new AllowedConversionOption { FromType = "Text", ToType = "Html" },
                new AllowedConversionOption { FromType = "ComplexRelationLookup", ToType = "Relationship" }
            };
        }
    }
}
