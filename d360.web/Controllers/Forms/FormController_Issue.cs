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

            List<string> ignoreObjects = new List<string>();
            string ignoreObjectTypeSQL = string.Empty;
            if (!Community.IsFusionEnabled())
            {
                ignoreObjects.Add(SystemObjects.FusionType.ToString());
                ignoreObjects.Add(SystemObjects.FusionAttributeType.ToString());
                ignoreObjects.Add(SystemObjects.FusionQueryAttributeType.ToString());
            }

            if (ignoreObjects.Count > 0)
                ignoreObjectTypeSQL = $" AND T.Object not in ({string.Join(",", ignoreObjects.Select(o => "'" + o + "'"))})";

            var availableTypes = Company.Query<SelectListItem>($@"select T.ID as [Value], {QueryConstants.HighLevelTypeCaseStatement} + coalesce(FAT.TextPath, T.[Name]) as [Text]
                from AssetType T
                left join FusionAttributeType FAT on T.[Object] = 'FusionAttributeType' and FAT.ID = T.ObjectID
                where not exists (select 1 from IssueTypeRelation where AssetTypeID = T.ID and IssueTypeID = @issueTypeId)
                {ignoreObjectTypeSQL}
                order by 2", new { issueTypeId }).ToList();

            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "AssetTypeID", Name = "Asset Type", FieldType = DataType.Lookup.ToString(), Items = availableTypes, Required = true });

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
            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("IssueType_AddFields"), NonNullableParameters]
        public JsonResult IssueType_AddFields()
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString() });
            list.Add(new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("Issue_AddFields")]
        public JsonResult Issue_AddFields(int issueTypeId)
        {
            var list = new List<EditableField>();
            var type = Company.GetById<IssueType>(issueTypeId);

            if (type == null) throw new NotFoundException("issue type");

            list.Add(new EditableField { FieldName = "IssueTypeID", FieldType = DataType.Hidden.ToString(), Value = issueTypeId.ToString() });

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.IssueType, issueTypeId).ToList(), 2);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddIssue")]
        public JsonResult AddIssue(FormCollection form)
        {
            try
            {
                var issueTypeId = parseIntField(form, "IssueTypeID");
                var objectId = parseIntField(form, "ObjectID");
                var objectType = parseTextField(form, "ObjectType");
                var desc = parseTextField(form, "ProblemDesc");
                int commentDetailID = 0;


                var issueType = Company.GetById<IssueType>(issueTypeId);

                if (issueType == null) throw new NoFormDataException("issue type");

                //get the object name
                var obj = Company.GetObjectDetail(objectType, objectId);

                if (obj == null) throw new NoFormDataException("GetObject");

                if (this.IsWriteActionDescriptionEnabled())
                {
                    var relations = new List<CommentRelation>();
                    var comment = new Comment();

                    relations.Add(new CommentRelation { ObjectID = Company.CurrentResourceID, ObjectType = SystemObjects.Resource.ToString(), Date = DateTime.UtcNow });

                    comment.OwnerObjectType = SystemObjects.Resource.ToString();
                    comment.OwnerObjectID = Company.CurrentResourceID;
                    comment.CommentTypeID = CommentType.Issue;
                    comment.Body = desc ?? $"New {issueType.Name} Raised.";


                    //add relation to current artifact
                    relations.Add(new CommentRelation { ObjectType = objectType, ObjectID = objectId, Date = DateTime.UtcNow });

                    var dtl = Company.AddComment(comment, relations).FirstOrDefault(i => i.ID == comment.ID);
                    commentDetailID = dtl.ID;
                }


                //insert issue into issue table
                var model = new Issue
                {
                    CreatedBy = Company.CurrentResourceID,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow,
                    IssueTypeID = issueTypeId,
                    Object = objectType,
                    ObjectID = objectId,
                    ObjectType = obj.Type,
                    ObjectTypeID = obj.TypeID,
                    CommentID = commentDetailID
                };


                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Issue, model.ID, Company.GetFieldTypesByObject(SystemObjects.IssueType, issueTypeId).ToList(), form, Server);
                Company.SaveOrUpdate(model, fields);

                return jsonSuccess("Successfully created issue.", model.ID.ToString(), "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }

        }

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddIssueTypeRelation")]
        public JsonResult AddIssueTypeRelation(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("IssueType");

                var model = new IssueTypeRelation
                {
                    IssueTypeID = parseIntField(form, "IssueTypeID"),
                    AssetTypeID = parseIntField(form, "AssetTypeID")
                };

                Company.Add(model);

                return jsonSuccess("Issue Type allocation successfully added.", model.AssetTypeID.ToString(), "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpDelete, ValidateInput(false), Route("DeleteIssueTypeRelation"), NonNullableParameters]
        public JsonResult DeleteIssueTypeRelation(int issueTypeID, int assetTypeID)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (issueTypeID < 1 || assetTypeID < 1) throw new InvalidDataException("IssueTypeRelation");

                var relation = Company.IssueTypeRelations.Where(i => i.IssueTypeID == issueTypeID && i.AssetTypeID == assetTypeID).FirstOrDefault();
                Company.Delete(relation);

                return jsonSuccess("Issue Type allocation successfully deleted.", assetTypeID.ToString(), "delete", HttpStatusCode.OK);

            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddIssueType")]
        public JsonResult AddIssueType(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("IssueType");

                var model = new IssueType
                {
                    Name = parseTextField(form, "Name"),
                    Description = parseTextField(form, "Description"),
                    IsSystem = false,
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow
                };

                Company.Add(model);

                if (model.ID > 0)
                {
                    Company.Add(new FieldType
                    {
                        ObjectID = model.ID,
                        Object = SystemObjects.IssueType.ToString(),
                        IsListable = true,
                        IsRequired = true,
                        IsEditable = true,
                        FriendlyName = "Description",
                        Name = "ProblemDesc",
                        SortOrder = 1,
                        Type = DataType.Html.ToString()
                    });
                }

                return jsonSuccess(model.Name + " successfully created.", model.ID.ToString(), "add", HttpStatusCode.Created);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpPut, ValidateInput(false), Route("EditIssueType")]
        public JsonResult EditIssueType(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("issuetype");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<IssueType>(id);

                if (model == null) throw new NotFoundException("issuetype");

                model.Name = form["Name"];
                model.Description = form["Description"];
                model.UpdatedBy = Company.CurrentResourceID;
                model.UpdatedOn = DateTime.UtcNow;

                Company.SaveOrUpdate(model);

                return jsonSuccess("Item successfully updated.", id.ToString(), "edit", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        [HttpDelete, Route("DeleteIssueType")]
        public JsonResult DeleteIssueType(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("issue type");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<IssueType>(id);
                if (model == null) throw new NotFoundException("issue type");

                var typeRelations = Company.IssueTypeRelations.Where(i => i.IssueTypeID == id).ToList();
                Company.IssueTypeRelations.RemoveRange(typeRelations);

                var customFields = Company.FieldTypes.Where(x => x.Object == SystemObjects.IssueType.ToString() && x.ObjectID == id);
                Company.FieldTypes.RemoveRange(customFields);
                Company.Delete<IssueType>(i => i.ID == id);

                return jsonSuccess("Item successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }


        #endregion
    }
}