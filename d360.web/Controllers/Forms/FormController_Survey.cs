using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;

using Resources;

namespace d360.web.Controllers
{
    public partial class FormController : BaseController
    {
        #region SurveyType

        #region Field Generation

        [Route("SurveyType_AddFields")]
        public JsonResult SurveyType_AddFields()
        {
            var list = new List<EditableField>();

            var items = new List<SelectListItem>();

            //artifacts
            items.AddRange(Company.AssetTypes.Where(x => x.Object == SystemObjects.ArtifactType.ToString()).OrderBy(i => i.Class).ThenBy(i => i.Name).Select(i => new { i.uid, i.Name, i.Class }).ToList().Select(i => new SelectListItem { Text = $"{i.Class.GetDisplayName()} :: {i.Name}", Value = i.uid.ToString() }));

            //models
            items.AddRange(Company.AssetTypes.Where(x => x.Object == SystemObjects.TaxonomyType.ToString()).OrderBy(i => i.Name).Select(i => new { i.uid, i.Name }).ToList().Select(i => new SelectListItem { Text = string.Format("Model Type :: {0}", i.Name), Value = i.uid.ToString() }));

            //rules
            items.AddRange(Company.AssetTypes.Where(x => x.Object == SystemObjects.RuleType.ToString()).OrderBy(i => i.Name).Select(i => new { i.uid, i.Name }).ToList().Select(i => new SelectListItem { Text = string.Format("Rule Type :: {0}", i.Name), Value = i.uid.ToString() }));

            var orderedItems = items.OrderBy(x => x.Text).ToList();

            list.Add(new EditableField 
            {
                Row = 1,
                Column = 1,
                Required = true, 
                FieldName = "Name",
                Name = "Name",
                FieldType = DataType.Text.ToString(), 
                Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) 
            });
            
            list.Add(new EditableField 
            {
                Row = 1, 
                Column = 2,
                Required = true,
                FieldName = "AssetTypeUid",
                Name = "Assign Survey To",
                FieldType = DataType.Lookup.ToString(), 
                Items = orderedItems 
            });
            
            list.Add(new EditableField 
            { 
                Row = 2,
                Column = 1,
                Required = true,
                FieldName = "ValidForDays",
                Name = "# of Days before user can retake",
                FieldType = DataType.Number.ToString() 
            });
            
            list.Add(new EditableField 
            {
                Row = 3, 
                Column = 1,
                Required = false,
                FieldName = "Description",
                Name = "Description",
                FieldType = DataType.Html.ToString() 
            });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">SurveyTypeID</param>
        [Route("SurveyType_EditFields"), NonNullableParameters]
        public JsonResult SurveyType_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<SurveyType>(id);

            list.Add(new EditableField 
            { 
                FieldName = "Uid",
                FieldType = DataType.Hidden.ToString(), 
                Value = a.Uid.ToString() 
            });
            
            list.Add(new EditableField 
            { 
                Row = 1,
                Column = 1, 
                Required = true,
                FieldName = "Name",
                Name = "Name", 
                FieldType = DataType.Text.ToString(),
                Value = a.Name, Validations = checkAndAddValidation(fieldType: "Text",
                                                                    friendlyName: "Name",
                                                                    required: true,
                                                                    pattern: "",
                                                                    minLength: 1,
                                                                    maxLength: 250) 
            });
           
            list.Add(new EditableField
            { 
                Row = 1,
                Column = 2, 
                Required = true, 
                FieldName = "ValidForDays",
                Name = "# of Days before user can retake",
                FieldType = DataType.Number.ToString(), 
                Value = a.ValidForDays.ToString() 
            });
           
            list.Add(new EditableField 
            { 
                Row = 2, 
                Column = 1,
                Required = false, 
                FieldName = "Description", 
                Name = "Description",
                FieldType = DataType.Html.ToString(), 
                Value = a.Description
            });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #endregion
    }
}
