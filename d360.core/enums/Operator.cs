using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum Operator
    {
        [
            Name("is"), 
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field, MetricGovernanceCheckType.Relation),
            OperatorAllowedDataTypes(DataType.Date, DataType.Decimal, DataType.Lookup, DataType.Number, DataType.Text), 
            OperatorFieldTypeRequirements(false)
        ]
        Equals = 1,
        [
            Name("is not"), 
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field, MetricGovernanceCheckType.Relation),
            OperatorAllowedDataTypes(DataType.Date, DataType.Decimal, DataType.Lookup, DataType.Number, DataType.Text), 
            OperatorFieldTypeRequirements(false)
        ]
        NotEquals,
        [
            Name("contains"), 
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Text), 
            OperatorFieldTypeRequirements(false)
        ]
        Contains,
        [
            Name("does not contain"), 
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Text), 
            OperatorFieldTypeRequirements(false)
        ]
        NotContains,
        [
            Name("starts with"), 
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Text), 
            OperatorFieldTypeRequirements(false)
        ]
        StartsWith,
        [
            Name("ends with"), 
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Text), 
            OperatorFieldTypeRequirements(false)
        ]
        EndsWith,
        [
            Name("is before"), 
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Date), 
            OperatorFieldTypeRequirements(false)
        ]
        Before,
        [
            Name("is after"), 
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Date), 
            OperatorFieldTypeRequirements(false)
        ]
        After,
        [
            NotYetUsed, 
            Name("is between"), 
            Description(""), 
            OperatorValueCountRange(2, 2),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Date, DataType.Decimal, DataType.Number), 
            OperatorFieldTypeRequirements(false)
        ]
        Between,
        [
            Name("is populated"), 
            Description(""), 
            OperatorValueCountRange(0, 0),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field, MetricGovernanceCheckType.Owner, MetricGovernanceCheckType.Predicate, MetricGovernanceCheckType.Relation),
            OperatorAllowedDataTypes(DataType.Boolean, DataType.Date, DataType.DateTime, DataType.Decimal, DataType.Html, DataType.JSON, DataType.JsonElement, DataType.Lookup, DataType.Number, DataType.Text), 
            OperatorFieldTypeRequirements(false)
        ]
        Populated,
        [
            Name("is not populated"), 
            Description(""), 
            OperatorValueCountRange(0, 0),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field, MetricGovernanceCheckType.Owner, MetricGovernanceCheckType.Predicate, MetricGovernanceCheckType.Relation),
            OperatorAllowedDataTypes(DataType.Boolean, DataType.Date, DataType.DateTime, DataType.Decimal, DataType.Html, DataType.JSON, DataType.JsonElement, DataType.Lookup, DataType.Number, DataType.Text), 
            OperatorFieldTypeRequirements(false)
        ]
        NotPopulated,
        [
            Name("is greater than"), 
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Decimal, DataType.Number), 
            OperatorFieldTypeRequirements(false)
            ]
        GreaterThan,
        [
            Name("is less than or equal to"), 
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Decimal, DataType.Number), 
            OperatorFieldTypeRequirements(false)
        ]
        LessThanOrEquals,
        [
            Name("is less than"), 
            Description(""), 
            OperatorValueCountRange(1, 1),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Decimal, DataType.Number), 
            OperatorFieldTypeRequirements(false)
        ]
        LessThan,
        [
            Name("is greater than or equal to"), 
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
            Description(""), 
            OperatorValueCountRange(1, 1000),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field, MetricGovernanceCheckType.Relation),
            OperatorAllowedDataTypes(DataType.Lookup), 
            OperatorFieldTypeRequirements(true)
            ]
        In,
        [
            NotYetUsed, 
            Name("not in"), 
            Description(""), 
            OperatorValueCountRange(1, 1000),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field, MetricGovernanceCheckType.Relation),
            OperatorAllowedDataTypes(DataType.Lookup), 
            OperatorFieldTypeRequirements(true)
        ]
        NotIn,
        [
            Name("is true"),
            Description(""),
            OperatorValueCountRange(0, 0),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Boolean),
            OperatorFieldTypeRequirements(true)
        ]
        IsTrue,
        [
            Name("is false"),
            Description(""),
            OperatorValueCountRange(0, 0),
            OperatorAllowedMeasureChecks(MetricGovernanceCheckType.Field),
            OperatorAllowedDataTypes(DataType.Boolean),
            OperatorFieldTypeRequirements(true)
        ]
        IsFalse
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

    }
}