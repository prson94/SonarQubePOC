using d360.core;
using d360.core.entities;
using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.Web.Mvc;

namespace d360.web.Models
{
    public class CountModel
    {
        public string Name { get; set; }
        public int? New { get; set; }
        public int? Version { get; set; }
        public string Step { get; set; }
        public int StepId { get; set; }
        public int? Total { get; set; }
        public int? Id { get; set; }
    }

    public class DetailReadOnlyModel
    {
        public DetailReadOnlyModel()
        {
            rows = new List<DetailReadOnlyRowModel>();
        }

        public int ObjectID { get; set; }
        public string Object { get; set; }
        public int ObjectTypeID { get; set; }
        public string ObjectType { get; set; }
        public string AssetName { get; set; }
        public string AssetTypeName { get; set; }
        public Guid AssetUid { get; set; }
        public long AssetID { get; set; }
        public bool HasResponsibilityReadAccess { get; set; }
        public bool CanEdit { get; set; }
        public List<dynamic> Scores { get; set; }
        public int columns { get; set; }
        public List<DetailReadOnlyRowModel> rows { get; set; }
    }

    public class LookupDataReadOnlyModel
    {
        public int FieldTypeId { get; set; }
        public long Value { get; set; }
        public long AssetId { get; set; }
        public string Url { get; set; }
        public string ColorJson { get; set; }
        public string DisplayText { get; set; }
    }

    public class DetailReadOnlyRowModel
    {
        public DetailReadOnlyRowModel()
        {
            FirstColumnFields = new List<ReadOnlyField>();
            SecondColumnFields = new List<ReadOnlyField>();
            Category = null;
        }

        public int columns { get; set; }
        public List<ReadOnlyField> FirstColumnFields { get; set; }
        public List<ReadOnlyField> SecondColumnFields { get; set; }

        public string Category { get; set; }
    }

    public class FieldLoader
    {
        public List<Field> GetFormDynamicFieldValues(SystemObjects type, int id, ICollection<FieldType> fieldTypes, FormCollection form, HttpServerUtilityBase Server = null, bool ignoreFieldIfNull = true)
        {
            var fields = new List<Field>();

            foreach (var ft in fieldTypes)
            {
                if (ft.Type != DataType.ComplexRelationLookup.ToString())
                {
                    string value = "";

                    if (form.AllKeys.Contains(ft.Name) || form.AllKeys.Contains(ft.Name + "_Name") || form.AllKeys.Contains(ft.Name + "_Url"))
                    {
                        switch (ft.Type)
                        {
                            case "Boolean":
                                value = form[ft.Name];
                                if (string.IsNullOrEmpty(value))
                                    value = null;
                                else
                                    value = (value == "on" || (value ?? "").ToUpper() == "TRUE").ToString();
                                break;
                            case "Html":
                                value = Server != null ? Server.HtmlDecode(form[ft.Name]) : HttpUtility.HtmlDecode(form[ft.Name]);
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
                            case "Date":
                                var stringDate = form[ft.Name];
                                DateTime dateVal = DateTime.MinValue;
                                //throw out any time piece sent in
                                if (DateTime.TryParse(stringDate, out dateVal))
                                {
                                    value = dateVal.ToShortDateString();
                                }
                                break;
                            case "DateTime":
                                var stringDateTime = form[ft.Name];
                                DateTime dateTimeVal = DateTime.MinValue;
                                //throw out any time piece sent in
                                if (DateTime.TryParse(stringDateTime, out dateTimeVal))
                                {
                                    value = dateTimeVal.ToString("s"); //already in utc
                                }
                                break;
                            case "Relationship":
                                //this will be handled differently.
                                break;
                            default:
                                value = form[ft.Name];
                                break;
                        }

                        if (ignoreFieldIfNull)
                        {
                            if (!string.IsNullOrEmpty(value))
                                fields.Add(new Field { FieldTypeID = ft.ID, ObjectID = id, ObjectType = type.ToString(), Value = value });
                        }
                        else
                        {
                            fields.Add(new Field { FieldTypeID = ft.ID, ObjectID = id, ObjectType = type.ToString(), Value = value });
                        }
                    }
                }
            }

            return fields;
        }
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
        [DataMember]
        public Guid Uid { get; set; }
    }

    public class ListUidItem
    {
        public string title { get; set; }
        public Guid? value { get; set; }
    }

    [DataContract(Namespace = constants.NAMESPACE)]
    public class ReadOnlyFieldValue
    {
        [DataMember]
        public string Value { get; set; }
        [DataMember]
        public string TooltipType { get; set; }

        [DataMember]
        public string TooltipContext { get; set; }

        [DataMember]
        public long? TooltipID { get; set; }
        [DataMember]
        public long? CreatedBy { get; set; }

        [DataMember]
        public string TooltipUrl { get; set; }

        [DataMember]
        public Guid uid { get; set; }
    }

    public class ReadOnlyFieldValueComparer : IEqualityComparer<ReadOnlyFieldValue>
    {

        public bool Equals(ReadOnlyFieldValue x, ReadOnlyFieldValue y)
        {
            if (Object.ReferenceEquals(x, y)) return true;

            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;

            return x.Value == y.Value;
        }



        public int GetHashCode(ReadOnlyFieldValue obj)
        {
            if (Object.ReferenceEquals(obj, null)) return 0;

            int value = obj.Value == null ? 0 : obj.Value.GetHashCode();

            return value;
        }
    }

    [DataContract(Namespace = constants.NAMESPACE)]
    public class ReadOnlyField
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string FieldName { get; set; }

        [DataMember]
        public string ScriptProperty { get; set; }

        [DataMember]
        public string Value { get; set; }

        [DataMember]
        public List<ReadOnlyFieldValue> Values { get; set; }

        [DataMember]
        public string FieldDescription { get; set; }

        [DataMember]
        public ComplexLookupType ComplexLookupType { get; set; } = ComplexLookupType.None;

        [DataMember]
        public string LookupObjectType { get; set; }

        [DataMember]
        public int LookupObjectID { get; set; }

        [DataMember]
        public int LookupFieldTypeID { get; set; }

        [DataMember]
        public int LookupType { get; set; }

        [DataMember]
        public bool HideHeader { get; set; }

        [DataMember]
        public bool HideFooter { get; set; }

        [DataMember]
        public bool HideFilter { get; set; }

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

        [DataMember]
        public string DataType { get; set; }

        [DataMember]
        public bool ShowIfEmpty { get; set; }

        /// <summary>
        /// Gets or sets if field identifies object.
        /// Is not-null only for dynamic fields of assets
        /// </summary>
        [DataMember]
        public bool? IsPartOfKey { get; set; }
    }

    [DataContract]
    public class ResponsibilityTypeRelationViewModel
    {
        public void LoadPermissionsFromMask()
        {
            if (PermissionsBitMask > 0)
            {
                var rawList = Permission.DeleteAsset.GetList();
                Permissions.AddRange(rawList.Where(i => (PermissionsBitMask & (int)i.ID) == (int)i.ID));
            }
        }

        [DataMember]
        public int ResponsibilityTypeID { get; set; }
        [DataMember]
        public string ResponsibilityTypeName { get; set; }
        [DataMember]
        public string AssetTypeName { get; set; }
        [DataMember]
        public int AssetTypeID { get; set; }
        [DataMember]
        public AssetTypeClass Class { get; set; }
        [DataMember]
        public string ClassName { get { return Class.GetDisplayName(); } }
        [DataMember]
        public string ObjectType { get; set; }
        [DataMember]
        public int ObjectID { get; set; }
        [DataMember]
        public int PermissionsBitMask { get; set; }
        [DataMember]
        public List<PermissionInfo> Permissions { get; set; } = new List<PermissionInfo>();
    }

    [DataContract(Name = "ObjectSurvey", Namespace = constants.NAMESPACE)]
    public class ObjectSurveyModel
    {
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public string Name { get; set; }
    }

    public class ObjectSurveyQuestionValuesModel
    {
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Value { get; set; }
    }

    public class SurveyResponseModel
    {
        public List<SurveyResponseQuestionModel> Questions { get; set; }
    }

    public class SurveyResponseQuestionModel
    {
        public int Id { get; set; }

        public string Name { get; set; }


        public string Comments { get; set; }

        public List<SurveyResponseValueModel> Values { get; set; }
    }

    public class SurveyResponseValueModel
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }

        public bool IsChecked { get; set; }
    }

    [DataContract]
    public class TagSuggestionModel
    {
        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public string TextPath { get; set; }

        [DataMember]
        public string Url { get; set; }

        [DataMember]
        public string ObjectTypeName { get; set; }

        [DataMember]
        public string IconForeColor { get; set; }

        [DataMember]
        public string IconBackColor { get; set; }

        [DataMember]
        public string Displayobject { get; set; }
        [DataMember]
        public Guid AssetUid { get; set; }
    }

    public class PowerBIReportViewModel
    {
        public Microsoft.PowerBI.Api.V2.Models.Report Report { get; set; }

        public string AccessToken { get; set; }
    }
}