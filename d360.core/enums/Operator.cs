using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace d360.core.enums
{
    [JsonConverter(typeof(StringEnumConverter), true)]
    public enum Operator
    {
        [
            Name("is"), 
            EnumMember(Value = "Equals"),
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field, MetricGovernanceCheckType.Relation),
            OperatorAllowedDataTypes(DataType.Boolean, DataType.Date, DataType.Decimal, DataType.Lookup, DataType.Number, DataType.Text), 
            OperatorFieldTypeRequirements(false)
        ]
        Equals = 1,
        [
            Name("is not"),
            EnumMember(Value = "NotEquals"),
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field, MetricGovernanceCheckType.Relation),
            OperatorAllowedDataTypes(DataType.Date, DataType.Decimal, DataType.Lookup, DataType.Number, DataType.Text), 
            OperatorFieldTypeRequirements(false)
        ]
        NotEquals,
        [
            NotYetUsed,
            Name("contains"),
            EnumMember(Value = "Contains"),
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Text), 
            OperatorFieldTypeRequirements(false)
        ]
        Contains,
        [
            NotYetUsed,
            Name("does not contain"),
            EnumMember(Value = "NotContains"),
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Text), 
            OperatorFieldTypeRequirements(false)
        ]
        NotContains,
        [
            NotYetUsed,
            Name("starts with"),
            EnumMember(Value = "StartsWith"),
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Text), 
            OperatorFieldTypeRequirements(false)
        ]
        StartsWith,
        [
            NotYetUsed,
            Name("ends with"),
            EnumMember(Value = "EndsWith"),
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Text), 
            OperatorFieldTypeRequirements(false)
        ]
        EndsWith,
        [
            Name("is before"),
            EnumMember(Value = "Before"),
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Date, DataType.DateTime), 
            OperatorFieldTypeRequirements(false)
        ]
        Before,
        [
            Name("is after"),
            EnumMember(Value = "After"),
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Date, DataType.DateTime), 
            OperatorFieldTypeRequirements(false)
        ]
        After,
        [
            NotYetUsed, 
            Name("is between"),
            EnumMember(Value = "Between"),
            Description(""), 
            OperatorValueCountRange(2, 2),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Date, DataType.Decimal, DataType.Number), 
            OperatorFieldTypeRequirements(false)
        ]
        Between,
        [
            NotYetUsed,
            Name("is populated"),
            EnumMember(Value = "Populated"),
            Description(""), 
            OperatorValueCountRange(0, 0),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field, MetricGovernanceCheckType.Owner, MetricGovernanceCheckType.Predicate, MetricGovernanceCheckType.Relation),
            OperatorAllowedDataTypes(DataType.Boolean, DataType.Date, DataType.DateTime, DataType.Decimal, DataType.Html, DataType.JSON, DataType.JsonElement, DataType.Lookup, DataType.Number, DataType.Text), 
            OperatorFieldTypeRequirements(false)
        ]
        Populated,
        [
            NotYetUsed,
            Name("is not populated"),
            EnumMember(Value = "NotPopulated"),
            Description(""), 
            OperatorValueCountRange(0, 0),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field, MetricGovernanceCheckType.Owner, MetricGovernanceCheckType.Predicate, MetricGovernanceCheckType.Relation),
            OperatorAllowedDataTypes(DataType.Boolean, DataType.Date, DataType.DateTime, DataType.Decimal, DataType.Html, DataType.JSON, DataType.JsonElement, DataType.Lookup, DataType.Number, DataType.Text), 
            OperatorFieldTypeRequirements(false)
        ]
        NotPopulated,
        [
            Name("is greater than"),
            EnumMember(Value = "GreaterThan"),
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Decimal, DataType.Number), 
            OperatorFieldTypeRequirements(false)
            ]
        GreaterThan,
        [
            Name("is less than or equal to"),
            EnumMember(Value = "LessThanOrEquals"),
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Decimal, DataType.Number), 
            OperatorFieldTypeRequirements(false)
        ]
        LessThanOrEquals,
        [
            Name("is less than"),
            EnumMember(Value = "LessThan"),
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Decimal, DataType.Number), 
            OperatorFieldTypeRequirements(false)
        ]
        LessThan,
        [
            Name("is greater than or equal to"),
            EnumMember(Value = "GreaterThanOrEquals"),
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Decimal, DataType.Number), 
            OperatorFieldTypeRequirements(false)
        ]
        GreaterThanOrEquals,
        [
            NotYetUsed, 
            Name("in"),
            EnumMember(Value = "In"),
            Description(""), 
            OperatorValueCountRange(1, 1000),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field, MetricGovernanceCheckType.Predicate, MetricGovernanceCheckType.Relation),
            OperatorAllowedDataTypes(DataType.Lookup), 
            OperatorFieldTypeRequirements(true)
            ]
        In,
        [
            NotYetUsed, 
            Name("not in"),
            EnumMember(Value = "NotIn"),
            Description(""), 
            OperatorValueCountRange(1, 1000),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field, MetricGovernanceCheckType.Predicate, MetricGovernanceCheckType.Relation),
            OperatorAllowedDataTypes(DataType.Lookup), 
            OperatorFieldTypeRequirements(true)
        ]
        NotIn,
        [
            NotYetUsed,
            Name("is true"),
            EnumMember(Value = "IsTrue"),
            Description(""),
            OperatorValueCountRange(0, 0),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Boolean),
            OperatorFieldTypeRequirements(true)
        ]
        IsTrue,
        [
            NotYetUsed,
            Name("is false"),
            EnumMember(Value = "IsFalse"),
            Description(""),
            OperatorValueCountRange(0, 0),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Boolean),
            OperatorFieldTypeRequirements(true)
        ]
        IsFalse,
        [
            Name("is on or before"),
            EnumMember(Value = "OnOrBefore"),
            Description(""),
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Date, DataType.DateTime),
            OperatorFieldTypeRequirements(false)
        ]
        OnOrBefore,
        [
            Name("is on or after"),
            EnumMember(Value = "OnOrAfter"),
            Description(""),
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Date, DataType.DateTime),
            OperatorFieldTypeRequirements(false)
        ]
        OnOrAfter
    }
    public class OperatorInfo
    {
        public Operator ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MinimumValueCount { get; set; }
        public int MaximumValueCount { get; set; }
        public List<OperatorDataTypeInfo> AllowedDataTypes { get; set; }
        public List<OperatorMetricGovernanceCheckTypeInfo> AllowedMeasureChecks { get; set; }
        public bool FieldRequiresMultipleValueSupport { get; set; }
    }
    public class OperatorDataTypeInfo
    {
        public DataType ID { get; set; }
        public string Name { get; set; }
    }
    public class OperatorMetricGovernanceCheckTypeInfo
    {
        public MetricGovernanceCheckType ID { get; set; }
        public string Name { get; set; }
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

        public static OperatorInfo GetAsInfo(this Operator type)
        {
            return type.GetAsList().SingleOrDefault(i => i.ID == type);
        }

        public static List<OperatorInfo> GetAsList(this Operator type)
        {
            var list = new List<OperatorInfo>();

            var checkInfos = MetricGovernanceCheckType.External.GetAsList();
            var dataTypeInfos = DataType.Boolean.GetDataTypeInfoList();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                if (tm.GetCustomAttribute(typeof(ObsoleteAttribute)) == null && tm.GetCustomAttribute(typeof(NotYetUsedAttribute)) == null)
                {
                    var enumValue = (Operator)Enum.Parse(typeof(Operator), tm.Name);

                    var dataTypes = (
                                    from dt in ((OperatorAllowedDataTypesAttribute)tm.GetCustomAttribute(typeof(OperatorAllowedDataTypesAttribute))).DataTypes
                                    join dti in dataTypeInfos on dt equals dti.ID
                                    select new OperatorDataTypeInfo { ID = dti.ID, Name = dti.ID.ToString() }
                                    ).ToList();

                    var checkTypes = (
                                     from dt in ((OperatorAllowedMeasureChecksAttribute)tm.GetCustomAttribute(typeof(OperatorAllowedMeasureChecksAttribute))).Checks
                                     join dti in checkInfos on dt equals dti.ID
                                     select new OperatorMetricGovernanceCheckTypeInfo { ID = dti.ID, Name = dti.ID.ToString() }
                                     ).ToList();

                    list.Add(new OperatorInfo
                    {
                        Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                        Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                        ID = enumValue,
                        MinimumValueCount = ((OperatorValueCountRangeAttribute)tm.GetCustomAttribute(typeof(OperatorValueCountRangeAttribute))).Min,
                        MaximumValueCount = ((OperatorValueCountRangeAttribute)tm.GetCustomAttribute(typeof(OperatorValueCountRangeAttribute))).Max,
                        AllowedDataTypes = dataTypes,
                        AllowedMeasureChecks = checkTypes,
                        FieldRequiresMultipleValueSupport = ((OperatorFieldTypeRequirementsAttribute)tm.GetCustomAttribute(typeof(OperatorFieldTypeRequirementsAttribute))).FieldRequiresMultipleValueSupport
                    });
                }
            }

            return list.OrderBy(i => i.Name).ToList();
        }

        /// <summary>
        /// Use this operator to compare a value to a set of values, applying its proper data type to the comparison.
        /// </summary>
        /// <param name="operator">The operator to use for comparison.</param>
        /// <param name="dataType">The data type to convert to (string values are from the DataType enumeration)</param>
        /// <param name="allowMultipleValues">Does field type allow multiple values.</param>
        /// <param name="values">The value set to check against, for comparison.</param>
        /// <param name="valueToCompare">The value that we are checking for match, based on operator and data type. An example would be the Value from the field we are checking.</param>
        /// <returns></returns>
        public static bool TestTwoValues(this Operator @operator, string dataType, bool allowMultipleValues, List<string> values, string valueToCompare)
        {
            bool result = false;

            switch (@operator)
            {
                case Operator.After:
                case Operator.Before:
                case Operator.OnOrAfter:
                case Operator.OnOrBefore:
                    if (dataType == DataType.Date.ToString() || dataType == DataType.DateTime.ToString())
                    {
                        if (DateTime.TryParse(values[0], out _) && DateTime.TryParse(valueToCompare, out _))
                        {
                            var conditionValue = DateTime.Parse(values[0]);
                            var fieldValue = DateTime.Parse(valueToCompare);
                            switch (@operator)
                            {
                                case Operator.After:
                                    result = (fieldValue > conditionValue);
                                    break;
                                case Operator.Before:
                                    result = (fieldValue < conditionValue);
                                    break;
                                case Operator.OnOrAfter:
                                    result = (fieldValue >= conditionValue);
                                    break;
                                case Operator.OnOrBefore:
                                    result = (fieldValue <= conditionValue);
                                    break;
                            }
                        }
                    }
                    break;
                case Operator.Between:
                    switch (dataType)
                    {
                        case "Date":
                        case "DateTime":
                            if (DateTime.TryParse(values[0], out _) && DateTime.TryParse(values[1], out _) && DateTime.TryParse(valueToCompare, out _))
                            {
                                var beforeValue = DateTime.Parse(values[0]);
                                var afterValue = DateTime.Parse(values[1]);
                                var fieldValue = DateTime.Parse(valueToCompare);
                                result = (fieldValue >= beforeValue && fieldValue <= afterValue);
                            }
                            break;
                        case "Decimal":
                            if (decimal.TryParse(values[0], out _) && decimal.TryParse(values[1], out _) && decimal.TryParse(valueToCompare, out _))
                            {
                                var beforeValue = decimal.Parse(values[0]);
                                var afterValue = decimal.Parse(values[1]);
                                var fieldValue = decimal.Parse(valueToCompare);
                                result = (fieldValue >= beforeValue && fieldValue <= afterValue);
                            }
                            break;
                        case "Number":
                            if (long.TryParse(values[0], out _) && long.TryParse(values[1], out _) && long.TryParse(valueToCompare, out _))
                            {
                                var beforeValue = long.Parse(values[0]);
                                var afterValue = long.Parse(values[1]);
                                var fieldValue = long.Parse(valueToCompare);
                                result = (fieldValue >= beforeValue && fieldValue <= afterValue);
                            }
                            break;
                    }
                    break;
                case Operator.Contains:
                    result = (valueToCompare ?? "").Contains(values[0]);
                    break;
                case Operator.EndsWith:
                    result = (valueToCompare ?? "").EndsWith(values[0]);
                    break;
                case Operator.Equals:
                    result = (valueToCompare.ToLower() == values[0].ToLower());
                    break;
                case Operator.GreaterThan:
                case Operator.GreaterThanOrEquals:
                case Operator.LessThan:
                case Operator.LessThanOrEquals:
                    switch (dataType)
                    {
                        case "Decimal":
                            if (decimal.TryParse(values[0], out _) && decimal.TryParse(valueToCompare, out _))
                            {
                                var conditionValue = decimal.Parse(values[0]);
                                var fieldValue = decimal.Parse(valueToCompare);
                                switch (@operator)
                                {
                                    case Operator.GreaterThan:
                                        result = (fieldValue > conditionValue);
                                        break;
                                    case Operator.GreaterThanOrEquals:
                                        result = (fieldValue >= conditionValue);
                                        break;
                                    case Operator.LessThan:
                                        result = (fieldValue < conditionValue);
                                        break;
                                    case Operator.LessThanOrEquals:
                                        result = (fieldValue <= conditionValue);
                                        break;
                                }
                                
                            }
                            break;
                        case "Number":
                            if (long.TryParse(values[0], out _) && long.TryParse(valueToCompare, out _))
                            {
                                var conditionValue = long.Parse(values[0]);
                                var fieldValue = long.Parse(valueToCompare);
                                switch (@operator)
                                {
                                    case Operator.GreaterThan:
                                        result = (fieldValue > conditionValue);
                                        break;
                                    case Operator.GreaterThanOrEquals:
                                        result = (fieldValue >= conditionValue);
                                        break;
                                    case Operator.LessThan:
                                        result = (fieldValue < conditionValue);
                                        break;
                                    case Operator.LessThanOrEquals:
                                        result = (fieldValue <= conditionValue);
                                        break;
                                }
                            }
                            break;
                    }
                    break;
                case Operator.In:
                    var fieldValuesIn = (valueToCompare ?? "").Split(',');
                    result = fieldValuesIn.Intersect(values).Any();
                    break;
                case Operator.IsFalse:
                    if (!string.IsNullOrEmpty(valueToCompare) && dataType == DataType.Boolean.ToString())
                    {
                        bool bValue;
                        if (bool.TryParse(valueToCompare, out bValue))
                        {
                            result = !bValue;
                        }
                    }
                    break;
                case Operator.IsTrue:
                    if (!string.IsNullOrEmpty(valueToCompare) && dataType == DataType.Boolean.ToString())
                    {
                        bool bValue;
                        if (bool.TryParse(valueToCompare, out bValue))
                        {
                            result = bValue;
                        }
                    }
                    break;
                case Operator.NotContains:
                    result = !(valueToCompare ?? "").Contains(values[0]);
                    break;
                case Operator.NotEquals:
                    result = !(valueToCompare ?? "").Equals(values[0]);
                    break;
                case Operator.NotIn:
                    var fieldValuesNotIn = (valueToCompare ?? "").Split(',');
                    result = !fieldValuesNotIn.Intersect(values).Any();
                    break;
                case Operator.NotPopulated:
                    result = (valueToCompare == null);
                    break;
                case Operator.Populated:
                    result = (valueToCompare != null);
                    break;
                case Operator.StartsWith:
                    result = (valueToCompare ?? "").StartsWith(values[0]);
                    break;
            }

            return result;
        }
    }
}