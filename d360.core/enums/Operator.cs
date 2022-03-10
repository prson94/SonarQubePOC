using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

using d360.core.types;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// version on C# in not appropriate
// ReSharper disable ConvertToNullCoalescingCompoundAssignment

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
            OperatorAllowedDataTypes(DataType.Date, DataType.Decimal, DataType.Lookup, DataType.Number, DataType.Text),
            OperatorAllowedDataTypesAdvancedFilter(DataType.Date, DataType.Decimal, DataType.Lookup, DataType.Number, DataType.Text, DataType.Path, DataType.Relationship,
                DataType.Counter),
            OperatorFieldTypeRequirements(false),
            SortOrder(300)
        ]
        Equals = 1,

        [
            Name("is not"),
            EnumMember(Value = "NotEquals"),
            Description(""),
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field, MetricGovernanceCheckType.Relation),
            OperatorAllowedDataTypes(DataType.Date, DataType.Decimal, DataType.Lookup, DataType.Number, DataType.Text),
            OperatorAllowedDataTypesAdvancedFilter(DataType.Date, DataType.Decimal, DataType.Lookup, DataType.Number, DataType.Text, DataType.Path, DataType.Relationship,
                DataType.Counter),
            OperatorFieldTypeRequirements(false),
            SortOrder(400)
        ]
        NotEquals,

        [
            //uncomment before the release
            //NotYetUsed,
            Name("contains"),
            EnumMember(Value = "Contains"),
            Description(""),
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Text),
            OperatorAllowedDataTypesAdvancedFilter(DataType.Text, DataType.Link, DataType.Html, DataType.Tag, DataType.Path, DataType.FieldFromRelationship),
            OperatorFieldTypeRequirements(false),
            SortOrder(100)
        ]
        Contains,

        [
            //uncomment before the release
            //NotYetUsed,
            Name("does not contain"),
            EnumMember(Value = "NotContains"),
            Description(""),
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Text),
            OperatorAllowedDataTypesAdvancedFilter(DataType.Text, DataType.Link, DataType.Html, DataType.Tag, DataType.Path, DataType.FieldFromRelationship),
            OperatorFieldTypeRequirements(false),
            SortOrder(200)
        ]
        NotContains,

        [
            //uncomment before the release
            //NotYetUsed,
            Name("starts with"),
            EnumMember(Value = "StartsWith"),
            Description(""),
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Text),
            OperatorAllowedDataTypesAdvancedFilter(DataType.Text, DataType.Path),
            OperatorFieldTypeRequirements(false),
            SortOrder(500)
        ]
        StartsWith,

        [
            //uncomment before the release
            //NotYetUsed,
            Name("ends with"),
            EnumMember(Value = "EndsWith"),
            Description(""),
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Text),
            OperatorAllowedDataTypesAdvancedFilter(DataType.Text, DataType.Path),
            OperatorFieldTypeRequirements(false),
            SortOrder(600)
        ]
        EndsWith,

        [
            Name("is before"),
            EnumMember(Value = "Before"),
            Description(""),
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Date),
            OperatorAllowedDataTypesAdvancedFilter(DataType.Date, DataType.DateTime),
            OperatorFieldTypeRequirements(false),
            SortOrder(500)
        ]
        Before,

        [
            Name("is after"),
            EnumMember(Value = "After"),
            Description(""),
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Date),
            OperatorAllowedDataTypesAdvancedFilter(DataType.Date, DataType.DateTime),
            OperatorFieldTypeRequirements(false),
            SortOrder(600)
        ]
        After,

        [
            Name("is between"),
            EnumMember(Value = "Between"),
            Description(""),
            OperatorValueCountRange(2, 2),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Date /*, DataType.Decimal, DataType.Number*/),
            OperatorAllowedDataTypesAdvancedFilter(DataType.Date, DataType.DateTime, DataType.Decimal, DataType.Number, DataType.Score, DataType.Counter),
            OperatorFieldTypeRequirements(false),
            SortOrder(1200)
        ]
        Between,

        [
            Name("is populated"),
            EnumMember(Value = "Populated"),
            Description(""),
            OperatorValueCountRange(0, 0),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Owner, MetricGovernanceCheckType.Predicate, MetricGovernanceCheckType.Relation,
                MetricGovernanceCheckType.Field), //remove field MetricGovernanceCheckType.Field before release  
            OperatorAllowedDataTypes(DataType.Boolean, DataType.Date, DataType.DateTime, DataType.Decimal, DataType.Html, DataType.Lookup, DataType.Number,
                DataType.Text), //comment out before release also 
            OperatorAllowedDataTypesAdvancedFilter(DataType.Boolean, DataType.Date, DataType.DateTime, DataType.Decimal, DataType.Html, DataType.Lookup, DataType.Number,
                DataType.Text, DataType.Link, DataType.Tag, DataType.Score, DataType.JSON, DataType.FieldFromRelationship, DataType.Relationship, DataType.Counter),
            OperatorFieldTypeRequirements(false),
            SortOrder(2000)
        ]
        Populated,

        [
            Name("is not populated"),
            EnumMember(Value = "NotPopulated"),
            Description(""),
            OperatorValueCountRange(0, 0),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Owner, MetricGovernanceCheckType.Predicate, MetricGovernanceCheckType.Relation,
                MetricGovernanceCheckType.Field), //remove field MetricGovernanceCheckType.Field before release  
            OperatorAllowedDataTypes(DataType.Boolean, DataType.Date, DataType.DateTime, DataType.Decimal, DataType.Html, DataType.Lookup, DataType.Number,
                DataType.Text), //comment out again before release 
            OperatorAllowedDataTypesAdvancedFilter(DataType.Boolean, DataType.Date, DataType.DateTime, DataType.Decimal, DataType.Html, DataType.Lookup, DataType.Number,
                DataType.Text, DataType.Link, DataType.Tag, DataType.Score, DataType.JSON, DataType.FieldFromRelationship, DataType.Relationship, DataType.Counter),
            OperatorFieldTypeRequirements(false),
            SortOrder(2100)
        ]
        NotPopulated,

        [
            Name("is greater than"),
            EnumMember(Value = "GreaterThan"),
            Description(""),
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Decimal, DataType.Number),
            OperatorAllowedDataTypesAdvancedFilter(DataType.Decimal, DataType.Number, DataType.Score, DataType.Counter),
            OperatorFieldTypeRequirements(false),
            SortOrder(900)
        ]
        GreaterThan,

        [
            Name("is less than or equal to"),
            EnumMember(Value = "LessThanOrEquals"),
            Description(""),
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Decimal, DataType.Number),
            OperatorAllowedDataTypesAdvancedFilter(DataType.Number, DataType.Counter),
            OperatorFieldTypeRequirements(false),
            SortOrder(1000)
        ]
        LessThanOrEquals,

        [
            Name("is less than"),
            EnumMember(Value = "LessThan"),
            Description(""),
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Decimal, DataType.Number),
            OperatorAllowedDataTypesAdvancedFilter(DataType.Decimal, DataType.Number, DataType.Score, DataType.Counter),
            OperatorFieldTypeRequirements(false),
            SortOrder(700)
        ]
        LessThan,

        [
            Name("is greater than or equal to"),
            EnumMember(Value = "GreaterThanOrEquals"),
            Description(""),
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Decimal, DataType.Number),
            OperatorAllowedDataTypesAdvancedFilter(DataType.Number, DataType.Counter),
            OperatorFieldTypeRequirements(false),
            SortOrder(1100)
        ]
        GreaterThanOrEquals,

        [
            Name("in"),
            EnumMember(Value = "In"),
            Description(""),
            OperatorValueCountRange(1, 1000),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field, MetricGovernanceCheckType.Predicate, MetricGovernanceCheckType.Relation),
            OperatorAllowedDataTypes(DataType.Lookup),
            OperatorAllowedDataTypesAdvancedFilter,
            OperatorFieldTypeRequirements(true),
            SortOrder(0)
        ]
        In,

        [
            Name("not in"),
            EnumMember(Value = "NotIn"),
            Description(""),
            OperatorValueCountRange(1, 1000),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field, MetricGovernanceCheckType.Predicate, MetricGovernanceCheckType.Relation),
            OperatorAllowedDataTypes(DataType.Lookup),
            OperatorAllowedDataTypesAdvancedFilter,
            OperatorFieldTypeRequirements(true),
            SortOrder(0)
        ]
        NotIn,

        [
            //uncomment before the release
            //NotYetUsed,
            Name("is true"),
            EnumMember(Value = "IsTrue"),
            Description(""),
            OperatorValueCountRange(0, 0),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Boolean),
            OperatorAllowedDataTypesAdvancedFilter(DataType.Boolean),
            OperatorFieldTypeRequirements(true),
            SortOrder(100)
        ]
        IsTrue,

        [
            //uncomment before the release
            //NotYetUsed,
            Name("is false"),
            EnumMember(Value = "IsFalse"),
            Description(""),
            OperatorValueCountRange(0, 0),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Boolean),
            OperatorAllowedDataTypesAdvancedFilter(DataType.Boolean),
            OperatorFieldTypeRequirements(true),
            SortOrder(200)
        ]
        IsFalse,

        [
            Name("is on or before"),
            EnumMember(Value = "OnOrBefore"),
            Description(""),
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Date),
            OperatorAllowedDataTypesAdvancedFilter(DataType.Date),
            OperatorFieldTypeRequirements(false),
            SortOrder(700)
        ]
        OnOrBefore,

        [
            Name("is on or after"),
            EnumMember(Value = "OnOrAfter"),
            Description(""),
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Date),
            OperatorAllowedDataTypesAdvancedFilter(DataType.Date),
            OperatorFieldTypeRequirements(false),
            SortOrder(800)
        ]
        OnOrAfter,

        [
            Name("is in band"),
            EnumMember(Value = "IsInBand"),
            Description(""),
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks,
            OperatorAllowedDataTypes(DataType.Score),
            OperatorAllowedDataTypesAdvancedFilter(DataType.Score),
            OperatorFieldTypeRequirements(false),
            SortOrder(0)
        ]
        IsInBand
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
        public int SortOrder { get; set; }
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
            //ignore operators which are used only on advanced filters
            var ignoreList = new List<Operator> { Operator.In, Operator.NotIn };
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
                        FieldRequiresMultipleValueSupport = ((OperatorFieldTypeRequirementsAttribute)tm.GetCustomAttribute(typeof(OperatorFieldTypeRequirementsAttribute)))
                            .FieldRequiresMultipleValueSupport,
                        SortOrder = ((SortOrderAttribute)tm.GetCustomAttribute(typeof(SortOrderAttribute))).Order
                    });
                }
            }

            return list.OrderBy(i => i.Name).Where(x => !ignoreList.Contains(x.ID)).ToList();
        }

        public static List<OperatorInfo> GetAsListForAdvancedFilters(this Operator type)
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
                        from dt in ((OperatorAllowedDataTypesAdvancedFilterAttribute)tm.GetCustomAttribute(typeof(OperatorAllowedDataTypesAdvancedFilterAttribute))).DataTypes
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
                        FieldRequiresMultipleValueSupport = ((OperatorFieldTypeRequirementsAttribute)tm.GetCustomAttribute(typeof(OperatorFieldTypeRequirementsAttribute)))
                            .FieldRequiresMultipleValueSupport,
                        SortOrder = ((SortOrderAttribute)tm.GetCustomAttribute(typeof(SortOrderAttribute))).Order
                    });
                }
            }

            return list.OrderBy(i => i.Name).ToList();
        }

        internal static DateTime ParseDateString(this string dt)
        {
            var value = DateTime.Parse(dt);
            if (dt.EndsWith("Z"))
            {
                value = value.ToUniversalTime();
            }

            return value;
        }

        /// <summary>
        /// Use this operator to compare a value to a set of values, applying its proper data type to the comparison.
        /// </summary>
        /// <param name="operator">The operator to use for comparison.</param>
        /// <param name="dataType">The data type to convert to (string values are from the DataType enumeration)</param>
        /// <param name="allowMultipleValues">Does field type allow multiple values.</param>
        /// <param name="values">The value set to check against, for comparison.</param>
        /// <param name="valueToCompare">The value that we are checking for match, based on operator and data type. An example would be the Value from the field we are checking.</param>
        /// <param name="testOperator"></param>
        /// <returns></returns>
        public static bool TestTwoValues(this Operator @operator, string dataType, bool allowMultipleValues, IReadOnlyList<string> values, string valueToCompare, ITestOperator testOperator = null)
        {
            testOperator = testOperator ?? new DefaultTestOperator();
            return testOperator.Execute(@operator, dataType, allowMultipleValues, values, valueToCompare);
        }
    }

    public interface ITestOperator
    {
        bool Execute(Operator @operator, string dataType, bool allowMultipleValues, IReadOnlyList<string> values, string valueToCompare);
    }

    internal abstract class TestOperatorBase : ITestOperator
    {
        protected static bool OneValueCondition<T>(IList<T> values, Func<T, bool> condition)
        {
            return values.Count == 1
                   && condition(values[0]);
        }

        protected static bool TwoValueCondition<T>(IList<T> values, Func<T, T, bool> condition)
        {
            return values.Count == 2
                   && condition(values[0], values[1]);
        }

        public abstract bool Execute(Operator @operator, string dataType, bool allowMultipleValues, IReadOnlyList<string> values, string valueToCompare);
    }

    internal class LookupTestOperator : TestOperatorBase
    {
        public override bool Execute(Operator @operator, string dataType, bool allowMultipleValues, IReadOnlyList<string> values, string valueToCompare)
        {
            // STEP #1. Check DataType
            var supportedTypes = new[] { DataType.Lookup };
            if (supportedTypes.Any(x => string.Equals(x.ToString(), dataType, StringComparison.OrdinalIgnoreCase)) == false)
            {
                // not supported data type
                return false;
            }

            // STEP #2. Convert values
            valueToCompare = valueToCompare ?? string.Empty;
            var fieldValue = valueToCompare.ToLowerInvariant().Split(',');
            var parsedValues = values.Select(x => x.ToLowerInvariant()).ToList();

            // STEP #3. Calculate result
            bool result = false;

            switch (@operator)
            {
                case Operator.After:
                case Operator.GreaterThan:
                case Operator.Before:
                case Operator.LessThan:
                case Operator.OnOrAfter:
                case Operator.GreaterThanOrEquals:
                case Operator.OnOrBefore:
                case Operator.LessThanOrEquals:
                case Operator.Between:
                case Operator.Contains:
                case Operator.NotContains:
                case Operator.StartsWith:
                case Operator.EndsWith:
                case Operator.IsFalse:
                case Operator.IsTrue:
                case Operator.NotPopulated:
                case Operator.Populated:
                    break;

                case Operator.Equals:
                    result = allowMultipleValues && fieldValue.Intersect(parsedValues).Any();

                    //: OneValueCondition(parsedValues, value => string.Equals(valueToCompare, value, StringComparison.InvariantCulture));
                    break;

                case Operator.In:
                    result = fieldValue.Intersect(parsedValues).Any();
                    break;

                case Operator.NotIn:
                    result = fieldValue.Intersect(parsedValues).Any() == false;
                    break;

                case Operator.NotEquals:
                    result = allowMultipleValues && fieldValue.Intersect(parsedValues).Any() == false;
                    //? 
                    //: OneValueCondition(parsedValues, value => string.Equals(valueToCompare, value, StringComparison.InvariantCulture)) == false;
                    break;
            }

            return result;
        }
    }

    internal class DefaultTestOperator : TestOperatorBase
    {
        public DefaultTestOperator()
        {
            // DI also can be reused
            Operators = new List<TestOperatorBase>
            {
                new BooleanTestOperator(),
                new DateTimeTestOperator(new DateTimeService()),
                new DecimalTestOperator(new DecimalService()),
                new Int64TestOperator(new Int64Service()),
                new LookupTestOperator(),
                new StringTestOperator()
            };
        }

        private ICollection<TestOperatorBase> Operators { get; set; }

        public override bool Execute(Operator @operator, string dataType, bool allowMultipleValues, IReadOnlyList<string> values, string valueToCompare)
        {
            values = values ?? new List<string>();

            foreach (var testOperator in Operators)
            {
                if (testOperator.Execute(@operator, dataType, allowMultipleValues, values, valueToCompare))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class StringTestOperator : TestOperatorBase
    {
        public override bool Execute(Operator @operator, string dataType, bool allowMultipleValues, IReadOnlyList<string> values, string valueToCompare)
        {
            // STEP #1. Check DataType - NOT Needed

            // STEP #2. Convert values - Normalize and  lower case string.
            valueToCompare = (valueToCompare ?? string.Empty);
            var fieldValue = valueToCompare.ToLowerInvariant();
            var parsedValues = values.Select(x => x.ToLowerInvariant()).ToList();

            // STEP #3. Calculate result
            bool result = false;

            switch (@operator)
            {
                case Operator.After:
                case Operator.GreaterThan:
                case Operator.Before:
                case Operator.LessThan:
                case Operator.OnOrAfter:
                case Operator.GreaterThanOrEquals:
                case Operator.OnOrBefore:
                case Operator.LessThanOrEquals:
                case Operator.Between:
                case Operator.IsFalse:
                case Operator.IsTrue:
                    break;

                case Operator.StartsWith:
                    result = OneValueCondition(parsedValues, value => fieldValue.StartsWith(value, StringComparison.InvariantCulture));
                    break;

                case Operator.EndsWith:
                    result = OneValueCondition(parsedValues, value => fieldValue.EndsWith(value, StringComparison.InvariantCulture));
                    break;

                case Operator.NotPopulated:
                    result = string.IsNullOrEmpty(fieldValue);
                    break;

                case Operator.Populated:
                    result = string.IsNullOrEmpty(fieldValue) == false;
                    break;

                case Operator.Equals:
                    result = OneValueCondition(parsedValues, value => string.Equals(fieldValue, value, StringComparison.InvariantCulture));
                    break;

                case Operator.In:
                    result = parsedValues.Any(value => fieldValue.IndexOf(value, StringComparison.InvariantCulture) != -1);
                    break;

                case Operator.Contains:
                    result = OneValueCondition(parsedValues, value => fieldValue.IndexOf(value, StringComparison.InvariantCulture) != -1);
                    break;

                case Operator.NotIn:
                    result = parsedValues.All(value => fieldValue.IndexOf(value, StringComparison.InvariantCulture) == -1);
                    break;

                case Operator.NotContains:
                    result = OneValueCondition(parsedValues, value => fieldValue.IndexOf(value, StringComparison.InvariantCulture) == -1);
                    break;

                case Operator.NotEquals:
                    result = OneValueCondition(parsedValues, value => string.Equals(fieldValue, value, StringComparison.InvariantCulture)) == false;
                    break;
            }

            return result;
        }
    }

    internal class Int64TestOperator : TestOperatorBase
    {
        private IInt64Service Int64Service { get; }

        public Int64TestOperator(IInt64Service int64Service)
        {
            Int64Service = int64Service;
        }

        public override bool Execute(Operator @operator, string dataType, bool allowMultipleValues, IReadOnlyList<string> values, string valueToCompare)
        {
            // STEP #1. Check DataType
            var supportedTypes = new[] { DataType.Number };
            if (supportedTypes.Any(x => string.Equals(x.ToString(), dataType, StringComparison.OrdinalIgnoreCase)) == false)
            {
                // not supported data type
                return false;
            }

            // STEP #2. Convert values
            if (Int64Service.TryParse(values, out var parsedValues) == false)
            {
                // cant parse values
                return false;
            }

            if (Int64Service.TryParse(valueToCompare, out var fieldValue) == false)
            {
                // can't parse valueToCompare
                return false;
            }

            // STEP #3. Calculate result
            bool result = false;

            switch (@operator)
            {
                case Operator.After:
                case Operator.GreaterThan:
                    result = OneValueCondition(parsedValues, value => fieldValue > value);
                    break;

                case Operator.Before:
                case Operator.LessThan:
                    result = OneValueCondition(parsedValues, value => fieldValue < value);
                    break;

                case Operator.OnOrAfter:
                case Operator.GreaterThanOrEquals:
                    result = OneValueCondition(parsedValues, value => fieldValue >= value);
                    break;

                case Operator.OnOrBefore:
                case Operator.LessThanOrEquals:
                    result = OneValueCondition(parsedValues, value => fieldValue <= value);
                    break;

                case Operator.Between:
                    result = TwoValueCondition(parsedValues, (value1, value2) => fieldValue >= value1 && fieldValue <= value2);
                    break;

                case Operator.Equals:
                    result = OneValueCondition(parsedValues, value => fieldValue == value);
                    break;

                case Operator.NotEquals:
                    result = OneValueCondition(parsedValues, value => fieldValue != value);
                    break;

                case Operator.Contains:
                case Operator.NotContains:
                case Operator.StartsWith:
                case Operator.EndsWith:
                case Operator.IsFalse:
                case Operator.IsTrue:
                case Operator.NotPopulated:
                case Operator.Populated:
                case Operator.In:
                case Operator.NotIn:
                    break;
            }

            return result;
        }
    }

    internal sealed class BooleanTestOperator : TestOperatorBase
    {
        public override bool Execute(Operator @operator, string dataType, bool allowMultipleValues, IReadOnlyList<string> values, string valueToCompare)
        {
            // check DataType
            var supportedTypes = new[] { DataType.Boolean };
            if (supportedTypes.Any(x => string.Equals(x.ToString(), dataType, StringComparison.OrdinalIgnoreCase)) == false)
            {
                // not supported data type
                return false;
            }

            // convert values
            var parsedValues = new List<bool>();
            foreach (var value in values)
            {
                if (bool.TryParse(value, out var parsedValue) == false)
                {
                    return false;
                }

                parsedValues.Add(parsedValue);
            }

            if (bool.TryParse(valueToCompare, out var fieldValue) == false)
            {
                return false;
            }

            // calculate result
            var result = false;

            switch (@operator)
            {
                case Operator.IsFalse:
                    result = !fieldValue;
                    break;
                case Operator.IsTrue:
                    result = fieldValue;
                    break;
                case Operator.Equals:
                    result = OneValueCondition(parsedValues, value => fieldValue == value);
                    break;
                case Operator.NotEquals:
                    result = OneValueCondition(parsedValues, value => fieldValue != value);
                    break;

                case Operator.After:
                case Operator.Before:
                case Operator.OnOrAfter:
                case Operator.OnOrBefore:
                case Operator.Contains:
                case Operator.NotContains:
                case Operator.StartsWith:
                case Operator.EndsWith:
                case Operator.NotPopulated:
                case Operator.Populated:
                case Operator.In:
                case Operator.NotIn:
                case Operator.Between:
                case Operator.GreaterThan:
                case Operator.GreaterThanOrEquals:
                case Operator.LessThan:
                case Operator.LessThanOrEquals:
                    break;
            }

            return result;
        }
    }

    internal sealed class DecimalTestOperator : TestOperatorBase
    {
        private IDecimalService DecimalService { get; }

        public DecimalTestOperator(IDecimalService decimalService)
        {
            DecimalService = decimalService;
        }

        public override bool Execute(Operator @operator, string dataType, bool allowMultipleValues, IReadOnlyList<string> values, string valueToCompare)
        {
            // check DataType
            var supportedTypes = new[] { DataType.Decimal };
            if (supportedTypes.Any(x => string.Equals(x.ToString(), dataType, StringComparison.OrdinalIgnoreCase)) == false)
            {
                // not supported data type
                return false;
            }

            // parse values
            if (DecimalService.TryParse(values, out var parsedValues) == false)
            {
                // can't parse values
                return false;
            }

            if (DecimalService.TryParse(valueToCompare, out var fieldValue) == false)
            {
                // can't parse valueToCompare
                return false;
            }

            // calculate result
            bool result = false;

            switch (@operator)
            {
                case Operator.After:
                case Operator.Before:
                case Operator.OnOrAfter:
                case Operator.OnOrBefore:
                case Operator.Contains:
                case Operator.NotContains:
                case Operator.StartsWith:
                case Operator.EndsWith:
                case Operator.IsFalse:
                case Operator.IsTrue:
                case Operator.NotPopulated:
                case Operator.Populated:
                case Operator.In:
                case Operator.NotIn:
                    break;

                case Operator.Between:
                    result = TwoValueCondition(parsedValues, (value1, value2) => fieldValue >= value1 && fieldValue <= value2);
                    break;
                case Operator.Equals:
                    result = OneValueCondition(parsedValues, value => fieldValue == value);
                    break;
                case Operator.GreaterThan:
                    result = OneValueCondition(parsedValues, value => fieldValue > value);
                    break;
                case Operator.GreaterThanOrEquals:
                    result = OneValueCondition(parsedValues, value => fieldValue >= value);
                    break;
                case Operator.LessThan:
                    result = OneValueCondition(parsedValues, value => fieldValue < value);
                    break;

                case Operator.LessThanOrEquals:
                    result = OneValueCondition(parsedValues, value => fieldValue <= value);
                    break;

                case Operator.NotEquals:
                    result = OneValueCondition(parsedValues, value => fieldValue != value);
                    break;
            }

            return result;
        }
    }

    internal class DateTimeTestOperator : TestOperatorBase
    {
        private IDateTimeService DateTimeService { get; }

        public DateTimeTestOperator(IDateTimeService dateTimeService)
        {
            DateTimeService = dateTimeService;
        }

        public override bool Execute(Operator @operator, string dataType, bool allowMultipleValues, IReadOnlyList<string> values, string valueToCompare)
        {
            // STEP #1. Check DataType
            var supportedTypes = new[] { DataType.Date, DataType.DateTime };
            if (supportedTypes.Any(x => string.Equals(x.ToString(), dataType, StringComparison.OrdinalIgnoreCase)) == false)
            {
                // not supported data type
                return false;
            }

            // STEP #2. Convert values
            if (DateTimeService.TryParse(values, out var parsedValues) == false)
            {
                // cant parse values
                return false;
            }

            if (DateTimeService.TryParse(valueToCompare, out var fieldValue) == false)
            {
                // can't parse valueToCompare
                return false;
            }

            // STEP #3. Calculate result
            bool result = false;

            switch (@operator)
            {
                case Operator.After:
                case Operator.GreaterThan:
                    result = OneValueCondition(parsedValues, value => fieldValue > value);
                    break;

                case Operator.Before:
                case Operator.LessThan:
                    result = OneValueCondition(parsedValues, value => fieldValue < value);
                    break;

                case Operator.OnOrAfter:
                case Operator.GreaterThanOrEquals:
                    result = OneValueCondition(parsedValues, value => fieldValue >= value);
                    break;

                case Operator.OnOrBefore:
                case Operator.LessThanOrEquals:
                    result = OneValueCondition(parsedValues, value => fieldValue <= value);
                    break;

                case Operator.Between:
                    result = TwoValueCondition(parsedValues, (value1, value2) => fieldValue >= value1 && fieldValue <= value2);
                    break;

                case Operator.Equals:
                    result = OneValueCondition(parsedValues, value => fieldValue == value);
                    break;

                case Operator.NotEquals:
                    result = OneValueCondition(parsedValues, value => fieldValue != value);
                    break;

                case Operator.Contains:
                case Operator.NotContains:
                case Operator.StartsWith:
                case Operator.EndsWith:
                case Operator.IsFalse:
                case Operator.IsTrue:
                case Operator.NotPopulated:
                case Operator.Populated:
                case Operator.In:
                case Operator.NotIn:
                    break;
            }

            return result;
        }
    }
}
