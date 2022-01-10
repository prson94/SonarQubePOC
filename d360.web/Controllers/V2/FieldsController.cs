using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.extensions;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using Dapper;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using d360.model.validators;
using d360.model.DataAccessLayer;
using Resources;
using System.Web.Http.Description;
using d360.core.helpers;
using System.Globalization;

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
        private readonly IAssetRepository AssetRepository;

        public FieldsController(ICoreComponentSet set, IStorageProvider storage, IQueueSource queueSource, IFieldsRepository fieldsRepository, IAssetRepository assetRepository)
            : base(set)
        {
            QueueSource = queueSource;
            Storage = storage;
            FieldsRepository = fieldsRepository;
            AssetRepository = assetRepository;
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
        /// <param name="ActionTypeUid">The action type Uid to retrieve field types for.</param>
        /// <param name="Name">The API Name to search for.</param>
        /// <param name="FriendlyName">The Friendly Name to search for.</param>
        /// <param name="Type">The data type to search for.</param>
        /// <param name="_pageSize">The number of results to return per page. The default value is 250.</param>
        /// <param name="_pageNum">The page number to return results for.</param>
        /// <returns>A list of field types corresponding to the given criteria, if any.</returns>
        [
            HttpGet,
            Route(""),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(FieldTypesApiViewModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetFieldTypesAsync(Guid? AssetTypeUid = null, Guid? RelationshipTypeUid = null, Guid? ActionTypeUid = null,
            string Name = "", string FriendlyName = "", DataType? Type = null, int? _pageSize = null, int? _pageNum = null)
        {
            var prefix = "Fields.GetFieldTypesAsync => ";
            var errorMessage = "";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                string isValid = isPageSizeAndNumValid(queryParams);

                if (!string.IsNullOrEmpty(isValid))
                {
                    throw new RestApiException(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, isValid);
                }
                var results = await FieldsRepository.GetFieldTypes(queryParams);
                if (results.Item2.StatusCode != HttpStatusCode.OK)
                {
                    throw new RestApiException(results.Item2.StatusCode, results.Item2.Error, results.Item2.Message);
                }

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
        /// You may only provide one of the following: `ActionTypeUid`, `AssetTypeUid`, or `RelationshipTypeUid`.
        /// 
        /// There are some general rules about the various field types:
        /// - `Boolean` *(True/False)*
        ///     1. Supports adding values through the Govern Application UI and REST API.        
        /// - `ComputedOwnershipLookup` *(Ownership Lookup)*
        ///     1. This is a computed field and does not support directly editing values.
        /// - `ComputedRelationshipField` *(Field from Relationship)*
        ///     1. This is a computed field and does not support directly editing values.
        /// - `ComputedRelationshipLookup` *(Relation Lookup)*
        ///     1. This is a computed field and does not support directly editing values.
        /// - `ComputedRelationshipReferenceList` *(Reference Item List from Relationship)*
        ///     1. This is a computed field and does not support directly editing values.
        /// - `Counter` *(Counter)*
        ///     1. Supports adding values through the REST API.
        /// - `Date` *(Date)*
        ///     1. Supports adding values through the Govern Application UI and REST API.
        /// - `DateTime` *(Date With Time)*
        ///     1. Supports adding values through the Govern Application UI and REST API.
        /// - `Decimal` *(Decimal Number)*
        ///     1. Supports adding values through the Govern Application UI and REST API.
        /// - `Html` *(Html/Richtext)*
        ///     1. Supports adding values through the Govern Application UI and REST API.
        /// - `Json` *(JSON)*
        ///     1. Supports adding only through the REST API.
        /// - `JsonElement` *(JSON Attribute)*
        ///     1. This is a computed field and does not support directly editing values.
        ///     2. The corresponding Json field must be added beforehand.
        ///     3. Valid values for `JsonAttribute.DataType` are: 
        ///         - **bigint** : Value representing a large 64-bit number greater than 2,147,483,647.
        ///         - **bit** : Value representing true/false.
        ///         - **date** : Value representing a date without a time component.
        ///         - **datetime** : Value representing a date with a time component.
        ///         - **float** : Value representing a variable length decimal number.
        ///         - **int** : Value representing a large 32-bit number less than 2,147,483,647.
        ///         - **nvarchar** : Value representing unicode text.
        /// - `Link` *(Link)*
        ///     1. Supports adding values through the Govern Application UI and REST API.
        ///     2. The expected data format for values is: `Link Name`|`Link Url`
        /// - `Lookup` *(List)*
        ///     1. Supports adding values through the Govern Application UI and REST API.
        /// - `Number` *(Number)*
        ///     1. Supports adding values through the Govern Application UI and REST API.
        /// - `Path` *(Asset Path)*
        ///     1. This is a computed field and does not support directly editing values.
        /// - `Relationship` *(Relationship)*
        ///     1. This is a computed field and does not support directly editing values.
        /// - `Score` *(Score)*
        ///     1. This is a computed field and does not support directly editing values.
        /// - `Tag` *(Tag)*
        ///     1. This is a computed field and does not support directly editing values.
        /// - `Text` *(Simple Text)*
        ///     1. Supports adding values through the Govern Application UI and REST API.
        /// </remarks>
        /// <returns>A list of field types corresponding to the given criteria, if any.</returns>
        [
            HttpPut,
            Route(""),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutFieldTypesAsync(FieldTypesApiEditModel model)
        {
            var prefix = "Fields.PutFieldTypesAsync => ";
            var errorMessage = "";

            try
            {

                if (model == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.JSONValidMessage)).ConfigureAwait(false);
                }

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

                    if (typeIdentifierInfoModel == null)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.ActionTypeUidIsNotValid, model.AssetTypeUid.Value.ToString()))).ConfigureAwait(false);
                    }
                }

                if (model.AssetTypeUid.HasValue)
                {
                    assetTypeIdentifierInfoModels = await Company.GetTypeIdentifierInfoModel(TypeIdentifierInfoModelType.AssetType, model.AssetTypeUid.Value);
                    typeIdentifierInfoModel = assetTypeIdentifierInfoModel = assetTypeIdentifierInfoModels.SingleOrDefault();

                    if (typeIdentifierInfoModel == null)
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.AssetTypeNotFound, model.AssetTypeUid.Value.ToString()))).ConfigureAwait(false);
                }

                if (model.RelationshipTypeUid.HasValue)
                {
                    relationshipTypeIdentifierInfoModels = await Company.GetTypeIdentifierInfoModel(TypeIdentifierInfoModelType.RelationshipType, model.RelationshipTypeUid.Value);
                    typeIdentifierInfoModel = relationshipTypeIdentifierInfoModel = relationshipTypeIdentifierInfoModels.SingleOrDefault();

                    if (typeIdentifierInfoModel == null)
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.RelationShipTypeUidNotFound, model.AssetTypeUid.Value.ToString()))).ConfigureAwait(false);
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
                        hasPermissions = typePermissions.Any(i => i.ID == Permission.EditAsset);
                    }
                }

                if (!hasPermissions)
                {
                    throw new RestApiException(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.ChangeFieldNotAllowed);
                }

                #endregion

                #region Validation

                var existingFields = FieldsRepository.GetFieldTypes(typeIdentifierInfoModel);
                var ExistingIntersectID = new List<Tuple<string, Guid>>();
                if (model.AssetTypeUid.HasValue)
                {
                    ExistingIntersectID = FieldsRepository.GetFieldInterSetUID(existingFields);
                }

                var validationStatus = FieldApiModelValidator.ValidateModel(model, actionTypeIdentifierInfoModel, assetTypeIdentifierInfoModel, relationshipTypeIdentifierInfoModel, existingFields, ExistingIntersectID);
                if (validationStatus.StatusCode != HttpStatusCode.OK)
                    throw new RestApiException(validationStatus.StatusCode, validationStatus.Error, validationStatus.Message);

                if (assetTypeIdentifierInfoModel != null && model.Fields.Any(x => x.Type.Counter != null))
                {
                    int currentAssetCount = Company.Assets.Where(x => x.AssetTypeID == assetTypeIdentifierInfoModel.ID).Count();
                    model.Fields.ForEach(ft =>
                    {
                        int? currentInitialIndex = Company.FieldTypes.Where(x => x.AssetTypeID == assetTypeIdentifierInfoModel.ID && x.Name == ft.Name).FirstOrDefault()?.CounterInitialIndex;
                        if (ft.Type.Counter != null)
                        {
                            if (ft.Type.Counter.CounterInitialIndex != currentInitialIndex && ft.Type.Counter.CounterInitialIndex <= currentAssetCount)
                            {
                                throw new RestApiException(HttpStatusCode.BadRequest, ApiMessages.FieldTypeError, string.Format(ApiMessages.CounterInitialValueHigherCurrentValue, currentAssetCount.ToString()));
                            }
                        }
                    });
                }

                if (model.Fields.Any(x => x.Type.Lookup != null))
                {
                    foreach (var ft in model.Fields.Where(x => x.Type.Lookup != null))
                    {
                        var exFt = existingFields.FirstOrDefault(x => x.Name == ft.Name);
                        if (exFt == null)
                        {
                            continue;
                        }
                        var hasFields = Company.Query<int>("select count(1) from field where fieldtypeid = @ftid", new { ftid = exFt.ID }).FirstOrDefault() > 0;
                        if (hasFields)
                        {
                            var newType = Company.AssetTypes.Where(x => x.uid == ft.Type.Lookup.List.Uid)
                                .Select(x => new { x.Object, x.ObjectID }).FirstOrDefault();
                            if (newType.Object.Replace("Type", "") != exFt.LookupObjectType || newType.ObjectID != exFt.LookupObjectID)
                            {
                                throw new RestApiException(HttpStatusCode.BadRequest, ApiMessages.ChangeFieldNotAllowed, string.Format(ApiMessages.LookupFieldTypeInUse, exFt.FriendlyName));
                            }
                        }

                    }
                }

                if (model.Action == FieldTypesApiEditAction.Replace)
                {
                    // This is a full replace, so we need to validate that there are no current assets before we allow this.
                    bool anyExistingItems = FieldsRepository.HasExistingItems(typeIdentifierInfoModel);

                    if (anyExistingItems)
                    {
                        throw new RestApiException(HttpStatusCode.BadRequest, ApiMessages.ExistItemInSystem, ApiMessages.ItemExistsNotReplaceMessage);
                    }
                }

                #endregion

                #region Validation done, time to do some work

                foreach (var field in model.Fields)
                {
                    if (field.Type?.Text?.Validation != null && (!string.IsNullOrEmpty(field.Type.Text.Validation.Pattern) || !field.Type.Text.Validation.IsRequired))
                    {
                        field.Type.Text.Validation.MinimumLength = 0;
                    }
                }

                var status = FieldsRepository.UpdateFields(model, typeIdentifierInfoModel);
                if (status.StatusCode != HttpStatusCode.OK)
                {
                    throw new RestApiException(status.StatusCode, status.Error, status.Message);
                }

                #endregion

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new ApiStatusResponse { Message = "Fields successfully updated.", Success = true, Uid = typeIdentifierInfoModel.Uid }))).ConfigureAwait(false);
            }
            catch (RestApiException ex)
            {
                errorMessage = ex.GetFullExceptionData(false);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(ReturnApiError(ex.Status, errorMessage))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(ReturnApiError(HttpStatusCode.InternalServerError, errorMessage))).ConfigureAwait(false);
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteFieldTypesAsync(FieldTypesApiDeleteModel model)
        {
            var prefix = "Fields.DeleteFieldTypesAsync => ";
            var errorMessage = "";

            try
            {
                (TypeIdentifierInfoModel typeIdentifierInfoModel, WorkHttpStatus validationStatus) = await GetTypeIdentifierInfoModelAndValidate(model).ConfigureAwait(false);

                if (model.AssetTypeUid.HasValue && typeIdentifierInfoModel != null && typeIdentifierInfoModel.Object == SystemObjects.TaskType.ToString())
                {
                    if (model.Fields.Any(x => new string[] { "Name", "GovernanceRole", "StepNo" }.Contains(x.Name)))
                    {
                        throw new RestApiException(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.DiagramAssetTypeSystemFieldValidation);
                    }
                }

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
                    throw new RestApiException(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.RemoveFieldNotAllowed);
                }

                #endregion

                #region Validation

                if (validationStatus.StatusCode != HttpStatusCode.OK)
                {
                    throw new RestApiException(validationStatus.StatusCode, validationStatus.Error, validationStatus.Message);
                }

                bool anyExistingItems = FieldsRepository.HasExistingItems(typeIdentifierInfoModel);

                List<FieldType> currentFieldTypes = FieldsRepository.GetFieldTypes(typeIdentifierInfoModel);
                bool anyResponsibilitiesUsingField = FieldsRepository.hasResponsibilityUsingField(typeIdentifierInfoModel, currentFieldTypes.FindAll(x => model.Fields.Any(f => f.Name == x.Name)));

                if (anyResponsibilitiesUsingField)
                {
                    throw new RestApiException(HttpStatusCode.BadRequest, ApiMessages.UsedinResponsibilityRules, ApiMessages.FieldUseInResponsibilityRule);
                }

                (var fieldValidatorStatus, List<string> fieldNamesToDelete) = FieldApiModelValidator.FieldValidator(model, anyExistingItems, currentFieldTypes);
                if (fieldValidatorStatus.StatusCode != HttpStatusCode.OK)
                {
                    throw new RestApiException(fieldValidatorStatus.StatusCode, fieldValidatorStatus.Error, fieldValidatorStatus.Message);
                }

                #endregion

                #region Validation done, time to do some work
                FieldsRepository.DeleteFields(currentFieldTypes, fieldNamesToDelete);

                #endregion

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new ApiStatusResponse { Message = "Fields successfully removed.", Success = true, Uid = typeIdentifierInfoModel.Uid }))).ConfigureAwait(false);
            }
            catch (RestApiException ex)
            {
                errorMessage = ex.GetFullExceptionData(false);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(ReturnApiError(ex.Status, errorMessage))).ConfigureAwait(false);
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
        /// Removes field types contained within your environment in batch.
        /// </summary>
        /// <remarks>
        /// You may only provide one of the following: ActionTypeUid, AssetTypeUid, or RelationshipTypeUid. Additionally, please keep in mind that the **Name** property for each item in the Fields collection refers to the **API Name** of the field.
        /// </remarks>
        /// <returns>The execution id of the batch process.</returns>
        [
            HttpDelete,
            Route("batch"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution's unique identifier to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "User is not an administrator.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteFieldTypesBacthAsync(FieldTypesApiDeleteModel model)
        {
            var prefix = "Fields.DeleteFieldTypesBacthAsync => ";
            var errorMessage = "";

            #region Security check

            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage)).ConfigureAwait(false);
            }

            #endregion

            try
            {
                (TypeIdentifierInfoModel typeIdentifierInfoModel, WorkHttpStatus validationStatus) = await GetTypeIdentifierInfoModelAndValidate(model).ConfigureAwait(false);

                if (model.AssetTypeUid.HasValue && typeIdentifierInfoModel != null && typeIdentifierInfoModel.Object == SystemObjects.TaskType.ToString())
                {
                    if (model.Fields.Any(x => new string[] { "Name", "GovernanceRole", "StepNo" }.Contains(x.Name)))
                    {
                        throw new RestApiException(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.DiagramAssetTypeSystemFieldValidation);
                    }
                }

                if (validationStatus.StatusCode != HttpStatusCode.OK)
                {
                    throw new RestApiException(validationStatus.StatusCode, validationStatus.Error, validationStatus.Message);
                }

                bool anyExistingItems = FieldsRepository.HasExistingItems(typeIdentifierInfoModel);

                List<FieldType> currentFieldTypes = FieldsRepository.GetFieldTypes(typeIdentifierInfoModel);
                bool anyResponsibilitiesUsingField = FieldsRepository.hasResponsibilityUsingField(typeIdentifierInfoModel, currentFieldTypes.FindAll(x => model.Fields.Any(f => f.Name == x.Name)));

                if (anyResponsibilitiesUsingField)
                {
                    throw new RestApiException(HttpStatusCode.BadRequest, ApiMessages.UsedinResponsibilityRules, ApiMessages.FieldUseInResponsibilityRule);
                }

                (var fieldValidatorStatus, List<string> fieldNamesToDelete) = FieldApiModelValidator.FieldValidator(model, anyExistingItems, currentFieldTypes);
                if (fieldValidatorStatus.StatusCode != HttpStatusCode.OK)
                {
                    throw new RestApiException(fieldValidatorStatus.StatusCode, fieldValidatorStatus.Error, fieldValidatorStatus.Message);
                }

                var execution = getApiExecution(fieldNamesToDelete != null ? fieldNamesToDelete.Count : 0, new ApiExecutionFields_DeleteFieldtypes { TypeIdentifierInfo = typeIdentifierInfoModel, FieldNamesToDelete = fieldNamesToDelete });

                var executionInfo = await FieldsRepository.BatchDeleteFields(execution);

                return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            new ApiExecutionRecievedResponse
                            {
                                ExecutionID = executionInfo.ExecutionID,
                                Message = ApiMessages.ExecutionIDStatus,
                                Uri = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Host}/api/v2/assets/executions/{executionInfo.ExecutionID}/status"
                            }
                        )
                    )
                ).ConfigureAwait(false);
            }
            catch (RestApiException ex)
            {
                errorMessage = ex.GetFullExceptionData(false);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(ReturnApiError(ex.Status, errorMessage))).ConfigureAwait(false);
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

        private async Task<(TypeIdentifierInfoModel, WorkHttpStatus)> GetTypeIdentifierInfoModelAndValidate(FieldTypesApiDeleteModel model)
        {
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

            var validationStatus = FieldApiModelValidator.ValidateModel(model, actionTypeIdentifierInfoModel, assetTypeIdentifierInfoModel, relationshipTypeIdentifierInfoModel);

            return (typeIdentifierInfoModel, validationStatus);
        }


        #region FormHelpers NOT TO BE EXPOSED IN SWAGGER DOC

        /// <summary>
        /// Gets the default values for the all lookups in the Field Form.
        /// </summary>
        /// <returns>A list of Lookup options, if any.</returns>
        [
            HttpGet,
            Route("GetLookups"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<HttpResponseMessage> GetLookups(Guid? AssetTypeUid = null, Guid? RelationshipTypeUid = null, Guid? ActionTypeUid = null)
        {
            var prefix = "Fields.GetLookups => ";
            var errorMessage = "";

            try
            {
                #region Load static lists

                int id = 0;
                SystemObjects type = SystemObjects.ArtifactType;
                AssetTypeClass @class = AssetTypeClass.Generic;
                if (AssetTypeUid != null)
                {
                    var assetType = Company.Filter<AssetType>(x => x.uid == AssetTypeUid).SingleOrDefault();
                    id = assetType.ObjectID;
                    Enum.TryParse(assetType.Object, out type);
                    @class = assetType.Class;
                }
                else if (ActionTypeUid != null)
                {
                    var issueType = Company.Filter<IssueType>(x => x.uid == ActionTypeUid).SingleOrDefault();
                    id = issueType.ID;
                    type = SystemObjects.IssueType;
                }
                else if (RelationshipTypeUid != null)
                {
                    var intersectType = Company.Filter<IntersectType>(i => i.uid == RelationshipTypeUid).SingleOrDefault();
                    id = intersectType.ID;
                    type = SystemObjects.IntersectType;
                }
                else
                {
                    throw new ArgumentNullException(ApiMessages.NotValidAssetActionRelationTypeProvided);
                }

                var lists = await Company.QueryAsync<dynamic>("exec utility.GetFieldTypeLookupList");
                var intersectTypes = lists.Where(i => i.type == "I").Select(i => new { i.value, i.title }).OrderBy(i => i.title);
                var attributes = lists.Where(i => i.type == "A").Select(i => new { i.value, i.title }).OrderBy(i => i.title);
                var lookups = lists.Where(i => i.type == "L").Select(i => new { i.value, i.title }).OrderBy(i => i.title);
                var filteredLookups = lists.Where(i => i.type == "FL").Select(i => new { i.value, i.title }).OrderBy(i => i.title);

                var complexLookupRelations = ComplexLookupRelationType.ChildItem.GetComplexLookupRelationTypeInfoList().ToList();

                var sType = type.ToString();

                IQueryable<IntersectTypeDetail> queryAllRelationships = Company.Filter<IntersectTypeDetail>(i =>
                    (i.Subject == sType && i.SubjectID == id) ||
                    (i.Object == sType && i.ObjectID == id)
                );

                //Hide self reference relationships for models and policies 
                if (type == SystemObjects.TaxonomyType || type == SystemObjects.PolicyType)
                {
                    queryAllRelationships = queryAllRelationships.Where(x => x.PredicateType != PredicateType.IntraTypeHierarchy);
                }

                var allRelationships = queryAllRelationships.ToList();

                var excludedFieldRelationshipPredicates = new List<PredicateType> { PredicateType.Diagram, PredicateType.DiagramUse, PredicateType.DiagramReference };

                var cardinalRelationships = allRelationships.Where(i =>
                    (i.Subject == sType && i.SubjectID == id && i.SubjectCardinality == Cardinality.One) ||
                    (i.Object == sType && i.ObjectID == id && i.ObjectCardinality == Cardinality.One)
                ).ToList();

                var fieldFromRelRelationships = allRelationships.Where(i =>
                    (!i.PredicateType.HasValue || !excludedFieldRelationshipPredicates.Contains(i.PredicateType.Value)) &&
                    (
                        (i.Subject == sType && i.SubjectID == id && i.ObjectCardinality == Cardinality.One) ||
                        (i.Object == sType && i.ObjectID == id && i.SubjectCardinality == Cardinality.One)
                    )
                ).ToList();

                var Field_Relationships = allRelationships
                    .Where(x => (!x.PredicateType.HasValue || !excludedFieldRelationshipPredicates.Contains(x.PredicateType.Value))
                                && x.PredicateType != PredicateType.InterTypeHierarchy
                                && x.Object != SystemObjects.IntersectType.ToString()
                                && x.Subject != SystemObjects.IntersectType.ToString()
                               )
                    .Select(i => new
                    {
                        title = ((i.Subject == sType && i.SubjectID == id) ?
                            $"{i.PredicateName} {i.ObjectAssetTypePath}" :
                            $"{i.PredicateInverse} {i.SubjectAssetTypePath}"),
                        value = i.Uid
                    }).OrderBy(i => i.title);

                var Field_CardinalRelationships = cardinalRelationships
                    .Select(i => new
                    {
                        title = ((i.Subject == sType && i.SubjectID == id) ?
                            $"{i.SubjectName} {i.PredicateName} {i.ObjectName}" :
                            $"{i.ObjectName} {i.PredicateInverse} {i.SubjectName}"),
                        value = i.Uid
                    });

                var Field_CardinalReferenceRelationships = cardinalRelationships
                    .Where(i =>
                        (i.Subject == sType && i.SubjectID == id) ?
                            (i.Object == SystemObjects.ReferenceItemType.ToString() && i.ObjectID == 0) :
                            (i.Subject == SystemObjects.ReferenceItemType.ToString() && i.SubjectID == 0)
                    )
                    .Select(i => new
                    {
                        title = ((i.Subject == sType && i.SubjectID == id) ?
                            $"{i.SubjectName} {i.PredicateName} {i.ObjectName}" :
                            $"{i.ObjectName} {i.PredicateInverse} {i.SubjectName}"),
                        value = i.Uid
                    });

                var Field_FieldFromRelRelationships = fieldFromRelRelationships.Select(i => new
                {
                    title = ((i.Subject == sType && i.SubjectID == id) ?
                            $"{i.SubjectName} {i.PredicateName} {i.ObjectName}" :
                            $"{i.ObjectName} {i.PredicateInverse} {i.SubjectName}"),
                    value = i.Uid
                });

                var patterns = new Dictionary<string, string>() {
                { "Choose sample...", "" },
                { "Email", @"^$|\b([A-Za-z0-9'_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})\b" },
                { "IP Address", @"^$|^([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3})$" },
                { "North American Phone", @"^$|\b\d{3}[-.]?\d{3}[-.]?\d{4}\b" },
                { "Internal Url", @"^$|\b(http(s)?:\/\/){1}([\da-z\.-]+)([\/\w \.-]*)*\/?\b" },
                { "Public Url", @"^$|\b(http(s)?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?\b" },
                { "US Zip Code", @"^(\d{5}(?:\-\d{4})?)$" }
            };
                var dataTypeOptions = DataType.Boolean.GetDataTypeInfoList(type)
                        .Where(i => !i.ReadOnly)
                        .Select(i => new
                        {
                            title = i.Description,
                            value = i.Name
                        })
                        .OrderBy(i => i.title).ToList();

                if (ActionTypeUid != null || RelationshipTypeUid != null)
                {
                    dataTypeOptions = dataTypeOptions.Where(x => x.value != "Path" && x.value != "Score").ToList();
                }

                bool enableJsonAttributes = false;

                try
                {
                    enableJsonAttributes = SettingsRepository.GetSettingValue<bool>(Setting.EnableJsonAttribute);
                }
                catch { }

                if (!enableJsonAttributes)
                {
                    dataTypeOptions = dataTypeOptions.Where(x => x.value != "JsonElement").ToList();
                }

                var disallowedPathClasses = new List<AssetTypeClass>() {
                    AssetTypeClass.Organization,
                    AssetTypeClass.User,
                };
                if (AssetTypeUid != null && disallowedPathClasses.Contains(@class))
                {
                    dataTypeOptions = dataTypeOptions.Where(x => x.value != "Path").ToList();
                }

                var disallowedScoreClasses = new List<AssetTypeClass>() {
                    AssetTypeClass.Organization,
                    AssetTypeClass.User,
                    AssetTypeClass.ReferenceItemType,
                    AssetTypeClass.Diagram
                };
                if (AssetTypeUid != null && disallowedScoreClasses.Contains(@class))
                {
                    dataTypeOptions = dataTypeOptions.Where(x => x.value != "Score").ToList();
                }

                if (AssetTypeUid != null && @class == AssetTypeClass.User)
                {
                    dataTypeOptions = dataTypeOptions.Where(x => x.value != "ComplexRelationLookup").ToList();
                }

                var jsonFieldType = new Dictionary<string, string>() {
                    { "Boolean", "bit" },
                    { "Date", "date" },
                    { "Date With Time", "datetime" },
                    { "Decimal", "float" },
                    { "Text", "nvarchar" },
                    { "Whole Number", "int" },
                    { "Whole Number (Large)", "bigint" },
                };
                var Field_JsonDataTypes = jsonFieldType.Select(i => new { title = i.Key, value = i.Value });
                var Field_JsonFields = Company.Filter<FieldType>(ft => ft.Object == sType && ft.ObjectID == id && ft.Type == "JSON")
                    .OrderBy(ft => ft.FriendlyName)
                    .Select(ft => new { ft.FriendlyName, ft.Name, ft.ID })
                    .ToList()
                    .Select(ft => new { title = $"{ft.FriendlyName} ({ft.Name})", value = ft.Name })
                    .ToList();

                List<dynamic> Field_ResponsibilityTypes = null;
                if (AssetTypeUid != null)
                {
                    Field_ResponsibilityTypes = Company.Query<dynamic>(@"SELECT rt.name AS title, rt.uid AS value
                    FROM ResponsibilityType rt
                    INNER JOIN ResponsibilityTypeRelation rtr ON rtr.ResponsibilityTypeID = rt.ID
                    INNER JOIN AssetType at ON rtr.ObjectType = at.Object AND rtr.ObjectID = at.ObjectID
                    WHERE at.uid = @uid
                    ORDER BY rt.name", new { uid = AssetTypeUid }).ToList();
                }
                #endregion


                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    Attributes = attributes,
                    Field_Relationships,
                    Field_JsonFields,
                    Field_JsonDataTypes,
                    Field_CardinalRelationships,
                    Field_FieldFromRelRelationships,
                    Field_CardinalReferenceRelationships,
                    Field_ResponsibilityTypes,
                    DataTypes = dataTypeOptions,
                    FilteredLookups = filteredLookups,
                    Patterns = patterns.Select(i => new { title = i.Key, value = i.Value }),
                    IntersectTypes = intersectTypes,
                    Lookups = lookups,
                    ComplexLookupRelations = complexLookupRelations.Select(x => new { ID = (int)x.ID, x.Name, x.DisplayName })
                });
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
        /// Gets the form data for the chosen Field Form.
        /// </summary>
        /// <returns>A the form options, if any.</returns>
        [
            HttpGet,
            Route("GetFieldTypeFormData"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public HttpResponseMessage GetFieldTypeFormData(string name, Guid? assetTypeUid = null, Guid? actionTypeUid = null, Guid? relationshipTypeUid = null)
        {
            var prefix = "Fields.GetFieldTypeFormData => ";
            var errorMessage = "";

            try
            {
                FieldType ft = null;
                if (assetTypeUid != null)
                {
                    int atID = Company.Filter<AssetType>(x => x.uid == assetTypeUid).SingleOrDefault().ID;
                    ft = Company.Filter<FieldType>(x => x.AssetTypeID == atID && x.Name == name).SingleOrDefault();
                }
                else if (actionTypeUid != null)
                {
                    int atID = Company.Filter<IssueType>(x => x.uid == actionTypeUid).SingleOrDefault().ID;
                    ft = Company.Filter<FieldType>(x => x.AssetTypeID == atID && x.Name == name).SingleOrDefault();
                }
                else if (relationshipTypeUid != null)
                {
                    var itID = Company.Filter<IntersectType>(i => i.uid == relationshipTypeUid).SingleOrDefault().ID;
                    ft = Company.Filter<FieldType>(x => x.AssetTypeID == itID && x.Name == name).SingleOrDefault();
                }
                else
                {
                    throw new ArgumentNullException(ApiMessages.NotValidAssetActionRelationTypeProvided);
                }

                List<dynamic> filteredLookupItems = null;
                List<dynamic> relationItems = null;
                dynamic ownershipLookupSettings = null;
                dynamic JsonElementSettings = null;
                dynamic refListFromRelSettings = null;

                if (ft != null)
                {

                    if (ft.Type == DataType.JsonElement.ToString())
                    {
                        if (!string.IsNullOrEmpty(ft.Definition))
                        {
                            JsonElementSettings = (dynamic)Newtonsoft.Json.JsonConvert.DeserializeObject(ft.Definition);
                        }
                    }

                    var lookup = Company.FieldTypeLookups.Where(i => i.FieldTypeID == ft.ID).FirstOrDefault();
                    if (lookup != null)
                    {
                        var definition = (dynamic)JsonConvert.DeserializeObject(lookup.Definition);

                        if (ft.Type == DataType.ComplexRelationLookup.ToString())
                        {
                            relationItems = new List<dynamic>();
                            foreach (var r in definition.Relations)
                            {
                                relationItems.Add(new
                                {
                                    r.ID,
                                    IntersectType = r.IntersectTypeID,
                                    ReferenceType = r.RelationType,
                                    ChildIntersectType = 0,
                                    DisplayFields = new List<dynamic>(),
                                    lookup.HideHeader,
                                    lookup.HideFooter,
                                    lookup.HideFilter,
                                    Direction = r.Direction ?? 2,
                                    r.Object,
                                    r.ObjectID,
                                    r.IntersectTypeUid,
                                    r.AssetTypeUid
                                });
                            }
                            if (definition.Fields != null)
                            {
                                foreach (var f in definition.Fields)
                                {
                                    if (f.RelationIndex == null)
                                    {
                                        f.RelationIndex = relationItems.FindIndex(i => i.AssetTypeUid == f.AssetTypeUid);
                                    }

                                    var r = ((int)f.RelationIndex > -1) ? relationItems[(int)f.RelationIndex] : null;

                                    if (r != null)
                                    {
                                        r.DisplayFields.Add(f);
                                    }
                                }
                            }
                        }
                        else if (ft.Type == DataType.OwnershipLookup.ToString())
                        {
                            ownershipLookupSettings = new
                            {
                                definition.DisplayAssignmentSource,
                                definition.ExpandGroupMembership,
                                definition.ResponsibilityType,
                                lookup.HideFilter,
                                lookup.HideFooter,
                                lookup.HideHeader
                            };
                        }
                        else if (ft.Type == DataType.RefListRelationship.ToString())
                        {
                            refListFromRelSettings = new
                            {
                                definition.DisplayRefListDescription
                            };
                        }
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    FieldType = ft,
                    FilteredLookupItems = filteredLookupItems,
                    JsonElementSettings,
                    OwnershipLookupSettings = ownershipLookupSettings,
                    RefListFromRelSettings = refListFromRelSettings,
                    RelationItems = relationItems
                });
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
        /// Gets the lookup tokens for the chosen lookup in the Field Form.
        /// </summary>
        /// <returns>A list of tokens, if any.</returns>
        [
            HttpGet,
            Route("GetFieldTypeLookupTokens"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public HttpResponseMessage GetFieldTypeLookupTokens(string identifier)
        {
            var prefix = "Fields.GetFieldTypeLookupTokens => ";
            var errorMessage = "";


            var excludedFieldTypes = new List<string>()
            {
                DataType.Path.ToString(),
                DataType.ComplexRelationLookup.ToString(),
                DataType.Score.ToString(),
            };


            try
            {
                SystemObjects type;
                int id;
                Dictionary<string, string> list = new Dictionary<string, string>();
                //special case for reference list
                if (Enum.TryParse(identifier, out type))
                {
                    id = 0;
                }
                else if (Guid.TryParse(identifier, out Guid Uid))
                {
                    var item = Company.Filter<AssetType>(x => x.uid == Uid).SingleOrDefault();
                    Enum.TryParse(item.Object, out type);
                    id = item.ObjectID;
                    list = Company.GetFieldTypesByObject(type, id)
                        .Where(i => !excludedFieldTypes.Contains(i.Type))
                        .Select(i => new { i.ID, i.Name })
                        .ToDictionary(i => i.Name, i => i.Name);

                }
                else
                {
                    throw new ArgumentNullException(string.Format(ApiMessages.InvalidValueMessage, identifier));
                }

                switch (type)
                {
                    case SystemObjects.ArtifactType:
                        list.Add("ID", "ID");
                        break;
                    case SystemObjects.ReferenceItem:
                    case SystemObjects.ReferenceItemType:
                        list = list.Prepend(new KeyValuePair<string, string>("Code", "Code")).ToDictionary(d => d.Key, d => d.Value);
                        break;
                    case SystemObjects.PolicyType:
                        list.Add("TextPath", "TextPath");
                        break;
                    case SystemObjects.Resource:
                    case SystemObjects.ResourceType:
                        list.Add("First Name", "FirstName");
                        list.Add("Last Name", "LastName");
                        list.Add("Email", "Email");
                        break;
                    case SystemObjects.TaxonomyType:
                        if (id == 0)
                        {
                            list.Add("Name", "Name");
                        }
                        else
                        {
                            list.Add("TextPath", "TextPath");
                        }
                        break;
                }

                return Request.CreateResponse(HttpStatusCode.OK, list.Select(i => new { title = i.Key, value = "{" + i.Value + "}" }));
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
        /// Gets the FieldFromRelationship values for the given intersect type UID for the Field Form.
        /// </summary>
        /// <returns>A list of FieldFromRelationship options, if any.</returns>
        [
            HttpGet,
            Route("GetFieldFromRelationshipFields"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public HttpResponseMessage GetFieldFromRelationshipFields(Guid intersectTypeUid, Guid? AssetTypeUid = null, Guid? RelationshipTypeUid = null, Guid? ActionTypeUid = null)
        {
            var prefix = "Fields.GetFieldFromRelationshipFields => ";
            var errorMessage = "";

            try
            {
                int id = 0;
                SystemObjects type = SystemObjects.ArtifactType;
                if (AssetTypeUid != null)
                {
                    var assetType = Company.Filter<AssetType>(x => x.uid == AssetTypeUid).SingleOrDefault();
                    id = assetType.ObjectID;
                    Enum.TryParse(assetType.Object, out type);
                }
                else if (ActionTypeUid != null)
                {
                    var issueType = Company.Filter<IssueType>(x => x.uid == ActionTypeUid).SingleOrDefault();
                    id = issueType.ID;
                    Enum.TryParse("IssueType", out type);
                }
                else if (RelationshipTypeUid != null)
                {
                    var it = Company.Filter<IntersectType>(i => i.uid == RelationshipTypeUid).SingleOrDefault();
                    id = it.ID;
                }
                else
                {
                    throw new ArgumentNullException(ApiMessages.NotValidAssetActionRelationTypeProvided);
                }

                var intersectType = Company.Filter<IntersectType>(x => x.uid == intersectTypeUid).SingleOrDefault();

                if (intersectType == null)
                {
                    throw new RestApiException(HttpStatusCode.BadRequest, string.Format(ActionApiMessages.RelationShipTypeUidNotFound, intersectTypeUid.ToString()));
                }

                var isSubject = (intersectType.Subject == type.ToString() && intersectType.SubjectID == id);

                var targetObjectType = isSubject ? intersectType.Object : intersectType.Subject;
                var targetObjectTypeID = isSubject ? intersectType.ObjectID : intersectType.SubjectID;

                var restrictedFields = DataType.Text.GetNotAllowedInFieldFromRelationship();
                var list = Company
                    .Filter<FieldType>(f => f.Object == targetObjectType && f.ObjectID == targetObjectTypeID)
                    .Where(i => !restrictedFields.Contains(i.Type))
                    .Select(i => new { i.Name, i.FriendlyName });

                return Request.CreateResponse(HttpStatusCode.OK, list.Select(i => new { title = i.FriendlyName, value = i.Name }));
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
        /// Gets the default values for the chosen lookup in the Field Form.
        /// </summary>
        /// <returns>A list of default options, if any.</returns>
        [
            HttpGet,
            Route("GetLookupDefaultValues"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<HttpResponseMessage> GetLookupDefaultValues(string Uid)
        {
            var prefix = "Fields.GetLookupDefaultValues => ";
            var errorMessage = "";

            try
            {
                Guid assetUid;
                Guid.TryParse(Uid, out assetUid);
                var list = new List<ListUidItem>();
                list.Add(new ListUidItem { title = "- No default -", value = null });
                var usersOnly = false;
                string sql = "";
                usersOnly = Company.Filter<AssetType>(x => x.uid == assetUid && x.Class == AssetTypeClass.User).Count() > 0;
                if (usersOnly)
                {
                    string HideD3SUsers = HideData3SixtyUsers() ? "" : " WHERE Email not like '%@data3sixty.com' and Email not like '%@infogix.com' and Email not like '%@precisely.com' ";
                    sql = $@"
                        select 
                            R.Uid as value,
                            (FirstName + ' ' + LastName)  as title  
                        from [reporting].[Global_Resource] r 
                        {HideD3SUsers}
                        order by title           
                    ";
                }
                else
                {
                    sql = $@"
                        select 
                            ast.Uid as value,
                            d.DisplayValue as title  
                        from asset ast 
                            inner join assettype astt on (ast.assettypeid = astt.id) 
                            cross apply [dbo].GetAssetDisplayValueById(ast.id) d 
                        where astt.Uid = @Uid order by d.DisplayValue
                                        
                    ";

                }

                list.AddRange(
                    await Company.QueryAsync<ListUidItem>(sql, new { Uid = assetUid }, ApiTimeout)
                );

                return Request.CreateResponse(HttpStatusCode.OK, list);
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
        /// Gets the  Reference Hierarchy for the given uid for the Field Form.
        /// </summary>
        /// <returns>A select list item list of options, if any.</returns>
        [
            HttpGet,
            Route("GetReferenceHierarchy"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public HttpResponseMessage GetReferenceHierarchy(string uid, Guid? AssetTypeUid = null, Guid? RelationshipTypeUid = null, Guid? ActionTypeUid = null)
        {
            var prefix = "Fields.GetReferenceHierarchy => ";
            var errorMessage = "";

            try
            {
                int id = 0;
                SystemObjects type = SystemObjects.ArtifactType;
                if (AssetTypeUid != null)
                {
                    var assetType = Company.Filter<AssetType>(x => x.uid == AssetTypeUid).SingleOrDefault();
                    id = assetType.ObjectID;
                    Enum.TryParse(assetType.Object, out type);
                }
                else if (ActionTypeUid != null)
                {
                    var issueType = Company.Filter<IssueType>(x => x.uid == ActionTypeUid).SingleOrDefault();
                    id = issueType.ID;
                    Enum.TryParse("IssueType", out type);
                }
                else if (RelationshipTypeUid != null)
                {
                    var it = Company.Filter<IntersectType>(i => i.uid == RelationshipTypeUid).SingleOrDefault();
                    id = it.ID;
                }
                else
                {
                    throw new ArgumentNullException(ApiMessages.NotValidAssetActionRelationTypeProvided);
                }
                AssetType refitem = null;
                if (Guid.TryParse(uid, out Guid refitemGuid))
                {
                    refitem = Company.Filter<AssetType>(x => x.uid == refitemGuid).SingleOrDefault();
                }

                var list = new List<PrimeSelectItem>();
                if (refitem != null && refitem.Object == SystemObjects.ReferenceItemType.ToString())
                {

                    string objectType = type.ToString();
                    //return possible hierarchy parents for this object type
                    var parent = Company.GetParentType(refitem.ObjectID, SystemObjects.ReferenceItemType);

                    if (parent != null)
                    {
                        //get possible parent reference list types defined for this object / object id they cant already be parents
                        list = Company.FieldTypes.Where(x => x.Object == objectType && x.ObjectID == id && x.LookupObjectType == "ReferenceItem" && x.LookupObjectID == parent.ObjectID).Select(i => new PrimeSelectItem { label = i.FriendlyName, value = i.Name }).ToList();
                        if (list.Count > 0)
                        {
                            list.Insert(0, new PrimeSelectItem { label = "", value = "" });
                        }
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, list);
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
        /// Gets the  GetLookupListFilter for the given uid for the Field Form.
        /// </summary>
        /// <returns>A select list item list of options, if any.</returns>
        [
            HttpGet,
            Route("GetLookupListFilter"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<HttpResponseMessage> GetLookupListFilter(string uid, Guid? assetTypeUid = null, Guid? actionTypeUid = null, Guid? relationshipTypeUid = null)
        {
            var prefix = "Fields.GetLookupListFilter => ";
            var errorMessage = "";

            try
            {
                string objectType = "";
                int objectId = 0;
                string listAssetObjectType = "";
                int listAssetObjectId = 0;

                //Get Object/ObjectID of Assettype that will be listed
                if (Guid.TryParse(uid, out Guid assetUid))
                {
                    AssetType listAssetType = Company.Filter<AssetType>(x => x.uid == assetUid).SingleOrDefault();
                    if (listAssetType != null)
                    {
                        listAssetObjectType = listAssetType.Object;
                        listAssetObjectId = listAssetType.ObjectID;
                    }
                }

                //Types of List assettypes that can have filtered lookups. If the list assettype is not of one of these types, return an empty list
                //If an invalid uid has been provided for the uid parameter, this will also return an empty list
                string[] allowedListTypes = { "ArtifactType", "TaxonomyType" };
                if (!allowedListTypes.Contains(listAssetObjectType))
                {
                    //return nothing no error;
                    return Request.CreateResponse(HttpStatusCode.OK, new List<dynamic>());
                }

                //Get Object/ObjectID of assettype/issuetype for which the lookup field is defined
                if (assetTypeUid != null)
                {
                    var assetType = Company.Filter<AssetType>(x => x.uid == assetTypeUid).SingleOrDefault();
                    objectType = assetType.Object;
                    objectId = assetType.ObjectID;
                }
                else if (actionTypeUid != null)
                {
                    var issueType = Company.Filter<IssueType>(x => x.uid == actionTypeUid).SingleOrDefault();
                    objectType = SystemObjects.IssueType.ToString();
                    objectId = issueType.ID;
                }
                else if (relationshipTypeUid != null)
                {
                    var intersectType = Company.Filter<IntersectType>(i => i.uid == relationshipTypeUid).SingleOrDefault();
                    objectType = SystemObjects.IntersectType.ToString();
                    objectId = intersectType.ID;
                }
                else
                {
                    throw new ArgumentNullException(ApiMessages.NotValidAssetActionRelationTypeProvided);
                }

                //AssetTypes that can have filtered Lookups
                string[] allowedAssetTypes = { "IssueType", "ArtifactType", "TaxonomyType", "PolicyType", "RuleType" };
                if (!allowedAssetTypes.Contains(objectType))
                {
                    //return nothing no error
                    return Request.CreateResponse(HttpStatusCode.OK, new List<dynamic>());
                }

                var predicateTypes = string.Join(",", PredicateType.DataLineage.GetAsList()
                    .Where(f => f.AllowEditFromRelationshipEditor && f.AllowIntersectTypeAssignment)
                    .Select(i => ((int)i.ID).ToString())
                    .ToArray());

                string sql = $@"SELECT 
                        Concat(lower(A.PredicateUID), '|',A.Direction) as PredicateValue,
                        A.PredicateUID,
                        A.Direction,
                        A.PredicateName, 
                        A.ObjectName, 
                        A.[Object], 
                        A.[ObjectID], 
                        B.FieldTypeID, 
                        B.[FriendlyName],
                        B.FieldTypeName,
						B.Type,
                        B.Class,
                        B.Name
                    FROM ( 
                        SELECT 
                            it.[ID] as IntersectTypeID, 
                            0 AS Direction, 
                            p.[UID] as PredicateUID, 
                            p.[Name] as PredicateName, 
                            ot.[Name] as ObjectName, 
                            it.[Object] as [Object], 
                            it.[ObjectID] as [ObjectID] 
                        FROM [dbo].[IntersectType] it 
                            join [dbo].[Predicate] p on it.[PredicateID] = p.[ID] 
                            join [dbo].[AssetType] ot on ot.[Object] = it.[Object] and ot.[ObjectId] = it.[ObjectID] 
                            join [dbo].[AssetType] st on st.[Object] = it.[Subject] and st.[ObjectId] = it.[SubjectID] 
                        where it.[Subject] = @listAssetObjectType 
                        and it.[SubjectID] = @listAssetObjectId
                        and p.Type IN ({predicateTypes})
                        and it.[Object] in ('ArtifactType', 'TaxonomyType')
                        UNION ALL 
                        SELECT 
                            it.[ID], 
                            1 AS Direction, 
                            p.[UID] as PredicateUID, 
                            p.[Inverse] as PredicateName,
                            st.[Name] as ObjectName, 
                            it.[Subject] as [Object], 
                            it.[SubjectID] as [ObjectID] 
                        FROM [dbo].[IntersectType] it 
                            join [dbo].[Predicate] p on it.[PredicateID] = p.[ID] 
                            join [dbo].[AssetType] ot on ot.[Object] = it.[Object] and ot.[ObjectId] = it.[ObjectID] 
                            join [dbo].[AssetType] st on st.[Object] = it.[Subject] and st.[ObjectId] = it.[SubjectID] 
                         where it.[Object] = @listAssetObjectType 
                         and it.[ObjectID] = @listAssetObjectId 
                         and p.Type IN ({predicateTypes})
                         and it.[Subject] in ('ArtifactType', 'TaxonomyType')
                        ) A LEFT OUTER JOIN
                    (SELECT 
                        ft.[ID] as FieldTypeID,
                        ft.Name as FieldTypeName,
                        ft.[FriendlyName], 
                        ft.[Object], 
                        ft.[ObjectID], 
                        at.Object as LookupObject, 
                        ft.LookupObjectID,
                        ft.Type,
                        at.Class,
                        at.Name
                    FROM [dbo].[FieldType] ft
                    INNER JOIN [dbo].[AssetType] at ON ft.LookupObjectType +'Type' = at.Object AND ft.LookupObjectID = at.ObjectID
                    WHERE ft.[ObjectID] = @objectId AND ft.[Object] = @objectType  
                    ) B ON A.[Object] = B.LookupObject AND A.ObjectID = B.LookupObjectID";
                var parms = new
                {
                    listAssetObjectType,
                    listAssetObjectId,
                    objectType,
                    objectId
                };
                var list = await Company.QueryAsync<dynamic>(sql, parms, ApiTimeout);

                return Request.CreateResponse(HttpStatusCode.OK, list.Select(i => new
                {
                    i.PredicateValue,
                    i.PredicateName,
                    i.FieldTypeName,
                    i.FriendlyName,
                    Info = string.IsNullOrEmpty(i.Name) ? "" : "List(" + (AssetTypeClass)i.Class + " : " + i.Name + ")" //@TODO use i.Type instead of hardcoded field type
                })
                );
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
        /// Used for complex lookup
        /// </summary>
        /// <param name="assetTypeUid">The uid of the asset Type></param>
        /// <returns>A list of relationship types</returns>
        [
            HttpGet,
            Route("GetStandardRelations"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<HttpResponseMessage> GetStandardRelations(Guid assetTypeUid)
        {
            var prefix = "Fields.GetStandardRelations => ";

            try
            {
                var intersectTypes = await Company.QueryAsync<dynamic>($@"select value, title from utility.GetIntersectTypesByType(@assetTypeUid) t 
                                                                          where not exists(select 1 from intersecttypedetail itd where itd.uid = t.uid and ((itd.object = @ObjectType and itd.ObjectID = 0) or (itd.subject = @ObjectType and itd.subjectID = 0)))",
                                                                          new { assetTypeUid, ObjectType = SystemObjects.ReferenceItemType.ToString() }, ApiTimeout);
                return Request.CreateResponse(HttpStatusCode.OK, intersectTypes);
            }
            catch (RestApiException ex)
            {
                return ReturnApiError(ex.Status, ex.GetFullExceptionData(false));
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// Used for complex lookup
        /// </summary>
        /// <param name="intersectTypeUid">The uid of the relationship type></param>
        /// <returns>A list of relationship types</returns>
        [
            HttpGet,
            Route("technicalrelationships"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<HttpResponseMessage> GetTechnicalRelationships(Guid intersectTypeUid)
        {
            var prefix = "Fields.GetTechnicalRelationships => ";

            try
            {
                var intersectTypes = await Company.QueryAsync<dynamic>($@"
select	cast(C.uid as varchar(36)) + '|' +  cast(C.ObjectUid as varchar(36)) + '|1' as value,
		C.ObjectName as title
from	IntersectType I
		inner join IntersectTypeDetail C on C.Subject = 'IntersectType' and C.SubjectID = I.ID
where	I.Uid = @intersectTypeUid", new { intersectTypeUid }, ApiTimeout);
                return Request.CreateResponse(HttpStatusCode.OK, intersectTypes);
            }
            catch (RestApiException ex)
            {
                return ReturnApiError(ex.Status, ex.GetFullExceptionData(false));
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// Used to get the parent types of a specific child type.
        /// </summary>
        /// <param name="assetTypeUid">The uid of the asset Type></param>
        /// <returns>A list of parent realtionship types</returns>
        [
            HttpGet,
            Route("GetParentRelations"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<HttpResponseMessage> GetParentRelations(Guid assetTypeUid)
        {
            var prefix = "Fields.GetParentRelations => ";
            var errorMessage = "";

            try
            {
                SystemObjects type;
                int id = 0;
                if (assetTypeUid != null)
                {
                    var at = Company.Filter<AssetType>(x => x.uid == assetTypeUid).SingleOrDefault();
                    Enum.TryParse(at.Object, out type);
                    id = at.ObjectID;
                }
                else
                {
                    return ReturnApiError(HttpStatusCode.NotFound, string.Format(ApiMessages.AssetNotFoundForAssetType, assetTypeUid.ToString()));
                }

                var intersectTypes = await Company.QueryAsync<dynamic>(
                    $@"select cast(uid as varchar(36)) + '|' + cast(SubjectUid as varchar(36)) + '|' + @direction as value, SubjectName as title from IntersectTypeDetail where PredicateType = @pt and ObjectUid = @assetTypeUid",
                    new { pt = (int)PredicateType.InterTypeHierarchy, assetTypeUid, direction = ((int)FieldTypeComplexLookupRelationDirection.Back).ToString() }
                    , ApiTimeout
                );

                return Request.CreateResponse(HttpStatusCode.OK, intersectTypes);
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
        /// Used to get the child types of a specific parent type.
        /// </summary>
        /// <param name="assetTypeUid">The uid of the asset Type></param>
        /// <returns>A list of child realtionship types</returns>
        [
            HttpGet,
            Route("GetChildRelations"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<HttpResponseMessage> GetChildRelations(Guid assetTypeUid)
        {
            var prefix = "Fields.GetChildRelations => ";
            var errorMessage = "";

            try
            {
                SystemObjects type;
                int id = 0;
                if (assetTypeUid != null)
                {
                    var at = Company.Filter<AssetType>(x => x.uid == assetTypeUid).SingleOrDefault();
                    Enum.TryParse(at.Object, out type);
                    id = at.ObjectID;
                }
                else
                {
                    return ReturnApiError(HttpStatusCode.NotFound, string.Format(ApiMessages.AssetNotFoundForAssetType, assetTypeUid.ToString()));
                }

                var intersectTypes = await Company.QueryAsync<dynamic>(
                    $@"select cast(uid as varchar(36)) + '|' + cast(ObjectUid as varchar(36)) + '|' + @direction as value, ObjectName as title from IntersectTypeDetail where PredicateType = @pt and SubjectUid = @assetTypeUid",
                    new { pt = (int)PredicateType.InterTypeHierarchy, assetTypeUid, direction = ((int)FieldTypeComplexLookupRelationDirection.Forward).ToString() }
                    , ApiTimeout
                );
                return Request.CreateResponse(HttpStatusCode.OK, intersectTypes);
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
        /// Gets the dislpay fields for a given intersecttypeid asset type uid
        /// </summary>
        /// <param name="intersectTypeUid">intersectTypeUid></param>
        /// <param name="assetTypeUid">assetTypeUid></param>
        /// <returns>A list of display fields</returns>
        [
            HttpGet,
            Route("GetRelationLookupDisplayFields"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public HttpResponseMessage GetRelationLookupDisplayFields(Guid assetTypeUid, Guid intersectTypeUid)
        {
            var prefix = "Fields.GetRelationLookupDisplayFields => ";
            var errorMessage = "";

            try
            {
                SystemObjects type;
                int id = 0;
                int intersectTypeID = 0;
                var intersectType = Company.Filter<IntersectType>(i => i.uid == intersectTypeUid).SingleOrDefault();
                if (intersectType != null)
                {
                    intersectTypeID = intersectType.ID;
                }
                var at = Company.Filter<AssetType>(x => x.uid == assetTypeUid).SingleOrDefault();
                if (at != null)
                {
                    Enum.TryParse(at.Object, out type);
                    id = at.ObjectID;
                }
                else
                {
                    return ReturnApiError(HttpStatusCode.NotFound, string.Format(ApiMessages.AssetNotFoundForAssetType, assetTypeUid.ToString()));
                }

                var restrictedTypes = DataType.Text.GetNotAllowedInRelationshipLookup();
                var list = Company.GetFieldTypesByObject(type, id)
                    .Where(i => !restrictedTypes.Contains(i.Type))
                    .Select(i => new { i.ID, i.Name })
                    .ToDictionary(i => i.Name, i => i.ID);

                if (type == SystemObjects.ReferenceItemType)
                {
                    if (id == 0)
                    {
                        list.Add("Name", 0);
                        if (!list.ContainsKey("Description"))
                        {
                            list.Add("Description", 0);
                        }
                    }
                    else
                    {
                        list.Add("Code", 0);
                    }
                }
                else if (type == SystemObjects.ResourceType)
                {
                    list.Add("FirstName", 0);
                    list.Add("LastName", 0);
                    list.Add("Email", 0);
                    list.Add("LastLoggedInOn", 0);
                    list.Add("DisplayValue", 0);
                }
                else
                {
                    list.Add("DisplayValue", 0);
                }

                list.Add("_assetPath", 0);

                var relList = Company.GetFieldTypesByObject(SystemObjects.IntersectType, intersectTypeID)
                    .Where(i => i.Type != DataType.Path.ToString())
                    .Select(i => new { i.ID, i.Name }).ToList();
                relList.ForEach(r =>
                {
                    list.Add($"Relation.{r.Name}", r.ID);
                });

                var sType = type.ToString();
                var relatedTypeList = Company.Filter<IntersectTypeDetail>(i =>
                    (i.Subject == sType && i.SubjectID == id) ||
                    (i.Object == sType && i.ObjectID == id)
                    ).ToList().Select(i => new
                    {
                        ID = i.ID,
                        Name = (i.Subject == sType && i.SubjectID == id) ? $"{i.ObjectName} ({i.PredicateName})" : $"{i.SubjectName} ({i.PredicateName})"
                    }).Distinct().ToList();
                relatedTypeList.ForEach(r =>
                {
                    if (!list.ContainsKey($"Related Item.{r.Name} ({r.ID})"))
                    {
                        list.Add($"Related Item.{r.Name} ({r.ID})", r.ID);
                    }
                });

                return Request.CreateResponse(HttpStatusCode.OK, list.Select(i => new { title = i.Key, value = $"{i.Value}|{i.Key}" }));
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
        /// checks if a relationship is listable
        /// </summary>
        /// <param name="intersectTypeUid">intersectTypeUid></param>
        /// <param name="assetTypeUid">assetTypeUid></param>
        /// <param name="actionTypeUid">actionTypeUid></param>
        /// <param name="relationshipTypeUid">relationshipTypeUid></param>
        /// <returns>bool value for isListable</returns>
        [
            HttpGet,
            Route("IsListableRelationship"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(bool)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public HttpResponseMessage IsListableRelationship(Guid intersectTypeUid, Guid? assetTypeUid = null, Guid? actionTypeUid = null, Guid? relationshipTypeUid = null)
        {
            var prefix = "Fields.IsListableRelationship => ";
            var errorMessage = "";

            try
            {
                SystemObjects type;
                int id = 0;
                var intersectType = Company.Filter<IntersectType>(i => i.uid == intersectTypeUid).SingleOrDefault();
                if (intersectType == null)
                    return ReturnApiError(HttpStatusCode.NotFound, string.Format(ActionApiMessages.RelationShipTypeUidNotFound, intersectTypeUid.ToString()));
                if (assetTypeUid != null)
                {
                    var at = Company.Filter<AssetType>(x => x.uid == assetTypeUid).SingleOrDefault();
                    Enum.TryParse(at.Object, out type);
                    id = at.ObjectID;
                }
                else if (actionTypeUid != null)
                {
                    var at = Company.Filter<IssueType>(x => x.uid == actionTypeUid).SingleOrDefault();
                    type = SystemObjects.IssueType;
                    id = at.ID;
                }
                else if (relationshipTypeUid != null)
                {
                    var it = Company.Filter<IntersectType>(i => i.uid == relationshipTypeUid).SingleOrDefault();
                    type = SystemObjects.IntersectType;
                    id = it.ID;
                }
                else
                {
                    return ReturnApiError(HttpStatusCode.NotFound, string.Format(ApiMessages.AssetNotFoundForAssetType, assetTypeUid.ToString()));
                }
                bool isListable = false;
                var sType = type.ToString();

                if (intersectType != null)
                {
                    if (intersectType.Subject == sType && intersectType.SubjectID == id && intersectType.ObjectCardinality == Cardinality.One)
                    {
                        isListable = true;
                    }
                    else if (intersectType.Object == sType && intersectType.ObjectID == id && intersectType.SubjectCardinality == Cardinality.One)
                    {
                        isListable = true;
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, isListable);
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
        /// Move a fields column order in the given direction
        /// </summary>
        /// <param name="model">Contains the nessasary parameters to move a fields sort order></param>
        /// <returns>Success or Failure</returns>
        [
            HttpPost,
            Route("move"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(bool)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type, relationship type, or action type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public HttpResponseMessage PerformMove(MoveModel model)
        {
            var prefix = "Fields.PerformMove => ";
            var errorMessage = "";

            try
            {

                SystemObjects type;
                int id = 0;
                int fieldTypeID = 0;
                var assetType = Company.Filter<AssetType>(x => x.uid == model.TypeUid).SingleOrDefault();
                var actionType = Company.Filter<IssueType>(x => x.uid == model.TypeUid).SingleOrDefault();
                var intersectType = Company.Filter<IntersectType>(i => i.uid == model.TypeUid).SingleOrDefault();
                var name = "";
                if (assetType != null)
                {
                    Enum.TryParse(assetType.Object, out type);
                    id = assetType.ObjectID;
                    name = assetType.Name;
                    fieldTypeID = Company.Filter<FieldType>(x => x.AssetTypeID == assetType.ID && x.Name == model.FieldTypename).SingleOrDefault().ID;
                }
                else if (actionType != null)
                {
                    type = SystemObjects.IssueType;
                    id = actionType.ID;
                    name = actionType.Name;
                    fieldTypeID = Company.Filter<FieldType>(x => x.ObjectID == actionType.ID && x.Object == "IssueType" && x.Name == model.FieldTypename).SingleOrDefault().ID;
                }
                else if (intersectType != null)
                {
                    type = SystemObjects.IntersectType;
                    id = intersectType.ID;
                    name = "intersectType:" + model.TypeUid.ToString();
                    fieldTypeID = Company.Filter<FieldType>(x => x.ObjectID == intersectType.ID && x.Object == "IntersectType" && x.Name == model.FieldTypename).SingleOrDefault().ID;
                }
                else
                {
                    return ReturnApiError(HttpStatusCode.NotFound, string.Format(ApiMessages.AssetNotFoundForAssetType, model.TypeUid.ToString()));
                }
                string message = "";
                errorMessage = string.Format("{0} could not be found for {1}.", model.FieldTypename, name);

                var sType = type.ToString();
                List<FieldType> list = Company.Filter<FieldType>(i => i.Object == sType && i.ObjectID == id).OrderBy(i => i.ColumnOrder).ThenBy(i => i.FriendlyName).ToList();

                if (list != null)
                {
                    //Verify the list colum order is an ordered list
                    //If not Chagne the field defintion to ordered before applying the perform move
                    int startSeq = list[0].ColumnOrder == 0 || list[0].ColumnOrder == 1 ? list[0].ColumnOrder : 1;
                    var listColumn = list.Select(x => x.ColumnOrder).ToList();
                    var seqList = Enumerable.Range(startSeq, list.Count);
                    if (!Enumerable.SequenceEqual<int>(listColumn, seqList))
                    {
                        var j = startSeq;
                        foreach (var f in list)
                        {
                            f.ColumnOrder = j++;
                        }
                        Company.Database.Connection.UpdateFieldMove(list, Company.CurrentResourceID);
                        list = Company.Filter<FieldType>(i => i.Object == sType && i.ObjectID == id).OrderBy(i => i.ColumnOrder).ThenBy(i => i.FriendlyName).ToList();
                    }

                    var fieldToMove = list.SingleOrDefault(i => i.ID == fieldTypeID);

                    var maxPosition = list.Count;

                    var currentPosition = fieldToMove.ColumnOrder;
                    var newPosition = (model.Direction == "up") ?
                        (currentPosition > 0 ? currentPosition - 1 : 0) :
                        (currentPosition < maxPosition ? currentPosition + 1 : maxPosition);

                    fieldToMove.ColumnOrder = newPosition;


                    var fieldFromMove = list.OrderBy(x => x.Name).FirstOrDefault(i => i.ColumnOrder == newPosition && i.ID != fieldTypeID);


                    if (fieldFromMove != null && fieldFromMove.ID != 0)
                    {
                        fieldFromMove.ColumnOrder = currentPosition;
                        Company.Database.Connection.UpdateFieldMove(fieldToMove, fieldFromMove, Company.CurrentResourceID);
                    }
                    else
                    {
                        Company.Database.Connection.UpdateFieldMove(fieldToMove, null, Company.CurrentResourceID);
                    }

                    return Request.CreateResponse(HttpStatusCode.OK, ApiMessages.FieldMovedSuccessfully);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, message);
                }
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
        /// Gets the score types available for the given asset type
        /// </summary>
        /// <returns>A list of score types if applicable.</returns>
        [
            HttpGet,
            Route("GetAvailableScoreTypes"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Uid for asset type does not correspond to a known type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public HttpResponseMessage GetAvailableScoreTypes(Guid assetTypeUid)
        {
            var prefix = "Fields.GetAvailableScoreTypes => ";
            var errorMessage = "";

            try
            {
                Dictionary<string, string> list = new Dictionary<string, string>();

                var assetType = Company.Filter<AssetType>(a => a.uid == assetTypeUid).FirstOrDefault();

                if (assetType == null)
                {
                    return ReturnApiError(HttpStatusCode.NotFound, ActionApiMessages.InvalidAssetTypeUid);
                }


                var types = Company.Query<int>(
                    "select distinct ScoreType from metrics.Allocation where AssetTypeUid = @assetTypeUid and [State] = 1"
                    , new { assetTypeUid }, ApiTimeout).ToList();

                foreach (var type in types)
                {
                    try
                    {
                        ScoreType scoreType = (ScoreType)type;

                        list.Add(scoreType.ToString(), scoreType.GetDisplayName());

                    }
                    catch
                    {
                        return ReturnApiError(HttpStatusCode.InternalServerError, string.Format(ApiMessages.ErrorScoreCasting, type.ToString()));
                    }
                }


                return Request.CreateResponse(HttpStatusCode.OK, list.Select(i => new { label = i.Value, value = i.Key }));
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

        #endregion

        #region Advanced filtering APIs
        /// <summary>
        /// Retrieves lookup values for an asset type and field name
        /// </summary>
        /// <returns>Returns a list of lookup values</returns>        
        /// <param name="assetTypeUid">Uid of the asset type</param>
        /// <param name="fieldName">Field name</param>
        [HttpGet,
            Route("{assetTypeUid:Guid}/lookupvalues/{fieldName}"),
             SwaggerResponse(HttpStatusCode.OK, "A list of filter values for a given asset type and field name.", typeof(List<string>)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error indicating the request is invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
            ]
        public HttpResponseMessage GetFilterVales(Guid assetTypeUid, string fieldName, int? skip = null, int? take = 0, string filter = null, bool isForAssetForm = false, Guid? assetUid = null)
        {
            var prefix = "Fields.GetFilterVales => ";
            try
            {
                if (assetTypeUid == Guid.Empty && fieldName == "EvaluatedAssetClass")
                {
                    var classInfos = AssetTypeClass.BusinessAsset.GetAsList().Where(x => x.ID == AssetTypeClass.BusinessAsset || x.ID == AssetTypeClass.TechnicalAsset);
                    if (!string.IsNullOrEmpty(filter))
                    {
                        classInfos = classInfos.Where(x => x.Name.ToLower(CultureInfo.InvariantCulture).Contains(filter.ToLower(CultureInfo.InvariantCulture).Trim('\''))
                        || x.Value.ToLower(CultureInfo.InvariantCulture).Contains(filter.ToLower(CultureInfo.InvariantCulture).Trim('\'')))
                            .ToList();
                    }

                    if (skip.HasValue && take.HasValue)
                    {
                        classInfos = classInfos.Skip(skip.Value).Take(take.Value).ToList();
                    }

                    return Request.CreateResponse(HttpStatusCode.OK, new { items = classInfos.Select(x => x.Name).ToList() });
                }

                int fieldTypeId = -1;

                var assetType = Company.AssetTypes
                    .FirstOrDefault(x => x.uid == assetTypeUid);
                var fieldType = Company.FieldTypes.FirstOrDefault(x => x.AssetTypeID == assetType.ID && x.Name == fieldName);


                string pagingQuery = "";
                string whereQuery = "";
                if (skip != null && take != null)
                {
                    pagingQuery = " OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY ";
                }

                //list items for parent field
                if (fieldType == null && fieldName.ToLowerInvariant() == "parentuid")
                {
                    string sql = "";
                    bool isHierarchyGrid = assetType.Object == "TaxonomyType" || assetType.Object == "PolicyType";

                    if (!isHierarchyGrid)
                    {
                        if (!string.IsNullOrEmpty(filter))
                        {
                            filter = "%" + filter + "%";
                            whereQuery += " and node.displaypath like @filter ";
                        }
                        sql = $@"declare @target nvarchar(255) 
                                declare @targetid int
                                
                                select @target = ito.Subject, @targetid = ito.SubjectId from AssetType at
                                inner join [IntersectType] ito on ito.Object = at.Object and ito.ObjectId = at.objectid
                                inner join [Predicate] po on ito.PredicateID = po.ID and po.Type in (3,4)
                                where at.id = @id
                                
                                declare @parentAssetTypeId int = (select top 1 id from assettype where object =@target and objectid = @targetid)
                                
                                select 
                                cast(a.uid as nvarchar(36)) as value,
                                coalesce(node.DisplayPath,'Path Missing') as text 
                                from Asset A
                                 inner join graph.AssetNodeDisplayPath Node on Node.id = a.id
                                where a.AssetTypeID = @parentAssetTypeId {whereQuery}
                                order by node.displaypath 
                                {pagingQuery}
                                option(recompile);

                                select count(*) from Asset A
                                 inner join graph.AssetNodeDisplayPath Node on Node.id = a.id
                                where a.AssetTypeID = @parentAssetTypeId {whereQuery};";
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(filter))
                        {
                            filter = "%" + filter + "%";
                            whereQuery += " and P.TextPath like @filter ";
                        }

                        var hierarchyItem = Company.Query<dynamic>($@"
                                                    select L.Level
                                                    from	Asset A
		                                                    inner join AssetType T on T.ID = A.AssetTypeID
		                                                    cross apply dbo.GetAssetLevelById(A.ID) L
                                                    where	A.uid = @assetUid
                                                    ", new { assetUid }).SingleOrDefault();

                        sql = $@"select	
		                                P.TextPath as text,
                                        cast(a.uid as nvarchar(36)) as value
                                from	Asset A
                                        inner join AssetType T on T.ID = A.AssetTypeID and T.Id = @id
		                                cross apply dbo.GetAssetTextPathById(A.ID, ' / ') P
                                        cross apply dbo.GetAssetLevelById(A.ID) LV
                                where coalesce(LV.[Level], 1) <= '{ hierarchyItem?.Level ?? 1}' {whereQuery}
                                order by P.TextPath 
                                {pagingQuery}
                                option (maxrecursion 100)

                                select	count(*)
                                from	Asset A
                                        inner join AssetType T on T.ID = A.AssetTypeID and T.Id = @id
		                                cross apply dbo.GetAssetTextPathById(A.ID, ' / ') P
                                        cross apply dbo.GetAssetLevelById(A.ID) LV
                                where coalesce(LV.[Level], 1) <= '{ hierarchyItem?.Level ?? 1}' {whereQuery}
                                option (maxrecursion 100)";
                    }


                    var resultsAssets = Company.Connection.QueryMultiple(sql, new { assetType.ID, skip, take, filter });

                    var items = resultsAssets.Read<DDLSelectItem>().ToList();
                    if (isHierarchyGrid)
                    {
                        items = items.Prepend(new DDLSelectItem { text = "- Root -", value = Guid.Empty.ToString() }).ToList();
                    }
                    var data = new
                    {
                        items,
                        count = resultsAssets.Read<int>().FirstOrDefault()
                    };

                    return Request.CreateResponse(HttpStatusCode.OK, data);
                }

                //case when fieldname coming from complex relation grid with coded names from procedure
                if (fieldType == null && fieldName.Contains("_"))
                {
                    fieldTypeId = int.Parse(fieldName.Split('_')[1]);
                    fieldType = Company.FieldTypes.FirstOrDefault(x => x.ID == fieldTypeId);
                }

                if (fieldType.Type == "FieldFromRelationship" && fieldType.LookupObjectFieldTypeID > 0)
                {
                    fieldTypeId = fieldType.LookupObjectFieldTypeID.Value;
                }
                else
                {
                    fieldTypeId = fieldType.ID;
                }

                if (!string.IsNullOrEmpty(filter))
                {
                    filter = "%" + filter + "%";
                    if (fieldType.Type == "Relationship")
                    {
                        whereQuery += " and node.displaypath like @filter ";
                    }
                    else
                    {
                        whereQuery += " and text like @filter ";
                    }
                }

                if (fieldType.Type == "Relationship")
                {
                    var sql = $@"
                                declare @target nvarchar(255) 
                                declare @targetid int

                                select  
                                @targetid = 
                                case when ft.object = it.subject and ft.objectid = it.subjectid then it.ObjectID
                                else it.SubjectID
                                end, 
                                @target = case when ft.object = it.subject and ft.objectid = it.subjectid then it.Object
                                else it.Subject
                                end
                                 from fieldtype ft
                                inner join [IntersectType] IT on IT.ID = ft.LookupObjectID
                                where ft.id = @fieldtypeid

                                declare @assetTypeId int = (select top 1 id from assettype where object =@target and objectid = @targetid)

                                select ObjectId as value,isnull(node.DisplayPath,'Path Missing') as text from Asset A
                                 inner join graph.AssetNodeDisplayPath Node on Node.id = a.id
                                where a.AssetTypeID = @assetTypeId {whereQuery}
                                order by node.displaypath
                                {pagingQuery}
                                OPTION(RECOMPILE);

                                select count(*) from Asset A
                                 inner join graph.AssetNodeDisplayPath Node on Node.id = a.id
                                where a.AssetTypeID = @assetTypeId {whereQuery};";

                    var resultsAssets = Company.Connection.QueryMultiple(sql, new { fieldTypeId, skip, take, filter });

                    var data = new
                    {
                        items = resultsAssets.Read<DDLSelectItem>().ToList(),
                        count = resultsAssets.Read<int>().FirstOrDefault()
                    };

                    return Request.CreateResponse(HttpStatusCode.OK, data);
                }


                bool hasColor = false;

                var colorjoin = $@"
                                        outer apply(SELECT FV = (SELECT V.Text as name, COALESCE(JSON_VALUE(ACJ.ColorJSON,'$.Value'), 'transparent') as color 
                                                    from Asset A 
                                                    outer apply dbo.GetAssetColorJsonByColor(A.Color) ACJ
													where A.Object = v.LookupObjectType and A.ObjectID = V.Value FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) 
                                        )colorJSON 
                                        ";

                string selectStatement = "v.text";
                string resourceJoin = "";

                if (fieldType.LookupObjectType == "Resource")
                {
                    bool hideData3SixtyUsers = HideData3SixtyUsers();
                    var hideData3SixtyUsersCondition = $@" and R.Email not like '%@data3sixty.com' and R.Email not like '%@infogix.com' and R.Email not like '%@precisely.com'";
                    resourceJoin = $@"
                                        inner join reporting.Global_resource R on R.ResourceID = V.Value and R.State <> 3 {(hideData3SixtyUsers ? hideData3SixtyUsersCondition : "")}
                                        ";
                }


                if (isForAssetForm)
                {
                    hasColor = Company.Connection.Query<int>(@"select count(1) from fieldtype ft
                    inner join assettype at on at.Object = ft.LookupObjectType + 'Type' and at.ObjectID = ft.LookupObjectID
                    inner join asset a on a.AssetTypeID = at.ID
                    where ft.id = @fieldTypeId and a.color is not null", new { fieldTypeId }).FirstOrDefault() > 0;
                    if (hasColor)
                    {
                        selectStatement = "JSON_VALUE(colorJson.FV,'$.name') AS text,JSON_VALUE(colorJson.FV,'$.color') AS color, v.value";
                    }
                    else
                    {
                        selectStatement = "v.text, v.value";
                        colorjoin = "";
                    }
                }
                string query = $@"
                    select {selectStatement} 
                    from FieldLookupValue V
                    {(fieldType.LookupObjectType == "Resource" ? resourceJoin : "")}
                    where @fieldTypeId = v.FieldTypeID
                    {whereQuery}
                    order by text asc
					{pagingQuery};

                    select count(1) from FieldLookupValue V
                        {(fieldType.LookupObjectType == "Resource" ? resourceJoin : "")}
                        where @fieldTypeId = FieldTypeID {whereQuery};
                    ";

                if (hasColor)
                {
                    query = $@"
                    drop table if exists #tempResults
			            select *
				        into #tempResults
                        from FieldLookupValue V
                        where @fieldTypeId = FieldTypeID {whereQuery}
                        order by text asc
					    {pagingQuery};

					 select {selectStatement} from #tempResults V {colorjoin};

                    select count(1) from FieldLookupValue V
                        where @fieldTypeId = FieldTypeID {whereQuery};
                    ";
                }

                var results = Company.Connection.QueryMultiple(query, new { fieldTypeId, skip, take, filter, fieldType.AllowAllLabel });

                if (!isForAssetForm)
                {
                    var data = new
                    {
                        items = results.Read<string>().ToList(),
                        count = results.Read<int>().FirstOrDefault()
                    };

                    return Request.CreateResponse(HttpStatusCode.OK, data);
                }
                else
                {
                    var items = new List<DDLSelectItem>();
                    if (fieldType.AllowAllValue)
                    {
                        items.Add(new DDLSelectItem { text = fieldType.AllowAllLabel, value = "0" });
                    }

                    items.AddRange(results.Read<DDLSelectItem>().ToList());
                    var count = results.Read<int>().FirstOrDefault();
                    if (items.Any(x => x.value == "0"))
                    {
                        count++;
                    }

                    var data = new
                    {
                        items,
                        count
                    };

                    return Request.CreateResponse(HttpStatusCode.OK, data);
                }


            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        public class DDLSelectItem
        {
            public string text { get; set; }
            public string value { get; set; }
            public string color { get; set; }
        }

        /// <summary>
        /// Retrieves lookup values for an asset type and field name
        /// </summary>
        /// <returns>Returns a list of lookup values</returns>        
        /// <param name="assetUid">Uid of the asset type</param>
        /// <param name="fieldName">Field name</param>
        /// <param name="filterName">Field name</param>
        [HttpGet,
         Route("{assetUid:Guid}/complexLookupvalues/{fieldName}/filter/{filterName}"),
         SwaggerResponse(HttpStatusCode.OK, "A list of filter values for a given asset type and field name.", typeof(List<string>)),
         SwaggerResponse(HttpStatusCode.BadRequest, "An error indicating the request is invalid.", typeof(ErrorResponse)),
         SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
         ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IHttpActionResult> GetFilterValuesForComplexFields(Guid assetUid, string fieldName, string filterName, int? skip = null, int? take = 0, string filter = null)
        {
            var prefix = "Fields.GetFilterValuesForComplexFields => ";
            try
            {
                var asset = AssetRepository.GetAssetByUID(assetUid);
                var assetType = Company.AssetTypes.FirstOrDefault(x => x.ID == asset.AssetTypeID);
                var dbArgs = new DynamicParameters();

                var fieldType = Company.FieldTypes.FirstOrDefault(x => x.AssetTypeID == assetType.ID && x.Name == fieldName);
                if (fieldType.Type == DataType.OwnershipLookup.ToString())
                {

                    if (filterName == "ResponsibilityTypeName")
                    {
                        string whereExpression = string.Empty;
                        if (!string.IsNullOrEmpty(filter))
                        {
                            whereExpression = " and rt.name like @filter";
                            dbArgs.Add("filter", $"%{filter}%");
                        }

                        var itemsQuery = $@"
                            select count(*) from ResponsibilityType rt
                            inner join ResponsibilityTypeRelation RTR on RTR.ResponsibilityTypeID = RT.ID
                            inner join AssetType at on at.Object = RTR.ObjectType  and at.objectid = rtr.objectid
                            where at.uid = @assetTypeUid {whereExpression}
                            
                            select rt.Name as 'value', rt.Name as 'title' from ResponsibilityType rt
                            inner join ResponsibilityTypeRelation RTR on RTR.ResponsibilityTypeID = RT.ID
                            inner join AssetType at on at.Object = RTR.ObjectType  and at.objectid = rtr.objectid
                            where at.uid = @assetTypeUid {whereExpression}";

                        dbArgs.Add("assetTypeUid", assetType.uid);

                        var gridReader = await Company.Database.Connection.QueryMultipleAsync(
                              new CommandDefinition(itemsQuery,
                              parameters: dbArgs,
                              commandTimeout: 60
                            ));

                        var data = new
                        {
                            count = gridReader.Read<int>().FirstOrDefault(),
                            items = gridReader.Read<dynamic>().ToList()
                        };

                        return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, data))).ConfigureAwait(false);
                    }
                    if (filterName == "ResourceName")
                    {
                        var ftl = Company.FieldTypeLookups.FirstOrDefault(x => x.FieldTypeID == fieldType.ID);
                        var definition = ftl.ParseOwnershipLookupDefinition();

                        dbArgs.Add("assettypeid", assetType.ID);
                        dbArgs.Add("assetId", asset.ID);

                        string selectSqlStatement = $@"
                                select '[' + ResponsibilityTypeName + '] - ' + ResourceName as 'title', 
                                ResourceName as 'value'
                                from #OwnershipLookupAssets";

                        if (!definition.ExpandGroupMembership)
                        {
                            selectSqlStatement = @"
                                select distinct SecurityAssetName as 'title',
						        SecurityAssetName as 'value'
                                from #OwnershipLookupAssets";
                        }

                        var possibleOwnersSql = $@"declare @id int = (select top 1 id from assettype where id = @assettypeid)

                    drop table if exists #OwnershipLookupAssets;
                    create table #OwnershipLookupAssets (
						AssetID bigint,
                        ResponsibilityTypeID int,
                        ResponsibilityTypeName nvarchar(250),
                        ResourceName nvarchar(501),
                        SecurityAsset char(1),
                        SecurityAssetName nvarchar(501),
                        Context nvarchar(max),
                        ResourceId int,
                        ResourceUid uniqueidentifier,
                        SecurityAssetId int,
                        SecurityAssetUid uniqueidentifier
					);
					insert into #OwnershipLookupAssets
                        SELECT [AssetID]
                              ,[ResponsibilityTypeID]
                              ,[ResponsibilityTypeName]
                              ,[ResourceName]
                              ,[SecurityAsset]
                              ,[SecurityAssetName]
                              ,[Context]
                              ,[ResourceId]
                              ,[ResourceUid]
                              ,[SecurityAssetId]
                              ,[SecurityAssetUid]
                        FROM [dbo].[ResponsibilityDetail] rd
                        where rd.assetid <> 0 and IsVisible = 1 and rd.[AssetTypeID] = @id and rd.AssetID = @assetId
                        union all
                        select a.[ID] as AssetID
                             ,rd.[ResponsibilityTypeID]
                             ,rd.[ResponsibilityTypeName]
                             ,rd.[ResourceName]
                             ,rd.[SecurityAsset]
                             ,rd.[SecurityAssetName]
                             ,rd.[Context]
                             ,rd.[ResourceId]
                             ,rd.[ResourceUid]
                             ,rd.[SecurityAssetId]
                             ,rd.[SecurityAssetUid]
                        from ResponsibilityDetail rd
                        inner join asset a on rd.assettypeid = a.assettypeid
                        where rd.assetid = 0 and IsVisible = 1 and rd.assettypeid = @id and a.id = @assetId
                        union all
                        select a.[ID] as AssetID
                             ,rd.[ResponsibilityTypeID]
                             ,rd.[ResponsibilityTypeName]
                             ,rd.[ResourceName]
                             ,rd.[SecurityAsset]
                             ,rd.[SecurityAssetName]
                             ,rd.[Context]
                             ,rd.[ResourceId]
                             ,rd.[ResourceUid]
                             ,rd.[SecurityAssetId]
                             ,rd.[SecurityAssetUid]
                        from ResponsibilityDetail rd
                        inner join asset a on rd.assetid = a.id
                        where rd.AssetTypeID = 0 and IsVisible = 1 and a.AssetTypeID = @id and a.id = @assetId;

                    create index cix_OwnershipLookupAssetId on #OwnershipLookupAssets (AssetId);                            
                        {selectSqlStatement}
                        ";

                        var gridReader = await Company.Database.Connection.QueryMultipleAsync(
                          new CommandDefinition(possibleOwnersSql,
                          parameters: dbArgs,
                          commandTimeout: 60
                        ));

                        var readItems = gridReader.Read<dynamic>().ToList();

                        if (!string.IsNullOrEmpty(filter))
                        {
                            List<dynamic> filtered = new List<dynamic>();
                            foreach (var item in readItems)
                            {
                                var dictData = item as IDictionary<string, object>;
                                if (dictData.ContainsKey("title"))
                                {
                                    if (dictData["title"].ToString().ToLower(CultureInfo.InvariantCulture).Contains(filter.ToLower(CultureInfo.InvariantCulture)))
                                    {
                                        filtered.Add(item);
                                    }
                                }
                            }
                            readItems = filtered;
                        }

                        var data = new
                        {
                            count = readItems.Count,
                            items = readItems
                        };

                        return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, data))).ConfigureAwait(false);
                    }

                    if (filterName == "SecurityAssetName")
                    {

                        var viaResources = $@"
            ; with owners as (select distinct
                    responsibilityTypeId,
		            securityAssetid,
	                '[' + ResponsibilityTypeName + '] - ' + SecurityAssetName as 'Name', 
                    case 
                        when SecurityAsset = 'R' then 'Resource'
                        when SecurityAsset = 'O' then 'Organization'
                        when SecurityAsset = 'G' then 'Group'
                        else[Type]
                            end as [Type],
                    SecurityAssetName
                            from ResponsibilityDetail
            where TypeID = @id
                    and[Type] = @Object and SecurityAsset <> 'R'
                    and IsVisible = 1)
            select o.Name as 'title', o.SecurityAssetName as 'value'
            from owners o
            cross apply(
            select top 1 * from
            ResponsibilityDetail rd where rd.ResponsibilityTypeID = o.responsibilityTypeId

                                                and rd.SecurityAssetID = o.SecurityAssetID and rd.TypeID = @id and rd.[Type] = @Object
            )Res
            order by o.[Name]
                        ";

                        dbArgs.Add("id", assetType.ObjectID);
                        dbArgs.Add("Object", assetType.Object);
                        var gridReader = await Company.Database.Connection.QueryMultipleAsync(
                          new CommandDefinition(viaResources,
                          parameters: dbArgs,
                          commandTimeout: 60
                        ));
                        var readItems = gridReader.Read<dynamic>().ToList();

                        if (!string.IsNullOrEmpty(filter))
                        {
                            List<dynamic> filtered = new List<dynamic>();
                            foreach (var item in readItems)
                            {
                                var dictData = item as IDictionary<string, object>;
                                if (dictData.ContainsKey("title"))
                                {
                                    if (dictData["title"].ToString().ToLower(CultureInfo.InvariantCulture).Contains(filter.ToLower(CultureInfo.InvariantCulture)))
                                    {
                                        filtered.Add(item);
                                    }
                                }
                            }
                            readItems = filtered;
                        }

                        var data = new
                        {
                            count = readItems.Count,
                            items = readItems
                        };

                        return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, data))).ConfigureAwait(false);
                    }
                }


                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new List<string>()))).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix}});
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(ReturnApiError(HttpStatusCode.InternalServerError, errorMessage))).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Retrieves complex lookup field types for an asset and field name
        /// </summary>
        /// <returns>Returns a list of field types</returns>        
        /// <param name="assetUid">Uid of the asset</param>
        /// <param name="fieldName">Field name</param>
        [HttpGet,
            Route("{assetUid:Guid}/complexlookupfields/{fieldName}"),
             SwaggerResponse(HttpStatusCode.OK, "A list of filter values for a given asset uid and field name.", typeof(List<FieldTypesApiViewModel>)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error indicating the request is invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
            ]
        public HttpResponseMessage GetComplexLookupFields(Guid assetUid, string fieldName)
        {

            var prefix = "Fields.GetFilterVales => ";
            try
            {
                FieldTypesApiViewModel response = new FieldTypesApiViewModel();
                response.items = new List<FieldTypeApiViewModel>();

                var asset = AssetRepository.GetAssetByUID(assetUid);
                var assetType = Company.AssetTypes.FirstOrDefault(x => x.ID == asset.AssetTypeID);
                var fieldType = Company.FieldTypes.FirstOrDefault(x => x.AssetTypeID == assetType.ID && x.Name == fieldName);
                if (fieldType.Type == DataType.OwnershipLookup.ToString())
                {
                    var ftl = Company.FieldTypeLookups.FirstOrDefault(x => x.FieldTypeID == fieldType.ID);
                    var definition = ftl.ParseOwnershipLookupDefinition();

                    response.items.Add(new FieldTypeApiViewModel { Name = "ResponsibilityTypeName", FriendlyName = "Responsibility", Type = new FieldTypeDataTypeApiViewModel { Lookup = new FieldTypeDataTypeLookupApiViewModel { List = new FieldTypeDataTypeLookupApiViewModel_List() } }, Category = "" });
                    response.items.Add(new FieldTypeApiViewModel { Name = "ResourceName", FriendlyName = "Assigned User/Group", Type = new FieldTypeDataTypeApiViewModel { Lookup = new FieldTypeDataTypeLookupApiViewModel { List = new FieldTypeDataTypeLookupApiViewModel_List() } }, Category = "" });
                    if (definition.DisplayAssignmentSource)
                    {
                        response.items.Add(new FieldTypeApiViewModel { Name = "SecurityAssetName", FriendlyName = "Via", Type = new FieldTypeDataTypeApiViewModel { Lookup = new FieldTypeDataTypeLookupApiViewModel { List = new FieldTypeDataTypeLookupApiViewModel_List() } }, Category = "" });
                    }
                    response.items.Add(new FieldTypeApiViewModel { Name = "Context", FriendlyName = "Context", Type = new FieldTypeDataTypeApiViewModel { Html = new FieldTypeDataTypeHtmlApiViewModel() }, Category = "" });
                }
                if (fieldType.Type == DataType.RefListRelationship.ToString()
                    || fieldType.Type == DataType.ComplexRelationLookup.ToString())
                {
                    Guid? assetTypeUid = Guid.Empty;
                    var fields = FieldsRepository.GetFieldDefinitionForComplexLookupFieldType(fieldType, assetUid, true).ToList();
                    if (fields.Count > 0)
                    {

                        var assettypeid = fields.Where(x => x.AssetTypeID != null).FirstOrDefault()?.AssetTypeID;
                        if (assettypeid.HasValue)
                        {
                            assetTypeUid = Company.AssetTypes.FirstOrDefault(x => x.ID == assettypeid)?.uid;
                        }
                    }

                    foreach (var f in fields)
                    {
                        var c = new FieldTypeApiViewModel
                        {
                            Name = f.Name,
                            FriendlyName = f.FriendlyName,
                            Category = "",
                            AssetTypeUid = assetTypeUid
                        };

                        c.Type = new FieldTypeDataTypeApiViewModel();
                        if (f.Type == DataType.Lookup.ToString())
                        {
                            if (fieldType.Type == DataType.ComplexRelationLookup.ToString())
                            {
                                var @object = f.LookupObjectType.EndsWith("Type") ? f.LookupObjectType : f.LookupObjectType + "Type";
                                var lookupAssetType = Company.AssetTypes.FirstOrDefault(x => x.Object == @object && x.ObjectID == f.LookupObjectID);
                                if (lookupAssetType != null)
                                {
                                    c.AssetTypeUid = lookupAssetType.uid;
                                }
                            }

                            c.Type.Lookup = new FieldTypeDataTypeLookupApiViewModel
                            {
                                List = new FieldTypeDataTypeLookupApiViewModel_List
                                {
                                    AllowMultipleValues = f.AllowMultipleValues
                                }
                            };
                        }

                        if (f.Type == DataType.Boolean.ToString())
                        {
                            c.Type.Boolean = new FieldTypeDataTypeBooleanApiViewModel();
                        }

                        if (f.Type == DataType.Date.ToString())
                        {
                            c.Type.Date = new FieldTypeDataTypeDateApiViewModel();
                        }

                        if (f.Type == DataType.DateTime.ToString())
                        {
                            c.Type.DateTime = new FieldTypeDataTypeDateTimeApiViewModel();
                        }
                        if (f.Type == DataType.Decimal.ToString())
                        {
                            c.Type.Decimal = new FieldTypeDataTypeDecimalApiViewModel();
                        }
                        if (f.Type == DataType.Html.ToString())
                        {
                            c.Type.Html = new FieldTypeDataTypeHtmlApiViewModel();
                        }
                        if (f.Type == DataType.JSON.ToString())
                        {
                            c.Type.Json = new FieldTypeDataTypeJsonApiViewModel();
                        }
                        if (f.Type == DataType.JsonElement.ToString())
                        {
                            c.Type.Text = new FieldTypeDataTypeTextApiViewModel();
                        }
                        if (f.Type == DataType.Link.ToString())
                        {
                            c.Type.Link = new FieldTypeDataTypeLinkApiViewModel();
                        }
                        if (f.Type == DataType.Number.ToString())
                        {
                            c.Type.Number = new FieldTypeDataTypeNumberApiViewModel();
                        }
                        if (f.Type == DataType.Score.ToString())
                        {
                            c.Type.Score = new FieldTypeDataTypeComputedScoreApiViewModel();
                            c.Type.Score.ScoreType = (f.ScoreType.HasValue && f.ScoreType == 1) ? ScoreType.Governance : ScoreType.DataQuality;
                        }
                        if (f.Type == DataType.Tag.ToString())
                        {
                            c.Type.Tag = new FieldTypeDataTypeTagApiViewModel();
                        }
                        if (f.Type == DataType.Text.ToString())
                        {
                            c.Type.Text = new FieldTypeDataTypeTextApiViewModel();
                        }
                        if (f.Type == DataType.Path.ToString())
                        {
                            c.Type.Path = new FieldTypeDataTypePathApiViewModel();
                        }
                        if (f.Type == DataType.Color.ToString())
                        {
                            c.Type.Lookup = new FieldTypeDataTypeLookupApiViewModel();
                            c.Type.Lookup.List = new FieldTypeDataTypeLookupApiViewModel_List
                            {
                                AllowMultipleValues = f.AllowMultipleValues,
                            };
                            c.FriendlyName = "Color";
                            c.Name = "Color";
                        }

                        if (f.Type == DataType.Relationship.ToString())
                        {
                            c.Type.Relationship = new FieldTypeDataTypeRelationshipApiViewModel();
                            c.Type.Relationship.IntersectTypeUid = Company.IntersectTypes.FirstOrDefault(x => x.ID == f.LookupObjectID).uid;
                            c.FriendlyName = c.FriendlyName.Replace("Related Item.", "");
                        }

                        if (f.Type == DataType.Counter.ToString())
                        {
                            c.Type.Counter = new FieldTypeCounterApiViewModel();
                            c.Type.Counter.CounterPrefix = f.CounterPrefix;
                        }

                        if (string.IsNullOrEmpty(c.FriendlyName))
                        {
                            c.FriendlyName = "#Missing Friendly Name";
                        }

                        response.items.Add(c);
                    }
                }
                return Request.CreateResponse(HttpStatusCode.OK, response);

            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix
    }
});

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        #endregion
    }
}
