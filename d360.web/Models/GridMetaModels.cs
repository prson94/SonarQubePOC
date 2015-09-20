using d360.core;
using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace d360.web.Models
{
    public class GridLayout
    {
        public GridLayout(List<FieldType> types)
        {
            Columns = new List<GridColumn>();
            Fields = new List<GridField>();

            types.ForEach(t => {
                var c = new GridColumn { datafield = t.Name, text = t.FriendlyName };
                var f = new GridField { name = t.Name };

                switch (t.Type)
                { 
                    case "Boolean":
                        f.type = "bool";
                        break;
                    case "Date":
                        c.cellsformat = "MMM d yyyy";
                        f.type = "date";
                        break;
                    case "DateTime":
                        c.cellsformat = "MMM d yyyy hh:mm:ss tt";
                        f.type = "date";
                        break;
                    case "Number":
                        f.type = "number";
                        break;
                    default:
                        f.type = "string";
                        break;
                }

                Columns.Add(c);
                Fields.Add(f);
            });
        }

        public List<GridColumn> Columns { get; set; }

        public List<GridField> Fields { get; set; }
    }

    public class GridField
    {
        public string name { get; set; }

        public string type { get; set; }
    }

    [DataContract]
    public class GridColumn 
    {
        public static string COLUMN_TYPE_NUMBER_READONLY = "number";
        public static string COLUMN_TYPE_CHECKBOX = "checkbox";
        public static string COLUMN_TYPE_NUMBER = "numberinput";
        public static string COLUMN_TYPE_DROPDOWN = "dropdownlist";
        public static string COLUMN_TYPE_COMBO = "combobox";
        public static string COLUMN_TYPE_DATE = "datetimeinput";
        public static string COLUMN_TYPE_STRING = "textbox";
        
        public static string FILTER_TYPE_CHECKBOX = "bool";
        public static string FILTER_TYPE_CHECKEDLIST = "checkedlist";
        public static string FILTER_TYPE_LIST = "list";
        public static string FILTER_TYPE_DATE = "date";
        public static string FILTER_TYPE_NUMBER = "number";
        public static string FILTER_TYPE_RANGE = "range";
        public static string FILTER_TYPE_STRING = "textbox";

        public GridColumn()
        {
            sortable = true;
            filterable = true;
            columntype = COLUMN_TYPE_STRING;
            filtertype = FILTER_TYPE_STRING;
            filteritems = new List<string>();
        }

        [DataMember]
        public string cellsformat { get; set; }

        [DataMember]
        public string datafield { get; set; }

        [DataMember]
        public string text { get; set; }

        [DataMember]
        public string width { get; set; }

        [DataMember]
        public bool sortable { get; set; }

        [DataMember]
        public bool filterable { get; set; }

        [DataMember]
        public string columntype { get; set; }

        [DataMember]
        public string filtertype { get; set; }

        [DataMember]
        public List<string> filteritems { get; set; }
    }
}