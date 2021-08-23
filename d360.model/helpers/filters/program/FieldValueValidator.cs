using d360.core.enums;
using System;
using System.Globalization;
using System.Linq;

namespace d360.model.helpers.filters.program
{
    public class FieldValueValidatorResult
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public object UpdatedValue { get; set; }
    }

    public interface IFieldValueValidator
    {
        FieldValueValidatorResult CheckValue(object value, string fieldName, string @operator);
    }

    public class NumberFieldValidator : IFieldValueValidator
    {
        public FieldValueValidatorResult CheckValue(object value, string fieldName, string @operator)
        {
            var result = new FieldValueValidatorResult();
            result.Status = true;
            int number = 0;
            if (!int.TryParse(value.ToString(), NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out number))
            {
                //parsing of thousands seperator fails on - symbol
                if (!int.TryParse(value.ToString(), out number))
                {
                    result.Status = false;
                    result.Message = $"Invalid numeric value for field '{fieldName}'";
                    return result;
                }
            }
            result.UpdatedValue = number;
            return result;
        }
    }

    public class DecimalFieldValidator : IFieldValueValidator
    {
        public FieldValueValidatorResult CheckValue(object value, string fieldName, string @operator)
        {
            var result = new FieldValueValidatorResult();
            result.Status = true;
            decimal dnumber = 0;
            if (!decimal.TryParse(value.ToString(), out dnumber))
            {
                result.Status = false;
                result.Message = $"Invalid decimal value for field '{fieldName}'";
                return result;
            }
            result.UpdatedValue = dnumber;
            return result;
        }
    }

    public class BooleanFieldValidator : IFieldValueValidator
    {
        public FieldValueValidatorResult CheckValue(object value, string fieldName, string @operator)
        {
            var result = new FieldValueValidatorResult();
            result.Status = true;

            bool boolean = false;
            var stringValue = value.ToString().ToLower(CultureInfo.InvariantCulture).Trim();
            if (stringValue == "0")
            {
                stringValue = "false";
            }

            if (stringValue == "1")
            {
                stringValue = "true";
            }

            if ("true".Contains(stringValue))
            {
                stringValue = "true";
            }
            if ("false".Contains(stringValue))
            {
                stringValue = "false";
            }

            if (!bool.TryParse(stringValue, out boolean))
            {
                result.Status = false;
                result.Message = $"Invalid boolean value for field '{fieldName}'";
                return result;
            }

            result.UpdatedValue = boolean;
            return result;
        }
    }

    public class DateFieldValidator : IFieldValueValidator
    {
        public FieldValueValidatorResult CheckValue(object value, string fieldName, string @operator)
        {
            var result = new FieldValueValidatorResult();
            result.Status = true;

            DateTime date;
            if (!DateTime.TryParse(value.ToString().Trim('\''), out date))
            {
                if (@operator == "ct" || @operator == "nct")
                {
                    value = value.ToString().Trim('\'').Replace("&apos;", "'");
                    result.UpdatedValue = value;
                    return result;
                }
                else
                {
                    result.Status = false;
                    result.Message = $"Invalid date value for field '{fieldName}'";
                    return result;
                }
            }
            else
            {
                value = date;
                if (@operator == "ct" || @operator == "nct")
                {
                    if (date == date.Date)
                    {
                        value = date.ToString("yyyy-MM-dd");
                    }
                }

                result.UpdatedValue = value;
                return result;
            }

        }
    }

    public class SystemDateFieldValidator : IFieldValueValidator
    {
        public FieldValueValidatorResult CheckValue(object value, string fieldName, string @operator)
        {
            var result = new FieldValueValidatorResult();
            result.Status = true;
            DateTime date;
            if (!DateTime.TryParse(value.ToString().Trim('\''), out date))
            {
                if (@operator == "ct" || @operator == "nct")
                {
                    value = value.ToString().Trim('\'').Replace("&apos;", "'");
                    result.UpdatedValue = value;
                    return result;
                }
                else
                {
                    result.Status = false;
                    result.Message = $"Invalid date value for field '{fieldName}'";
                    return result;
                }
            }
            else
            {
                value = date;
                if (@operator == "ct" || @operator == "nct")
                {

                    if (date == date.Date)
                    {
                        value = date.ToString("yyyy-MM-dd");
                    }
                }

                if (@operator == "le")
                {
                    //CreatedOn and UpdatedOn system fields are DateTime, but UI filtering is treating them as
                    //date fields. In case of "Less or Equal" we need to update date to take into account equal dates
                    date = date.AddHours(23);
                    date = date.AddMinutes(59);
                    date = date.AddSeconds(59);
                    date = date.AddMilliseconds(999);
                    value = date;
                }
                result.UpdatedValue = value;
                return result;
            }

        }
    }

    public class AssetTypeClassFieldValidator : IFieldValueValidator
    {
        public FieldValueValidatorResult CheckValue(object value, string fieldName, string @operator)
        {
            var result = new FieldValueValidatorResult();
            result.Status = true;

            var classes = AssetTypeClass.BusinessAsset.GetAsList();
            var match = classes.FirstOrDefault(x => x.Name.ToLower(CultureInfo.InvariantCulture) == value.ToString().ToLower(CultureInfo.InvariantCulture).Trim('\'')
            || x.Value.ToLower(CultureInfo.InvariantCulture) == value.ToString().ToLower(CultureInfo.InvariantCulture).Trim('\''));

            if (match == null)
            {
                result.Status = false;
                result.Message = $"Invalid AssetTypeClass value for field '{fieldName}'";
                return result;
            }

            result.UpdatedValue = (int)match.ID;
            return result;
        }
    }

    public class TextFieldValidator : IFieldValueValidator
    {
        public FieldValueValidatorResult CheckValue(object value, string fieldName, string @operator)
        {
            var result = new FieldValueValidatorResult();
            result.Status = true;
            result.UpdatedValue = value.ToString().Trim('\'').Replace("&apos;", "'");
            return result;
        }
    }
}
