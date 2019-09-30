using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.extensions;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using Dapper;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using d360.model.validators;
using d360.model.DataAccessLayer;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling glossary-related data such as artifacts and models.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/fields"),
        Authorize,
        StringEnumController
    ]
    public class FieldsController : BaseV2ApiController
    {
        #region DI

        IQueueSource QueueSource;
        IStorageProvider Storage;
        IFieldsRepository FieldsRepository;

        public FieldsController(ICommunityContext community, ICompanyContext company, IStorageProvider storage, IQueueSource queueSource, IFieldsRepository fieldsRepository)
            : base(community, company)
        {
            QueueSource = queueSource;
            Storage = storage;
            FieldsRepository = fieldsRepository;
        }

        #endregion



        /// <summary>
        /// Retrieves field types contained within your environment.
        /// </summary>
        /// <remarks>
        /// If using Uid parameters, you may only provide one of the following: ActionTypeUid, AssetTypeUid, or RelationshipTypeUid.
        /// </remarks>
        /// <param name="AssetTypeUid">The asset type Uid to retrieve field types for.</param>
        /// <param name="RelationshipTypeUid">The relationship type Uid to retrieve field types for.</param>
        /// /// <param name="ActionTypeUid">The action type Uid to retrieve field types for.</param>
        /// <param name="Name">The API Name to search for.</param>
        /// <param name="FriendlyName">The Friendly Name to search for.</param>
        /// <param name="Type">The data type to search for.</param>
        /// <param name="_pageSize">The number of results to return per page. The default value is 200.</param>
        /// <param name="_pageNum">The page number to return results for.</param>
        /// <returns>A list of field types corresponding to the given criteria, if any.</returns>
        [
            HttpGet,
            Route(""),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(FieldTypesApiViewModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetFieldTypesAsync(Guid? AssetTypeUid = null, Guid? RelationshipTypeUid = null, Guid? ActionTypeUid = null, 
            string Name = "", string FriendlyName = "", DataType? Type = null, int? _pageSize = null, int? _pageNum = null)
        {
            var prefix = "Fields.GetFieldTypesAsync => ";
            var errorMessage = "";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var results = await FieldsRepository.GetFieldTypes(queryParams);
                if(results.Item2.StatusCode != HttpStatusCode.OK)
                    throw new RestApiException(results.Item2.StatusCode, results.Item2.Error, results.Item2.Message);

                return Request.CreateResponse(HttpStatusCode.OK, results.Item1);
            }
            catch (RestApiException ex)
            {
                errorMessage = ex.GetFullExceptionData(false);
                return ReturnApiError(ex.Status, errorMessage);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }

        }

        /// <summary>
        /// Adds or updates field types contained within your environment based on a specified ActionTypeUid, AssetTypeUid, or RelationshipTypeUid.
        /// </summary>
        /// <remarks>
        /// You may only provide one of the following: ActionTypeUid, AssetTypeUid, or RelationshipTypeUid.
        /// </remarks>
        /// <returns>A list of field types corresponding to the given criteria, if any.</returns>
        [
            HttpPut,
            Route(""),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutFieldTypesAsync(FieldTypesApiEditModel model)
        {
            var prefix = "Fields.PutFieldTypesAsync => ";
            var errorMessage = "";

            try
            {
                #region GetData
                TypeIdentifierInfoModel typeIdentifierInfoModel = null;

                IEnumerable<TypeIdentifierInfoModel> actionTypeIdentifierInfoModels = null;
                TypeIdentifierInfoModel actionTypeIdentifierInfoModel = null;

                IEnumerable<TypeIdentifierInfoModel> assetTypeIdentifierInfoModels = null;
                TypeIdentifierInfoModel assetTypeIdentifierInfoModel = null;

                IEnumerable<TypeIdentifierInfoModel> relationshipTypeIdentifierInfoModels = null;
                TypeIdentifierInfoModel relationshipTypeIdentifierInfoModel = null;

                if (model.ActionTypeUid.HasValue)
                {
                    actionTypeIdentifierInfoModels = await Company.GetTypeIdentifierInfoModel(TypeIdentifierInfoModelType.ActionType, model.ActionTypeUid.Value);
                    typeIdentifierInfoModel = actionTypeIdentifierInfoModel = actionTypeIdentifierInfoModels.SingleOrDefault();
                }

                if (model.AssetTypeUid.HasValue)
                {
                    assetTypeIdentifierInfoModels = await Company.GetTypeIdentifierInfoModel(TypeIdentifierInfoModelType.AssetType, model.AssetTypeUid.Value);
                    typeIdentifierInfoModel = assetTypeIdentifierInfoModel = assetTypeIdentifierInfoModels.SingleOrDefault();
                }

                if (model.RelationshipTypeUid.HasValue)
                {
                    relationshipTypeIdentifierInfoModels = await Company.GetTypeIdentifierInfoModel(TypeIdentifierInfoModelType.RelationshipType, model.RelationshipTypeUid.Value);
                    typeIdentifierInfoModel = relationshipTypeIdentifierInfoModel = relationshipTypeIdentifierInfoModels.SingleOrDefault();
                }
                #endregion

                #region SecurityCheck

                bool hasPermissions = false;

                if (Company.CurrentResourceIsAdmin)
                {
                    hasPermissions = true;
                }
                else
                {
                    var typePermissions = Company.GetTypePermissions(typeIdentifierInfoModel.Object, typeIdentifierInfoModel.ObjectID);
                    if (typePermissions != null)
                    {
                        hasPermissions = typePermissions.Any(i => i.ID == Permission.ModifyAsset);
                    }
                }

                if (!hasPermissions)
                {
                    throw new RestApiException(HttpStatusCode.Unauthorized, "Not authorized", "You do not have permissions to change fields on this type.");
                }
                #endregion

                #region Validation
                var existingFields = FieldsRepository.GetFieldTypes(typeIdentifierInfoModel);
                var isFusionEnabled = Community.IsFusionEnabled();
                var validationStatus = FieldApiModelValidator.ValidateModel(model, actionTypeIdentifierInfoModel, assetTypeIdentifierInfoModel, relationshipTypeIdentifierInfoModel, isFusionEnabled, existingFields);
                if (validationStatus.StatusCode != HttpStatusCode.OK)
                    throw new RestApiException(validationStatus.StatusCode, validationStatus.Error, validationStatus.Message);

                if (model.Action == FieldTypesApiEditAction.Replace)
                {
                    // This is a full replace, so we need to validate that there are no current assets before we allow this.
                    bool anyExistingItems = FieldsRepository.HasExistingItems(typeIdentifierInfoModel);

                    if (anyExistingItems)
                    {
                        throw new RestApiException(HttpStatusCode.BadRequest, "Existing items in system", $"There are existing items in your environment. You may not perform a Replace action until those items are removed.");
                    }
                }

                #endregion

                #region Validation done, time to do some work
                var status = FieldsRepository.UpdateFields(model, typeIdentifierInfoModel);
                if(status.StatusCode != HttpStatusCode.OK)
                    throw new RestApiException(status.StatusCode, status.Error, status.Message);

                #endregion

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new ApiStatusResponse { Message = "Fields successfully updated.", Success = true, Uid = typeIdentifierInfoModel.Uid })));
            }
            catch (RestApiException ex)
            {
                errorMessage = ex.GetFullExceptionData(false);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(ReturnApiError(ex.Status, errorMessage)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(ReturnApiError(HttpStatusCode.InternalServerError, errorMessage)));
            }

        }

        /// <summary>
        /// Removes field types contained within your environment.
        /// </summary>
        /// <remarks>
        /// You may only provide one of the following: ActionTypeUid, AssetTypeUid, or RelationshipTypeUid. Additionally, please keep in mind that the **Name** property for each item in the Fields collection refers to the **API Name** of the field.
        /// </remarks>
        /// <returns>A list of field types corresponding to the given criteria, if any.</returns>
        [
            HttpDelete,
            Route(""),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteFieldTypesAsync(FieldTypesApiDeleteModel model)
        {
            var prefix = "Fields.DeleteFieldTypesAsync => ";
            var errorMessage = "";

            try
            {
                #region GetData
                TypeIdentifierInfoModel typeIdentifierInfoModel = null;

                IEnumerable<TypeIdentifierInfoModel> actionTypeIdentifierInfoModels = null;
                TypeIdentifierInfoModel actionTypeIdentifierInfoModel = null;

                IEnumerable<TypeIdentifierInfoModel> assetTypeIdentifierInfoModels = null;
                TypeIdentifierInfoModel assetTypeIdentifierInfoModel = null;

                IEnumerable<TypeIdentifierInfoModel> relationshipTypeIdentifierInfoModels = null;
                TypeIdentifierInfoModel relationshipTypeIdentifierInfoModel = null;

                if (model.ActionTypeUid.HasValue)
                {
                    actionTypeIdentifierInfoModels = await Company.GetTypeIdentifierInfoModel(TypeIdentifierInfoModelType.ActionType, model.ActionTypeUid.Value);
                    typeIdentifierInfoModel = actionTypeIdentifierInfoModel = actionTypeIdentifierInfoModels.SingleOrDefault();
                }

                if (model.AssetTypeUid.HasValue)
                {
                    assetTypeIdentifierInfoModels = await Company.GetTypeIdentifierInfoModel(TypeIdentifierInfoModelType.AssetType, model.AssetTypeUid.Value);
                    typeIdentifierInfoModel = assetTypeIdentifierInfoModel = assetTypeIdentifierInfoModels.SingleOrDefault();
                }

                if (model.RelationshipTypeUid.HasValue)
                {
                    relationshipTypeIdentifierInfoModels = await Company.GetTypeIdentifierInfoModel(TypeIdentifierInfoModelType.RelationshipType, model.RelationshipTypeUid.Value);
                    typeIdentifierInfoModel = relationshipTypeIdentifierInfoModel = relationshipTypeIdentifierInfoModels.SingleOrDefault();
                }
                #endregion

                #region Security check

                bool hasPermissions = false;

                if (Company.CurrentResourceIsAdmin)
                {
                    hasPermissions = true;
                }
                else
                {
                    var typePermissions = Company.GetTypePermissions(typeIdentifierInfoModel.Object, typeIdentifierInfoModel.ObjectID);
                    if (typePermissions != null)
                    {
                        hasPermissions = typePermissions.Any(i => i.ID == Permission.DeleteAsset);
                    }
                }

                if (!hasPermissions)
                {
                    throw new RestApiException(HttpStatusCode.Unauthorized, "Not authorized", "You do not have permissions to remove fields on this type.");
                }

                #endregion

                #region Validation
                var validationStatus = FieldApiModelValidator.ValidateModel(model, actionTypeIdentifierInfoModel, assetTypeIdentifierInfoModel, relationshipTypeIdentifierInfoModel);
                if (validationStatus.StatusCode != HttpStatusCode.OK)
                    throw new RestApiException(validationStatus.StatusCode, validationStatus.Error, validationStatus.Message);

                bool anyExistingItems = FieldsRepository.HasExistingItems(typeIdentifierInfoModel);

                List<FieldType> currentFieldTypes = FieldsRepository.GetFieldTypes(typeIdentifierInfoModel);

                (var fieldValidatorStatus,List<string> fieldNamesToDelete) = FieldApiModelValidator.FieldValidator(model, anyExistingItems, currentFieldTypes);
                if (fieldValidatorStatus.StatusCode != HttpStatusCode.OK)
                    throw new RestApiException(fieldValidatorStatus.StatusCode, fieldValidatorStatus.Error, fieldValidatorStatus.Message);

                #endregion

                #region Validation done, time to do some work
                FieldsRepository.DeleteFields(currentFieldTypes, fieldNamesToDelete);

                #endregion

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new ApiStatusResponse { Message = "Fields successfully removed.", Success = true, Uid = typeIdentifierInfoModel.Uid })));
            }
            catch (RestApiException ex)
            {
                errorMessage = ex.GetFullExceptionData(false);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(ReturnApiError(ex.Status, errorMessage)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(ReturnApiError(HttpStatusCode.InternalServerError, errorMessage)));
            }

        }

    }
}
