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

using Resources;

namespace d360.web.Controllers
{
    public partial class FormController : BaseController
    {
        private readonly List<AssetTypeClass> allowedVersionClasses = 
            new List<AssetTypeClass> 
            { 
                AssetTypeClass.BusinessAsset, 
                AssetTypeClass.TechnicalAsset, 
                AssetTypeClass.Model,
                AssetTypeClass.Policy, 
                AssetTypeClass.Rule,
                AssetTypeClass.Reference 
            };

        #region Custom API Service

        public JsonResult CustomAPIService_AddFields()
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>
            {
                new EditableField 
                {
                    Row = 1, 
                    Column = 1, 
                    Required = true,
                    FieldName = "Name", 
                    Name = FieldInfo.Name_Name, 
                    FieldDescription = "", 
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
                    FieldName = "URIPrefix", 
                    Name = "URI Segment", 
                    FieldDescription = "",
                    FieldType = DataType.Text.ToString(),
                    Validations = checkAndAddValidation(fieldType: "Text",
                                                        friendlyName: "URIPrefix",
                                                        required: true,
                                                        pattern: "([A-Z]*[a-z]*[0-9]*){1,80}",
                                                        minLength: 1,
                                                        maxLength: 80,
                                                        validationMessage: "Must be between 1 and 80 alphanumeric characters in length.") 
                },

                new EditableField 
                {
                    Row = 2, 
                    Column = 1, 
                    FieldName = "MaxAge",
                    Name = "Cache Max-Age (seconds)", 
                    FieldDescription = "", 
                    FieldType = DataType.Number.ToString(),
                    Validations = checkAndAddValidation(fieldType: "Number",
                                                        friendlyName: "MaxAge",
                                                        required: true,
                                                        pattern: "(3[2-8][0-9]{2}|39[0-8][0-9]|399[0-9]|[4-9][0-9]{3}|[1-7][0-9]{4}|8[0-3][0-9]{3}|84000)",
                                                        minLength: null,
                                                        maxLength: null,
                                                        validationMessage: "Please enter a cache max-age value between 3,200-84,000 seconds.  ") },
                
                new EditableField 
                { 
                    Row = 3, 
                    Column = 1, 
                    FieldName = "Description", 
                    Name = FieldInfo.Description_Name,
                    FieldDescription = "", 
                    FieldType = DataType.Html.ToString() 
                }
            };

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CustomAPIService_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>();
            var a = Company.ApiServices.Where(x => x.ID == id).FirstOrDefault();

            if (a == null)
            {
                return jsonException(FormControllerApiMessage.NotFoundServiceEdit, HttpStatusCode.NotFound);
            }

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
                Name = FieldInfo.Name_Name, 
                FieldDescription = "", 
                FieldType = DataType.Text.ToString(), 
                Validations = checkAndAddValidation(fieldType: "Text",
                                                    friendlyName: "Name",
                                                    required: true,
                                                    pattern: "",
                                                    minLength: 1,
                                                    maxLength: 250),
                Value = a.Name 
            });
            
            list.Add(new EditableField 
            { 
                Row = 1,
                Column = 2,
                Required = true, 
                FieldName = "URIPrefix", Name = "URI Segment", FieldDescription = "", FieldType = DataType.Text.ToString(), Value = a.UriPrefix, Validations = checkAndAddValidation("Text", "URIPrefix", true, "([A-Z]*[a-z]*[0-9]*){1,80}", 1, 80, "Must be between 1 and 80 alphanumeric characters in length.") });
            
            list.Add(new EditableField 
            { 
                Row = 2,
                Column = 1, 
                FieldName = "MaxAge", 
                Name = "Cache Max-Age (seconds)",
                FieldDescription = "", 
                FieldType = DataType.Number.ToString(),
                Value = a.MaximumCacheAge.ToString(),
                Validations = checkAndAddValidation(fieldType: "Number",
                                                    friendlyName: "MaxAge",
                                                    required: true,
                                                    pattern: "(3[2-8][0-9]{2}|39[0-8][0-9]|399[0-9]|[4-9][0-9]{3}|[1-7][0-9]{4}|8[0-3][0-9]{3}|84000)",
                                                    minLength: null,
                                                    maxLength: null,
                                                    validationMessage: "Please enter a cache max-age value between 3,200-84,000 seconds.  ") 
            });
            
            list.Add(new EditableField 
            { 
                Row = 3, 
                Column = 1, 
                FieldName = "Description", 
                Name = FieldInfo.Description_Name, 
                FieldDescription = "",
                FieldType = DataType.Html.ToString(),
                Value = a.Description 
            });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddService")]
        public JsonResult AddService(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                }

                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.serviceConstant);
                }

                var name = parseTextField(form, "Name");
                var prefix = parseTextField(form, "URIPrefix");

                if (string.IsNullOrEmpty(name))
                {
                    return jsonException(FormControllerApiMessage.APIServiceNameNull, HttpStatusCode.NotFound);
                }

                if (string.IsNullOrEmpty(prefix))
                {
                    return jsonException(FormControllerApiMessage.APIServicePrefixNull, HttpStatusCode.NotFound);
                }

                var service = new ApiService
                {
                    Name = name,
                    Description = parseTextField(form, "Description"),
                    UriPrefix = prefix,
                    MaximumCacheAge = parseIntField(form, "MaxAge")
                };

                Company.Add(service);

                return jsonSuccess(FormControllerApiMessage.ServiceCreated, service.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpPut, ValidateInput(false), Route("EditService")]
        public JsonResult EditService(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
                }

                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.serviceConstant);
                }

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ApiService>(id);

                if (model == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.ApiService);
                }

                model.Name = parseTextField(form, "Name");
                model.UriPrefix = parseTextField(form, "URIPrefix");
                model.Description = parseTextField(form, "Description");
                model.MaximumCacheAge = parseIntField(form, "MaxAge");

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

        #region Custom API Service Namespaces
        public JsonResult CustomAPINamespace_AddFields(int serviceId)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>
            {
                new EditableField 
                { 
                    FieldName = "ServiceID",
                    FieldType = DataType.Hidden.ToString(), 
                    Value = serviceId.ToString() 
                },
                
                new EditableField 
                {
                    Row = 1, 
                    Column = 1,
                    Required = true, 
                    FieldName = "Name", 
                    Name = "Element Name", 
                    FieldDescription = "", 
                    FieldType = DataType.Text.ToString(), 
                    Validations = checkAndAddValidation(fieldType: "Text",
                                                        friendlyName: "Namespace",
                                                        required: true,
                                                        pattern: "",
                                                        minLength: 1,
                                                        maxLength: 250) 
                },
                
                new EditableField 
                { 
                    Row = 2, 
                    Column = 1, 
                    Required = true, 
                    FieldName = "Namespace", 
                    Name = "Namespace", 
                    FieldDescription = "", 
                    FieldType = DataType.Text.ToString(), 
                    Validations = checkAndAddValidation(fieldType: "Text",
                                                        friendlyName: "Namespace",
                                                        required: true,
                                                        pattern: "",
                                                        minLength: 1,
                                                        maxLength: 250) 
                }
            };

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CustomAPINamespace_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>();
            var a = Company.ApiNamespaces.Where(x => x.ID == id).FirstOrDefault();

            if (a == null)
            {
                return jsonException(FormControllerApiMessage.NotFoundServiceEdit, HttpStatusCode.NotFound);
            }

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
                Name = "Element Name",
                FieldDescription = "", 
                FieldType = DataType.Text.ToString(), 
                Validations = checkAndAddValidation(fieldType: "Text",
                                                    friendlyName: "Name",
                                                    required: true,
                                                    pattern: "",
                                                    minLength: 1,
                                                    maxLength: 250), 
                Value = a.Node 
            });
            
            list.Add(new EditableField 
            {
                Row = 2, 
                Column = 1,
                Required = true, 
                FieldName = "Namespace", 
                Name = "Namespace", 
                FieldDescription = "",
                FieldType = DataType.Text.ToString(), 
                Validations = checkAndAddValidation(fieldType: "Text",
                                                    friendlyName: "Namespace",
                                                    required: true,
                                                    pattern: "",
                                                    minLength: 1,
                                                    maxLength: 250), 
                Value = a.Namespace 
            });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddNamespace")]
        public JsonResult AddNamespace(FormCollection form)
        {
            try
            {
                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.serviceConstant);
                }

                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                }

                var name = parseTextField(form, "Name");
                var ns = parseTextField(form, "Namespace");
                var serviceId = parseIntField(form, "ServiceID");

                if (string.IsNullOrEmpty(name))
                {
                    return jsonException(FormControllerApiMessage.APINamespaceNameNull, HttpStatusCode.NotFound);
                }

                if (string.IsNullOrEmpty(ns))
                {
                    return jsonException(FormControllerApiMessage.APINamespaceNull, HttpStatusCode.NotFound);
                }

                var apiNamespace = new ApiNamespace
                {
                    ServiceID = serviceId,
                    Node = name,
                    Namespace = ns
                };

                Company.Add(apiNamespace);

                return jsonSuccess(FormControllerApiMessage.NamespaceCreated, apiNamespace.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpPut, ValidateInput(false), Route("EditNamespace")]
        public JsonResult EditNamespace(FormCollection form)
        {
            try
            {
                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.serviceConstant);
                }

                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
                }

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ApiNamespace>(id);

                if (model == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.ApiService);
                }

                model.Node = parseTextField(form, "Name");
                model.Namespace = parseTextField(form, "Namespace");

                Company.Update(model);

                return jsonSuccess(FormControllerApiMessage.NamespaceUpdated, id.ToString(), "edit", HttpStatusCode.OK);
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

        private JsonResult DeleteCustomAPINamespace(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
                }

                var id = parseIntField(form, "ID");

                Company.Delete<ApiNamespace>(o => o.ID == id);

                return jsonSuccess(FormControllerApiMessage.NamespaceRemoved, id.ToString(), "delete", HttpStatusCode.OK);
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

        #region Custom API Service Endpoint

        public JsonResult CustomAPIServiceEndpoint_AddFields(int serviceId)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>
            {
                new EditableField 
                { 
                    FieldName = "ServiceID",
                    FieldType = DataType.Hidden.ToString(),
                    Value = serviceId.ToString() 
                },
                
                new EditableField 
                { 
                    Row = 1, 
                    Column = 1,
                    Required = true, 
                    FieldName = "Name", 
                    Name = FieldInfo.Name_Name,
                    FieldDescription = "", 
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
                    FieldName = "URIPrefix",
                    Name = "URI Segment",
                    FieldDescription = "", 
                    FieldType = DataType.Text.ToString(), 
                    Validations = checkAndAddValidation(fieldType: "Text",
                                                        friendlyName: "URIPrefix",
                                                        required: true,
                                                        pattern: "([A-Z]*[a-z]*[0-9]*){1,80}",
                                                        minLength: 1,
                                                        maxLength: 80,
                                                        validationMessage: "Must be between 1 and 80 alphanumeric characters in length.") 
                },
                
                new EditableField 
                { 
                    Row = 1, 
                    Column = 3, 
                    Required = true, 
                    FieldName = "ItemNode", 
                    Name = "Item Element Name", 
                    FieldDescription = "", 
                    FieldType = DataType.Text.ToString(), 
                    Value = "item", 
                    Validations = checkAndAddValidation(fieldType: "Text",
                                                        friendlyName: "URIPrefix",
                                                        required: true,
                                                        pattern: "([A-Z]*[a-z]*[0-9]*){1,80}",
                                                        minLength: 1,
                                                        maxLength: 50,
                                                        validationMessage: "Must be between 1 and 50 alphanumeric characters in length.") 
                },
                
                new EditableField { Row = 2, Column = 1, FieldName = "Description", Name = FieldInfo.Description_Name, FieldDescription = "", FieldType = DataType.Html.ToString() }
            };

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CustomAPIServiceEndpoint_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>();
            var a = Company.ApiEndpoints.Where(x => x.ID == id).FirstOrDefault();

            if (a == null)
            {
                return jsonException(FormControllerApiMessage.NotFoundServiceEdit, HttpStatusCode.NotFound);
            }

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
                Name = FieldInfo.Name_Name,
                FieldDescription = "", 
                FieldType = DataType.Text.ToString(), 
                Validations = checkAndAddValidation(fieldType: "Text",
                                                    friendlyName: "Name",
                                                    required: true,
                                                    pattern: "",
                                                    minLength: 1,
                                                    maxLength: 250), 
                Value = a.Name
            });
            
            list.Add(new EditableField 
            { 
                Row = 1, 
                Column = 2, 
                Required = true, 
                FieldName = "URIPrefix", 
                Name = "URI Segment",
                FieldDescription = "", 
                FieldType = DataType.Text.ToString(),
                Value = a.UriPrefix, 
                Validations = checkAndAddValidation(fieldType: "Text",
                                                    friendlyName: "URIPrefix",
                                                    required: true,
                                                    pattern: "([A-Z]*[a-z]*[0-9]*){1,80}",
                                                    minLength: 1,
                                                    maxLength: 80,
                                                    validationMessage: "Must be between 1 and 80 alphanumeric characters in length.") 
            });
            
            list.Add(new EditableField 
            { 
                Row = 1,
                Column = 3,
                Required = true,
                FieldName = "ItemNode", 
                Name = "Item Element Name", 
                FieldDescription = "",
                FieldType = DataType.Text.ToString(), 
                Value = a.ItemNode ?? "item", 
                Validations = checkAndAddValidation(fieldType: "Text",
                                                    friendlyName: "URIPrefix",
                                                    required: true,
                                                    pattern: "([A-Z]*[a-z]*[0-9]*){1,80}",
                                                    minLength: 1,
                                                    maxLength: 50,
                                                    validationMessage: "Must be between 1 and 50 alphanumeric characters in length.") 
            });
            
            list.Add(new EditableField 
            { 
                Row = 2,
                Column = 1,
                FieldName = "Description", 
                Name = FieldInfo.Description_Name,
                FieldDescription = "", 
                FieldType = DataType.Html.ToString(),
                Value = a.Description 
            });

            return Json(list, JsonRequestBehavior.AllowGet);
        }


        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddServiceEndpoint")]
        public JsonResult AddServiceEndpoint(FormCollection form)
        {
            try
            {
                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.endpoint);
                }

                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                }

                var serviceId = parseIntField(form, "ServiceID");
                var name = parseTextField(form, "Name");
                var prefix = parseTextField(form, "URIPrefix");
                var itemNode = parseTextField(form, "ItemNode");

                if (string.IsNullOrEmpty(name))
                {
                    return jsonException(FormControllerApiMessage.APIServiceEndpointNameNull, HttpStatusCode.NotFound);
                }

                if (string.IsNullOrEmpty(prefix))
                {
                    return jsonException(FormControllerApiMessage.APIServiceEndpointPrefixNull, HttpStatusCode.NotFound);
                }

                var endpoint = new ApiEndpoint
                {
                    Name = name,
                    Description = parseTextField(form, "Description"),
                    UriPrefix = prefix,
                    ServiceID = serviceId,
                    ItemNode = itemNode
                };

                Company.Add(endpoint);

                return jsonSuccess(FormControllerApiMessage.ServiceEndpointCreated, endpoint.ID.ToString(), "add", HttpStatusCode.Created);
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


        [HttpPut, ValidateInput(false), Route("EditServiceEndpoint")]
        public JsonResult EditServiceEndpoint(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
                }

                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.endpoint);
                }

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ApiEndpoint>(id);

                if (model == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.ApiServiceEndpoint);
                }

                model.Name = parseTextField(form, "Name");
                model.UriPrefix = parseTextField(form, "URIPrefix");
                model.Description = parseTextField(form, "Description");
                model.ItemNode = parseTextField(form, "ItemNode");

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

        #region Custom API Service Endpoint Version

        public JsonResult CustomAPIServiceEndpointVersion_AddFields(int endpointId)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>
            {
                new EditableField { FieldName = "EndpointID", FieldType = DataType.Hidden.ToString(), Value = endpointId.ToString() },
                
                new EditableField { Row = 1, Column = 1, Required = true, FieldName = "URIPrefix", Name = "URI Segment", FieldDescription = "", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "URIPrefix", true, "([A-Z]*[a-z]*[0-9]*){1,80}", 1, 80, "Must be between 1 and 80 alphanumeric characters in length.") },
                
                new EditableField { Row = 2, Column = 1, Required = true, FieldName = "MajorVersion", Name = "Major Version", FieldDescription = "", FieldType = DataType.Number.ToString() },
                
                new EditableField { Row = 2, Column = 2, Required = true, FieldName = "MinorVersion", Name = "Minor Version", FieldDescription = "", FieldType = DataType.Number.ToString() },

                new EditableField
                {
                    Row = 3,
                    Column = 1,
                    Required = true,
                    FieldName = "AssetType",
                    Name = "Asset Type",
                    FieldDescription = "",
                    FieldType = DataType.Lookup.ToString(),
                    Items = Company.AssetTypes.Where(x => allowedVersionClasses.Contains(x.Class)).ToList()
                    .Select(i => new SelectListItem
                    {
                        Value = i.ID.ToString(),
                        Text = $"{i.Class.GetDisplayName()} :: {i.Name}"
                    }).OrderBy(x => x.Text).ToList()
                }
            };

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CustomAPIServiceEndpointVersion_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>();
            var a = Company.ApiEndpointVersions.Where(x => x.ID == id).FirstOrDefault();

            if (a == null)
            {
                return jsonException(FormControllerApiMessage.NoFoundSericeEndpointVersionToEdit, HttpStatusCode.NotFound);
            }

            var ent = Company.ApiEntities.FirstOrDefault(x => x.EndpointVersionID == a.ID);

            if (ent == null)
            {
                return jsonException(FormControllerApiMessage.NoFoundSericeEndpointVersionEntityToEdit, HttpStatusCode.NotFound);
            }

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
                FieldName = "URIPrefix",
                Name = "URI Segment", 
                FieldDescription = "",
                FieldType = DataType.Text.ToString(), 
                Value = a.UriPrefix, 
                Validations = checkAndAddValidation(fieldType: "Text",
                                                    friendlyName: "URIPrefix",
                                                    required: true,
                                                    pattern: "([A-Z]*[a-z]*[0-9]*){1,80}",
                                                    minLength: 1,
                                                    maxLength: 80,
                                                    validationMessage: "Must be between 1 and 80 alphanumeric characters in length.")
            });
            
            list.Add(new EditableField 
            { 
                Row = 2, 
                Column = 1, 
                Required = true, 
                FieldName = "MajorVersion", 
                Name = "Major Version", 
                FieldDescription = "", 
                FieldType = DataType.Number.ToString(), 
                Value = a.MajorVersion.ToString()
            });
            
            list.Add(new EditableField 
            {
                Row = 2, 
                Column = 2,
                Required = true,
                FieldName = "MinorVersion",
                Name = "Minor Version",
                FieldDescription = "",
                FieldType = DataType.Number.ToString(), 
                Value = a.MinorVersion.ToString() 
            });

            list.Add(new EditableField
            {
                Row = 3,
                Column = 1,
                Required = true,
                FieldName = "AssetType",
                Name = "Asset Type",
                FieldDescription = "",
                FieldType = DataType.Lookup.ToString(),
                Items = Company.AssetTypes.Where(x => allowedVersionClasses.Contains(x.Class)).ToList()
                    .Select(i => new SelectListItem
                    {
                        Value = i.ID.ToString(),
                        Text = $"{i.Class.GetDisplayName()} :: {i.Name}",
                        Selected = i.ID == ent.AssetTypeID
                    }).OrderBy(x => x.Text).ToList()
            });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddServiceEndpointVersion")]
        public JsonResult AddServiceEndpointVersion(FormCollection form)
        {
            try
            {
                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.endpoint);
                }

                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                }

                var endpointId = parseIntField(form, "EndpointID");
                var prefix = parseTextField(form, "URIPrefix");
                var majorVersion = parseIntField(form, "MajorVersion");
                var minorVersion = parseIntField(form, "MinorVersion");
                var assetType = parseIntField(form, "AssetType");

                if (string.IsNullOrEmpty(prefix))
                {
                    return jsonException(FormControllerApiMessage.APIServiceEndpointVersionPrefixNull, HttpStatusCode.NotFound);
                }

                var version = new ApiEndpointVersion
                {
                    MajorVersion = majorVersion,
                    MinorVersion = minorVersion,
                    UriPrefix = prefix,
                    EndpointID = endpointId
                };

                Company.Add(version);

                var entity = new ApiEntity
                {
                    AssetTypeID = assetType,
                    EndpointVersionID = version.ID,
                };

                Company.Add(entity);

                return jsonSuccess(FormControllerApiMessage.VersionCreated, version.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpPut, ValidateInput(false), Route("EditServiceEndpointVersion")]
        public JsonResult EditServiceEndpointVersion(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
                }

                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.version);
                }

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ApiEndpointVersion>(id);

                if (model == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.apiserviceversion);
                }

                model.UriPrefix = parseTextField(form, "URIPrefix");
                model.MajorVersion = parseIntField(form, "MajorVersion");
                model.MinorVersion = parseIntField(form, "MinorVersion");

                Company.Update(model);

                var assetTypeID = parseIntField(form, "AssetType");

                var entity = Company.ApiEntities.FirstOrDefault(x => x.EndpointVersionID == model.ID);

                if (entity == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.ApiServiceVersionEntity);
                }

                entity.AssetTypeID = assetTypeID;

                Company.Update(entity);

                return jsonSuccess(FormControllerApiMessage.VersionUpdated, id.ToString(), "edit", HttpStatusCode.OK);
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

        #region Custom API Service Endpoint Version Uri

        public JsonResult CustomAPIVersionUri_AddFields(int versionId)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
            }

            var entity = Company.ApiEntities.First(x => x.EndpointVersionID == versionId);

            var list = new List<EditableField>
            {
                new EditableField 
                { 
                    FieldName = "EntityID", 
                    FieldType = DataType.Hidden.ToString(), 
                    Value = entity.ID.ToString() 
                },
                
                new EditableField
                {
                    Row = 1,
                    Column = 1,
                    Required = true,
                    FieldName = "UriType",
                    Name = "Type",
                    FieldDescription = "",
                    FieldType = DataType.Lookup.ToString(),
                    Items =  new List<SelectListItem>
                    {
                        new SelectListItem{Text = "Singleton", Value = "2"},
                        new SelectListItem{Text = "Collection", Value = "1"},
                    }
                },
                
                new EditableField 
                { 
                    Row = 1, 
                    Column = 2, 
                    Required = true, 
                    FieldName = "Format", 
                    Name = "Segment", 
                    FieldDescription = "", 
                    FieldType = DataType.Text.ToString(), 
                    Validations = checkAndAddValidation(fieldType: "Text",
                                                        friendlyName: "Format",
                                                        required: true,
                                                        pattern: "([A-Z]*[a-z]*[0-9]*){1,80}",
                                                        minLength: 1,
                                                        maxLength: 80,
                                                        validationMessage: "Must be between 1 and 80 alphanumeric characters in length.") 
                }
            };

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CustomAPIVersionUri_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>();
            var a = Company.ApiEntityUris.Where(x => x.ID == id).FirstOrDefault();

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
                FieldName = "UriType",
                Name = "Type",
                FieldDescription = "",
                FieldType = DataType.Lookup.ToString(),
                Items = new List<SelectListItem>()
                {
                    new SelectListItem{Text = "Singleton", Value = "2", Selected = a.UriType == ApiUriType.Singleton},
                    new SelectListItem{Text = "Collection", Value = "1", Selected = a.UriType == ApiUriType.Collection},
                }
            });

            list.Add(new EditableField 
            {
                Row = 1, 
                Column = 2, 
                Required = true, 
                FieldName = "Format",
                Name = "Segment",
                FieldDescription = "",
                FieldType = DataType.Text.ToString(), 
                Value = a.Format, 
                Validations = checkAndAddValidation(fieldType: "Text",
                                                    friendlyName: "Format",
                                                    required: true,
                                                    pattern: "([A-Z]*[a-z]*[0-9]*){1,80}",
                                                    minLength: 1,
                                                    maxLength: 80,
                                                    validationMessage: "Must be between 1 and 80 alphanumeric characters in length.") 
            });

            return Json(list, JsonRequestBehavior.AllowGet);
        }


        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddServiceEndpointVersionUri")]
        public JsonResult AddServiceEndpointVersionUri(FormCollection form)
        {
            try
            {
                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.uriConstant);
                }

                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                }

                var entityId = parseIntField(form, "EntityID");
                var format = parseTextField(form, "Format");
                var uriType = (ApiUriType)parseIntField(form, "UriType");

                if (string.IsNullOrEmpty(format))
                {
                    return jsonException(FormControllerApiMessage.ApiServiceUriFormatIsNull, HttpStatusCode.NotFound);
                }

                var uri = new ApiEntityUri
                {
                    Format = format,
                    EntityID = entityId,
                    UriType = uriType
                };

                Company.Add(uri);

                return jsonSuccess(FormControllerApiMessage.UriCreated, uri.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpPut, ValidateInput(false), Route("EditServiceEndpointVersionUri")]
        public JsonResult EditServiceEndpointVersionUri(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
                }

                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.version);
                }

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ApiEntityUri>(id);

                if (model == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.ApiServiceVersionUri);
                }

                model.Format = parseTextField(form, "Format");
                model.UriType = (ApiUriType)parseIntField(form, "UriType");

                Company.Update(model);

                return jsonSuccess(FormControllerApiMessage.VersionUpdated, id.ToString(), "edit", HttpStatusCode.OK);
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

        #region Custom API Service Endpoint Version Field

        public JsonResult CustomAPIVersionField_AddFields(int versionId)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
            }

            var entity = Company.ApiEntities.First(x => x.EndpointVersionID == versionId);

            //get field types for this entity
            var list = new List<EditableField>
            {
                new EditableField 
                { 
                    FieldName = "EntityID", 
                    FieldType = DataType.Hidden.ToString(), 
                    Value = entity.ID.ToString() 
                },
                
                new EditableField
                {
                    Row = 1,
                    Column = 1,
                    Required = true,
                    FieldName = "FieldTypeID",
                    Name = "Field",
                    FieldDescription = "",
                    FieldType = DataType.Lookup.ToString(),
                    Items = Company.FieldTypes.Where(x => x.AssetTypeID == entity.AssetTypeID).ToList()
                    .Select(i => new SelectListItem
                                {
                                    Value = i.ID.ToString(),
                                    Text = i.FriendlyName
                                }).OrderBy(x => x.Text).ToList()
                },
                
                new EditableField 
                { 
                    Row = 1, 
                    Column = 2, 
                    Required = true, 
                    FieldName = "AllowSort", 
                    Name = "Allow Sort", 
                    FieldDescription = "", 
                    FieldType = DataType.Boolean.ToString() 
                },
                
                new EditableField 
                { 
                    Row = 2, 
                    Column = 1, 
                    Required = true, 
                    FieldName = "AllowSelect", 
                    Name = "Allow Select", 
                    FieldDescription = "", 
                    FieldType = DataType.Boolean.ToString()
                },
                
                new EditableField 
                { 
                    Row = 2,
                    Column = 1, 
                    Required = true, 
                    FieldName = "AllowFilter", 
                    Name = "Allow Filter", 
                    FieldDescription = "",
                    FieldType = DataType.Boolean.ToString()
                },
                
                new EditableField 
                { 
                    Row = 3,
                    Column = 1,
                    Required = false,
                    FieldName = "JsonFieldNameOverride",
                    Name = "Json Field Name Override",
                    FieldDescription = "",
                    FieldType = DataType.Text.ToString() 
                },
                
                new EditableField 
                { 
                    Row = 4, 
                    Column = 1, 
                    Required = false, 
                    FieldName = "XmlFieldNameOverride", 
                    Name = "Xml Field Name Override",
                    FieldDescription = "",
                    FieldType = DataType.Text.ToString() 
                }
            };

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CustomAPIVersionField_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
            }

            var list = new List<EditableField>();
            var a = Company.ApiEntityFieldTypes.Where(x => x.ID == id).FirstOrDefault();
            var entity = Company.ApiEntities.First(x => x.ID == a.EntityID);

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
                FieldName = "FieldTypeID",
                Name = "Field",
                FieldDescription = "",
                FieldType = DataType.Lookup.ToString(),
                Items = Company.FieldTypes.Where(x => x.AssetTypeID == entity.AssetTypeID).ToList()
                    .Select(i => new SelectListItem
                    {
                        Value = i.ID.ToString(),
                        Text = i.FriendlyName,
                        Selected = a.FieldTypeID == i.ID
                    }).OrderBy(x => x.Text).ToList()
            });

            list.Add(new EditableField 
            { 
                Row = 1, 
                Column = 2,
                Required = true,
                FieldName = "AllowSort", 
                Name = "Allow Sort", 
                FieldDescription = "", 
                FieldType = DataType.Boolean.ToString(), 
                Value = a.AllowSort.ToString() 
            });
            
            list.Add(new EditableField 
            { 
                Row = 2, 
                Column = 1,
                Required = true, 
                FieldName = "AllowSelect",
                Name = "Allow Select",
                FieldDescription = "", 
                FieldType = DataType.Boolean.ToString(),
                Value = a.AllowSelect.ToString() 
            });
            
            list.Add(new EditableField 
            {
                Row = 2,
                Column = 1, 
                Required = true,
                FieldName = "AllowFilter",
                Name = "Allow Filter",
                FieldDescription = "",
                FieldType = DataType.Boolean.ToString(),
                Value = a.AllowFilter.ToString() 
            });
            
            list.Add(new EditableField 
            {
                Row = 3,
                Column = 1, 
                Required = false,
                FieldName = "JsonFieldNameOverride", 
                Name = "Json Field Name Override",
                FieldDescription = "", 
                FieldType = DataType.Text.ToString(),
                Value = a.JsonFieldNameOverride 
            });
            
            list.Add(new EditableField 
            { 
                Row = 4,
                Column = 1, 
                Required = false,
                FieldName = "XmlFieldNameOverride",
                Name = "Xml Field Name Override",
                FieldDescription = "", 
                FieldType = DataType.Text.ToString(),
                Value = a.XmlFieldNameOverride 
            });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddServiceEndpointVersionField")]
        public JsonResult AddServiceEndpointVersionField(FormCollection form)
        {
            try
            {
                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.uriConstant);
                }

                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                }

                var entityId = parseIntField(form, "EntityID");
                var fieldTypeId = parseIntField(form, "FieldTypeID");
                var allowSort = parseBooleanField(form, "AllowSort");
                var allowSelect = parseBooleanField(form, "AllowSelect");
                var allowFilter = parseBooleanField(form, "AllowFilter");
                var jsonFieldNameOverride = parseTextField(form, "JsonFieldNameOverride");
                var xmlFieldNameOverride = parseTextField(form, "XmlFieldNameOverride");

                var field = new ApiEntityFieldType
                {
                    FieldTypeID = fieldTypeId,
                    EntityID = entityId,
                    AllowFilter = allowFilter,
                    AllowSelect = allowSelect,
                    AllowSort = allowSort
                };

                if (!string.IsNullOrWhiteSpace(jsonFieldNameOverride))
                {
                    field.JsonFieldNameOverride = jsonFieldNameOverride;
                }

                if (!string.IsNullOrWhiteSpace(xmlFieldNameOverride))
                {
                    field.XmlFieldNameOverride = xmlFieldNameOverride;
                }

                Company.Add(field);

                return jsonSuccess(FormControllerApiMessage.FieldCreated, field.EntityID.ToString(), "add", HttpStatusCode.Created);
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

        private JsonResult DeleteCustomAPIEndPoint(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");

                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
                }

                var o = Company.GetById<ApiEndpoint>(id);

                Company.Delete(o);

                return jsonSuccess(FormControllerApiMessage.EndPointRemoved, id.ToString(), "delete", HttpStatusCode.OK);
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

        private JsonResult DeleteCustomAPIService(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");

                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
                }

                var o = Company.GetById<ApiService>(id);

                Company.Delete(o);

                return jsonSuccess(FormControllerApiMessage.ApiServiceRemoved, id.ToString(), "delete", HttpStatusCode.OK);
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

        private JsonResult DeleteCustomAPIVersion(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");

                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
                }

                var o = Company.GetById<ApiEndpointVersion>(id);

                Company.Delete(o);

                return jsonSuccess(FormControllerApiMessage.ApiEndPointVersionRemoved, id.ToString(), "delete", HttpStatusCode.OK);
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

        private JsonResult DeleteCustomAPIUri(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");

                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
                }

                var o = Company.GetById<ApiEntityUri>(id);

                Company.Delete(o);

                return jsonSuccess(FormControllerApiMessage.ApiEndPointUriRemoved, id.ToString(), "delete", HttpStatusCode.OK);
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
        public JsonResult DeleteApiField(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");
                var o = Company.GetById<ApiEntityFieldType>(id);

                if (o == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.ApiField);
                }

                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
                }

                var multiSelectRecords = Company.ApiEntityFieldTypeMultiSelectFields.Where(i => i.EntityFieldTypeID == id);

                if (multiSelectRecords.Any())
                {
                    Company.ApiEntityFieldTypeMultiSelectFields.RemoveRange(multiSelectRecords);
                }

                Company.Delete(o);

                return jsonSuccess(FormControllerApiMessage.ApiFieldRemoved, id.ToString(), "delete", HttpStatusCode.OK);
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

        public JsonResult EditApiField(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
                }

                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.field);
                }

                var id = parseIntField(form, "ID");
                var model = Company.GetById<ApiEntityFieldType>(id);

                if (model == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.ApiField);
                }

                model.FieldTypeID = parseIntField(form, "FieldTypeID");
                model.AllowFilter = parseBooleanField(form, "AllowFilter");
                model.AllowSelect = parseBooleanField(form, "AllowSelect");
                model.AllowSort = parseBooleanField(form, "AllowSort");

                var jsonFieldNameOverride = parseTextField(form, "JsonFieldNameOverride");
                var xmlFieldNameOverride = parseTextField(form, "XmlFieldNameOverride");

                if (string.IsNullOrWhiteSpace(jsonFieldNameOverride))
                {
                    model.JsonFieldNameOverride = null;
                }
                else
                {
                    model.JsonFieldNameOverride = jsonFieldNameOverride;
                }

                if (string.IsNullOrWhiteSpace(xmlFieldNameOverride))
                {
                    model.XmlFieldNameOverride = null;
                }
                else
                {
                    model.XmlFieldNameOverride = xmlFieldNameOverride;
                }

                Company.Update(model);

                return jsonSuccess(FormControllerApiMessage.ApiFieldUpdated, id.ToString(), "edit", HttpStatusCode.OK);
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
