using System.Collections.Generic;
using System.Collections.Specialized;
using d360.core.entities;
using d360.core;
using System.Runtime.Serialization;
using System.Web.Mvc;
using System.Web;

namespace d360.web.Models
{
    [DataContract(Name = "artifacts", Namespace = constants.NAMESPACE)]
    public class ArtifactModelRequestList : List<ArtifactModelRequest> { }

    [DataContract(Name = "artifact", Namespace = constants.NAMESPACE)]
    public class ArtifactModelRequest : Dictionary<string, object> { }

    public class AttributeNode
    {
        public AttributeNode()
        {
            Children = new List<AttributeNode>();
        }

        public string AttributeType { get; set; }
        public int AttributeTypeID { get; set; }
        public int ID { get; set; }
        public int FusionIntersectID { get; set; }
        public string Text { get; set; }
        public int ObjectID { get; set; }
        public string ObjectType { get; set; }
        public bool IsFolderAttribute { get; set; }
        public List<AttributeNode> Children { get; set; }
    }

    public class ClaimsMatrixDisplayModel
    {
        public int ResponsibilityTypeID { get; set; }
        public List<ClaimsMatrixEditorItemModel> Items { get; set; }
    }

    public class CommentTag
    {
        public string Object { get; set; }
        public int ObjectID { get; set; }
    }

    public class CommentData
    {
        public string ObjectType { get; set; }
        public int? ObjectID { get; set; }
        public Comment Comment { get; set; }

        public List<CommentTag> Tags { get; set; }
    }

    public class CommentRequestData
    {
        public string ObjectType { get; set; }

        public int? ObjectID { get; set; }

        public int Skip { get; set; }

        public int Take { get; set; }

        public int DateFilter { get; set; }

        public int TypeFilter { get; set; }
    }

    public class DomainHierarchyItem
    {
        public DomainHierarchyItem()
        {
            expanded = true;
        }

        public string Type { get; set; }
        public int ID { get; set; }
        public string HierarchyID { get; set; }
        public string ParentHierarchyID { get; set; }
        public string Name { get; set; }
        public bool expanded { get; set; }
    }

    [DataContract(Namespace = constants.NAMESPACE)]
    public class FilterObjectItem
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Type { get; set; }
        [DataMember]
        public int ID { get; set; }
    }

    [DataContract(Namespace = constants.NAMESPACE)]
    public class DisplayField
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string FriendlyName { get; set; }
        [DataMember]
        public string Value { get; set; }
    }

    public class DynamicField
    {
        public string Name { get; set; }
        public string FriendlyName { get; set; }
        public string Description { get; set; }

        public string Value { get; set; }

        public bool IsLookup { get; set; }

        public int? Length { get; set; }
        public int? MaximumLength { get; set; }
        public int? MinimumLength { get; set; }
        public string Pattern { get; set; }

        public string Type { get; set; }
        public NameValueCollection Options { get; set; }
    }

    public class FieldLoader
    {
        //public Fields LoadFields(ICollection<FieldType> fieldTypes, XElement existingValues, IResourceService service)
        //{
        //    var fields = new Fields();
        //    foreach (var xf in fieldTypes)
        //    {
        //        var f = new DynamicField
        //        {
        //            FriendlyName = xf.FriendlyName,
        //            Description = xf.Description,
        //            IsLookup = !string.IsNullOrEmpty(xf.LookupObjectType),
        //            Name = xf.Name,
        //            Type = xf.Type.ToString(),
        //            Value = ""
        //        };

        //        if (existingValues != null)
        //        {
        //            if (existingValues.Element(xf.Name) != null)
        //            {
        //                f.Value = existingValues.Element(xf.Name).Value;
        //            }
        //        }

        //        f.Length = f.Length;
        //        f.MaximumLength = f.MaximumLength;
        //        f.MinimumLength = f.MinimumLength;
        //        f.Pattern = f.Pattern;

        //        if (f.IsLookup)
        //        {
        //            f.Options = service.LoadLookupList(xf);
        //        }

        //        fields.Add(f);
        //    }

        //    return fields;
        //}
        public List<Field> GetFormDynamicFieldValues(ICollection<FieldType> fieldTypes, FormCollection form, HttpServerUtilityBase Server)
        {
            var fields = new List<Field>();

            foreach (var ft in fieldTypes)
            {
                string value = "";

                switch (ft.Type)
                {
                    case "Html":
                        value = Server.HtmlDecode(form[ft.Name]);
                        break;
                    case "Link":
                        var rawLinkName = form[ft.Name + ".Name"];
                        var rawLinkUrl = form[ft.Name + ".Url"];
                        value = string.Format("{0}|{1}", rawLinkName, rawLinkUrl);
                        break;
                    case "UncLink":
                        var rawUncLinkName = form[ft.Name + "_Name"];
                        var rawUncLinkUrl = form[ft.Name + "_Url"];
                        value = string.Format("{0}|{1}", rawUncLinkName, rawUncLinkUrl);
                        break;
                    default:
                        value = form[ft.Name];
                        break;
                }

                if (!string.IsNullOrEmpty(value))
                    fields.Add(new Field { FieldTypeID = ft.ID, Value = value });
            }
            return fields;
        }

        public List<Field> GetFormDynamicFieldValues(SystemObjects type, int id, ICollection<FieldTypeWithRelation> fieldTypes, FormCollection form, HttpServerUtilityBase Server)
        {
            var fields = new List<Field>();

            foreach (var ft in fieldTypes)
            {
                string value = "";

                switch (ft.Type)
                {
                    case "Boolean":
                        value = form[ft.Name];
                        value = (value == "on").ToString();
                        break;
                    case "Html":
                        value = Server.HtmlDecode(form[ft.Name]);
                        break;
                    case "Link":
                        var rawLinkName = form[ft.Name + "_Name"];
                        var rawLinkUrl = form[ft.Name + "_Url"];
                        value = string.Format("{0}|{1}", rawLinkName, rawLinkUrl);
                        break;
                    case "UncLink":
                        var rawUncLinkName = form[ft.Name + "_Name"];
                        var rawUncLinkUrl = form[ft.Name + "_Url"];
                        value = string.Format("{0}|{1}", rawUncLinkName, rawUncLinkUrl);
                        break;
                    default:
                        value = Server.HtmlEncode(form[ft.Name]);
                        break;
                }

                if (!string.IsNullOrEmpty(value))
                    fields.Add(new Field { FieldTypeID = ft.ID, ObjectID = id, ObjectType = type.ToString(), Value = value });
            }
            return fields;
        }
    }

    public class Fields : List<DynamicField>
    {

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

    public class GridField
    {
        public string name { get; set; }

        public string type { get; set; }
    }

    public class GridLayout
    {
        public GridLayout(List<FieldType> types)
        {
            Columns = new List<GridColumn>();
            Fields = new List<GridField>();

            types.ForEach(t =>
            {
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

    [DataContract(Namespace = constants.NAMESPACE)]
    public class ReadOnlyField
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string FieldName { get; set; }

        [DataMember]
        public string Value { get; set; }

        [DataMember]
        public string FieldDescription { get; set; }

        [DataMember]
        public List<string> MultipleValues { get; set; }

        [DataMember]
        public int? Row { get; set; }

        [DataMember]
        public int? Column { get; set; }

        [DataMember]
        public string Group { get; set; }

        [DataMember]
        public string TooltipType { get; set; }

        [DataMember]
        public string TooltipContext { get; set; }

        [DataMember]
        public int? TooltipID { get; set; }

        [DataMember]
        public string TooltipUrl { get; set; }
    }

    [DataContract(Namespace = constants.NAMESPACE)]
    public class ReadOnlySection
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<ReadOnlyField> Fields { get; set; }
    }

    public class ReportOverlayModel : ObjectModel
    {
        public int ReportID { get; set; }
        public string ReportName { get; set; }
        public List<SelectListItem> ObjectTypes { get; set; }
    }

    public class ResponsibilityTypeHierarchy
    {
        public int StartID { get; set; }
        public string StartName { get; set; }
        public int? EndID { get; set; }
        public string EndName { get; set; }
    }

    [DataContract(Name = "Survey", Namespace = constants.NAMESPACE)]
    public class SurveyModel
    {
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public int ResourceID { get; set; }
        [DataMember]
        public string ResourceName { get; set; }
        [DataMember]
        public int PercentComplete { get; set; }
    }
}