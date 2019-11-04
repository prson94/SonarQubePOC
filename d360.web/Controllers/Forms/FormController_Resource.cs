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
        #region Resource

        string passwordRegex = Validation.Password_Regex;
        string passwordRegexMessage = Validation.Password_Requirements;

        #region Field Generation

        /// <param name="id">ResourceTypeID</param>
        [Route("Resource_AddFields"), NonNullableParameters]
        public JsonResult Resource_AddFields(int id)
        {
            var list = new List<EditableField>();

            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var stateList = CompanyResourceState.Active.GetList().Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList();

            list.Add(new EditableField { FieldName = "ResourceTypeID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "FirstName", Name = "First Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "First Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "LastName", Name = "Last Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Last Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Email", Name = "Email/Username", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Email", true, "", 1, 500) });//@"^([A-Za-z0-9_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})$", null, null, "be an email address") });
            list.Add(new EditableField { Row = 2, Column = 2, Required = true, FieldName = "Password", Name = "Password", FieldType = DataType.Password.ToString(), Validations = checkAndAddValidation("Text", "Password", true, passwordRegex, null, null, passwordRegexMessage) });
            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "IsAdministrator", Name = "Administrator?", FieldType = DataType.Boolean.ToString() });
            list.Add(new EditableField { Row = 3, Column = 2, Required = true, FieldName = "State", Name = "Active?", FieldType = DataType.Lookup.ToString(), Items = stateList, Value = ((int)CompanyResourceState.Active).ToString() });

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.ResourceType, id).ToList(), 5);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ResourceID</param>
        [Route("Resource_EditFields"), NonNullableParameters]
        public JsonResult Resource_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Community.GetById<Resource>(id, i => i.CompanyResources);

            var stateList = CompanyResourceState.Active.GetList().Select(i => new SelectListItem { Text = i.Name, Value = ((int)i.ID).ToString() }).ToList();
            var cr = a.CompanyResources.Single(i => i.CompanyID == Company.CurrentCompanyID);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "FirstName", Name = "First Name", FieldType = DataType.Text.ToString(), Value = a.FirstName, Validations = checkAndAddValidation("Text", "First Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "LastName", Name = "Last Name", FieldType = DataType.Text.ToString(), Value = a.LastName, Validations = checkAndAddValidation("Text", "Last Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Email", Name = "Email/Username", FieldType = DataType.Text.ToString(), Value = a.Email, Validations = checkAndAddValidation("Text", "Email", true, "", 1, 500) });
            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "IsAdministrator", Name = "Administrator?", FieldType = DataType.Boolean.ToString(), Value = cr.IsAdministrator.ToString() });
            list.Add(new EditableField { Row = 3, Column = 2, Required = true, FieldName = "State", Name = "Active?", FieldType = DataType.Lookup.ToString(), Items = stateList, Value = ((int)cr.State).ToString() });

            list = (
                loadDynamicFields(
                    SystemObjects.Resource.ToString(),
                    id,
                    list,
                    Company.GetFieldTypesByObject(SystemObjects.ResourceType, a.ResourceTypeID).ToList(),
                    Company.GetFieldRelationsByObject(SystemObjects.Resource, id).ToList(),
                    4
                )
            );

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("Resource_EditMyInfoFields")]
        public JsonResult Resource_EditMyInfoFields()
        {
            var list = new List<EditableField>();
            var id = Company.CurrentResourceID;
            var a = Community.GetById<Resource>(id);

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "FirstName", Name = "First Name", FieldType = DataType.Text.ToString(), Value = a.FirstName, Validations = checkAndAddValidation("Text", "First Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "LastName", Name = "Last Name", FieldType = DataType.Text.ToString(), Value = a.LastName, Validations = checkAndAddValidation("Text", "Last Name", true, "", 1, 250) });

            list = (
                loadDynamicFields(
                    SystemObjects.Resource.ToString(),
                    id,
                    list,
                    Company.GetFieldTypesByObject(SystemObjects.ResourceType, a.ResourceTypeID).ToList(),
                    Company.GetFieldRelationsByObject(SystemObjects.Resource, id).ToList(),
                    2
                )
            );

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("Resource_ChangeMyPasswordFields")]
        public JsonResult Resource_ChangeMyPasswordFields()
        {
            var list = new List<EditableField>();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "CurrentPassword", Name = "Current Password", FieldType = DataType.Password.ToString(), Validations = checkAndAddValidation("Text", "Current Password", true, "", 7, 25) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "NewPassword", Name = "New Password", FieldType = DataType.Password.ToString(), Validations = checkAndAddValidation("Text", "New Password", true, passwordRegex, null, null, passwordRegexMessage) });
            list.Add(new EditableField { Row = 2, Column = 2, Required = true, FieldName = "ConfirmNewPassword", Name = "Confirm New Password", FieldType = DataType.Password.ToString(), Validations = checkAndAddValidation("Text", "Confirm New Password", true, passwordRegex, null, null, passwordRegexMessage) });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddResource")]
        public JsonResult AddResource(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("resource");

                int typeID = 1;
                var email = form["Email"].Trim();

                var a = Community.Filter<Resource>(i => i.Email == email).FirstOrDefault();

                var id = 0;

                // Only add resource account if it does not already exist.
                if (a == null)
                {
                    a = new Resource
                    {
                        ResourceTypeID = typeID,
                        FirstName = parseNameField(form, "FirstName"),
                        LastName = parseNameField(form, "LastName"),
                        Email = parseTextField(form, "Email"),
                        Username = parseTextField(form, "Email"),
                        Password = "temp"
                    };

                    Community.Add(a);

                    id = a.ID;
                    Community.ChangePassword(a.ID, "", form["Password"]);
                }
                else
                {
                    id = a.ID;
                    var globalResource = Company.Filter<GlobalReportingResource>(i => i.ResourceID == id).FirstOrDefault();
                    if (globalResource != null && globalResource.State != CompanyResourceState.Deleted)
                    {
                        throw new ConflictException("Error", "The specified email address / username is already in use.");
                    }
                }

                var firstName = parseNameField(form, "FirstName");
                var lastName = parseNameField(form, "LastName");
                var isAdmin = parseBooleanField(form, "IsAdministrator");
                var state = parseEnumField<CompanyResourceState>(form, "State");
                var companyResource = Community.Filter<CompanyResource>(i => i.CompanyID == Community.CurrentCompanyID && i.ResourceID == id).FirstOrDefault();

                if (companyResource == null)
                {
                    companyResource = new CompanyResource
                    {
                        CompanyID = Company.CurrentCompanyID,
                        IsAdministrator = isAdmin,
                        ResourceID = id,
                        State = state
                    };
                    Community.Add(companyResource);
                }
                else
                {
                    companyResource.IsAdministrator = isAdmin;
                    companyResource.State = state;
                    Community.Update(companyResource);
                }

                if (!GetCompanyResources().Any(i => i.ResourceID == a.ID))
                {
                    GlobalReportingResource gr = new GlobalReportingResource
                    {
                        IsAdministrator = isAdmin,
                        ResourceID = id,
                        Email = a.Email,
                        LastName = lastName,
                        FirstName = firstName,
                        State = state,
                        UpdatedOn = DateTime.UtcNow,
                        Uid = a.Uid
                    };

                    Company.Add(gr);
                }
                else
                {
                    GlobalReportingResource gr = Company.Filter<GlobalReportingResource>(i => i.ResourceID == id).FirstOrDefault();

                    gr.FirstName = firstName;
                    gr.LastName = lastName;
                    gr.Email = a.Email;
                    gr.IsAdministrator = isAdmin;
                    gr.State = state;
                    gr.UpdatedOn = DateTime.UtcNow;

                    Company.Update(gr);
                }

                // Dynamic fields
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Resource, a.ID, Company.GetFieldTypesByObject(SystemObjects.ResourceType, typeID).ToList(), form, Server);
                Company.AddOrUpdateFields(fields);

                return jsonSuccess("User successfully created.", a.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("ResetResourcePassword")]
        public JsonResult ResetResourcePassword(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);


                if (!form.HasKeys()) throw new NoFormDataException("resource");

                var id = parseIntField(form, "ID");
                var model = Community.GetById<Resource>(id);

                if (model == null) throw new NotFoundException("resource");

                //valid user at this point generate a password
                ResetResourcePassword(model.ID, model.FirstName, model.Email, model.FormatDisplayName());

                return jsonSuccess("Users password has been successfully updated!", id.ToString(), "reset", HttpStatusCode.OK);

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

        [HttpDelete, Route("DeleteResource")]
        public JsonResult DeleteResource(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("resource");

                var id = parseIntField(form, "ID");

                if (id <= 0) throw new NotFoundException("Resource with ID less than or equal to 0 cannot be removed.");

                var model = Community.Filter<CompanyResource>(i => i.ResourceID == id && i.CompanyID == Company.CurrentCompanyID).SingleOrDefault();
                var globalResource = Company.Filter<GlobalReportingResource>(x => x.ResourceID == id).SingleOrDefault();
                if (model == null) throw new NotFoundException("resource");
                if (globalResource == null) throw new NotFoundException("resource");
                model.State = CompanyResourceState.Deleted;
                globalResource.State = CompanyResourceState.Deleted;

                Community.Update<CompanyResource>(model);
                Company.Update<GlobalReportingResource>(globalResource);

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

        [HttpDelete, Route("DeleteResourceByID"), NonNullableParameters]
        public JsonResult DeleteResourceByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteResource(form);
        }

        [HttpPut, ValidateInput(false), Route("EditResource")]
        public JsonResult EditResource(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                if (!form.HasKeys()) throw new NoFormDataException("resource");

                var id = parseIntField(form, "ID");
                var model = Community.GetById<Resource>(id);

                if (model == null) throw new NotFoundException("resource");

                if (id <= 0) throw new NotFoundException("Resource with ID less than or equal to 0 cannot be removed.");

                var newEmail = parseTextField(form, "Email");

                if (string.IsNullOrEmpty(newEmail)) throw new NoFormDataException("Resource doesnt have a valid email / username specified.");

                //we need to compare the new email to the old email.  If they are different, we need to check if the new email already exists for another user
                // if the username is already in use we should throw an error to prevent this from happening as the other account should be updated
                if (string.Compare(newEmail, model.Username, true) != 0)
                {
                    //check if the resource already exists in community
                    var a = Community.Filter<Resource>(i => i.Email == newEmail).FirstOrDefault();

                    if (a != null) throw new Exception("Cannot update the user.  The specified email address / username is already in use.");
                }

                // Static fields
                model.FirstName = parseNameField(form, "FirstName");
                model.LastName = parseNameField(form, "LastName");
                model.Email = newEmail;
                model.Username = newEmail;
                model.UpdatedOn = DateTime.UtcNow;

                Community.Update(model);    //Must be first before saving fields.

                var cr = Community.Filter<CompanyResource>(i => i.ResourceID == id && i.CompanyID == Company.CurrentCompanyID).SingleOrDefault();
                if (cr != null)
                {
                    cr.State = parseEnumField<CompanyResourceState>(form, "State");
                    cr.IsAdministrator = parseBooleanField(form, "IsAdministrator");
                    Community.Update(cr);
                }

                GlobalReportingResource gr = Company.Filter<GlobalReportingResource>(i => i.ResourceID == id).FirstOrDefault();

                gr.FirstName = model.FirstName;
                gr.LastName = model.LastName;
                gr.Email = model.Email;
                gr.IsAdministrator = cr.IsAdministrator;
                gr.State = cr.State;
                gr.UpdatedOn = DateTime.UtcNow;

                Company.Update(gr);

                // Dynamic fields
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Resource, model.ID, Company.GetFieldTypesByObject(SystemObjects.ResourceType, model.ResourceTypeID).ToList(), form, Server, false);
                Company.AddOrUpdateFields(fields);


                return jsonSuccess("Resource successfully updated.", id.ToString(), "edit", HttpStatusCode.OK);
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

        [HttpPut, ValidateInput(false), Route("EditMyInfo")]
        public JsonResult EditMyInfo(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("resource");

                var model = Community.GetById<Resource>(Company.CurrentResourceID);

                if (model == null) throw new NotFoundException("resource");

                // Static fields
                model.FirstName = parseNameField(form, "FirstName");
                model.LastName = parseNameField(form, "LastName");
                model.UpdatedOn = DateTime.UtcNow;

                // Dynamic fields
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Resource, model.ID, Company.GetFieldTypesByObject(SystemObjects.ResourceType, model.ResourceTypeID).ToList(), form, Server, false);
                Company.AddOrUpdateFields(fields);

                Community.Update<Resource>(model);

                return jsonSuccess("Info successfully updated.", Company.CurrentResourceID.ToString(), "edit", HttpStatusCode.OK);
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

        [HttpPut, ValidateInput(false), Route("ChangeMyPassword")]
        public JsonResult ChangeMyPassword(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("resource");

                var model = Community.GetById<Resource>(Company.CurrentResourceID);

                if (model == null) throw new NotFoundException("resource");

                var currentpassword = parseTextField(form, "CurrentPassword");
                var password1 = parseTextField(form, "NewPassword");
                var password2 = parseTextField(form, "ConfirmNewPassword");

                if (!password1.Equals(password2))
                {
                    throw new ConflictException("Password values do not match", "Password values do not match.  Please try again.");
                }

                Community.ChangePassword(Company.CurrentResourceID, currentpassword, password1);

                return jsonSuccess("Password successfully updated.", Company.CurrentResourceID.ToString(), "edit", HttpStatusCode.OK);
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

        #region Group

        #region Form Get/Post


        #region Group : Add User


        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), ActionName("ResourceGroup"), Route("ResourceGroup")]
        public JsonResult PostResourceGroup(ResourceGroup[] model)
        {
            try
            {
                if (!Company.HasAssetPermission(SystemObjects.Group, model[0].GroupID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                foreach (var m in model)
                    Company.Add(m);

                return jsonSuccess("User successfully assigned.", model[0].ResourceID.ToString(), "add", HttpStatusCode.Created);
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


        [HttpGet, Route("GetGroupUserList"), NonNullableParameters]
        public JsonNetResult GetGroupUserList(int id, int pagenum, int pagesize, string sortDataField, string sortOrder, string gbfilter)
        {

            string querySql;
            var dbArgs = new Dapper.DynamicParameters();

            var hideUsersSql = "";

            if (HideData3SixtyUsers())
            {
                hideUsersSql = " and (r.Email not like '%@data3sixty.com' and r.Email not like '%@infogix.com')";
            }

            querySql = @"
			select  r.LastName + ', ' + r.FirstName as Text, 'Resource|' + cast(r.ResourceID as varchar) + '|' + r.LastName + ', ' + r.FirstName  as [Value],'User' as [Type] from reporting.Global_Resource r                                    
			where r.[State] = @userStatus 
			and  not exists   (select 1 from ResourceGroup where Groupid =@id   and ResourceID= r.ResourceID) "
            + hideUsersSql;
            dbArgs.Add("id", id);
            dbArgs.Add("userStatus", CompanyResourceState.Active);

            if (!string.IsNullOrEmpty(gbfilter))
            {
                querySql = string.Format(@"select * from ({0}) gb where  [Text] like '%' +   @gbfilter + '%'", querySql);
                dbArgs.Add("gbfilter", gbfilter);
            }
            var countSql = string.Format(@"select count(1) from ({0}) A", querySql);
            var sql = string.Format(@"select * from ({0}) A", querySql);
            countSql = applyFilteringSuffixBind(countSql, Request, dbArgs);
            int totalCount = Company.Query<int>(countSql, dbArgs).First();

            sql = applySortSuffix(sql, sortDataField, sortOrder, "Text", "asc");
            sql = applyPagingSuffix(sql, pagenum, pagesize);

            var query = Company.Query<dynamic>(sql, dbArgs);

            return new JsonNetResult
            {
                Data = new { total = totalCount, results = query },
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        #endregion

        #region Group : Delete User

        [HttpDelete, ActionName("ResourceGroup"), Route("ResourceGroup"), NonNullableParameters]
        public JsonResult DeleteResourceGroup(int groupID, int resourceID)
        {
            try
            {
                if (!Company.HasAssetPermission(SystemObjects.Group, groupID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                Company.Delete<ResourceGroup>(i => i.GroupID == groupID && i.ResourceID == resourceID);

                return jsonSuccess("User successfully removed from group.", resourceID.ToString(), "delete", HttpStatusCode.OK);
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

        #region Group : Delete

        [HttpDelete, Route("DeleteGroup")]
        public JsonResult DeleteGroup(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("group");

                var id = parseIntField(form, "ID");
                var model = Company.GetById<Group>(id);
                if (model == null) throw new NotFoundException("group");

                if (!Company.HasAssetPermission(SystemObjects.Group, id, Permission.DeleteAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(model);

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

        [HttpDelete, Route("DeleteGroupByID"), NonNullableParameters]
        public JsonResult DeleteGroupByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteGroup(form);
        }

        #endregion

        #region Group : Edit

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), ActionName("Group"), Route("Group")]
        public JsonResult PostGroup(Group model)
        {
            try
            {
                if (!Company.HasAssetTypePermission(SystemObjects.Group, 0, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                Company.Add(model);

                Company.Add(new ResourceGroup { GroupID = model.ID, ResourceID = (int)model.PrimaryOwnerResourceID });
                try
                {
                    if (model.SecondaryOwnerResourceID.HasValue)
                    {
                        if (!model.PrimaryOwnerResourceID.Equals(model.SecondaryOwnerResourceID))
                            Company.Add(new ResourceGroup { GroupID = model.ID, ResourceID = model.SecondaryOwnerResourceID.Value });
                    }
                }
                catch
                {
                }

                long assetId = Company.Assets.FirstOrDefault(x => x.Object == "Group" && x.ObjectID == model.ID).ID;

                Company.CreateOrUpdateDisplayValue(assetId);

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

        [HttpPut, ValidateInput(false), ActionName("Group"), Route("Group")]
        public JsonResult PutGroup(Group model)
        {
            try
            {
                var existing = Company.GetById<Group>(model.ID);
                if (existing == null) throw new NotFoundException("group");

                if (!Company.HasAssetPermission(SystemObjects.Group, existing.ID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                existing.Name = model.Name;
                existing.Description = model.Description;
                existing.PrimaryOwnerResourceID = model.PrimaryOwnerResourceID;
                existing.SecondaryOwnerResourceID = model.SecondaryOwnerResourceID;

                Company.Update(existing);

                var currentGroupUsers = Company.Filter<ResourceGroup>(i => i.GroupID == model.ID).Select(i => i.ResourceID).ToList();

                if (!currentGroupUsers.Any(o => o == model.PrimaryOwnerResourceID))
                {
                    Company.Add(new ResourceGroup { GroupID = model.ID, ResourceID = model.PrimaryOwnerResourceID.Value });
                }
                if (model.SecondaryOwnerResourceID.HasValue)
                {
                    if (!currentGroupUsers.Any(o => o == model.SecondaryOwnerResourceID))
                    {
                        Company.Add(new ResourceGroup { GroupID = model.ID, ResourceID = model.SecondaryOwnerResourceID.Value });
                    }
                }

                return jsonSuccess(model.Name + " successfully updated.", model.ID.ToString(), "edit", HttpStatusCode.OK);
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

        [HttpGet, ActionName("Group"), Route("Group"), NonNullableParameters]
        public JsonNetResult GetGroup(int id)
        {
            var group = new Group();
            var resourceList = new List<SelectListItem>();

            if (id == 0)
            {
                resourceList = GetCompanyResources()
                    .OrderBy(i => i.LastName)
                    .ThenBy(i => i.FirstName)
                    .Select(i => new { ID = i.ResourceID, i.FirstName, i.LastName })
                    .ToList()
                    .Select(i => new SelectListItem { Text = string.Format("{0}, {1}", i.LastName, i.FirstName), Value = i.ID.ToString() })
                    .ToList();
            }
            else
            {
                group = Company.GetById<Group>(id);

                var primaryOwner = GetCompanyResources().Where(x => x.ResourceID == group.PrimaryOwnerResourceID).FirstOrDefault();
                var secondaryOwner = GetCompanyResources().Where(x => x.ResourceID == group.SecondaryOwnerResourceID).FirstOrDefault();
                group.PrimaryOwnerName = primaryOwner != null ? primaryOwner.LastName + ", " + primaryOwner.FirstName : "";
                group.SecondaryOwnerName = secondaryOwner != null ? secondaryOwner.LastName + ", " + secondaryOwner.FirstName : "";

                var currentUsers = Company.Filter<ResourceGroup>(i => i.GroupID == id).Select(i => i.ResourceID).ToList();
                resourceList = GetCompanyResources()
                    .Select(i => new { ID = i.ResourceID, i.FirstName, i.LastName, MembershipStatus = currentUsers.Any(o => o == i.ResourceID) ? "Current Member" : "Not Yet a Member" })
                    .OrderBy(i => i.MembershipStatus)
                    .ThenBy(i => i.LastName)
                    .ThenBy(i => i.FirstName)
                    .ToList()
                    .Select(i => new SelectListItem { Group = new SelectListGroup { Name = i.MembershipStatus }, Text = string.Format("{0}, {1}", i.LastName, i.FirstName), Value = i.ID.ToString() })
                    .ToList();
            }

            resourceList.Insert(0, new SelectListItem { Text = "None", Value = "", Group = new SelectListGroup { Name = "" } });

            return new JsonNetResult
            {
                Data = new
                {
                    group,
                    resourceList,
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        #endregion

        #endregion

        #endregion
    }
}