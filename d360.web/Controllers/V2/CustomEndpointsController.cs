using d360.core.entities;
using d360.model;
using d360.web.Models;
using d360.web.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System.Threading.Tasks;
using System.Web.Http.Description;
using Resources;
using d360.model.DataAccessLayer;

namespace d360.web.Controllers.V2
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [
    ApiVersion("2.0"),
    RoutePrefix("api/v{version:apiVersion}/customendpoints"),
    Authorize
]
    public class CustomEndpointsController : BaseV2ApiController
    {

        #region DI

        public CustomEndpointsController(ICommunityContext community, ICompanyContext company, ISettingsRepository settingsRepository) :base(community, company, settingsRepository)
        {

        }
        #endregion

        /// <summary>
        /// Gets the field types
        /// </summary>
        /// <param name="versionId">End Point Version Id</param>
        /// <returns>The field types</returns>
        [
              HttpGet,
              Route("Version/FieldEditor/FieldTypes"),
              SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
              SwaggerResponse(HttpStatusCode.OK, "A list of Field types"),
              SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset was not found.", typeof(ErrorResponse)),
              SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
          ]
        public async Task<IHttpActionResult> CustomAPIVersionFieldEditor_GetFieldTypes(int versionId)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.ForbiddenUserNotAuthorizedMessage)).ConfigureAwait(false);
            }

            var entity = Company.ApiEntities.First(x => x.EndpointVersionID == versionId);

            if (entity == null)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound,ApiMessages.ApiEntryNotFound)).ConfigureAwait(false);
            }

            var fieldTypes = Company.FieldTypes.Where(x => x.AssetTypeID == entity.AssetTypeID).ToList();
     
            return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, fieldTypes))).ConfigureAwait(false);

        }

        
        /// <summary>
        /// Gets the lookupFields for a fieldtypeID
        /// </summary>
        /// <param name="fieldTypeId">Field Type Id</param>
        /// <returns>List of lookup fields</returns>
        [
            HttpGet,
            Route("Version/FieldEditor/LookupFields"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of lookup fields.", typeof(List<System.Web.Mvc.SelectListItem>)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> CustomAPIVersionFieldEditor_GetLookupFields(int fieldTypeId)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.ForbiddenUserNotAuthorizedMessage)).ConfigureAwait(false);

            var fieldType = Company.GetById<FieldType>(fieldTypeId);

            if (fieldType == null)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ApiMessages.FieldTypeNotFound)).ConfigureAwait(false);
            }


            if (!fieldType.AllowMultipleValues || fieldType.LookupObjectType != "ReferenceItem")
            {
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new List<System.Web.Mvc.SelectListItem>()))).ConfigureAwait(false);
            }



            var fields = Company.FieldTypes
                .Where(f => f.Object == "ReferenceItemType" && f.ObjectID == fieldType.LookupObjectID)
                .Select(i => new System.Web.Mvc.SelectListItem { Text = i.FriendlyName, Value = i.ID.ToString() })
                .ToList();


            return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, fields))).ConfigureAwait(false);


        }

        /// <summary>
        /// Gets the model for field editor
        /// </summary>
        /// <param name="id">ApiEntityFieldType Id</param>
        /// <returns>The model for the field editor</returns>
        [
            HttpGet,
            Route("Version/FieldEditor/model"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "Model for field editor."),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> CustomAPIVersionFieldEditor_EditModel(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.ForbiddenUserNotAuthorizedMessage)).ConfigureAwait(false);
            }

            var model = Company.GetById<ApiEntityFieldType>(id);

            if (model == null)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ApiMessages.FieldTypeNotFound)).ConfigureAwait(false);
            }

            var entity = Company.GetById<ApiEntity>(model.EntityID);
            var fieldType = Company.GetById<FieldType>(model.FieldTypeID);

            var multiSelectRecords = Company.ApiEntityFieldTypeMultiSelectFields
                .Where(i => i.EntityFieldTypeID == id)
                .Select(i => i.FieldTypeID.ToString());

            var multiSelectFieldTypes = Company.FieldTypes
                .Where(f => f.Object == "ReferenceItemType" && f.ObjectID == fieldType.LookupObjectID)
                .Select(i => new System.Web.Mvc.SelectListItem() { Text = i.FriendlyName, Value = i.ID.ToString() })
                .ToList();

            var selectedFields = multiSelectFieldTypes.Where(i => multiSelectRecords.Contains(i.Value)).ToList();

            var fieldTypes = Company.FieldTypes.Where(x => x.AssetTypeID == entity.AssetTypeID).ToList();

            var Data = new
            {
                model,
                fieldTypes,
                multiSelectFieldTypes,
                selectedFields
            };


            return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, Data))).ConfigureAwait(false);

        }


        /// <summary>
        /// Adds a given  field type based on the specific field type
        /// </summary>
        /// <param name="model">API Entity Field Type</param>
        /// <returns>An HTTP status code and newly added field type</returns>
        [
    HttpPost,
    Route("Version/FieldEditor/Field"),
    SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
   SwaggerResponse(HttpStatusCode.OK, "API Entity Field Type", typeof(ApiEntityFieldType)),
    SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset was not found.", typeof(ErrorResponse)),
    SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
]

        public async Task<IHttpActionResult> AddCustomAPIVersionField(ApiEntityFieldType model)
        {
            var prefix = "CustomEndPoints.AddCustomAPIVersionField => ";
            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.ForbiddenUserNotAuthorizedMessage)).ConfigureAwait(false);
            }

            if (model == null)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ApiMessages.ErrorInvalidDatasetMessage)).ConfigureAwait(false);
            }

            try
            {
                if (model.ID < 1)
                {
                    Company.Add(model);
                    Company.SaveChanges();

                    if (model?.MultiSelectFields?.Any() ?? false)
                    {
                        Company.ApiEntityFieldTypeMultiSelectFields.AddRange(
                            model.MultiSelectFields.Select(i => new ApiEntityFieldTypeMultiSelectField { EntityFieldTypeID = model.ID, FieldTypeID = i.FieldTypeID }).ToList());
                    }
                }
                else
                {
                    var existing = Company.ApiEntityFieldTypes.FirstOrDefault(i => i.ID == model.ID);

                    if (existing == null)
                    {
                        throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,ApiMessages.ApiEntryNotFound));
                    }

                    var existingMultiSelect = Company.ApiEntityFieldTypeMultiSelectFields.Where(i => i.EntityFieldTypeID == model.ID).ToList();
                    Company.ApiEntityFieldTypeMultiSelectFields.RemoveRange(existingMultiSelect);

                    existing.FieldTypeID = model.FieldTypeID;
                    existing.JsonFieldNameOverride = model.JsonFieldNameOverride;
                    existing.XmlFieldNameOverride = model.XmlFieldNameOverride;
                    existing.ItemNameOverride = model.ItemNameOverride;
                    existing.AllowFilter = model.AllowFilter;
                    existing.AllowSelect = model.AllowSelect;
                    existing.AllowSort = model.AllowSort;

                    Company.SaveChanges();

                    if (model?.MultiSelectFields?.Any() ?? false)
                    {
                        Company.ApiEntityFieldTypeMultiSelectFields.AddRange(model.MultiSelectFields);
                    }
                    Company.SaveChanges();
                    
                }
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {

                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }

           

        }

    }
}
