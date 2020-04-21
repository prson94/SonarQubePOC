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
using Resources;
using System.Web.Http.Description;

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
                string isValid = isPageSizeAndNumValid(queryParams);

                if (!string.IsNullOrEmpty(isValid))
                {
                    throw new RestApiException(HttpStatusCode.BadRequest, "Invalid request", isValid);
                }
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
        /// You may only provide one of the following: `ActionTypeUid`, `AssetTypeUid`, or `RelationshipTypeUid`.
        /// 
        /// There are some general rules about the various field types:
        /// - `Boolean` *(True/False)*
        ///     1. Supports adding values through the Govern Application UI and REST API.
        /// - `ComputedFusionLookup` *(Fusion Lookup)*
        ///     1. This is a computed field and does not support directly editing values.
        /// - `ComputedOwnershipLookup` *(Ownership Lookup)*
        ///     1. This is a computed field and does not support directly editing values.
        /// - `ComputedRelationshipField` *(Field from Relationship)*
        ///     1. This is a computed field and does not support directly editing values.
        /// - `ComputedRelationshipLookup` *(Relation Lookup)*
        ///     1. This is a computed field and does not support directly editing values.
        /// - `ComputedRelationshipReferenceList` *(Reference Item List from Relationship)*
        ///     1. This is a computed field and does not support directly editing values.
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
        /// - `Relationship` *(Relationship)*
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutFieldTypesAsync(FieldTypesApiEditModel model)
        {
            var prefix = "Fields.PutFieldTypesAsync => ";
            var errorMessage = "";

            try
            {

                if (model == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

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
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Action Type with Uid {model.AssetTypeUid.Value} could not be found."));
                }

                if (model.AssetTypeUid.HasValue)
                {
                    assetTypeIdentifierInfoModels = await Company.GetTypeIdentifierInfoModel(TypeIdentifierInfoModelType.AssetType, model.AssetTypeUid.Value);
                    typeIdentifierInfoModel = assetTypeIdentifierInfoModel = assetTypeIdentifierInfoModels.SingleOrDefault();

                    if (typeIdentifierInfoModel == null)
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with Uid {model.AssetTypeUid.Value} could not be found."));
                }

                if (model.RelationshipTypeUid.HasValue)
                {
                    relationshipTypeIdentifierInfoModels = await Company.GetTypeIdentifierInfoModel(TypeIdentifierInfoModelType.RelationshipType, model.RelationshipTypeUid.Value);
                    typeIdentifierInfoModel = relationshipTypeIdentifierInfoModel = relationshipTypeIdentifierInfoModels.SingleOrDefault();

                    if (typeIdentifierInfoModel == null)
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Relationship Type with Uid {model.AssetTypeUid.Value} could not be found."));
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
                    throw new RestApiException(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, "You do not have permissions to change fields on this type.");
                }
                #endregion

                #region Validation
                var existingFields = FieldsRepository.GetFieldTypes(typeIdentifierInfoModel);
                var ExistingIntersectID = new List<Tuple<string, Guid>>();
                if (model.AssetTypeUid.HasValue)
                {
                    ExistingIntersectID = FieldsRepository.GetFieldInterSetUID(existingFields);
                }
                
                var isFusionEnabled = Community.IsFusionEnabled();
                var validationStatus = FieldApiModelValidator.ValidateModel(model, actionTypeIdentifierInfoModel, assetTypeIdentifierInfoModel, relationshipTypeIdentifierInfoModel, isFusionEnabled, existingFields, ExistingIntersectID);
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
                
                foreach (var field in model.Fields)
                {
                    if(field.Type?.Text?.Validation != null && (!string.IsNullOrEmpty(field.Type.Text.Validation.Pattern) || !field.Type.Text.Validation.IsRequired))
                    {
                        field.Type.Text.Validation.MinimumLength = 0;
                    }                    
                }

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
                    throw new RestApiException(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, "You do not have permissions to remove fields on this type.");
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<HttpResponseMessage> GetLookups(string fieldtypename, Guid? AssetTypeUid = null, Guid? RelationshipTypeUid = null, Guid? ActionTypeUid = null, bool isNg = false)
        {
            var prefix = "Fields.GetLookups => ";
            var errorMessage = "";

            try
            {
                #region Load static lists

                int id = 0;
                int fieldtypeid = 0;
                FieldType fieldType;
                SystemObjects type = SystemObjects.ArtifactType;
                if (AssetTypeUid != null)
                {
                    var assetType = Company.Filter<AssetType>(x => x.uid == AssetTypeUid).SingleOrDefault();
                    id = assetType.ObjectID;
                    Enum.TryParse(assetType.Object, out type);
                    fieldType = Company.Filter<FieldType>(x => x.AssetTypeID == id && x.Name == fieldtypename).SingleOrDefault();
                }
                else if (ActionTypeUid != null)
                {
                    var issueType = Company.Filter<IssueType>(x => x.uid == ActionTypeUid).SingleOrDefault();
                    id = issueType.ID;
                    Enum.TryParse("IssueType", out type);
                    fieldType = Company.Filter<FieldType>(x => x.Object == "IssueType" && x.ObjectID == id && x.Name == fieldtypename).SingleOrDefault();
                }
                else if (RelationshipTypeUid != null)
                {
                    var intersectType = Company.Filter<IntersectType>(i => i.uid == RelationshipTypeUid).SingleOrDefault();
                    id = intersectType.ID;
                    fieldType = Company.Filter<FieldType>(x => x.Object == "IntersectType" && x.ObjectID == id && x.Name == fieldtypename).SingleOrDefault();
                }
                else
                {
                    throw new Exception("No assetTypeUid or actionTypeUid or relationshipTypeUid provided");
                }
                fieldtypeid = fieldType == null ? 0 : fieldType.ID;

                var lists = await Company.QueryAsync<dynamic>("exec utility.GetFieldTypeLookupList");
                var intersectTypes = lists.Where(i => i.type == "I").Select(i => new { i.value, i.title }).OrderBy(i => i.title);
                var attributes = lists.Where(i => i.type == "A").Select(i => new { i.value, i.title }).OrderBy(i => i.title);
                var fusionAttributeTypes = lists.Where(i => i.type == "F").Select(i => new { i.value, i.title }).OrderBy(i => i.title);
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

                var cardinalRelationships = allRelationships.Where(i =>
                    (i.Subject == sType && i.SubjectID == id && i.SubjectCardinality == Cardinality.One) ||
                    (i.Object == sType && i.ObjectID == id && i.ObjectCardinality == Cardinality.One)
                ).ToList();

                var fieldFromRelRelationships = allRelationships.Where(i =>
                    (i.Subject == sType && i.SubjectID == id && i.ObjectCardinality == Cardinality.One) ||
                    (i.Object == sType && i.ObjectID == id && i.SubjectCardinality == Cardinality.One)
                ).ToList();

                IEnumerable<int> LookupObjectIDs = await Company.QueryAsync<int>(@"select distinct LookupObjectID from [FieldType] ft 
                                                                  where (Object = @objectType and ObjectID = @objectid) 
                                                                  and (LookupObjectID is not null) and Type = 'Relationship' 
                                                                  and not exists (select 1 from [FieldType] ft2 
                                                                                  where ft2.id = @ffieldtypeid
                                                                                  and   ft2.LookupObjectID = ft.LookupObjectID
                                                                                  and   ft2.LookupObjectID is not null)", new { objectType = sType, objectid = id, ffieldtypeid = fieldtypeid });

                var Field_Relationships = allRelationships
                    .Where(x => x.PredicateType != PredicateType.InterTypeHierarchy
                                && x.Object != SystemObjects.IntersectType.ToString()
                                && x.Subject != SystemObjects.IntersectType.ToString()
                                && !LookupObjectIDs.Contains(x.ID))
                    .Select(i => new
                    {
                        title = ((i.Subject == sType && i.SubjectID == id) ?
                            $"{i.SubjectName} {i.PredicateName} {i.ObjectName}" :
                            $"{i.ObjectName} {i.PredicateInverse} {i.SubjectName}"),
                        value = i.Uid
                    });

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

                if (!Community.IsFusionEnabled())
                {
                    dataTypeOptions = dataTypeOptions.Where(x => x.value != "FusionLookup").ToList();
                }

                var jsonFieldType = new Dictionary<string, string>()
            {
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

                #endregion


                return Request.CreateResponse(HttpStatusCode.OK, new {
                        Attributes = attributes,
                        Field_Relationships,
                        Field_JsonFields,
                        Field_JsonDataTypes,
                        Field_CardinalRelationships,
                        Field_FieldFromRelRelationships,
                        Field_CardinalReferenceRelationships,
                        DataTypes = dataTypeOptions,
                        FilteredLookups = filteredLookups,
                        Patterns = patterns.Select(i => new { title = i.Key, value = i.Value }),
                        IntersectTypes = intersectTypes,
                        FusionAttributeTypes = fusionAttributeTypes,
                        Lookups = lookups,
                        ComplexLookupRelations = complexLookupRelations.Select(x => new { ID = (int)x.ID, x.Name, x.DisplayName}) 
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public HttpResponseMessage GetFieldTypeFormData(string name, Guid? assetTypeUid, Guid? actionTypeUid, Guid? relationshipTypeUid)
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
                    throw new Exception("No assetTypeUid or actionTypeUid or relationshipTypeUid provided");
                }

                List<dynamic> filteredLookupItems = null;
                List<dynamic> fusionItems = null;
                List<dynamic> relationItems = null;
                dynamic ownershipLookupSettings = null;
                dynamic JsonElementSettings = null;

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
                        var definition = (dynamic)Newtonsoft.Json.JsonConvert.DeserializeObject(lookup.Definition);

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
                                    Direction = r.Direction ?? 0,
                                    r.Object,
                                    r.ObjectID
                                });
                            }
                            if (definition.Fields != null)
                            {
                                foreach (var f in definition.Fields)
                                {
                                    var r = relationItems.Where(i => i.Object == f.Object && i.ObjectID == f.ObjectID).FirstOrDefault();

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
                                lookup.HideFilter,
                                lookup.HideFooter,
                                lookup.HideHeader
                            };
                        }
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, new {                    
                        FieldType = ft,
                        FilteredLookupItems = filteredLookupItems,
                        FusionItems = fusionItems,
                        JsonElementSettings,
                        OwnershipLookupSettings = ownershipLookupSettings,
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public HttpResponseMessage GetFieldTypeLookupTokens(string identifier)
        {
            var prefix = "Fields.GetFieldTypeLookupTokens => ";
            var errorMessage = "";

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
                else if(Guid.TryParse(identifier, out Guid Uid))
                {
                    var item = Company.Filter<AssetType>(x => x.uid == Uid).SingleOrDefault();
                    Enum.TryParse(item.Object, out type);
                    id = item.ObjectID;
                    list = Company.GetFieldTypesByObject(type, id)
                        .Where(i => i.Type != DataType.Attribute.ToString() && i.Type != DataType.ComplexRelationLookup.ToString())
                        .Select(i => new { i.ID, i.Name })
                        .ToDictionary(i => i.Name, i => i.Name);

                }
                else
                {
                    throw new Exception("Invalid Identifier provided.");
                }

                switch (type)
                {
                    case SystemObjects.ArtifactType:
                        list.Add("ID", "ID");
                        break;
                    case SystemObjects.ReferenceItem:
                    case SystemObjects.ReferenceItemType:
                        list.Add("Code", "Code");
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
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
                    throw new Exception("No assetTypeUid or actionTypeUid or relationshipTypeUid provided");
                }

                var intersectType = Company.Filter<IntersectType>(x => x.uid == intersectTypeUid).SingleOrDefault();

                if (intersectType == null)
                    throw new RestApiException(HttpStatusCode.BadRequest, $"No IntersecType found for [{intersectTypeUid.ToString()}]");

                var isSubject = (intersectType.Subject == type.ToString() && intersectType.SubjectID == id);

                var targetObjectType = isSubject ? intersectType.Object : intersectType.Subject;
                var targetObjectTypeID = isSubject ? intersectType.ObjectID : intersectType.SubjectID;

                var list = Company.Filter<FieldType>(f => f.Object == targetObjectType && f.ObjectID == targetObjectTypeID)
                    .Where(i => i.Type != DataType.Attribute.ToString() &&
                            i.Type != DataType.ComplexRelationLookup.ToString() &&
                            i.Type != DataType.Relationship.ToString() &&
                            i.Type != DataType.JSON.ToString()
                            && i.Type != DataType.Tag.ToString())
                    .Select(i => new { i.ID, i.Name })
                    .Distinct()
                    .ToDictionary(i => i.Name, i => i.ID);

                return Request.CreateResponse(HttpStatusCode.OK, list.Select(i => new { title = i.Key, value = i.Value }));
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)            
        ]
        public async Task<HttpResponseMessage> GetLookupDefaultValues(string Uid)
        {
            var prefix = "Fields.GetLookupDefaultValues => ";
            var errorMessage = "";

            try
            {
                Guid assetUid;
                if(Guid.TryParse(Uid, out assetUid))
                {
                    //handle cases for reference list and models 
                }
                var list = new List<ListIntItem>();
                list.Add(new ListIntItem { title = "- No default -", value = null });
                var usersOnly = false;
                string sql = "";
                usersOnly = Company.Filter<AssetType>(x => x.uid == assetUid && x.Class == AssetTypeClass.User).Count() > 0;
                if (usersOnly)
                {
                    string HideD3SUsers = HideData3SixtyUsers() ? "": " WHERE Email not like '%@data3sixty.com' and Email not like '%@infogix.com' "; 
                    sql = $@"
                        select 
                            R.ResourceID as value,
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
                            ast.ObjectID as value,
                            d.DisplayValue as title  
                        from asset ast 
                            inner join assettype astt on (ast.assettypeid = astt.id) 
                            cross apply [dbo].GetAssetDisplayValueById(ast.id) d 
                        where astt.Uid = @Uid order by d.DisplayValue
                                        
                    ";

                }

                list.AddRange(
                    await Company.QueryAsync<ListIntItem>(sql, new { Uid = assetUid })
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
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
                    throw new Exception("No assetTypeUid or actionTypeUid or relationshipTypeUid provided");
                }
                AssetType refitem = null;
                if(Guid.TryParse(uid, out Guid refitemGuid))
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
                        if (list.Count > 0) list.Insert(0, new PrimeSelectItem { label = "", value = "" });
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<HttpResponseMessage> GetLookupListFilter(string uid, Guid? assetTypeUid = null, Guid? actionTypeUid = null, Guid? relationshipTypeUid = null)
        {
            var prefix = "Fields.GetLookupListFilter => ";
            var errorMessage = "";

            try
            {
                string type = "";
                int id = 0;
                string objectType = "";
                int objectId = 0;
                if (assetTypeUid != null)
                {
                    var assetType = Company.Filter<AssetType>(x => x.uid == assetTypeUid).SingleOrDefault();
                    type = assetType.Object;
                    id = assetType.ID;
                }
                else if (actionTypeUid != null)
                {
                    var issueType = Company.Filter<IssueType>(x => x.uid == actionTypeUid).SingleOrDefault();
                    type = SystemObjects.IssueType.ToString();
                    id = issueType.ID;
                }
                else if (relationshipTypeUid != null)
                {
                    var intersectType = Company.Filter<IntersectType>(i => i.uid == relationshipTypeUid).SingleOrDefault();
                    type = SystemObjects.IntersectType.ToString();
                    id = intersectType.ID;
                }
                else
                {
                    throw new Exception("No assetTypeUid or actionTypeUid or relationshipTypeUid provided");
                }

                string[] allowedAssetTypes = { "IssueType", "ArtifactType", "TaxonomyType", "PolicyType", "RuleType" };
                string[] allowedListTypes = { "Artifact", "Taxonomy" };

                if (!allowedAssetTypes.Contains(type))
                {
                    //return nothing no error
                    return null;
                }
                if (allowedListTypes.Contains(objectType))
                {
                    //return nothing no error;
                    return null;
                }

                var predicateTypes = string.Join(",", PredicateType.DataLineage.GetAsList()
                    .Where(f => f.AllowEditFromRelationshipEditor && f.AllowIntersectTypeAssignment)
                    .Select(i => ((int)i.ID).ToString())
                    .ToArray());

                string sql = $@"SELECT 
                        Concat(A.PredicateID, '|',A.Direction) as PredicateValue, 
                        A.PredicateName, 
                        A.ObjectName, 
                        A.[Object], 
                        A.[ObjectID], 
                        B.FieldTypeID, 
                        B.[FriendlyName],
						B.Type,
                        B.Class,
                        B.Name
                    FROM ( 
                        SELECT 
                            it.[ID] as IntersectTypeID, 
                            0 AS Direction, 
                            p.[ID] as PredicateID, 
                            p.[Name] as PredicateName, 
                            ot.[Name] as ObjectName, 
                            it.[Object] as [Object], 
                            it.[ObjectID] as [ObjectID] 
                        FROM [dbo].[IntersectType] it 
                            join [dbo].[Predicate] p on it.[PredicateID] = p.[ID] 
                            join [dbo].[AssetType] ot on ot.[Object] = it.[Object] and ot.[ObjectId] = it.[ObjectID] 
                            join [dbo].[AssetType] st on st.[Object] = it.[Subject] and st.[ObjectId] = it.[SubjectID] 
                        where it.[Subject] = @objectType 
                        and it.[SubjectID] = @objectId
                        and p.Type IN ({predicateTypes})
                        and it.[Object] in ('ArtifactType', 'TaxonomyType')
                        UNION ALL 
                        SELECT 
                            it.[ID], 
                            1 AS Direction, 
                            p.[ID] as PredicateID, 
                            p.[Inverse] as PredicateName,
                            st.[Name] as ObjectName, 
                            it.[Subject] as [Object], 
                            it.[SubjectID] as [ObjectID] 
                        FROM [dbo].[IntersectType] it 
                            join [dbo].[Predicate] p on it.[PredicateID] = p.[ID] 
                            join [dbo].[AssetType] ot on ot.[Object] = it.[Object] and ot.[ObjectId] = it.[ObjectID] 
                            join [dbo].[AssetType] st on st.[Object] = it.[Subject] and st.[ObjectId] = it.[SubjectID] 
                         where it.[Object] = @objectType 
                         and it.[ObjectID] = @objectId 
                         and p.Type IN ({predicateTypes})
                         and it.[Subject] in ('ArtifactType', 'TaxonomyType')
                        ) A LEFT OUTER JOIN
                    (SELECT 
                        ft.[ID] as FieldTypeID,
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
                    WHERE ft.[ObjectID] = @id AND ft.[Object] = @type  
                    ) B ON A.[Object] = B.LookupObject AND A.ObjectID = B.LookupObjectID";
                var parms = new
                {
                    objectType = objectType,
                    objectId = objectId,
                    type = type,
                    id = id
                };
                var list = await Company.QueryAsync<dynamic>(sql, parms);

                return Request.CreateResponse(HttpStatusCode.OK, list.Select(i => new
                    {
                        PredicateValue = i.PredicateValue,
                        PredicateName = i.PredicateName,
                        FieldTypeID = i.FieldTypeID,
                        FriendlyName = i.FriendlyName,
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<HttpResponseMessage> GetStandardRelations(Guid assetTypeUid)
        {
            var prefix = "Fields.GetStandardRelations => ";
            var errorMessage = "";

            try
            {
                string type = "";
                int id = 0;
                if (assetTypeUid != null)
                {
                    var at = Company.Filter<AssetType>(x => x.uid == assetTypeUid).SingleOrDefault();
                    type = at.Object;
                    id = at.ObjectID;
                }
                else 
                {
                    type = "IntersectType";
                    id = 0;
                }
                var intersectTypes = await Company.QueryAsync<dynamic>($@"select value, title from utility.GetIntersectTypesByType('{type}', {id}) order by title");

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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public HttpResponseMessage GetParentRelations(Guid assetTypeUid)
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
                    return ReturnApiError(HttpStatusCode.NotFound, $"No asset found for Uid [${assetTypeUid.ToString()}]");
                }
                dynamic list = null;
                BaseIntObject parent;

                switch (type)
                {
                    case SystemObjects.ArtifactType:
                        list = new List<AssetType>();
                        parent = Company.GetParentType(id, SystemObjects.ArtifactType);
                        if (parent != null)
                            list.Add((AssetType)parent);

                        return Request.CreateResponse(HttpStatusCode.OK, ((List<AssetType>)list).Select(i => new { value = $"0|{i.uid}", title = i.Name })
                        .Where(i => i.title != null)
                        .ToList());
                    case SystemObjects.FusionAttributeType:
                        list = new List<AssetType>();
                        parent = Company.GetParentType(id, SystemObjects.FusionAttributeType);
                        if (parent != null)
                            list.Add((AssetType)parent);

                        return Request.CreateResponse(HttpStatusCode.OK, ((List<AssetType>)list).Select(i => new { value = $"0{i.uid}", title = i.Name })
                            .Where(i => i.title != null)
                            .ToList());
                }

                return Request.CreateResponse(HttpStatusCode.OK, new List<dynamic>());
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public HttpResponseMessage GetChildRelations(Guid assetTypeUid)
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
                    return ReturnApiError(HttpStatusCode.NotFound, $"No asset found for Uid [${assetTypeUid.ToString()}]");
                }
                
                switch (type)
                {
                    case SystemObjects.ArtifactType:
                        return Request.CreateResponse(HttpStatusCode.OK, Company.GetChildTypes(id, SystemObjects.ArtifactType)
                            .ToList()
                            .Select(i => new { value = $"0|{i.uid}|0", title = i.Name })
                            .ToList());
                    case SystemObjects.FusionAttributeType:
                        return Request.CreateResponse(HttpStatusCode.OK, Company.GetChildTypes(id, SystemObjects.FusionAttributeType)
                            .ToList()
                            .Select(i => new { value = $"0|{i.uid}|0", title = i.Name })
                            .ToList());
                }

                return Request.CreateResponse(HttpStatusCode.OK, new List<dynamic>());
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
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
                    intersectTypeID = intersectType.ID;
                var at = Company.Filter<AssetType>(x => x.uid == assetTypeUid).SingleOrDefault();
                if (at != null)
                {
                    Enum.TryParse(at.Object, out type);
                    id = at.ObjectID;
                }
                else
                {
                    return ReturnApiError(HttpStatusCode.NotFound, $"No asset found for Uid [${assetTypeUid.ToString()}]");
                }

                var list = Company.GetFieldTypesByObject(type, id)
                 .Where(i => i.Type != DataType.Attribute.ToString()
                         && i.Type != DataType.Relationship.ToString()
                         && i.Type != DataType.OwnershipLookup.ToString()
                         && i.Type != DataType.RefListRelationship.ToString()
                         && i.Type != DataType.ComplexRelationLookup.ToString()
                         && i.Type != DataType.JSON.ToString()
                         && i.Type != DataType.Tag.ToString())
                 .Select(i => new { i.ID, i.Name })
                 .ToDictionary(i => i.Name, i => i.ID);

                if (type == SystemObjects.ReferenceItemType)
                {
                    if (id == 0)
                    {
                        list.Add("Name", 0);
                        if (!list.ContainsKey("Description"))
                            list.Add("Description", 0);
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
                else if (type == SystemObjects.FusionAttributeType)
                {
                    list.Add("Name", 0);
                }
                else if (type == SystemObjects.FusionQueryAttributeType)
                {
                    list.Add("Name", 0);
                    list.Add("DisplayValue", 0);
                }
                else
                {
                    list.Add("DisplayValue", 0);
                }

                list.Add("TextPath", 0);

                var relList = Company.GetFieldTypesByObject(SystemObjects.IntersectType, intersectTypeID)
                    .Where(i => i.Type != DataType.Attribute.ToString())
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
                    if (list.ContainsKey($"Related Item.{r.Name}"))
                    {
                        list.Add($"Related Item.{r.Name} ({r.ID})", r.ID);
                    }
                    else
                    {
                        list.Add($"Related Item.{r.Name}", r.ID);
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

        #endregion

    }
}
