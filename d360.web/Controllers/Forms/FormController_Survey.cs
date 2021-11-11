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
using System.Linq;
using System.Net;
using System.Web.Mvc;

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
            items.AddRange(Company.AssetTypes.Where(x => x.Object == SystemObjects.ArtifactType.ToString()).OrderBy(i => i.Class).ThenBy(i => i.Name).Select(i => new { i.Object, i.ObjectID, i.Name, i.Class }).ToList().Select(i => new SelectListItem { Text = $"{i.Class.GetDisplayName()} :: {i.Name}", Value = $"{i.Object}|{i.ObjectID}" }));

            //models
            items.AddRange(Company.AssetTypes.Where(x => x.Object == SystemObjects.TaxonomyType.ToString()).OrderBy(i => i.Name).Select(i => new { ID = i.ObjectID, i.Name }).ToList().Select(i => new SelectListItem { Text = string.Format("Model Type :: {0}", i.Name), Value = string.Format("{0}|{1}", SystemObjects.TaxonomyType.ToString(), i.ID) }));

            //rules
            items.AddRange(Company.AssetTypes.Where(x => x.Object == SystemObjects.RuleType.ToString()).OrderBy(i => i.Name).Select(i => new { ID = i.ObjectID, i.Name }).ToList().Select(i => new SelectListItem { Text = string.Format("Rule Type :: {0}", i.Name), Value = string.Format("{0}|{1}", SystemObjects.RuleType.ToString(), i.ID) }));

            var orderedItems = items.OrderBy(x => x.Text).ToList();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Object", Name = "Assign Survey To", FieldType = DataType.Lookup.ToString(), Items = orderedItems });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "ValidForDays", Name = "# of Days before user can retake", FieldType = DataType.Number.ToString() });
            list.Add(new EditableField { Row = 3, Column = 1, Required = false, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString() });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">SurveyTypeID</param>
        [Route("SurveyType_EditFields"), NonNullableParameters]
        public JsonResult SurveyType_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<SurveyType>(id);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });            
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "ValidForDays", Name = "# of Days before user can retake", FieldType = DataType.Number.ToString(), Value = a.ValidForDays.ToString() });
            list.Add(new EditableField { Row = 2, Column = 1, Required = false, FieldName = "Description", Name = "Description", FieldType = DataType.Html.ToString(), Value = a.Description });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddSurveyType")]
        public JsonResult AddSurveyType(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.SurveyType);
                }
                var otVal = form["Object"].Split('|').ToList();
                var ot = (SystemObjects)Enum.Parse(typeof(SystemObjects), otVal[0]);
                var oid = int.Parse(otVal[1]);                

                var model = new SurveyType
                {
                    Name = parseTextField(form, "Name"),
                    Object = ot.ToString(),
                    ObjectID = oid,
                    ValidForDays = parseNullableIntField(form, "ValidForDays", 1).GetValueOrDefault(1),
                    Description = parseTextField(form,"Description")
                };
                Company.Add(model);

                return jsonSuccess(string.Format(ApiMessages.SucessfullyCreated, model.Name), model.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpDelete, Route("DeleteSurveyType")]
        public JsonResult DeleteSurveyType(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.SurveyType);
                }
                var id = parseIntField(form, "ID");
                // delete this surveys questions..

                Company.Delete<Question>(i => i.Survey.SurveyTypeID == id);
                Company.Delete<Survey>(i => i.SurveyTypeID == id);

                Company.Delete<QuestionType>(i => i.SurveyTypeID == id);
                Company.Delete<SurveyType>(i => i.ID == id);

                return jsonSuccess(string.Format(ApiMessages.SucessfullyRemoved,FormControllerApiMessage.Item), id.ToString(), "delete", HttpStatusCode.OK);
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

        [HttpPut, ValidateInput(false), Route("EditSurveyType")]
        public JsonResult EditSurveyType(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.SurveyType);
                }
                var id = parseIntField(form, "ID");
                var model = Company.GetById<SurveyType>(id);
                if (model == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.SurveyType);
                }
                model.Name = parseTextField(form, "Name");
                model.ValidForDays = parseNullableIntField(form, "ValidForDays", 1).GetValueOrDefault(1);
                model.Description = parseTextField(form, "Description");

                Company.Update(model);

                return jsonSuccess(string.Format(ApiMessages.SucessfullyUpdated, model.Name), id.ToString(), "edit", HttpStatusCode.OK);
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

        #endregion

        #region QuestionType

        #region JSON Feeds

        [Route("QuestionType_FormData"), NonNullableParameters]
        public JsonNetResult QuestionType_FormData(int surveyTypeID, int id = 0)
        {
            QuestionType qt = null;
            List<QuestionTypeItemEditorModel> items = null;

            var options = QuestionDisplayStyle.Radio.GetResponseTypeDisplayStyleInfoList().Where(x => x.ID != QuestionDisplayStyle.Rating).Select(i => new KnockoutDisplayItem { title = i.Description, value = ((int)i.ID).ToString() });

            if (id > 0)
            {
                qt = Company.GetById<QuestionType>(id, i => i.QuestionTypeOptions);

                if (qt.QuestionTypeOptions != null)
                {
                    if (qt.QuestionTypeOptions.Count > 0)
                    {
                        items = new List<QuestionTypeItemEditorModel>();
                        foreach (var i in qt.QuestionTypeOptions)
                        {
                            items.Add(new QuestionTypeItemEditorModel
                            {
                                ID = i.ID,
                                Name = i.Name,
                                Value = i.Value
                            });
                        }
                    }
                }
            }
            else
            {
                qt = new QuestionType { Name = "", DisplayStyle = QuestionDisplayStyle.Radio, SurveyTypeID = surveyTypeID, Description = "" };
            }

            return new JsonNetResult
            {
                Data = new QuestionTypeEditorModel
                {
                    Name = qt.Name,
                    Description = qt.Description,
                    DisplayStyle = qt.DisplayStyle,
                    SurveyTypeID = surveyTypeID,
                    DisplayStyleOptions = options.ToList(),
                    ID = id,
                    Items = items,
                    LimitedChangesOnly = false
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #endregion

        #region Form Get/Post

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddQuestionType")]
        public JsonResult AddQuestionType(QuestionTypeEditorModel model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var val = model.Validation();

                if (!val.Valid)
                {
                    throw new ConflictException(FormControllerApiMessage.ErrorOccurred, val.Message);
                }

                var qt = new QuestionType
                {
                    Name = model.Name,
                    SurveyTypeID = model.SurveyTypeID,
                    DisplayStyle = model.DisplayStyle,
                    Description = model.Description,
                    QuestionTypeOptions = new List<QuestionTypeOption>()
                };

                foreach (var item in model.Items)
                {
                    var itemVal = item.Validation();
                    if (itemVal.Valid)
                    {
                        qt.QuestionTypeOptions.Add(new QuestionTypeOption { Name = item.Name, Value = item.Value });
                    }
                }

                Company.Add(qt);

                return jsonSuccess(string.Format(ApiMessages.SucessfullyCreated,FormControllerApiMessage.SurveyQuestion), qt.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpDelete, Route("DeleteQuestionType")]
        public JsonResult DeleteQuestionType(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.ResponseType);
                }
                var id = parseIntField(form, "ID");
                Company.Delete<QuestionType>(i => i.ID == id);

                return jsonSuccess(string.Format(ApiMessages.SucessfullyRemoved,FormControllerApiMessage.SurveyQuestion), id.ToString(), "delete", HttpStatusCode.OK);
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

        [HttpPut, ValidateInput(false), Route("EditQuestionType")]
        public JsonResult EditQuestionType(QuestionTypeEditorModel model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var val = model.Validation();

                if (!val.Valid)
                {
                    throw new ConflictException(FormControllerApiMessage.ErrorOccurred, val.Message);
                }

                var qt = Company.GetById<QuestionType>(model.ID, i => i.QuestionTypeOptions);

                if (qt == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.Question);
                }
                qt.Name = model.Name;
                qt.DisplayStyle = model.DisplayStyle;
                qt.Description = model.Description;

                //Process new and updated options.
                foreach (var item in model.Items)
                {
                    var itemVal = item.Validation();
                    if (itemVal.Valid)
                    {
                        if (item.ID > 0)
                        {
                            if (qt.QuestionTypeOptions.Any(i => i.ID == item.ID))
                            {
                                qt.QuestionTypeOptions.Single(i => i.ID == item.ID).Name = item.Name;
                                qt.QuestionTypeOptions.Single(i => i.ID == item.ID).Value = item.Value;
                            }
                        }
                        else
                        {
                            qt.QuestionTypeOptions.Add(new QuestionTypeOption { Name = item.Name, Value = item.Value });
                        }
                    }
                }

                //Process deleted options.
                var IDs = new List<int>();
                foreach (var item in qt.QuestionTypeOptions)
                {
                    if (!model.Items.Any(i => i.ID == item.ID))
                    {
                        IDs.Add(item.ID);
                    }
                }

                foreach (var id in IDs)
                {
                    var qto = qt.QuestionTypeOptions.Single(i => i.ID == id);
                    Company.QuestionTypeOptions.Remove(qto);
                }

                Company.Update(qt);

                return jsonSuccess(string.Format(ApiMessages.SucessfullyUpdated,FormControllerApiMessage.SurveyQuestion), qt.ID.ToString(), "update", HttpStatusCode.OK);
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

        #endregion
    }
}