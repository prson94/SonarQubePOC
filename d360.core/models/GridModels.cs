using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.core.Models
{

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
            columngroup = null;
            columntype = COLUMN_TYPE_STRING;
            columnWidth = null;
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
        public bool sortable { get; set; }

        [DataMember]
        public bool filterable { get; set; }

        [DataMember]
        public string columngroup { get; set; }

        [DataMember]
        public string columntype { get; set; }

        [DataMember]
        public string filtertype { get; set; }

        [DataMember]
        public List<string> filteritems { get; set; }

        [DataMember]
        public string objectfield { get; set; }

        [DataMember]
        public string objectidfield { get; set; }

        [DataMember]
        public string urlfield { get; set; }

        [DataMember]
        public string uidfield { get; set; }

        [DataMember]
        public string contextfield { get; set; }

        [DataMember]
        public string description { get; set; }

        [DataMember]
        public int? columnWidth { get; set; }

        [DataMember]
        public int parentFieldTypeID { get; set; }

        [DataMember]
        public bool canHaveMultipleFilters { get; set; }

        [DataMember]
        public string apiName { get; set; }

        [DataMember]
        public string fieldType { get; set; }
    }

    [DataContract]
    public class GridFilterColumn : GridColumn
    {
        public GridFilterColumn()
        {

        }
        public GridFilterColumn(GridColumn val)
        {
            cellsformat = val.cellsformat;
            datafield = val.datafield;
            text = val.text;
            sortable = val.sortable;
            filterable = val.filterable;
            columntype = val.columntype;
            filtertype = val.filtertype;
            filteritems = val.filteritems;
            parentFieldTypeID = val.parentFieldTypeID;
            canHaveMultipleFilters = val.canHaveMultipleFilters;
            fieldType = val.fieldType;
            apiName = val.apiName;
        }

        [DataMember]
        public bool hiddenfield { get; set; }

        [DataMember]
        public string id { get; set; }
    }

    public class GridField
    {
        public string name { get; set; }

        public string type { get; set; }

        public string apiName { get; set; }

        public string defaultFilter { get; set; }

        public int sortOrder { get; set; }
    }

}
