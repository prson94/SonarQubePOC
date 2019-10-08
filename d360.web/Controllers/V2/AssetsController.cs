using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using d360.model.DataAccessLayer;
using d360.core.validators;
using System.Web.Http.Description;
using d360.core.resources;
using Resources;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling assets of varying types and classes.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/assets"),
        Authorize
    ]
    public class AssetsController : BaseV2ApiController
    {
        #region DI

        IQueueSource QueueSource;
        IStorageProvider Storage;
        IAssetRepository AssetRepository;
        ITagRepository tagRepository;

        public AssetsController(ICommunityContext community, ICompanyContext company, IStorageProvider storage, IQueueSource queueSource, IAssetRepository repository, ITagRepository tagRepository)
            : base(community, company)
        {
            QueueSource = queueSource;
            Storage = storage;
            this.AssetRepository = repository;
            this.tagRepository = tagRepository;
        }

        #endregion

        /// <summary>
        /// Retrieves a list of all asset types classes.
        /// </summary>
        /// <returns>Returns a list of asset type classes.</returns>
        [
            HttpGet,
            Route("classes"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset type classes.", typeof(List<AssetTypeClassInfo>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public HttpResponseMessage GetAssetTypeClassesAsync()
        {
            var prefix = "Assets.GetAssetTypeClassesAsync => ";
            var errorMessage = "";

            try
            {
                var classes = AssetRepository.GetAssetTypeList();
                return Request.CreateResponse(HttpStatusCode.OK, classes);
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
        /// GET a list of asset types.
        /// </summary>
        /// <param name="Class">Allows for filtering the Asset type's by Class.</param>
        /// <param name="FusionTypeUID">Filter by Fusion type UID. Only applicable for FusionQuery and FusionAttribute classes.</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("types"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset types.", typeof(List<AssetTypeApiViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetAssetTypesAsync(AssetTypeClass? Class = null, string FusionTypeUID = null)
        {
            var prefix = "Assets.GetAssetTypesAsync => ";
            var errorMessage = "";

            try
            {
                Guid? fusionTypeGuid = Guid.Empty;
                if (!string.IsNullOrEmpty(FusionTypeUID))
                {
                    if(Class == null || (Class == AssetTypeClass.FusionQuery || Class == AssetTypeClass.FusionAttribute))
                    {
                        fusionTypeGuid = Guid.Parse(FusionTypeUID);
                    }
                    else
                    {
                        throw new Exception("Invalid class type for Fusion type UID.");
                    }
                }

                var assetTypes = await AssetRepository.GetAssetType(Class, fusionTypeGuid);

                return Request.CreateResponse(HttpStatusCode.OK, assetTypes);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                if (ex is FormatException)
                {
                    errorMessage = errorMessage.Replace("Guid", "Uid");
                }

                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// Retrieves assets for the given asset type unique identifier.
        /// </summary>
        /// <remarks>
        /// In addition to the below query parameters a field name for the asset type can be specified to filter by exact match. For example MyCustomField=someExactValue.
        /// </remarks>
        /// <param name="assetTypeUid">The unique identifier of the asset type.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("{assetTypeUid:Guid}"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(AssetsApiViewModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by AssetId.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_predicateUid", "The Uid of a predicate type to return relationships for. If specified the results will include relationships of this predicate type. Assets without this type of relationship defined will be omitted.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_subjectUid", "The Uid of the subject side of a relationship to filter by in addition to filtering by predicate type. _predicateUid is required.", DataType = "string", ParameterType = "query", Required = false),
        ]
        public async Task<IHttpActionResult> GetAssetsAsync(Guid assetTypeUid)
        {
            var prefix = "Assets.GetAssetsAsync => ";
            var errorMessage = "";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var validator = new AssetTypeValidator(this.Company);
                if(!validator.IsValidOrderByFieldForGetAssets(assetTypeUid, queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Invalid order passed in the request"));

                var results = await AssetRepository.GetAssets(assetTypeUid, queryParams);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "AssetTypeUid", assetTypeUid.ToString() }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }

        }

        /// <summary>
        /// Get field types for the given asset type Uid
        /// </summary>
        /// <param name="assetTypeUid">The Uid of the asset type</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("fields/{assetTypeUid}"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(AssetsApiViewModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetAssetsTypeFieldsAsync(Guid assetTypeUid)
        {
            var prefix = "Assets.GetAssetsTypeFieldsAsync => ";
            var errorMessage = "";

            try
            {
                var fieldTypes = AssetRepository.GetFieldTypes(assetTypeUid) as object;

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, fieldTypes)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }


        /// <summary>
        /// Add an asset type based on Asset Type Class
        /// </summary>
        /// <remarks>
        /// This endpoint can add the following asset type class
        /// Business,Technical,Model,Organization,Policy,Reference,Rule
        /// </remarks>
        /// <param name="model">Asset Type</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            Route(""),
            SwaggerRequestExample(typeof(AssetTypeInsert), typeof(AssetTypeInsertExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Newly asset type Uid and success / failure message.", typeof(AssetTypeSuccess)),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset Type not found based on Uid provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to create an asset type", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Conflict, "You already have an asset type with the specified name", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostAssetTypeAsync(AssetTypeInsert model)
        {
            var prefix = "Assets.PostAssetTypeAsync => ";
            
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage));

                var validator = new AssetTypeValidator(this.Company);

                AssetType parentAssetType = null;
                if (model.ParentUid.HasValue && model.ParentUid != Guid.Empty)
                {
                    parentAssetType = AssetRepository.GetAssetTypeByUID((Guid)model.ParentUid);
                }

                Predicate predicate = null;
                if (model.Hierarchy != null && model.Hierarchy.PredicateUid.HasValue && model.Hierarchy.PredicateUid != Guid.Empty)
                {
                    predicate = AssetRepository.GetPredicateByUID((Guid)model.Hierarchy.PredicateUid);
                }

                var validationStatus = validator.ValidateModel(true, model, parentAssetType, predicate);
                if (validationStatus.StatusCode != HttpStatusCode.OK)
                    return await Task.FromResult(errorMessageResponse(validationStatus.StatusCode, validationStatus.Error, validationStatus.Message));

                if (model.UseAsTransformation && (model.Class != AssetTypeClass.BusinessAsset && model.Class != AssetTypeClass.TechnicalAsset))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Use As Transformation", AssetTypeErrors.TransformationClassRestriction));

                if (model.CanOwnFusion.HasValue && model.CanOwnFusion.Value && model.Class != AssetTypeClass.BusinessAsset)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Can Own Fusion", "Can Own Fusion can be set only asset types that are of class Business"));

                if (AssetRepository.IsReachedTransformationLimit(model))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Reached Transformation limit", AssetTypeErrors.TransformationLimitExceeded));

                AssetType assetType = null;
                var nameFriendlyName = "Name";
                var isNamePartOfKey = true;

                var insertStatus = AssetRepository.AddAssetType(model, assetType, parentAssetType, predicate, Company.CurrentResourceID, out nameFriendlyName, out isNamePartOfKey);
                if (insertStatus.Item1 != HttpStatusCode.OK)
                    return await Task.FromResult(errorMessageResponse(insertStatus.Item1, insertStatus.Item2, insertStatus.Item3));

                AssetRepository.UpsertObjectStyle(model.Object, model.ObjectID, model.IconStyle.ForeColor, model.IconStyle.BackColor, model.Name);

                if (model.ObjectID > 0)
                {
                    if (model.Class != AssetTypeClass.FusionAttribute && model.Class != AssetTypeClass.Reference)
                    {
                        Company.Add(new FieldType
                        {
                            ObjectID = model.ObjectID,
                            Object = model.Object,
                            IsListable = true,
                            IsRequired = true,
                            IsEditable = true,
                            FriendlyName = nameFriendlyName,
                            Name = "Name",
                            MaximumLength = 500,
                            MinimumLength = 1,
                            SortOrder = 1,
                            Type = DataType.Text.ToString(),
                            IsDisplayable = true,
                            IsPartOfKey = isNamePartOfKey
                        });
                    }
                }

                assetType = AssetRepository.GetAssetTypeByModel(model);

                if (assetType == null) return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Type", AssetTypeErrors.NotFoundGeneric));

                var result = new AssetTypeSuccess { Uid = assetType.uid, Message = "Asset Type is created", Success = true };

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result)));
            }
            catch (BaseException ex)
            {
                return await Task.FromResult(errorMessageResponse(ex.StatusCode, ex.StatusMessage, ex.StatusDescription));
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Error", errorMessage));
            }
        }

        /// <summary>
        /// Updates an asset type based on the specific asset type unique identifier.
        /// </summary>
        /// <remarks>
        /// This endpoint can update the following asset type class
        /// Business,Technical,Model,Organization,Policy,Reference,Rule
        /// </remarks>
        /// <param name="model"></param>
        /// <returns></returns>
        [
            HttpPut,
            Route(""),
            SwaggerRequestExample(typeof(AssetTypeInsert), typeof(Models.AssetTypeInsertExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Update asset type and success / failure message.", typeof(AssetTypeSuccess)),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset Type not found based on Uid provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Assets already exist with assigned parents. You may not change the parent of this asset type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "You have not provided a proper predicate based on its asset type class.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Display Format contains invalid field references.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Conflict, "If attempting to alter certain properties of a child asset type and there is a conflict within your Govern environment. For example, changing the predicate between a parent a child asset type", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutAssetTypeAsync(AssetTypeInsert model)
        {
            var prefix = "Assets.PutAssetTypeAsync => ";
            
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage));

                var validator = new AssetTypeValidator(this.Company);

                AssetType assetType = AssetRepository.GetAssetTypeByUID(model.Uid);

                AssetType parentAssetType = null;
                if (model.ParentUid != null && model.ParentUid != Guid.Empty)
                    parentAssetType = AssetRepository.GetAssetTypeByUID((Guid)model.ParentUid);

                Predicate predicate = null;
                if (model.Hierarchy != null && model.Hierarchy.PredicateUid.HasValue && model.Hierarchy.PredicateUid != Guid.Empty)
                    predicate = AssetRepository.GetPredicateByUID((Guid)model.Hierarchy.PredicateUid);

                var validationStatus = validator.ValidateModel(false, model, parentAssetType, predicate, assetType);
                if (validationStatus.StatusCode != HttpStatusCode.OK)
                    return await Task.FromResult(errorMessageResponse(validationStatus.StatusCode, validationStatus.Error, validationStatus.Message));

                if (model.UseAsTransformation && (model.Class != AssetTypeClass.BusinessAsset && model.Class != AssetTypeClass.TechnicalAsset))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Use As Transformation", AssetTypeErrors.TransformationClassRestriction));

                if (model.CanOwnFusion.HasValue && model.CanOwnFusion.Value && model.Class != AssetTypeClass.BusinessAsset)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Can Own Fusion", "Can Own Fusion can be set only asset types of class Business"));

                if (AssetRepository.IsReachedTransformationLimit(model))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Reached Transformation limit", AssetTypeErrors.TransformationLimitExceeded));

                var updateStatus = AssetRepository.UpdateAssetType(model, assetType, parentAssetType, predicate);
                if (updateStatus.Item1 != HttpStatusCode.OK)
                    return await Task.FromResult(errorMessageResponse(updateStatus.Item1, updateStatus.Item2, updateStatus.Item3));

                AssetRepository.UpsertObjectStyle(model.Object, model.ObjectID, model.IconStyle.ForeColor, model.IconStyle.BackColor, model.Name);


                //update affected display values
                Company.CreateOrUpdateTypeDisplayValuesAsync(model.ObjectID, model.Object.ToString());

                var result = new AssetTypeSuccess { Uid = model.Uid, Message = $"{model.Name} successfully updated.", Success = true };

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result)));

            }
            catch (BaseException ex)
            {
                return await Task.FromResult(errorMessageResponse(ex.StatusCode, ex.StatusMessage, ex.StatusDescription));
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Error", errorMessage));
            }
        }

        /// <summary>
        /// Adds a given set of assets based on the specific asset type unique identifier. Use this endpoint if you want to process under 250 items and need immediate results.
        /// </summary>
        /// <remarks>
        /// When using the ExecutionItemUid, keep in mind:
        /// * ExecutionItemUid is optional.
        /// * If you do not wish to provide an ExecutionItemUid, remove the entire line, including the preceding comma (, "ExecutionItemUid": "00000000-0000-0000-0000-000000000000").
        /// * If you provide ExecutionItemUids, values must be a unique across the entire request body.
        /// * You do not have to provide ExecutionItemUid values for all entries in a request.
        /// * ExecutionItemUid values, if provided, are returned in the response to allow you to correlate success / failure per item.
        /// 
        /// Workflows - This endpoint will trigger any associated workflows for the add actions taken on assets as part of this API call.
        /// </remarks>
        /// <param name="assetTypeUid">The unique identifier of the asset type.</param>
        /// <param name="triggersWorkflow">Optional query string parameter that allows you to enable / disabled workflow events from being triggered as a result of actions taken from this API call.  Defaults to enabled meaning workflow events will be triggered if there are any.</param>
        /// <param name="lookupFieldsPassedByValue">Optional query string parameter that allows you to pass list values numeric value instead of plain text value.  The default value for this is false.</param>
        /// <param name="assets">The payload of your request.</param>        
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            Route("{assetTypeUid:Guid}"),
            SwaggerRequestExample(typeof(AssetInsert), typeof(AssetInsertsExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of bulk asset results, including any error messages.", typeof(List<DatabaseBulkAssetResult>)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add assets of this type.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostAssetsAsync(Guid assetTypeUid, List<AssetInsert> assets, bool triggersWorkflow = true, bool lookupFieldsPassedByValue = false)
        {
            var prefix = "Assets.PostBulkAssetsAsync => ";
            var errorMessage = "";

            try
            {
                AssetType assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);

                if (!Company.HasAssetTypePermission(assetType.Object, assetType.ObjectID, Permission.ModifyAsset))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, "You are not allowed to add assets of this type."));

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with Uid {assetTypeUid} could not be found."));

                if (assets == null)
                    assets = readRequestJsonContent<List<AssetInsert>>(Request).Result;

                if (assets == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                if (assets.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"You may only provide a maximum of {MAX_SYNCHRONOUS_API_ITEM_COUNT} assets in this request. Please call the BATCH API to submit more than {MAX_SYNCHRONOUS_API_ITEM_COUNT} items."));

                var execution = getApiExecution(assets.Count, new ApiExecutionFields_PostAssets { AssetTypeUid = assetTypeUid });

                bool fieldJsonPropertyLoadLimitToTopLevel = true;
                try
                {
                    fieldJsonPropertyLoadLimitToTopLevel = Community.GetCompanySettingByKey<bool>("FieldJsonPropertyLoadLimitToTopLevel");
                }
                catch (Exception ex)
                {
                }
                
                var results = AssetRepository.PostAssets(assets, assetType, execution, fieldJsonPropertyLoadLimitToTopLevel, triggersWorkflow, lookupFieldsPassedByValue);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }

            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "AssetTypeUid", assetTypeUid.ToString() },
                    { "AssetCount", $"{((assets != null) ? assets.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Updates a given set of assets based on the specific asset type unique identifier. Use this endpoint if you want to process under 250 items and need immediate results.
        /// </summary>
        /// <remarks>
        /// When using the ExecutionItemUid, keep in mind:
        /// * ExecutionItemUid is optional.
        /// * If you do not wish to provide an ExecutionItemUid, remove the entire line, including the preceding comma (, "ExecutionItemUid": "00000000-0000-0000-0000-000000000000").
        /// * If you provide ExecutionItemUids, values must be a unique across the entire request body.
        /// * You do not have to provide ExecutionItemUid values for all entries in a request.
        /// * ExecutionItemUid values, if provided, are returned in the response to allow you to correlate success / failure per item.
        /// 
        /// Workflows - This endpoint will trigger any associated workflows for the update actions taken on assets as part of this API call.
        /// </remarks>
        /// <param name="assetTypeUid">The unique identifier of the asset type.</param>
        /// <param name="triggersWorkflow">Optional query string parameter that allows you to enable / disabled workflow events from being triggered as a result of actions taken from this API call.  Defaults to enabled meaning workflow events will be triggered if there are any.</param>
        /// <param name="lookupFieldsPassedByValue">Optional query string parameter that allows you to pass list values numeric value instead of plain text value.  The default value for this is false.</param>
        /// <param name="assets">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPut,
            Route("{assetTypeUid:Guid}"),
            SwaggerRequestExample(typeof(AssetUpdate), typeof(AssetUpdatesExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of bulk asset results, including any error messages.", typeof(List<DatabaseBulkAssetResult>)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add assets of this type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutAssetsAsync(Guid assetTypeUid, List<AssetUpdate> assets, bool triggersWorkflow = true, bool lookupFieldsPassedByValue = false)
        {
            var prefix = "Assets.PutAssetsAsync => ";
            var errorMessage = "";
            
            try
            {
                AssetType assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with Uid {assetTypeUid} could not be found."));

                if (assets == null)
                    assets = readRequestJsonContent<List<AssetUpdate>>(Request).Result;

                if (assets == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                if (assets.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"You may only provide a maximum of {MAX_SYNCHRONOUS_API_ITEM_COUNT} assets in this request. Please call the BATCH API to submit more than {MAX_SYNCHRONOUS_API_ITEM_COUNT} items."));

                var execution = getApiExecution(assets.Count, new ApiExecutionFields_PutAssets { AssetTypeUid = assetTypeUid });

                bool fieldJsonPropertyLoadLimitToTopLevel = true;
                try
                {
                    fieldJsonPropertyLoadLimitToTopLevel = bool.Parse(Community.GetCompanySettings().Single(i => i.Key == "FieldJsonPropertyLoadLimitToTopLevel").Value);
                }
                catch (Exception ex)
                {
                }

                var results = AssetRepository.PutAssets(assets, assetType, execution, fieldJsonPropertyLoadLimitToTopLevel, triggersWorkflow, lookupFieldsPassedByValue);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "AssetTypeUid", assetTypeUid.ToString() },
                    { "AssetCount", $"{((assets != null) ? assets.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Removes a given set of assets based on the specific asset type unique identifier. Use this endpoint if you want to process under 250 items and need immediate results.
        /// </summary>
        /// <remarks>
        /// When using the ExecutionItemUid, keep in mind:
        /// * ExecutionItemUid is optional.
        /// * If you do not wish to provide an ExecutionItemUid, remove the entire line, including the preceding comma (, "ExecutionItemUid": "00000000-0000-0000-0000-000000000000").
        /// * If you provide ExecutionItemUids, values must be a unique across the entire request body.
        /// * You do not have to provide ExecutionItemUid values for all entries in a request.
        /// * ExecutionItemUid values, if provided, are returned in the response to allow you to correlate success / failure per item.
        /// 
        /// Workflows - This endpoint will trigger any associated workflows for the delete actions taken on assets as part of this API call.
        /// </remarks>
        /// <param name="assetTypeUid">The unique identifier of the asset type.</param>
        /// <param name="triggersWorkflow">Optional query string parameter that allows you to enable / disabled workflow events from being triggered as a result of actions taken from this API call.  Defaults to enabled meaning workflow events will be triggered if there are any.</param>
        /// <param name="assets">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
            Route("{assetTypeUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of bulk asset results, including any error messages.", typeof(List<DatabaseBulkAssetResult>)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add assets of this type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteAssetsAsync(Guid assetTypeUid, AssetDeletes assets, bool triggersWorkflow = true)
        {
            var prefix = "Assets.DeleteAssetsAsync => ";
            var errorMessage = "";            

            try
            {
                AssetType assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with Uid {assetTypeUid} could not be found."));

                if (assets == null)
                    assets = readRequestJsonContent<AssetDeletes>(Request).Result;

                if (assets == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                if (assets.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"You may only provide a maximum of {MAX_SYNCHRONOUS_API_ITEM_COUNT} assets in this request. Please call the BATCH API to submit more than {MAX_SYNCHRONOUS_API_ITEM_COUNT} items."));

                var execution = getApiExecution(assets.Count, new ApiExecutionFields_DeleteAssets { AssetTypeUid = assetTypeUid });
                List<DatabaseBulkAssetResult> results = AssetRepository.DeleteAsset(assets, assetType, execution, triggersWorkflow);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "AssetTypeUid", assetTypeUid.ToString() },
                    { "AssetCount", $"{((assets != null) ? assets.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }


        /// <summary>
        /// Gets the score and the status of a Asset by its Uid
        /// </summary>
        /// <param name="assetUid">The asset Uid</param>
        /// <returns></returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route("GetScoreAndStatus/{assetUid}"), 
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(Object)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public dynamic GetScoreAndStatus(Guid assetUid)
        {
            return Company.GetAssetStatusAndScore(assetUid);
        }


        #region Batch

        /// <summary>
        /// Adds a given set of assets based on the specific asset type unique identifier. This endpoint is meant for a greater number of items as it stores the asset list for asynchronous or batch processing.
        /// </summary>
        /// <remarks>
        /// When using the ExecutionItemUid, keep in mind:
        /// * ExecutionItemUid is optional.
        /// * If you do not wish to provide an ExecutionItemUid, remove the entire line, including the preceding comma (, "ExecutionItemUid": "00000000-0000-0000-0000-000000000000").
        /// * If you provide ExecutionItemUids, values must be a unique across the entire request body.
        /// * You do not have to provide ExecutionItemUid values for all entries in a request.
        /// * ExecutionItemUid values, if provided, are returned in the response to allow you to correlate success / failure per item.
        /// 
        /// Workflows - This endpoint will trigger any associated workflows for the add actions taken on assets as part of this API call.
        /// </remarks>
        /// <param name="assetTypeUid">The unique identifier of the asset type.</param>
        /// <param name="triggersWorkflow">Optional query string parameter that allows you to enable / disabled workflow events from being triggered as a result of actions taken from this API call.  Defaults to enabled meaning workflow events will be triggered if there are any.</param>
        /// <param name="assets">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            Route("batch/{assetTypeUid:Guid}"),
            SwaggerRequestExample(typeof(AssetInsert), typeof(AssetInsertsExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution ID to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add assets of this type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostBulkAssetsAsync(Guid assetTypeUid, List<AssetInsert> assets, bool triggersWorkflow = true)
        {
            var prefix = "Assets.PostBulkAssetsAsync => ";
            var errorMessage = "";

            try
            {
                AssetType assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with Uid {assetTypeUid} could not be found."));

                if (assets == null)
                    assets = readRequestJsonContent<List<AssetInsert>>(Request).Result;

                if (assets == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                var execution = getApiExecution(assets.Count, new ApiExecutionFields_PostAssets { AssetTypeUid = assetTypeUid });

                ApiExecutionInfo executionInfo = await AssetRepository.PostBulkAssets(assets, execution, triggersWorkflow);

                var result = Request.CreateResponse(
                            HttpStatusCode.OK,
                            new ApiExecutionRecievedResponse
                            {
                                ExecutionID = executionInfo.ExecutionID,
                                Message = "Now processing request. Please check back with this ExecutionID for status.",
                                Uri = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Host}/api/v2/assets/executions/{executionInfo.ExecutionID}/status"
                            });

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(result));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "AssetTypeUid", assetTypeUid.ToString() },
                    { "AssetCount", $"{((assets != null) ? assets.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Updates a given set of assets based on the specific asset type Uid. This endpoint is meant for a greater number of items as it stores the asset list for asynchronous or batch processing.
        /// </summary>
        /// <remarks>
        /// When using the ExecutionItemUid, keep in mind:
        /// * ExecutionItemUid is optional.
        /// * If you do not wish to provide an ExecutionItemUid, remove the entire line, including the preceding comma (, "ExecutionItemUid": "00000000-0000-0000-0000-000000000000").
        /// * If you provide ExecutionItemUids, values must be a unique across the entire request body.
        /// * You do not have to provide ExecutionItemUid values for all entries in a request.
        /// * ExecutionItemUid values, if provided, are returned in the response to allow you to correlate success / failure per item.
        /// 
        /// Workflows - This endpoint will trigger any associated workflows for the update actions taken on assets as part of this API call.
        /// </remarks>
        /// <param name="assetTypeUid">The unique identifier of the asset type.</param>
        /// <param name="triggersWorkflow">Optional query string parameter that allows you to enable / disabled workflow events from being triggered as a result of actions taken from this API call.  Defaults to enabled meaning workflow events will be triggered if there are any.</param>
        /// <param name="assets">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPut,
            Route("batch/{assetTypeUid:Guid}"),
            SwaggerRequestExample(typeof(AssetUpdate), typeof(AssetUpdatesExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution ID to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add assets of this type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutBulkAssetsAsync(Guid assetTypeUid, List<AssetUpdate> assets, bool triggersWorkflow = true)
        {
            var prefix = "Assets.PutBulkAssetsAsync => ";
            var errorMessage = "";

            try
            {
                AssetType assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with Uid {assetTypeUid} could not be found."));

                if (assets == null)
                    assets = readRequestJsonContent<List<AssetUpdate>>(Request).Result;

                if (assets == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                var execution = getApiExecution(assets.Count, new ApiExecutionFields_PutAssets { AssetTypeUid = assetTypeUid });
                var executionInfo = await AssetRepository.PutBulkAssets(assetTypeUid, assets, execution, triggersWorkflow);

                return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            new ApiExecutionRecievedResponse
                            {
                                ExecutionID = executionInfo.ExecutionID,
                                Message = "Now processing request. Please check back with this ExecutionID for status.",
                                Uri = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Host}/api/v2/assets/executions/{executionInfo.ExecutionID}/status"
                            }
                        )
                    )
                );
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "AssetTypeUid", assetTypeUid.ToString() },
                    { "AssetCount", $"{((assets != null) ? assets.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Removes a given set of assets based on the specific asset type Uid. This endpoint is meant for a greater number of items as it stores the asset list for asynchronous or batch processing.
        /// </summary>
        /// <remarks>
        /// When using the ExecutionItemUid, keep in mind:
        /// * ExecutionItemUid is optional.
        /// * If you do not wish to provide an ExecutionItemUid, remove the entire line, including the preceding comma (, "ExecutionItemUid": "00000000-0000-0000-0000-000000000000").
        /// * If you provide ExecutionItemUids, values must be a unique across the entire request body.
        /// * You do not have to provide ExecutionItemUid values for all entries in a request.
        /// * ExecutionItemUid values, if provided, are returned in the response to allow you to correlate success / failure per item.
        /// 
        /// Workflows - This endpoint will trigger any associated workflows for the delete actions taken on assets as part of this API call.
        /// </remarks>
        /// <param name="assetTypeUid">The unique identifier of the asset type.</param>
        /// <param name="triggersWorkflow">Optional query string parameter that allows you to enable / disabled workflow events from being triggered as a result of actions taken from this API call.  Defaults to enabled meaning workflow events will be triggered if there are any.</param>
        /// <param name="assets">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
            Route("batch/{assetTypeUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution's unique identifier to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add assets of this type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteBulkAssetsAsync(Guid assetTypeUid, AssetDeletes assets, bool triggersWorkflow = true)
        {
            var prefix = "Assets.DeleteBulkAssetsAsync => ";
            var errorMessage = "";

            try
            {
                AssetType assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with Uid {assetTypeUid} could not be found."));

                if (assets == null)
                    assets = readRequestJsonContent<AssetDeletes>(Request).Result;

                if (assets == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                var execution = getApiExecution(assets.Count, new ApiExecutionFields_DeleteAssets { AssetTypeUid = assetTypeUid });

                var executionInfo = await AssetRepository.BulkDeleteAssets(assetTypeUid, assets, execution, triggersWorkflow);

                return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            new ApiExecutionRecievedResponse
                            {
                                ExecutionID = executionInfo.ExecutionID,
                                Message = "Now processing request. Please check back with this ExecutionID for status.",
                                Uri = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Host}/api/v2/assets/executions/{executionInfo.ExecutionID}/status"
                            }
                        )
                    )
                );
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "AssetTypeUid", assetTypeUid.ToString() },
                    { "AssetCount", $"{((assets != null) ? assets.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Removes a given set of asset types. This endpoint is meant for a greater number of items as it stores the asset list for asynchronous or batch processing.
        /// </summary>
        /// <remarks>
        /// When using the ExecutionItemUid, keep in mind:
        /// * ExecutionItemUid is optional.
        /// * If you do not wish to provide an ExecutionItemUid, remove the entire line, including the preceding comma (, "ExecutionItemUid": "00000000-0000-0000-0000-000000000000").
        /// * If you provide ExecutionItemUids, values must be a unique across the entire request body.
        /// * You do not have to provide ExecutionItemUid values for all entries in a request.
        /// * ExecutionItemUid values, if provided, are returned in the response to allow you to correlate success / failure per item.
        /// </remarks>
        /// <param name="assetTypes">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution's unique identifier to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to remove asset types.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteBulkAssetTypesAsync(AssetTypeDeletes assetTypes)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, "You are not allowed to remove asset types."));

            var prefix = "Assets.DeleteBulkAssetTypesAsync => ";
            var errorMessage = "";

            try
            {
                if (assetTypes == null)
                    assetTypes = readRequestJsonContent<AssetTypeDeletes>(Request).Result;

                if (assetTypes == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));
                var execution = getApiExecution(assetTypes.Count, new ApiExecutionFields_DeleteAssetTypes { });

                ApiExecutionInfo executionInfo = await AssetRepository.DeleteBulkAssetTypes(assetTypes, execution);

                return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            new ApiExecutionRecievedResponse
                            {
                                ExecutionID = executionInfo.ExecutionID,
                                Message = "Now processing request. Please check back with this ExecutionID for status.",
                                Uri = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Host}/api/v2/assets/executions/{executionInfo.ExecutionID}/status"
                            }
                        )
                    )
                );
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "AssetTypeCount", $"{((assetTypes != null) ? assetTypes.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// GETs the status of an execution record, including the results for the execution.
        /// </summary>
        /// <param name="executionUid">The execution's unique identifier to retrieve status for.</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("executions/{executionUid:Guid}/status"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "An execution status including a list of assets.", typeof(ApiExecutionStatusModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your status was not found.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetExecutionStatus(Guid executionUid)
        {
            var prefix = "Assets.GetExecutionStatus => ";
            var errorMessage = "";

            try
            {
                ApiExecution dbExecutionItem = AssetRepository.GetExecutionItemByUid(executionUid);

                if (dbExecutionItem == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", "Execution unique identifier not found."));
                }

                var info = new ApiExecutionInfo { CompanyID = Company.CurrentCompanyID, ExecutionID = executionUid };

                List<DatabaseBulkAssetResult> results = null;
                try
                {
                    var resultsJson = Storage.GetFileContentsAsString(info.StorageFolder, info.ResponseFileName);
                    results = JsonConvert.DeserializeObject<List<DatabaseBulkAssetResult>>(resultsJson);
                }
                catch
                {
                }

                var statusModel = new ApiExecutionStatusModel
                {
                    CompletedOn = dbExecutionItem.CompletedOn,
                    Error = dbExecutionItem.Error,
                    Fields = Newtonsoft.Json.Linq.JObject.Parse(dbExecutionItem.Fields),
                    Processed = dbExecutionItem.Processed,
                    StartedOn = dbExecutionItem.StartedOn,
                    Total = dbExecutionItem.Total,
                    Results = results
                };

                return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            statusModel
                        )
                    )
                );
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "ExecutionUid", executionUid.ToString() }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        #endregion

        #region AssetTag
        /// <summary>
        /// Creates association between an existing asset and an existing tag.
        /// </summary>
        /// <remarks>
        /// An Administrator can create any tag association. A non-administrative user can only create tag associations for assets to which they have read access.
        /// </remarks>
        /// <param name="assetTags">Collection of assets and tags to associate.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            Route("tags"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Creates association between an existing Asset and an existing tag, returns the UID of asset/tag association.", typeof(List<AssetTagSuccessApiModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))             
        ]
        public IHttpActionResult PostAssetTag(List<AssetTagApiModel> assetTags)
        {
            List<AssetTagSuccessApiModel> resultList = new List<AssetTagSuccessApiModel>();
            Tag currentTag;
            foreach (var assetTagApi in assetTags)
            {
                AssetTagSuccessApiModel result;
                if(assetTagApi.TagUID == Guid.Empty)
                {
                    currentTag = tagRepository.GetTagByName(assetTagApi.TagName);
                }
                else
                {
                    currentTag = tagRepository.GetTagByUid(assetTagApi.TagUID);
                }
                if (currentTag == null)
                {
                    result = new AssetTagSuccessApiModel()
                    {
                        Message = $"Invalid TagUid {assetTagApi.TagUID} ,no tag exists with the specified uid.",
                        Success = false
                    };

                    resultList.Add(result);
                    continue;
                }

                var asset = this.AssetRepository.GetAssetByUID(assetTagApi.AssetUID);
                if (asset == null)
                {
                    result = new AssetTagSuccessApiModel()
                    {
                        Message = $"Invalid uid {assetTagApi.AssetUID} no asset exists with the specified uid.",
                        Success = false
                    };
                    resultList.Add(result);
                    continue;
                }

                if (this.tagRepository.DoesAssetTagExists(currentTag.ID, asset.ID))
                {
                    result = new AssetTagSuccessApiModel()
                    {
                        Message = $"TagUID {assetTagApi.TagUID} and AssetUID {assetTagApi.AssetUID} association  already exists , it is not valid to add a second association",
                        Success = false
                    };
                    resultList.Add(result);
                    continue;
                }
                if (!Company.HasAssetDefaultReadPermission(asset.Object, asset.ObjectID))
                {
                    result = new AssetTagSuccessApiModel()
                    {
                        Message = $"A non-admin user can only create a tag association to assets they have access to",
                        Success = false
                    };
                    resultList.Add(result);
                    continue;
                }
                AssetTag assetTag = this.tagRepository.CreateAssetTag(currentTag.ID, asset.ID);
                if (assetTag != null)
                {
                    var tag = this.tagRepository.GetTagById(assetTag.TagID);
                    result = new AssetTagSuccessApiModel()
                    {
                        Message = $"Asset / Tag Association  created",
                        Uid = tag.uid,
                        Success = true
                    };
                    resultList.Add(result);
                }
                else
                {
                    result = new AssetTagSuccessApiModel()
                    {
                        Message = $"TagUID {assetTagApi.TagUID} and AssetUID {assetTagApi.AssetUID} association  already exists , it is not valid to add a second association",
                        Success = false
                    };
                    resultList.Add(result);
                }

            }
            return ResponseMessage(Request.CreateResponse<List<AssetTagSuccessApiModel>>(HttpStatusCode.OK, resultList));
        }

        /// <summary>
        /// Removes the association between an existing asset and an existing tag.
        /// </summary>
        /// <remarks>An Administrator can remove any tag association. A non-administrative user can only remove tag associations for assets to which they have read access.
        /// </remarks>
        /// <param name="assetTags">Collection of assets and tags to remove tag associations for.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
            Route("tags"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Removes the association between an existing asset and an existing tag, returns the Uid of removed asset/tag association.", typeof(List<AssetTagSuccessApiModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))             
        ]
        public IHttpActionResult DeleteAssetTag(List<AssetTagApiModel> assetTags)
        {
            List<AssetTagSuccessApiModel> resultList = new List<AssetTagSuccessApiModel>();
            foreach (var assetTagApi in assetTags)
            {
                AssetTagSuccessApiModel result;

                var currentTag = tagRepository.GetTagByUid(assetTagApi.TagUID);
                if (currentTag == null)
                {
                    result = new AssetTagSuccessApiModel()
                    {
                        Message = $"Invalid TagUid {assetTagApi.TagUID} ,no tag exists with the specified uid.",
                        Success = false
                    };
                    resultList.Add(result);
                    continue;
                }

                var asset = this.AssetRepository.GetAssetByUID(assetTagApi.AssetUID);
                if (asset == null)
                {
                    result = new AssetTagSuccessApiModel()
                    {
                        Message = $"Invalid uid {assetTagApi.AssetUID} no asset exists with the specified uid.",
                        Success = false
                    };
                    resultList.Add(result);
                    continue;
                }

                if (!this.tagRepository.DoesAssetTagExists(currentTag.ID, asset.ID))
                {
                    result = new AssetTagSuccessApiModel
                    {
                        Message = $"TagUID {assetTagApi.TagUID} and AssetUID {assetTagApi.AssetUID} association  does not exists",
                        Success = false
                    };
                    resultList.Add(result);
                    continue;
                }

                if (!this.tagRepository.IsAuthorizedToDeleteAssetTag(currentTag.ID, asset.ID))
                {
                    result = new AssetTagSuccessApiModel()
                    {
                        Message = $"A non-admin user can only remove the tag(Uid:  {assetTagApi.TagUID}) association to an asset (Uid: {assetTagApi.AssetUID}) if they initially created the association for or they have edit rights to asset",
                        Success = false
                    };
                    resultList.Add(result);
                    continue;
                }
                AssetTag assetTag = this.tagRepository.GetAssetTag(currentTag.ID, asset.ID);
                if (assetTag != null && this.tagRepository.DeleteAssetTag(currentTag.ID, asset.ID))
                {
                    result = new AssetTagSuccessApiModel()
                    {
                        Message = $"Asset / Tag Association  Deleted",
                        Uid = assetTag.UID.Value,
                        Success = true
                    };
                    resultList.Add(result);
                }
                else
                {
                    result = new AssetTagSuccessApiModel()
                    {
                        Message = $"TagUID {assetTagApi.TagUID} and AssetUID {assetTagApi.AssetUID} association  does not exists",
                        Success = false
                    };
                    resultList.Add(result);
                }

            }
            return ResponseMessage(Request.CreateResponse<List<AssetTagSuccessApiModel>>(HttpStatusCode.OK, resultList));

        }
        #endregion
    }
}