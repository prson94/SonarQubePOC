using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using d360.core;
using System.Xml.Linq;
using System.Web.Mvc;
using d360.core.entities;
using System.Collections.Specialized;

namespace d360.web.Models
{
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
                        value = form[ft.Name];
                        break;
                }
                fields.Add(new Field { FieldTypeID = ft.ID, ObjectID = id, ObjectType = type.ToString(), Value = value });
            }
            return fields;
        }
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

    public class Fields : List<DynamicField>
    {

    }
}