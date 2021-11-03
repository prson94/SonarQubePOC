using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    public partial class FormController : BaseController
    {
        #region Issue Types

        [Route("IssueTypeRelation_AddFields"), NonNullableParameters]
        public JsonResult IssueTypeRelation_AddFields(int issueTypeId)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "IssueTypeID", FieldType = DataType.Hidden.ToString(), Value = issueTypeId.ToString() });

            string ignoreObjectTypeSQL = string.Empty;
            List<string> ignoreObjects = new List<string>
            {
                SystemObjects.ReferenceItemType.ToString()
            };

            if (ignoreObjects.Count > 0)
                ignoreObjectTypeSQL = $" AND T.Object not in ({string.Join(",", ignoreObjects.Select(o => "'" + o + "'"))})";           

            var availableTypes = Company.Query<SelectListItem>($@"select convert(nvarchar(36), T.Uid) as [Value], {QueryConstants.HighLevelTypeCaseStatement} + coalesce(P.[Path], T.[Name]) as [Text]
                from AssetType T
                cross apply dbo.GetAssetTypeTextPathById(T.ID, ' / ') P
                where not exists (select 1 from IssueTypeRelation where AssetTypeID = T.ID and IssueTypeID = @issueTypeId)
                {ignoreObjectTypeSQL}
                AND T.Class != {(int) AssetTypeClass.Diagram}
                order by 2", new { issueTypeId }).ToList();

            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "AssetTypeUid", Name = "Asset Type", FieldType = DataType.Lookup.ToString(), Items = availableTypes, Required = true });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("IssueType_EditFields"), NonNullableParameters]
        public JsonResult IssueType_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<IssueType>(id);

            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { FieldName = "Uid", FieldType = DataType.Hidden.ToString(), Value = a.uid.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Required = true, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250),  Value = a.Name });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("IssueType_AddFields"), NonNullableParameters]
        public JsonResult IssueType_AddFields()
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Required = true, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("Issue_AddFields")]
        public JsonResult Issue_AddFields(int issueTypeId)
        {
            var list = new List<EditableField>();
            var type = Company.GetById<IssueType>(issueTypeId);

            if (type == null)
            {
                throw new NotFoundException(FormControllerApiMessage.IssueType);
            }
            list.Add(new EditableField { FieldName = "IssueTypeID", FieldType = DataType.Hidden.ToString(), Value = issueTypeId.ToString() });

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.IssueType, issueTypeId).ToList(), 2, false);

            return Json(list, JsonRequestBehavior.AllowGet);
        }         

        #endregion
    }
}