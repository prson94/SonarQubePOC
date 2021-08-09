using d360.core.enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace d360.core
{
    public class EmojiValueAttribute : Attribute
    {
        public int Value { get; private set; }
        public EmojiValueAttribute(int value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Emojis in the same group are mutually exclusive
    /// </summary>
    public class EmojiGroupAttribute : Attribute
    {
        public string Name { get; private set; }
        public EmojiGroupAttribute(string name)
        {
            Name = name;
        }
    }

    public class BackColorAttribute : Attribute
    {
        public string Color { get; private set; } = "#000";
        public BackColorAttribute(string color)
        {
            Color = color;
        }
    }

    public class ForeColorAttribute : Attribute
    {
        public string Color { get; private set; } = "#000";
        public ForeColorAttribute(string color)
        {
            Color = color;
        }
    }

    public class AllowIntersectTypeAsSubjectAttribute : Attribute
    {
        public bool Allowed { get; private set; } = true;
        public AllowIntersectTypeAsSubjectAttribute(bool allowed)
        {
            Allowed = allowed;
        }
    }

    public class AllowCommentsOnAssetAttribute : Attribute
    {
        public bool Allowed { get; private set; } = false;
        public AllowCommentsOnAssetAttribute()
        {
            Allowed = true;
        }
    }

    public class AllowOwnershipAttribute : Attribute
    {
        public bool Allowed { get; private set; } = true;
        public AllowOwnershipAttribute(bool allowed)
        {
            Allowed = allowed;
        }
    }

    public class AllowMultiplePredicatesAttribute : Attribute
    {
        public bool Allowed { get; private set; } = true;
        public AllowMultiplePredicatesAttribute(bool allowed)
        {
            Allowed = allowed;
        }
    }

    public class LineageVersionsSupportedAttribute : Attribute
    {
        public int[] Versions { get; private set; }
        public LineageVersionsSupportedAttribute(params int[] versions)
        {
            Versions = versions;
        }
    }

    public class SubjectAssetClassesSupportedAttribute : Attribute
    {
        public enums.AssetTypeClass[] Classes { get; private set; }
        public SubjectAssetClassesSupportedAttribute(params enums.AssetTypeClass[] classes)
        {
            Classes = classes;
        }
    }

    public class ObjectAssetClassesSupportedAttribute : Attribute
    {
        public enums.AssetTypeClass[] Classes { get; private set; }
        public ObjectAssetClassesSupportedAttribute(params enums.AssetTypeClass[] classes)
        {
            Classes = classes;
        }
    }

    public class AllowIntersectTypeAssignmentAttribute : Attribute
    {
        public bool Allowed { get; private set; } = true;
        public AllowIntersectTypeAssignmentAttribute(bool allowed)
        {
            Allowed = allowed;
        }
    }

    public class AllowDifferentSubjectObjectAttribute : Attribute
    {
        public bool Allowed { get; private set; } = true;
        public AllowDifferentSubjectObjectAttribute(bool allowed)
        {
            Allowed = allowed;
        }
    }

    public class ForceDifferentSubjectObjectAttribute : Attribute
    {
        public bool Allowed { get; private set; } = true;
        public ForceDifferentSubjectObjectAttribute(bool allowed)
        {
            Allowed = allowed;
        }
    }

    public class SingleRelationshipByFunctionalTypeAttribute : Attribute
    {
        public bool Allowed { get; private set; } = true;
        public SingleRelationshipByFunctionalTypeAttribute(bool allowed)
        {
            Allowed = allowed;
        }
    }

    public class AllowSurveyAttribute : Attribute
    {
        public bool Allowed { get; private set; } = true;
        public AllowSurveyAttribute(bool allowed)
        {
            Allowed = allowed;
        }
    }

    public class IconAttribute : Attribute
    {
        public string Icon { get; private set; } = "";
        public IconAttribute(string icon)
        {
            Icon = icon;
        }
    }

    public class NameAttribute : Attribute
    {
        public string Name { get; private set; } = "";
        public NameAttribute(string name)
        {
            Name = name;
        }
    }

    public class IsAllowedAutoDisplayParentAttribute : Attribute
    {
        public bool _isAllowedAutoDisplayParent = false;
        public bool IsAllowedAutoDisplayParent { get { return _isAllowedAutoDisplayParent; } }
        public IsAllowedAutoDisplayParentAttribute(bool _isAllowed)
        {
            _isAllowedAutoDisplayParent = _isAllowed;
        }
    }

    public class IsTypeAttribute : Attribute
    {
        public bool IsType { get; private set; }

        public IsTypeAttribute(bool isType)
        {
            IsType = isType;
        }
    }

    public class AllowEditFromPredicateEditorAttribute : Attribute
    {
        public bool Allowed { get; private set; }

        public AllowEditFromPredicateEditorAttribute(bool allowed)
        {
            Allowed = allowed;
        }
    }

    public class AllowEditFromRelationshipEditorAttribute : Attribute
    {
        public bool Allowed { get; private set; }

        public AllowEditFromRelationshipEditorAttribute(bool allowed)
        {
            Allowed = allowed;
        }
    }

    public class ExcludeDataTypeAttribute : Attribute
    {
        public DataType Excluded { get; private set; }

        public ExcludeDataTypeAttribute(DataType exclude)
        {
            this.Excluded = exclude;
        }
    }
    public class EnumConverter : StringEnumConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            bool isValidEnum = true;
            if (reader.Value != null)
            {
                int enumValue;
                bool isNumeric = int.TryParse(reader.Value.ToString(), out enumValue);
                if (isNumeric && !Enum.IsDefined(objectType, enumValue))
                {
                    isValidEnum = false;
                }
                if (!isNumeric && !Enum.IsDefined(objectType, reader.Value))
                {
                    isValidEnum = false;
                }

                if (!isValidEnum)
                {
                    var ex = new JsonSerializationException($"Requested value '{reader.Value}' was not found.", new Exception("Invalid enum value"));
                    ex.Source = "Newtonsoft.Json";
                    throw ex;
                }
            }

            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
    }

    public class NotYetUsedAttribute : Attribute
    {
        public NotYetUsedAttribute()
        {
        }
    }

    public class OperatorAllowedDataTypesAttribute : Attribute
    {
        public DataType[] DataTypes { get; private set; }
        public OperatorAllowedDataTypesAttribute(params DataType[] dataTypes)
        {
            DataTypes = dataTypes;
        }
    }

    public class OperatorAllowedDataTypesAdvancedFilterAttribute : Attribute
    {
        public DataType[] DataTypes { get; private set; }
        public OperatorAllowedDataTypesAdvancedFilterAttribute(params DataType[] dataTypes)
        {
            DataTypes = dataTypes;
        }
    }

    public class OperatorAllowedMeasureChecksAttribute : Attribute
    {
        public MetricGovernanceCheckType[] Checks { get; private set; }
        public OperatorAllowedMeasureChecksAttribute(params MetricGovernanceCheckType[] checks)
        {
            Checks = checks;
        }
    }

    public class OperatorFieldTypeRequirementsAttribute : Attribute
    {
        public bool FieldRequiresMultipleValueSupport { get; private set; }
        public OperatorFieldTypeRequirementsAttribute(bool fieldRequiresMultipleValueSupport)
        {
            FieldRequiresMultipleValueSupport = fieldRequiresMultipleValueSupport;
        }
    }

    public class OperatorValueCountRangeAttribute : Attribute
    {
        public int Min { get; private set; } = 1;
        public int Max { get; private set; } = 1;
        public OperatorValueCountRangeAttribute(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }


    public class SortOrderAttribute : Attribute
    {
        public int Order { get; private set; }
        public SortOrderAttribute(int order)
        {
            Order = order;
        }
    }

    public class QueueSettingNameAttribute : Attribute
    {
        public string Name { get; private set; } = "";
        public QueueSettingNameAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Used by swagger to exclude particular properties in the swagger page
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SwaggerExcludeAttribute : Attribute
    {
    }
}
