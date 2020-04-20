using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace d360.core
{
    public class BackColorAttribute : Attribute
    {
        private string _color = "#000";
        public string Color { get { return _color; } }
        public BackColorAttribute(string color)
        {
            _color = color;
        }
    }

    public class ForeColorAttribute : Attribute
    {
        private string _color = "#000";
        public string Color { get { return _color; } }
        public ForeColorAttribute(string color)
        {
            _color = color;
        }
    }

    public class AllowIntersectTypeAsSubjectAttribute : Attribute
    {
        private bool _allowed = true;
        public bool Allowed { get { return _allowed; } }
        public AllowIntersectTypeAsSubjectAttribute(bool allowed)
        {
            _allowed = allowed;
        }
    }

    public class AllowOwnershipAttribute : Attribute
    {
        private bool _allowed = true;
        public bool Allowed { get { return _allowed; } }
        public AllowOwnershipAttribute(bool allowed)
        {
            _allowed = allowed;
        }
    }

    public class AllowMultiplePredicatesAttribute : Attribute
    {
        private bool _allowed = true;
        public bool Allowed { get { return _allowed; } }
        public AllowMultiplePredicatesAttribute(bool allowed)
        {
            _allowed = allowed;
        }
    }

    public class LineageVersionsSupportedAttribute : Attribute
    {
        public int[] Versions { get; set; }
        public LineageVersionsSupportedAttribute(params int[] versions)
        {
            Versions = versions;
        }
    }

    public class SubjectAssetClassesSupportedAttribute : Attribute
    {
        public enums.AssetTypeClass[] Classes { get; set; }
        public SubjectAssetClassesSupportedAttribute(params enums.AssetTypeClass[] classes)
        {
            Classes = classes;
        }
    }

    public class ObjectAssetClassesSupportedAttribute : Attribute
    {
        public enums.AssetTypeClass[] Classes { get; set; }
        public ObjectAssetClassesSupportedAttribute(params enums.AssetTypeClass[] classes)
        {
            Classes = classes;
        }
    }

    public class AllowIntersectTypeAssignmentAttribute : Attribute
    {
        private bool _allowed = true;
        public bool Allowed { get { return _allowed; } }
        public AllowIntersectTypeAssignmentAttribute(bool allowed)
        {
            _allowed = allowed;
        }
    }

    public class AllowDifferentSubjectObjectAttribute : Attribute
    {
        private bool _allowed = true;
        public bool Allowed { get { return _allowed; } }
        public AllowDifferentSubjectObjectAttribute(bool allowed)
        {
            _allowed = allowed;
        }
    }

    public class ForceDifferentSubjectObjectAttribute : Attribute
    {
        private bool _allowed = true;
        public bool Allowed { get { return _allowed; } }
        public ForceDifferentSubjectObjectAttribute(bool allowed)
        {
            _allowed = allowed;
        }
    }

    public class SingleRelationshipByFunctionalTypeAttribute : Attribute
    {
        private bool _allowed = true;
        public bool Allowed { get { return _allowed; } }
        public SingleRelationshipByFunctionalTypeAttribute(bool allowed)
        {
            _allowed = allowed;
        }
    }

    public class AllowSurveyAttribute : Attribute
    {
        private bool _allowed = true;
        public bool Allowed { get { return _allowed; } }
        public AllowSurveyAttribute(bool allowed)
        {
            _allowed = allowed;
        }
    }

    public class IconAttribute : Attribute
    {
        private string _icon = "";
        public string Icon { get { return _icon; } }
        public IconAttribute(string icon)
        {
            _icon = icon;
        }
    }

    public class NameAttribute : Attribute
    {
        private string _name = "";
        public string Name { get { return _name; } }
        public NameAttribute(string name)
        {
            _name = name;
        }
    }

    public class EnableAuditAttribute : Attribute
    {
        public bool Enabled { get; set; }

        public EnableAuditAttribute(bool enabled)
        {
            Enabled = enabled;
        }
    }

    public class IsTypeAttribute : Attribute
    {
        public bool IsType { get; set; }

        public IsTypeAttribute(bool isType)
        {
            IsType = isType;
        }
    }

    public class AllowEditFromPredicateEditorAttribute : Attribute
    {
        public bool Allowed { get; set; }

        public AllowEditFromPredicateEditorAttribute(bool allowed)
        {
            Allowed = allowed;
        }
    }

    public class AllowEditFromRelationshipEditorAttribute : Attribute
    {
        public bool Allowed { get; set; }

        public AllowEditFromRelationshipEditorAttribute(bool allowed)
        {
            Allowed = allowed;
        }
    }

    public class ExcludeDataTypeAttribute : Attribute
    {
        public DataType Excluded { get; set; }

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
                if(!isNumeric && !Enum.IsDefined(objectType, reader.Value))
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
}
