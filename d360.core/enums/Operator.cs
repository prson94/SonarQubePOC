using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum Operator
    {
        [Name("is"), Description(""), OperatorValueCountRange(1, 1), OperatorAllowedDataTypes(DataType.Boolean, DataType.Date, DataType.Decimal, DataType.Lookup, DataType.Number), OperatorFieldTypeRequirements(false)]
        Equals = 1,
        [Name("is not"), Description(""), OperatorValueCountRange(1, 1), OperatorAllowedDataTypes(DataType.Date, DataType.Decimal, DataType.Lookup, DataType.Number), OperatorFieldTypeRequirements(false)]
        NotEquals,
        [Name("contains"), Description(""), OperatorValueCountRange(1, 1), OperatorAllowedDataTypes(DataType.Text), OperatorFieldTypeRequirements(false)]
        Contains,
        [Name("does not contain"), Description(""), OperatorValueCountRange(1, 1), OperatorAllowedDataTypes(DataType.Text), OperatorFieldTypeRequirements(false)]
        NotContains,
        [Name("starts with"), Description(""), OperatorValueCountRange(1, 1), OperatorAllowedDataTypes(DataType.Text), OperatorFieldTypeRequirements(false)]
        StartsWith,
        [Name("ends with"), Description(""), OperatorValueCountRange(1, 1), OperatorAllowedDataTypes(DataType.Text), OperatorFieldTypeRequirements(false)]
        EndsWith,
        [Name("is before"), Description(""), OperatorValueCountRange(1, 1), OperatorAllowedDataTypes(DataType.Date), OperatorFieldTypeRequirements(false)]
        Before,
        [Name("is after"), Description(""), OperatorValueCountRange(1, 1), OperatorAllowedDataTypes(DataType.Date), OperatorFieldTypeRequirements(false)]
        After,
        [Name("is between"), Description(""), OperatorValueCountRange(2, 2), OperatorAllowedDataTypes(DataType.Date, DataType.Decimal, DataType.Number), OperatorFieldTypeRequirements(false)]
        Between,
        [Name("is populated"), Description(""), OperatorValueCountRange(0, 0), OperatorAllowedDataTypes(DataType.Boolean, DataType.Date, DataType.DateTime, DataType.Decimal, DataType.Html, DataType.JSON, DataType.JsonElement, DataType.Lookup, DataType.Number, DataType.Text), OperatorFieldTypeRequirements(false)]
        Populated,
        [Name("is not populated"), Description(""), OperatorValueCountRange(0, 0), OperatorAllowedDataTypes(DataType.Boolean, DataType.Date, DataType.DateTime, DataType.Decimal, DataType.Html, DataType.JSON, DataType.JsonElement, DataType.Lookup, DataType.Number, DataType.Text), OperatorFieldTypeRequirements(false)]
        NotPopulated,
        [Name("is greater than"), Description(""), OperatorValueCountRange(1, 1), OperatorAllowedDataTypes(DataType.Decimal, DataType.Number), OperatorFieldTypeRequirements(false)]
        GreaterThan,
        [Name("is less than or equal to"), Description(""), OperatorValueCountRange(1, 1), OperatorAllowedDataTypes(DataType.Decimal, DataType.Number), OperatorFieldTypeRequirements(false)]
        LessThanOrEquals,
        [Name("is less than"), Description(""), OperatorValueCountRange(1, 1), OperatorAllowedDataTypes(DataType.Decimal, DataType.Number), OperatorFieldTypeRequirements(false)]
        LessThan,
        [Name("is greater than or equal to"), Description(""), OperatorValueCountRange(1, 1), OperatorAllowedDataTypes(DataType.Decimal, DataType.Number), OperatorFieldTypeRequirements(false)]
        GreaterThanOrEquals,
        [Name("in"), Description(""), OperatorValueCountRange(1, 1000), OperatorAllowedDataTypes(DataType.Lookup), OperatorFieldTypeRequirements(true)]
        In,
        [Name("not in"), Description(""), OperatorValueCountRange(1, 1000), OperatorAllowedDataTypes(DataType.Lookup), OperatorFieldTypeRequirements(true)]
        NotIn
    }
    public class OperatorInfo
    {
        public Operator ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MinimumValueCount { get; set; }
        public int MaximumValueCount { get; set; }
        public List<DataType> AllowedDataTypes { get; set; }
        public bool FieldRequiresMultipleValueSupport { get; set; }

    }
    public static class OperatorClassExtensions
    {
        public static string GetDisplayName(this Operator type)
        {
            try
            {
                return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
            }
            catch
            {
                return type.ToString();
            }
        }

        public static List<DataType> GetAllowedDataTypesAcrossOperators(this Operator type)
        {
            var list = new List<DataType>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                if (tm.GetCustomAttribute(typeof(ObsoleteAttribute)) == null)
                {
                    var types = ((OperatorAllowedDataTypesAttribute)tm.GetCustomAttribute(typeof(OperatorAllowedDataTypesAttribute))).DataTypes;
                    var unique = types.Except(list);
                    list.AddRange(unique);
                }
            }

            return list;
        }

        public static List<OperatorInfo> GetAsList(this Operator type)
        {
            var list = new List<OperatorInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                if (tm.GetCustomAttribute(typeof(ObsoleteAttribute)) == null)
                {
                    var enumValue = (Operator)Enum.Parse(typeof(Operator), tm.Name);

                    list.Add(new OperatorInfo
                    {
                        Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                        Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                        ID = enumValue,
                        MinimumValueCount = ((OperatorValueCountRangeAttribute)tm.GetCustomAttribute(typeof(OperatorValueCountRangeAttribute))).Min,
                        MaximumValueCount = ((OperatorValueCountRangeAttribute)tm.GetCustomAttribute(typeof(OperatorValueCountRangeAttribute))).Max,
                        AllowedDataTypes = ((OperatorAllowedDataTypesAttribute)tm.GetCustomAttribute(typeof(OperatorAllowedDataTypesAttribute))).DataTypes.ToList(),
                        FieldRequiresMultipleValueSupport = ((OperatorFieldTypeRequirementsAttribute)tm.GetCustomAttribute(typeof(OperatorFieldTypeRequirementsAttribute))).FieldRequiresMultipleValueSupport
                    });
                }
            }

            return list.OrderBy(i => i.Name).ToList();
        }

    }
}