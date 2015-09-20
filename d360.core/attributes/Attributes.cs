using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace d360.core
{
    public class AllowOwnershipAttribute : Attribute
    {
        private bool _allowed = true;
        public bool Allowed { get { return _allowed; } }
        public AllowOwnershipAttribute(bool allowed)
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

    public class ObjectTypeAttribute : Attribute
    {
        public int ObjectTypeID { get; set; }
        public string ObjectType { get; set; }

        public ObjectTypeAttribute(int objectTypeID, string objectType)
        {
            ObjectTypeID = objectTypeID;
            ObjectType = objectType;
        }
    }

    public class AssignableTypesAttribute : Attribute
    {
        public SystemObjects[] Values { get; set; }

        public AssignableTypesAttribute(params SystemObjects[] values)
        {
            this.Values = values;
        }
    }
}
