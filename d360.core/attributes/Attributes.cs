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

    public class GraphAttribute : Attribute
    {
        private string _graph = "Glossary";
        public string Graph { get { return _graph; } }
        public GraphAttribute(string graph)
        {
            _graph = graph;
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

    public class CompanySettingActive : Attribute
    {
        public String Setting { get; set; }

        public CompanySettingActive(string setting)
        {
            this.Setting = setting;
        }
    }

}
