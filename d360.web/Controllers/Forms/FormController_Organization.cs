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
        #region Organization

        #region Field Generation

        [Route("Organization_AddFields"), NonNullableParameters]
        public JsonResult Organization_AddFields(int ot)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>
            {
                new EditableField 
                { 
                    FieldName = "OrganizationTypeID",
                    FieldType = DataType.Hidden.ToString(),
                    Value = ot.ToString()
                },
                
                new EditableField 
                { 
                    Row = 1,
                    Column = 1,
                    Required = true,
                    FieldName = "Name",
                    Name = "Name", 
                    FieldType = DataType.Text.ToString(),
                    Validations = checkAndAddValidation(fieldType: "Text",
                                                        friendlyName: "Name",
                                                        required: true,
                                                        pattern: "",
                                                        minLength: 1,
                                                        maxLength: 250) 
                },
                
                new EditableField 
                {
                    Row = 1, 
                    Column = 2, 
                    Required = true, 
                    FieldName = "AdministratorEmail",
                    Name = "Administrator Email", 
                    FieldType = DataType.Text.ToString(), 
                    Validations = checkAndAddValidation(fieldType: "Text",
                                                        friendlyName: "Name",
                                                        required: true,
                                                        pattern: "",
                                                        minLength: 1,
                                                        maxLength: 250) 
                }
            };

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.OrganizationType, ot).ToList(), 2, false);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">Organization ID</param>
        [Route("Organization_EditFields"), NonNullableParameters]
        public JsonResult Organization_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>();
            var a = Company.GetById<Organization>(id);

            list.Add(new EditableField 
            { 
                FieldName = "ID",
                FieldType = DataType.Hidden.ToString(), 
                Value = a.ID.ToString() 
            });

            list.Add(new EditableField 
            { 
                Row = 1,
                Column = 1, 
                Required = true, 
                FieldName = "Name",
                Name = "Name",
                FieldType = DataType.Text.ToString(),
                Value = Server.HtmlDecode(a.Name),
                Validations = checkAndAddValidation(fieldType: "Text",
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
                FieldName = "AdministratorEmail",
                Name = "Administrator Email",
                FieldType = DataType.Text.ToString(),
                Value = a.AdministratorEmail, 
                Validations = checkAndAddValidation(fieldType: "Text",
                                                    friendlyName: "Name",
                                                    required: true,
                                                    pattern: "",
                                                    minLength: 1,
                                                    maxLength: 250) 
            });

            list = loadDynamicFields(
                    SystemObjects.Organization.ToString(),
                    id,
                    list,
                    Company.GetFieldTypesByObject(SystemObjects.OrganizationType, a.OrganizationTypeID).ToList(),
                    Company.GetFieldRelationsByObject(SystemObjects.Organization, id).ToList(),
                    2,
                    false,
                    false
                );

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), ActionName("Organization"), Route("Organization")]
        public JsonResult PostOrganization(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                }

                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.Organization);
                }

                int typeID = parseIntField(form, "OrganizationTypeID");
                var type = Company.GetById<OrganizationType>(typeID);

                if (type == null)
                {
                    throw new NotFoundException(FormInfo.OrganizationType);
                }

                var a = new Organization
                {
                    Name = parseTextField(form, "Name"),
                    AdministratorEmail = parseTextField(form, "AdministratorEmail"),
                    OrganizationTypeID = typeID
                };

                var emailRegex = @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$";
                var regex = new System.Text.RegularExpressions.Regex(emailRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (!regex.IsMatch(a.AdministratorEmail))
                {
                    return jsonException(FormControllerApiMessage.emailNotValid, HttpStatusCode.Forbidden);
                }

                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.OrganizationType, typeID).ToList();
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Organization, a.ID, fieldTypes, form, Server);
                Company.SaveOrUpdate(a, fields);

                dynamic custom = new
                {
                    a.Name,
                    action = "add"
                };

                return jsonSuccess(string.Format(ApiMessages.SucessfullyCreated, FormControllerApiMessage.Organization), a.ID.ToString(), "add", HttpStatusCode.Created, custom);
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

        [HttpPut, ActionName("Organization"), Route("Organization")]
        public JsonResult PutOrganization(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
                }

                var id = parseIntField(form, "ID");
                var existing = Company.GetById<Organization>(id);

                if (existing == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.Organization);
                }

                existing.Name = parseTextField(form, "Name");
                existing.AdministratorEmail = parseTextField(form, "AdministratorEmail");

                var emailRegex = @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$";
                var regex = new System.Text.RegularExpressions.Regex(emailRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (!regex.IsMatch(existing.AdministratorEmail))
                {
                    return jsonException(FormControllerApiMessage.emailNotValid, HttpStatusCode.Forbidden);
                }

                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.OrganizationType, existing.OrganizationTypeID).ToList();
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Organization, existing.ID, fieldTypes, form, Server, false);
                Company.SaveOrUpdate<Organization>(existing, fields);

                dynamic custom = new
                {
                    existing.Name,
                    action = "edit"
                };

                return jsonSuccess(string.Format(ApiMessages.SucessfullyUpdated, FormControllerApiMessage.Organization), id.ToString(), "edit", HttpStatusCode.OK, custom);
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

        [HttpDelete, ActionName("Organization"), Route("Organization"), NonNullableParameters]
        public JsonResult DeleteOrganization(int id)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
                }

                var model = Company.GetById<Organization>(id);

                if (model == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.Organization);
                }

                //get child records
                var domains = Company.Filter<OrganizationDomain>(i => i.OrganizationID == model.ID);
                var invitations = Company.Filter<OrganizationInvitation>(i => i.OrganizationID == model.ID);
                var resources = Company.Filter<OrganizationResource>(i => i.OrganizationID == model.ID);
                var registrations = Company.Filter<OrganizationRegistration>(i => i.OrganizationID == model.ID);

                Company.OrganizationDomains.RemoveRange(domains);
                Company.OrganizationInvitations.RemoveRange(invitations);
                Company.OrganizationResources.RemoveRange(resources);
                Company.OrganizationRegistrations.RemoveRange(registrations);

                model.State = State.Deleted;

                Company.SaveChanges();

                dynamic custom = new
                {
                    model.Name,
                    action = "delete"
                };

                return jsonSuccess(string.Format(ApiMessages.SucessfullyRemoved, FormControllerApiMessage.Organization), id.ToString(), "delete", HttpStatusCode.OK, custom);
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

        #region Organization Contract

        #region Field Generation

        [Route("Contract_AddFields"), NonNullableParameters]
        public JsonResult Contract_AddFields(int o = 0)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>();

            var contractTypes = ContractType.OrganizationTermsOfUse.GetEnumList().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();

            list.Add(new EditableField 
            { 
                FieldName = "OrganizationID", 
                FieldType = DataType.Hidden.ToString(),
                Value = o.ToString() 
            });
            
            list.Add(new EditableField
            { 
                Row = 1,
                Column = 1,
                Required = true, 
                FieldName = "Title", 
                Name = "Title",
                FieldType = DataType.Text.ToString(),
                Validations = checkAndAddValidation(fieldType: "Text",
                                                    friendlyName: "Title",
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
                FieldName = "ContractType",
                Name = "Contract Type",
                FieldType = DataType.Lookup.ToString(), 
                Items = contractTypes 
            });
            
            list.Add(new EditableField
            {
                Row = 2, 
                Column = 1, 
                Required = true,
                FieldName = "Body",
                Name = "Body", 
                FieldType = DataType.Html.ToString() 
            });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">Contract ID</param>
        [Route("Contract_EditFields"), NonNullableParameters]
        public JsonResult Contract_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>();
            var a = Company.GetById<Contract>(id);

            list.Add(new EditableField 
            { 
                FieldName = "ID", 
                FieldType = DataType.Hidden.ToString(), 
                Value = a.ID.ToString() 
            });

            var contractTypes = ContractType.OrganizationTermsOfUse.GetEnumList().Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();

            list.Add(new EditableField 
            { 
                Row = 1, 
                Column = 1, 
                Required = true, 
                FieldName = "Title",
                Name = "Title",
                FieldType = DataType.Text.ToString(),
                Value = Server.HtmlDecode(a.Title),
                Validations = checkAndAddValidation(fieldType: "Text",
                                                    friendlyName: "Title",
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
                FieldName = "ContractType", 
                Name = "Contract Type", 
                FieldType = DataType.Lookup.ToString(), 
                Value = a.ContractType.ToString(), 
                Items = contractTypes 
            });
            
            list.Add(new EditableField 
            { 
                Row = 2,
                Column = 1,
                Required = true,
                FieldName = "Body", 
                Name = "Body", 
                FieldType = DataType.Html.ToString(),
                Value = a.Body 
            });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [HttpGet, Route("Contract/{id:int}")]
        public JsonResult GetContract(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
            }

            var contract = Company.GetById<Contract>(id);

            if (contract.PublishedOn.HasValue)
            {
                contract.PublishedOn = new DateTime(contract.PublishedOn.Value.Ticks, DateTimeKind.Utc);
            }

            if (contract.UpdatedOn.HasValue)
            {
                contract.UpdatedOn = new DateTime(contract.UpdatedOn.Value.Ticks, DateTimeKind.Utc);
            }

            return Json(new
            {
                contract.ID,
                contract.Title,
                contract.Body,
                contract.OrganizationID,
                contract.ContractType,
                contract.State,
                PublishedOn = contract.PublishedOn.HasValue ? ((DateTime)contract.PublishedOn).ToString("o") : null,
                UpdatedOn = contract.UpdatedOn.HasValue ? ((DateTime)contract.UpdatedOn).ToString("o") : null,
                contract.UpdatedBy,
                contract.CreatedOn,
                contract.CreatedBy
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPut, Route("Contract")]
        public JsonResult PutContract(Contract model, bool publish = false)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
                }

                int id = model.ID;

                if (id < 1)
                {
                    throw new NotFoundException(FormControllerApiMessage.Contract);
                }

                var contract = Company.GetById<Contract>(id);

                if (contract == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.Contract);
                }


                contract.Title = model.Title;
                contract.Body = model.Body;
                contract.ContractType = model.ContractType;

                if (publish)
                {
                    contract.PublishedOn = DateTime.UtcNow;

                    if (contract.ContractType == ContractType.OrganizationTermsOfUse && contract.OrganizationID.HasValue)
                    {
                        var org = Company.GetById<Organization>((int)contract.OrganizationID);
                        org.Accepted = false;
                        org.AcceptedBy = null;
                        org.DateAccepted = null;

                        Company.SaveOrUpdate(org);
                    }
                }

                Company.SaveOrUpdate(contract);

                dynamic custom = new
                {
                    title = model.Title,
                    action = "edit"
                };

                return jsonSuccess(string.Format(ApiMessages.SucessfullyUpdated, $"{model.ContractType.GetDisplayName()} {FormControllerApiMessage.Contract}"), id.ToString(), "edit", HttpStatusCode.OK, custom);

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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("Contract")]
        public JsonResult PostContract(Contract model, bool publish = false)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                }

                var contract = new Contract
                {
                    OrganizationID = model.OrganizationID,
                    Title = model.Title,
                    Body = model.Body,
                    ContractType = model.ContractType
                };

                if (publish)
                {
                    contract.PublishedOn = DateTime.UtcNow;
                    if (contract.ContractType == ContractType.OrganizationTermsOfUse && contract.OrganizationID.HasValue)
                    {
                        var org = Company.GetById<Organization>((int)contract.OrganizationID);
                        org.Accepted = false;
                        org.AcceptedBy = null;
                        org.DateAccepted = null;
                        Company.SaveOrUpdate(org);
                    }
                }

                Company.Add(contract);

                dynamic custom = new
                {
                    title = contract.Title,
                    action = "add"
                };

                return jsonSuccess(string.Format(ApiMessages.SucessfullyCreated, $"{contract.ContractType.GetDisplayName()} {FormControllerApiMessage.Contract}"), contract.ID.ToString(), "add", HttpStatusCode.Created, custom);

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

        [HttpDelete, ActionName("Contract"), Route("Contract"), NonNullableParameters]
        public JsonResult DeleteContract(int id)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
                }

                var o = Company.GetById<Contract>(id);

                if (o == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.Contract);
                }
                o.State = State.Deleted;
                Company.SaveOrUpdate(o);

                dynamic custom = new
                {
                    title = o.Title,
                    action = "delete"
                };

                return jsonSuccess(string.Format(ApiMessages.SucessfullyRemoved, $"{o.ContractType.GetDisplayName()} {FormControllerApiMessage.Contract}"), id.ToString(), "delete", HttpStatusCode.OK, custom);
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

        #region Organization Domain

        #region Field Generation

        /// <param name="o">Organization ID</param>
        [Route("OrganizationDomain_AddFields"), NonNullableParameters]
        public JsonResult OrganizationDomain_AddFields(int o)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>
            {
                new EditableField 
                {
                    FieldName = "OrganizationID",
                    FieldType = DataType.Hidden.ToString(),
                    Value = o.ToString() 
                },
                
                new EditableField 
                { 
                    Row = 1, 
                    Column = 1, 
                    Required = true,
                    FieldName = "Domain",
                    Name = "Domain", 
                    FieldType = DataType.Text.ToString(), 
                    Validations = checkAndAddValidation(fieldType: "Text",
                                                        friendlyName: "Domain",
                                                        required: true,
                                                        pattern: "",
                                                        minLength: 5,
                                                        maxLength: 500) 
                }
            };

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">Organization Domain ID</param>
        [Route("OrganizationDomain_EditFields"), NonNullableParameters]
        public JsonResult OrganizationDomain_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>();
            var a = Company.GetById<OrganizationDomain>(id);

            list.Add(new EditableField 
            { 
                FieldName = "ID",
                FieldType = DataType.Hidden.ToString(),
                Value = a.ID.ToString() 
            });

            list.Add(new EditableField
            { 
                Row = 1, 
                Column = 1, 
                Required = true, 
                FieldName = "Domain", 
                Name = "Domain", 
                FieldType = DataType.Text.ToString(), 
                Value = a.Domain,
                Validations = checkAndAddValidation(fieldType: "Text",
                                                    friendlyName: "Domain",
                                                    required: true,
                                                    pattern: "",
                                                    minLength: 5,
                                                    maxLength: 500) 
            });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), ActionName("OrganizationDomain"), Route("OrganizationDomain")]
        public JsonResult PostOrganizationDomain(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                }

                var o = new OrganizationDomain
                {
                    OrganizationID = parseIntField(form, "OrganizationID"),
                    Domain = parseTextField(form, "Domain")
                };

                if (Company.Any<OrganizationDomain>(i => i.OrganizationID == o.OrganizationID && i.Domain == o.Domain))
                {
                    return jsonException(FormControllerApiMessage.DomainPartOfOrganization, HttpStatusCode.Forbidden);
                }

                Company.Add(o);

                dynamic custom = new
                {
                    action = "add"
                };

                return jsonSuccess(string.Format(ApiMessages.SucessfullyCreated, FormControllerApiMessage.OrganizationDomain), o.ID.ToString(), "add", HttpStatusCode.Created, custom);
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

        [HttpPut, ActionName("OrganizationDomain"), Route("OrganizationDomain")]
        public JsonResult PutOrganizationDomain(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
                }

                var id = parseIntField(form, "ID");
                var existing = Company.GetById<OrganizationDomain>(id);

                if (existing == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.OrganizationDomain);
                }
                existing.Domain = parseTextField(form, "Domain");

                if (Company.Any<OrganizationDomain>(i => i.OrganizationID == existing.OrganizationID && i.Domain == existing.Domain && i.ID != existing.ID))
                {
                    return jsonException(FormControllerApiMessage.DomainPartOfOrganization, HttpStatusCode.Forbidden);
                }

                Company.Update(existing);

                dynamic custom = new
                {
                    action = "edit"
                };

                return jsonSuccess(string.Format(ApiMessages.SucessfullyUpdated, FormControllerApiMessage.OrganizationDomain), id.ToString(), "edit", HttpStatusCode.OK, custom);
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

        [HttpDelete, ActionName("OrganizationDomain"), Route("OrganizationDomain"), NonNullableParameters]
        public JsonResult DeleteOrganizationDomain(int id)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
                }

                var model = Company.GetById<OrganizationDomain>(id);

                if (model == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.OrganizationDomain);
                }

                Company.Delete(model);

                dynamic custom = new
                {
                    action = "delete"
                };

                return jsonSuccess(string.Format(ApiMessages.SucessfullyRemoved, FormControllerApiMessage.OrganizationDomain), id.ToString(), "delete", HttpStatusCode.OK, custom);
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

        #region Organization Invitation

        #region Field Generation

        /// <param name="o">Organization ID</param>
        [Route("OrganizationInvitation_AddFields"), NonNullableParameters]
        public JsonResult OrganizationInvitation_AddFields(int o)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>
            {
                new EditableField 
                {
                    FieldName = "OrganizationID",
                    FieldType = DataType.Hidden.ToString(), 
                    Value = o.ToString() 
                },
                
                new EditableField 
                { 
                    Row = 1, 
                    Column = 1,
                    Required = true, 
                    FieldName = "Email",
                    Name = "Email",
                    FieldType = DataType.Text.ToString(),
                    Validations = checkAndAddValidation(fieldType: "Text",
                                                        friendlyName: "Email",
                                                        required: true,
                                                        pattern: "",
                                                        minLength: 5,
                                                        maxLength: 500) 
                }
            };

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">Organization Invitation ID</param>
        [Route("OrganizationInvitation_EditFields"), NonNullableParameters]
        public JsonResult OrganizationInvitation_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>();
            var a = Company.GetById<OrganizationInvitation>(id);

            list.Add(new EditableField 
            {
                FieldName = "ID",
                FieldType = DataType.Hidden.ToString(), 
                Value = a.ID.ToString() 
            });

            list.Add(new EditableField 
            { 
                Row = 1,
                Column = 1,
                Required = true,
                FieldName = "Email",
                Name = "Email", 
                FieldType = DataType.Text.ToString(), 
                Value = a.Email, 
                Validations = checkAndAddValidation(fieldType: "Text",
                                                    friendlyName: "Email",
                                                    required: true,
                                                    pattern: "",
                                                    minLength: 5,
                                                    maxLength: 500) 
            });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), ActionName("OrganizationInvitation"), Route("OrganizationInvitation")]
        public JsonResult PostOrganizationInvitation(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                }

                var a = new OrganizationInvitation
                {
                    OrganizationID = parseIntField(form, "OrganizationID"),
                    Email = parseTextField(form, "Email")
                };

                var emailRegex = @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$";
                var regex = new System.Text.RegularExpressions.Regex(emailRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (!regex.IsMatch(a.Email))
                {
                    return jsonException(FormControllerApiMessage.emailNotValid, HttpStatusCode.Forbidden);
                }

                if (Company.Any<OrganizationInvitation>(i => i.OrganizationID == a.OrganizationID && i.Email == a.Email))
                {
                    return jsonException("This email has already been invited to this organization", HttpStatusCode.Forbidden);
                }

                var userIsAlreadyRegistered = Company.Query<dynamic>(@"select 1 from organizationresource g
                    inner join reporting.Global_Resource r on r.ResourceID = g.ResourceID
                    where r.Email = @Email and g.OrganizationID = @OrganizationID", new { a.Email, a.OrganizationID }).Count() > 0;

                if (userIsAlreadyRegistered)
                {
                    return jsonException(FormControllerApiMessage.UserAlreadyRegisteredThisOrganization, HttpStatusCode.Forbidden);
                }

                Company.Add(a);

                dynamic custom = new
                {
                    action = "add"
                };

                return jsonSuccess(string.Format(ApiMessages.SucessfullyCreated, FormControllerApiMessage.OrganizationInvitation), a.ID.ToString(), "add", HttpStatusCode.Created, custom);
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

        [HttpPut, ActionName("OrganizationInvitation"), Route("OrganizationInvitation")]
        public JsonResult PutOrganizationInvitation(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
                }

                var id = parseIntField(form, "ID");
                var existing = Company.GetById<OrganizationInvitation>(id);

                if (existing == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.OrganizationInvitation);
                }

                existing.Email = parseTextField(form, "Email");

                var emailRegex = @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$";
                var regex = new System.Text.RegularExpressions.Regex(emailRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (!regex.IsMatch(existing.Email))
                {
                    return jsonException(FormControllerApiMessage.emailNotValid, HttpStatusCode.Forbidden);
                }

                if (Company.Any<OrganizationInvitation>(i => i.OrganizationID == existing.OrganizationID && i.Email == existing.Email && i.ID != existing.ID))
                {
                    return jsonException(FormControllerApiMessage.EmailAlreadyInviteThisOrganization, HttpStatusCode.Forbidden);
                }

                var userIsAlreadyRegistered = Company.Query<dynamic>(@"select 1 from organizationresource g
                    inner join reporting.Global_Resource r on r.ResourceID = g.ResourceID
                    where r.Email = @Email and g.OrganizationID = @OrganizationID", new { existing.Email, existing.OrganizationID }).Any();
                
                if (userIsAlreadyRegistered)
                {
                    return jsonException(FormControllerApiMessage.UserAlreadyRegisteredThisOrganization, HttpStatusCode.Forbidden);
                }

                Company.Update(existing);

                dynamic custom = new
                {
                    action = "edit"
                };

                return jsonSuccess(string.Format(ApiMessages.SucessfullyUpdated, FormControllerApiMessage.OrganizationInvitation), id.ToString(), "edit", HttpStatusCode.OK, custom);
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

        [HttpDelete, ActionName("OrganizationInvitation"), Route("OrganizationInvitation"), NonNullableParameters]
        public JsonResult DeleteOrganizationInvitation(int id)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
                }

                var model = Company.GetById<OrganizationInvitation>(id);

                if (model == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.OrganizationInvitation);
                }

                Company.Delete(model);

                dynamic custom = new
                {
                    action = "delete"
                };

                return jsonSuccess(string.Format(ApiMessages.SucessfullyRemoved, FormControllerApiMessage.OrganizationInvitation), id.ToString(), "delete", HttpStatusCode.OK, custom);
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

        #region Organization Type

        [HttpDelete, ActionName("OrganizationType"), Route("OrganizationType"), NonNullableParameters]
        public JsonResult DeleteOrganizationType(int id)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
                }

                AssetType assetType = Company.AssetTypes.Where(a => a.Object == "OrganizationType" && a.ObjectID == id).FirstOrDefault();

                if (assetType == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.organizationType);
                }

                var execution = new ApiExecution
                {
                    ExecutionID = Guid.NewGuid(),
                    StartedOn = DateTime.UtcNow,
                    Route = Request?.Url?.LocalPath,
                    Method = Request?.HttpMethod,
                    ResourceID = Company.CurrentResourceID,
                    Total = 1,
                    Fields = "{}",
                    Error = 0,
                    Processed = 0
                };

                Company.Add(execution);
                Company.SaveChanges();

                var deletes = new AssetTypeDeletes
                {
                    new AssetTypeDelete
                    { 
                        Cascade = false,
                        ExecutionItemUid = Guid.NewGuid(),
                        Uid = assetType.uid 
                    }
                };

                var deleteAssetTypesResults = Company.RemoveAssetTypes(execution, deletes, 28800); //dbExecutionTimeout = 8 hours

                execution.CompletedOn = DateTime.UtcNow;
                execution.Processed = deleteAssetTypesResults.Count(r => r.Success);
                execution.Error = deleteAssetTypesResults.Count(r => !r.Success);

                if (execution.Error > 0)
                {
                    string message = deleteAssetTypesResults.First().Message;
                    execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
                }

                Company.Update(execution);
                Company.SaveChanges();

                if (execution.Error > 0)
                {
                    throw new ArgumentNullException(FormControllerApiMessage.NotDeleteOrganizationType);
                }

                dynamic custom = new
                {
                    assetType.Name,
                    action = "delete"
                };

                return jsonSuccess(string.Format(ApiMessages.SucessfullyRemoved, FormControllerApiMessage.Organization), id.ToString(), "delete", HttpStatusCode.OK, custom);
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
