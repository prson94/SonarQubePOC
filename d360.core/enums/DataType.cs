using d360.core.entities;
using d360.core.helpers;
using d360.core.resources;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace d360.core
{

    public enum DataType
    {
        [Description("None")]
        None = 0, // Not used as a type ;used for logical computing

        [Description("True/False")]
        Boolean = 1 << 0,

        [Description("Date"), SqlDataType("date")]
        Date = 1 << 1,

        [Description("Date With Time"), SqlDataType("datetime")]
        DateTime = 1 << 2,

        [Description("Hidden"), ReadOnly(true)]
        Hidden = 1 << 4,

        [Description("Html/Richtext")]
        Html = 1 << 5,

        [Description("Number"), SqlDataType("bigint")]
        Number = 1 << 6,

        [Description("Decimal Number"), SqlDataType("float")]
        Decimal = 1 << 7,

        [Description("List")]
        Lookup = 1 << 8,

        [Description("Simple Text")]
        Text = 1 << 9,

        [Description("Link")]
        Link = 1 << 11,

        [Description("Color Picker"), ReadOnly(true)]
        Color = 1 << 13,

        [Description("Asset Path")]
        Path = 1 << 15,

        [Description("Relation Lookup")]
        ComplexRelationLookup = 1 << 17,

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
        Counter = 1 << 28,

		[Description("System")]
		System = 1 << 29
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
		public static string AsSqlDataType(this DataType type)
		{
			SqlDataTypeAttribute dt = type.GetType()
				.GetMember(type.ToString())
				.Where(m => m.MemberType == MemberTypes.Field)
				.Single().GetCustomAttributes(typeof(SqlDataTypeAttribute))
				.Cast<SqlDataTypeAttribute>()
				.SingleOrDefault();
			if (dt == null)
			{
				return "nvarchar(max)";
			}
			else
			{
				return dt.Datatype;
			}
		}

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
                var enumValue = (DataType)Enum.Parse(typeof(DataType), tm.Name);

                var info = new DataTypeInfo
                {
                    ReadOnly = (aReadOnly != null) ? aReadOnly.IsReadOnly : false,
                    Description = DescriptionAsDisplayString(enumValue),
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
                {
                    continue;
                }
                var enumValue = (DataType)Enum.Parse(typeof(DataType), tm.Name);

                var info = new DataTypeInfo
                {
                    ReadOnly = (aReadOnly != null) ? aReadOnly.IsReadOnly : false,
                    Description = DescriptionAsDisplayString(enumValue),
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

        private static string DescriptionAsDisplayString(DataType type)
        {
            switch (type)
            {
                case DataType.Boolean: return Enums.FieldType_Boolean;
                case DataType.Color: return Enums.FieldType_ColorPicker;
                case DataType.ComplexRelationLookup: return Enums.FieldType_RelationLookup;
                case DataType.Counter: return Enums.FieldType_Counter;
                case DataType.Date: return Enums.FieldType_Date;
                case DataType.DateTime: return Enums.FieldType_DateTime;
                case DataType.Decimal: return Enums.FieldType_Decimal;
                case DataType.FieldFromRelationship: return Enums.FieldType_FieldFromRel;
                case DataType.Hidden: return Enums.FieldType_Hidden;
                case DataType.Html: return Enums.FieldType_Html;
                case DataType.JSON: return Enums.FieldType_JSON;
                case DataType.JsonElement: return Enums.FieldType_JSONAttribute;
                case DataType.Link: return Enums.FieldType_Link;
                case DataType.Lookup: return Enums.FieldType_List;
                case DataType.None: return Enums.FieldType_None;
                case DataType.Number: return Enums.FieldType_Number;
                case DataType.OwnershipLookup: return Enums.FieldType_OwnershipLookup;
                case DataType.Path: return Enums.FieldType_AssetPath;
                case DataType.RefListRelationship: return Enums.FieldType_ReferenceItemListFromRel;
                case DataType.Relationship: return Enums.FieldType_Relationship;
                case DataType.Score: return Enums.FieldType_Score;
                case DataType.Tag: return Enums.FieldType_Tag;
                case DataType.Text: return Enums.FieldType_SimpleText;
				case DataType.System: return Enums.FieldType_System;

				default: throw new ArgumentOutOfRangeException("DataType");
            }
        }

		public static FieldValidationResult ValidateBoolean(this DataType type, string name, string value)
		{
			FieldValidationResult result = new();

			if (value != null)
			{
				bool bValue;
				if (bool.TryParse(value, out bValue))
				{
					result.CorrectedValue = bValue.ToString().ToLower();
				}
				else
				{
					result.IsValid = false;
					result.Message += string.Format(CompanyContextApiError.ValidateBoolValue, name);
				}
			}

			return result;
		}

		public static FieldValidationResult ValidateDate(this DataType type, string name, string value)
		{
			FieldValidationResult result = new();

			if (value != null)
			{
				DateTime dValue;
				if (DateTime.TryParse(value, out dValue))
				{
					result.CorrectedValue = dValue.Date.ToString();
				}
				else
				{
					result.IsValid = false;
					result.Message += string.Format(CompanyContextApiError.FieldNameValidate, name, "date");
				}
			}

			return result;
		}

		public static FieldValidationResult ValidateDateTime(this DataType type, string name, string value)
		{
			FieldValidationResult result = new();

			if (value != null)
			{
				DateTime dValue;
				if (DateTime.TryParse(value, out dValue))
				{
					result.CorrectedValue = dValue.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
				}
				else
				{
					result.IsValid = false;
					result.Message += string.Format(CompanyContextApiError.FieldNameValidate, name, "datetime");
				}
			}

			return result;
		}

		public static FieldValidationResult ValidateDecimal(this DataType type, string name, int? length, decimal? minLength, decimal? maxLength, string value)
		{
			string decimalFormatString = $"0.{string.Join("", Enumerable.Repeat("#", 18))}";
			FieldValidationResult result = new();

			if (value != null)
			{
				decimal dValue;
				if (decimal.TryParse(value, out dValue))
				{
					if (minLength.HasValue)
					{
						if (dValue < minLength.Value)
						{
							result.IsValid = false;
							result.Message += string.Format(CompanyContextApiError.NumericMinimumValueCheck, name, minLength.Value.ToString(decimalFormatString));
						}
					}
					if (maxLength.HasValue)
					{
						if (dValue > maxLength.Value)
						{
							result.IsValid = false;
							result.Message += string.Format(CompanyContextApiError.NumericMaximumValueCheck, name, maxLength.Value.ToString(decimalFormatString));
						}
					}
				}
				else 
				{
					result.IsValid = false;
					result.Message += string.Format(CompanyContextApiError.FieldNameValidate, name, "decimal");
				}
				if (length.HasValue)
				{
					if (value.Length != length.Value)
					{
						result.IsValid = false;
						result.Message += string.Format(CompanyContextApiError.CheckExactLength, name, length.Value);
					}
				}
			}

			return result;
		}

		public static FieldValidationResult ValidateLink(this DataType type, string name, string value)
		{
			FieldValidationResult result = new();

			if (value != "")
			{
				if (value.Count(c => c == '|') != 1 && !value.Equals('|'))
				{
					result.IsValid = false;
					result.Message += string.Format(CompanyContextApiError.ValidateLinkValue, name);
				}
				else 
				{
					// Remove 'inner' trailing/leading spaces in link value.
					result.CorrectedValue = Regex.Replace(value, "(\\s*\\|\\s*)", "|");
				}
			}

			return result;
		}

		public static FieldValidationResult ValidateList(this DataType type, string name, bool allowMultiple, string value)
		{
			FieldValidationResult result = new();

			if (value != "")
			{
				if (!allowMultiple && value.Split(',').Length > 1)
				{
					result.IsValid = false;
					result.Message += string.Format(CompanyContextApiError.FieldNotAllowedMultipleValies, name);
				}
			}

			return result;
		}

		public static FieldValidationResult ValidateNumber(this DataType type, string name, int? length, decimal? minLength, decimal? maxLength, string value)
		{
			string decimalFormatString = $"0.{string.Join("", Enumerable.Repeat("#", 18))}";
			FieldValidationResult result = new();

			if (value != null)
			{
				long dValue;
				if (long.TryParse(value, out dValue))
				{
					if (minLength.HasValue)
					{
						if (dValue < minLength.Value)
						{
							result.IsValid = false;
							result.Message += string.Format(CompanyContextApiError.NumericMinimumValueCheck, name, minLength.Value.ToString(decimalFormatString));
						}
					}
					if (maxLength.HasValue)
					{
						if (dValue > maxLength.Value)
						{
							result.IsValid = false;
							result.Message += string.Format(CompanyContextApiError.NumericMaximumValueCheck, name, maxLength.Value.ToString(decimalFormatString));
						}
					}
				}
				else
				{
					result.IsValid = false;
					result.Message += string.Format(CompanyContextApiError.ValidateNumberFieldRange, name, -9223372036854775808, 9223372036854775807);
				}
				if (length.HasValue)
				{
					if (value.Length != length.Value)
					{
						result.IsValid = false;
						result.Message += string.Format(CompanyContextApiError.CheckExactLength, name, length.Value);
					}
				}
			}

			return result;
		}

		public static FieldValidationResult ValidateText(this DataType type, string name, int? length, decimal? minLength, decimal? maxLength, string pattern, string value)
		{
			string decimalFormatString = $"0.{string.Join("", Enumerable.Repeat("#", 18))}";
			FieldValidationResult result = new();

			if (value != "")
			{
				if (length.HasValue)
				{
					if (value.Length != length.Value)
					{
						result.IsValid = false;
						result.Message += string.Format(CompanyContextApiError.CheckExactLength, name, length.Value);
					}
				}

				if (minLength.HasValue)
				{
					if (value.Length < minLength.Value)
					{
						result.IsValid = false;
						result.Message += string.Format(CompanyContextApiError.NumericMinimumValueCheck, name, minLength.Value.ToString(decimalFormatString));
					}
				}

				if (maxLength.HasValue)
				{
					if (value.Length > maxLength.Value)
					{
						result.IsValid = false;
						result.Message += string.Format(CompanyContextApiError.ExceedsMaximumLength, name, maxLength.Value.ToString(decimalFormatString));
					}
				}

				if (!string.IsNullOrEmpty(pattern))
				{
					if (!Regex.IsMatch(value, pattern))
					{
						result.IsValid = false;
						result.Message += string.Format(CompanyContextApiError.RegularExpressionPatternMatch, name);
					}
				}
			}

			return result;
		}

		public static FieldValidationResult ValidateRequirement(this DataType type, string name, bool required, string value)
		{
			FieldValidationResult result = new();

			value = (value ?? "").Trim();

			if (string.IsNullOrEmpty(value) && required)
			{
				result.IsValid = false;
				result.Message += string.Format(CompanyContextApiError.FieldValueIsRequired, name);
			}

			return result;
		}

		public static FieldValidationResult ValidateRestricted(this DataType type, string name, string typeName)
		{
			FieldValidationResult result = new();

			List<string> restrictedFieldTypes = DataType.Text.GetNotAllowedToUpdateViaAssetApi();
			if (restrictedFieldTypes.Contains(typeName))
			{
				result.IsValid = false;
				result.Message += string.Format(CompanyContextApiError.RestrictFieldTypeUpdate, name, typeName);
			}

			return result;
		}

	}
}
