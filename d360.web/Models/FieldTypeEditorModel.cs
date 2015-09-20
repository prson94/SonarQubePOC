using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using d360.core.entities;
using d360.core;

namespace d360.web.Models
{
    public class FieldTypeEditorModel
    {
        public FieldTypeEditorModel()
        {
            FieldIsUsed = false;
        }

        public bool FieldIsUsed { get; set; }

        public string FormUri { get; set; }

        public string FormMethod { get; set; }

        public string FormName { get; set; }

        public FieldType FieldType { get; set; }
        
        public List<EditableFieldItem> DataTypes 
        { 
            get 
            {
                DataType t = DataType.Boolean;
                return  t.GetDataTypeInfoList()
                    .Where(i => !i.ReadOnly)
                    .Select(i => new EditableFieldItem
                    {
                        Text = i.Description,
                        Value = i.Name
                    })
                    .OrderBy(i => i.Text)
                    .ToList();
            } 
        }

        public List<EditableFieldItem> Lookups
        {
            get;
            set;
        }
    }
}