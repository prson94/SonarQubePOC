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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using d360.model.DataAccessLayer;
using d360.core.validators;
using System.Web.Http.Description;
using d360.core.resources;
using Resources;
using System.IO;
using d360.model.helpers.filters;
using System.Data.Entity;
using SpreadsheetLight;
using d360.model.helpers;
using System.Data;
using System.Threading;
using d360.core.Models;
using d360.model.helpers;
using System.Web;

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
        IRelationshipRepository relationshipRepository;
        IFieldsRepository fieldsRepository;

        public AssetsController(ICommunityContext community, ICompanyContext company, IStorageProvider storage, IQueueSource queueSource, IAssetRepository repository, ITagRepository tagRepository,
            IRelationshipRepository relationshipRepository, IFieldsRepository fieldsRepository, ISettingsRepository settingsRepository)
            : base(community, company, settingsRepository)
        {
            QueueSource = queueSource;
            Storage = storage;
            this.AssetRepository = repository;
            this.tagRepository = tagRepository;
            this.relationshipRepository = relationshipRepository;
            this.fieldsRepository = fieldsRepository;
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
            SwaggerResponse(HttpStatusCode.OK, "A list of asset type classes. The Generic and ReferenceItemType class types are used internally, and are not intended for use in general data requests.", typeof(List<AssetTypeClassInfo>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
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
                    { ApiMessages.EndpointMethod, prefix }
                });

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// GET a list of asset types.
        /// </summary>
        /// <param name="Class">Allows for filtering the Asset type's by Class.The Generic and ReferenceItemType class types are used internally, and are not intended for use in general data requests.</param>
        /// <param name="assetTypeUid">Filter by Asset type UID.</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("types"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json"),
            SwaggerParameter("UseAsTransformation", "Filter results by Use As Transformation setting. This filter is used to show only Business and Technical asset types which have been marked as transformational asset types in their configuration. Transformational assets have special meaning in the asset browser. Please see the Govern user guide for further details about transformational assets.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("Hierarchical", "Filter results by Hierarchical setting. This value is used to show Model and Policy Types.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("AutoDisplayDescription", "Filter results by Auto Display Description setting. This value is used by the Govern UI to have the Description shown on the asset list page by default or not.", DataType = "boolean", ParameterType = "query", Required = false),            
            SwaggerParameter("AutoDisplayParent", "Filter results by AutoDisplayParent setting. The value is used by the Govern UI to display or hide the parent column on the data grids.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset Type not found based on Uid provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset types.", typeof(List<AssetTypeApiViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetAssetTypesAsync(AssetTypeClass? Class = null, Guid? assetTypeUid = null)
        {
            var prefix = "Assets.GetAssetTypesAsync => ";
            var errorMessage = "";

            try
            {
                if (assetTypeUid != null && assetTypeUid.HasValue && assetTypeUid.Value != Guid.Empty)
                {
                    var assetType = this.AssetRepository.GetAssetTypeByUID(assetTypeUid.Value);
                    if (assetType == null)
                        if (assetType == null) return ReturnApiError(HttpStatusCode.BadRequest, AssetTypeErrors.NotFoundGeneric);
                }
                var queryParams = Request.GetQueryNameValuePairs();

                var assetTypes = await AssetRepository.GetAssetType(queryParams, Class, assetTypeUid);

                return Request.CreateResponse(HttpStatusCode.OK, assetTypes);
            }
            catch (ArgumentException ex)
            {
                return ReturnApiError(HttpStatusCode.BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                if (ex is FormatException)
                {
                    errorMessage = errorMessage.Replace("Guid", ApiMessages.UidConstant);
                }

                SendException(ex, new Dictionary<string, string>() {
                    {ApiMessages.EndpointMethod, prefix }
                });

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// Retrieves assets for the given asset type unique identifier.
        /// </summary>
        /// <remarks>
        /// In addition to the below query parameters a field name for the asset type can be specified to filter by exact match. For example MyCustomField=someExactValue.
        /// *  If you use the object asset type Uid as the assetTypeUid value, only use of the subjectUid filter is supported.
        /// *  If you use the subject asset type Uid as the assetTypeUid value, only use of the objectUid filter is supported.
        /// *  If you use either the subjectUid or objectUid filter, the predicateUid must be included in the request. 
        /// *  If you do not include the predicateUid, any values given in the subjectUid or objectUid field are ignored.
        /// 
        /// Advanced filtering is done using _filter parameter and filter expressions are specified using field name, operator and value. For example city eq 'Redmond'.
        /// *  For comparison operators you can use eq (equal), ne (not equal), gt (greater than), ge (greater than or equal), lt (less than), le (less than or equal) and ct (contains) which allows usage of (*) symbol as wildcard
        /// *  Chaining of filter expressions is done using 'and' or 'or' logical operator. IE. city eq 'Redmond' OR city ct 'Lo'.
        /// 
        /// Relationship filtering is done using _relationFilter parameter and filter expressions are specified using relationship type UID, operator and Asset UID. IE. {Relationship Type UID} eq {Asset UID}.
        /// *  For comparison operators you can use eq (equal), ne (not equal)
        /// *  Chaining of relationship filter expressions is done using 'and' or 'or' logical operator.
        /// 
        /// If the requested content media type is "application/octet-stream", the response will be an Excel document with the asset results and the assetTypeUid as the file name.
        /// </remarks>
        /// <param name="assetTypeUid">The unique identifier of the asset type.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("{assetTypeUid:Guid}"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(AssetsApiViewModel)),
            SwaggerProduces("application/json", "text/json", "application/xml", "text/xml", "application/octet-stream"),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error to indicate that your request to retrieve this asset is forbidden due to lack of permissions to view it.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by AssetId.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_predicateUid", "The Uid of a predicate type to return relationships for. If specified the results will include relationships of this predicate type. Assets without this type of relationship defined will be omitted.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_subjectUid", "The Uid of the subject side of a relationship to filter by in addition to filtering by predicate type. _predicateUid is required.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_objectUid", "The Uid of the object side of a relationship to filter by in addition to filtering by predicate type. _predicateUid is required.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_assetUid", "Filter by provided asset Uid. Multiple asset Uids can be provided delimited by comma", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_parentUid", "Filter by provided parent asset Uid.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_simpleFilter", "The text or phrase you want to find within the listable fields of an asset. Filtering is done using 'Starts with' logic. Asterisk (*) symbol can be used as a wild card character to match any character.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_ownedBy", "The parameter takes a comma separated list of user or group uids. Only assets which are owned by any one or more of the provided owners are returned.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_notOwnedBy", "The parameter takes a comma separated list of user or group uids. Only assets which are not owned by any one or more of the provided owners are returned.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_filter", ADVANCED_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_relationFilter", "The filter expression used to filter assets by relation to other asset.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("useTypeLevelDefaultSorts", "If the value is False and the _order parameter is not specified the results will be ordered by Asset ID by default. If True, results are sorted by sort field defined in Asset Type field definition.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_loadPermissionDetails", "If the value is set to True, the results will include permission details for each asset. The default value is False.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_includeParent", "If the value is True, the results will include parent UID and parent display name for each asset. The default value is False.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_onlyListableFields", "If the value is True, the results will include only listable fields. If False, all fields will be returned. The default value is False.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Allows you to disable including the count of the total number of results across pages in the response.  The default is true meaning the total count is included.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_includeFields", "A comma delimited list of fields to include in the results. By default all fields are included.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_includeColor", "Allows you to disable returning the Color value for assets. The default value is true.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_exporttemplateuid", "The Uid of the template which will be used when exporting results.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_includeCreatedModifiedBy", "Include the CreatedByUid, and ModifiedByUid fields in the response. The default value is false meaning these values are not returned.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_includeOwnershipLookup", "Include the OwnershipLookup fields in the response. The default value is false meaning these values are not returned.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_includeProfilingCheck", "Include a check for whether or not the asset has Data Profiling.", DataType = "boolean", ParameterType = "query", Required = false),
        ]
        public async Task<IHttpActionResult> GetAssetsAsync(Guid assetTypeUid, CancellationToken cancellationToken)
        {
            var prefix = "Assets.GetAssetsAsync => ";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();

                string isValid = isPageSizeAndNumValid(queryParams);

                if (!string.IsNullOrEmpty(isValid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , isValid));
                }

                var isStreamResponse = Request?.Headers?.Accept?.Any(a => a.MediaType == "application/octet-stream") ?? false;

                var validator = new AssetTypeValidator(this.Company);
                var assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.NotFoundBasedOnUid));

                if (assetType.Class == AssetTypeClass.Group || assetType.Class == AssetTypeClass.User)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, string.Format(AssetsApiMessages.CorrectEndpoint, assetType.Class.ToString(), Request.RequestUri.Scheme, Request.RequestUri.Host, (assetType.Class == AssetTypeClass.Group ? AssetTypeErrors.GroupEndPoint : AssetTypeErrors.UserEndPoint))));
                }

                //if the user is not an admin make sure they can read this asset type if not tell them they are forbidden
                if (!Company.CurrentResourceIsAdmin && !Company.HasAssetTypePermission(assetType.Object, assetType.ID, Permission.ReadAsset))//(await Company.HasAssetTypeReadPermission(assetType.ID)))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.InvalidRequest , AssetsApiMessages.RestrictReadAssettype));
                }

                if (!validator.IsValidOrderByFieldForGetAssets(assetTypeUid, queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , ApiMessages.InvalidOrderRequese));

                if (!validator.IsValidOrderDirectionGetAssets(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , ApiMessages.InvalidDirection));

                if (!validator.IsValidOwnersGetAssets(queryParams, "_ownedby"))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , AssetsApiMessages.InvalidUserGroupRequestAsOwner));

                if (!validator.IsValidOwnersGetAssets(queryParams, "_notownedby"))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , AssetsApiMessages.InvalidUserGroupRequestAsNonOwner));

                if (!validator.IsValidGetAssets(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , ActionApiMessages.InvalidAssetUid));


                if (!validator.IsValidRelationFilter(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , AssetsApiMessages.FilterResrictPredicateUid));

                if (!validator.IsValidIncludeTotalFlag(queryParams))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest ,ApiMessages.InvalidIncludeTotal));


                if (queryParams.Any(x => x.Key.ToLower() == "_exporttemplateuid") && !isStreamResponse)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , AssetsApiMessages.ExportTemplateMessage));
                }

                HttpResponseMessage response;

                if (isStreamResponse)
                {
                    if (queryParams.Any(x => x.Key.ToLower() == "_exporttemplateuid"))
                    {
                        Guid exportTemplateUID;
                        if (!Guid.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "_exporttemplateuid").Value, out exportTemplateUID))
                        {
                            return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , AssetsApiMessages.InvalidExportTemplatedUid));
                        }
                        var template = (await AssetRepository.GetExportTemplates(exportTemplateUID: exportTemplateUID)).FirstOrDefault();

                        if (template == null)
                        {
                            return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(AssetsApiMessages.ExportTemplateUidNotExist, exportTemplateUID.ToString())));
                        }

                        if (template.AssetTypeID != assetType.ID)
                        {
                            return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , AssetsApiMessages.InvalidExportTemplateCurrent));
                        }
                        List<FieldType> fieldsForCustomExport = new List<FieldType>();
                        var paramList = queryParams.ToList();

                        if (template.IncludeParent)
                        {
                            paramList.RemoveAll(x => x.Key.ToLower() == "_includeparent");
                            paramList.Add(new KeyValuePair<string, string>("_includeparent", "true"));
                            queryParams = paramList;
                        }
                        queryParams = queryParams.Where(x => x.Key.ToLower() != "_listcolorsasjson");
                        var results = await AssetRepository.GetAssets(assetType, queryParams, cancellationToken: cancellationToken);

                        SLDocument document = GetCustomExportSheet(assetType, template, fieldsForCustomExport, results);

                        // Select the first worksheet as the active one.
                        var firstSheet = document.GetWorksheetNames()[0];
                        document.SelectWorksheet(firstSheet);

                        var stream = new MemoryStream();
                        document.SaveAs(stream);

                        byte[] bytes = stream.ToArray();

                        response = createFileResponseMessage(HttpStatusCode.OK, $"{assetTypeUid}.xlsx", bytes);
                    }
                    else
                    {

                        bool isHierachyItem = false;
                        var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_ishierachyitem").Value;
                        bool.TryParse(value, out isHierachyItem);

                        bool isChildItem = false;
                        var valueChild = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_ischildtab").Value;
                        bool.TryParse(valueChild, out isChildItem);

                        var paramList = queryParams.Where(x => x.Key.ToLower() != "_listcolorsasjson").ToList();

                        paramList.RemoveAll(x => x.Key.ToLower() == "_includeownershiplookup");
                        paramList.Add(new KeyValuePair<string, string>("_includeownershiplookup", "true"));

                        queryParams = paramList;

                        SLDocument results;
                        if (isHierachyItem)
                        {
                            results = await AssetRepository.GetHierarchyExcel(assetTypeUid, queryParams, true);
                        }
                        else
                        {
                            results = await AssetRepository.GetAssetsExcel(assetTypeUid, queryParams, isChildItem);
                        }

                        var stream = new MemoryStream();
                        results.SaveAs(stream);
                        byte[] bytes = stream.ToArray();

                        response = createFileResponseMessage(HttpStatusCode.OK, $"{assetTypeUid}.xlsx", bytes);
                    }
                }
                else
                {
                    var results = await AssetRepository.GetAssets(assetType, queryParams, cancellationToken: cancellationToken);
                    response = Request.CreateResponse(HttpStatusCode.OK, results);
                }


                return await Task.FromResult<IHttpActionResult>(ResponseMessage(response));
            }
            catch (ArgumentException ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, errorMessage));
            }
            catch (FilterExpressionParserException ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.FilterExpressionParseError, errorMessage));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    {ApiMessages.EndpointMethod, prefix },
                    { AssetsApiMessages.AssetTypeUid, assetTypeUid.ToString() }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError,ApiMessages.UnknownError, errorMessage));
            }

        }

        /// <summary>
        /// Retrieves assets based on a search of its full path. You also have the option of pre-filtering the types of assets you wish 
        /// to target, based on Uid, Class, or specific properties defined on an asset type.
        /// </summary>
        /// <param name="model">
        /// An object containing:
        /// 1. searchPhrase: The text or phrase you want to find within the path of an asset. 
        /// 2. filters: An array or list of different filters you want to limit the search scope to. There are a complex set of filters you can use such as:
        ///     1. Uid: The asset type Uid to filter by.
        ///     2. Class: An enumeration value (BusinessAsset, TechnicalAsset, Model, Policy, etc.) indicating the class of asset you want to limit your search to.
        ///     3. UseAsTransformation: A true/false value indicating whether you want to limit your search only assets that can be used as a transformation or not.
        ///     4. AsSideOfRelationship: Limit your asset search only to assets that have the option of participating in a relationship based on whether it is:
        ///         1. Side: "Subject" or "Object" of a relationship.
        ///         2. PredicateType: Whether it can participate in a relationship whose predicate functional type is based on one of the available enumeration values.
        ///         3. PredicateUid: Whether it can participate in a relationship using a specific predicate, based on its Uid.
        /// </param>
        /// <returns>A list of search results based on the filter criteria provided, along with an HTTP status code and message.</returns>
        [
            HttpPost,
            Route("paths"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(AssetsByPathApiViewModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetAssetsByPathAsync(AssetsByPathApiRequestModel model)
        {
            var prefix = "Assets.GetAssetsByPathAsync => ";

            try
            {
                #region Validation

                if (model == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , ApiMessages.EmptyInvalidParameterSet));

                if (string.IsNullOrEmpty(model.searchPhrase))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , AssetsApiMessages.ProvideSearchPhrase));

                if (model.filters == null)
                {
                    model.filters = new List<AssetsByPathItemApiFilterRequestModel>();
                }

                if (model.filters.Count() > 0)
                {
                    if (!model.filters.Any(i =>
                        (i.AsSideOfRelationship != null) ||
                        i.Class.HasValue ||
                        i.Uid.HasValue ||
                        i.UseAsTransformation.HasValue))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , AssetsApiMessages.ProvidePreFilterCriteria));
                    }
                }

                #endregion

                var results = await AssetRepository.GetAssetsByPath(model);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { ApiMessages.EndpointMethod, prefix },
                    { AssetsApiMessages.model, JsonConvert.SerializeObject(model) }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage));
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
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

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage));
            }
        }


        /// <summary>
        /// Add an asset type based on Asset Type Class
        /// </summary>
        /// <remarks>
        /// This endpoint can add the following asset type classes:  
        /// - BusinessAsset
        /// - Model
        /// - Organization
        /// - Policy
        /// - Reference
        /// - Rule
        /// - TechnicalAsset  
        ///   
        /// You also have the option of providing a Uid for this new asset type. This is particularly useful in a migration scenario where you want to migrate an asset type from one environment to another. The default is to not provide one, in which case a Uid will be automatically generated.
        /// </remarks>
        /// <param name="model">The asset type model to add.</param>
        /// <returns>The Uid of the new asset type, a success status, and a message.</returns>
        [
            HttpPost,
            Route(""),
            SwaggerRequestExample(typeof(AssetTypeUpsert), typeof(AssetTypeInsertExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Newly asset type Uid and success / failure message.", typeof(AssetTypeSuccess)),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset Type not found based on Uid provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to create an asset type", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request is badly formatted or has failed validation.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostAssetTypeAsync(AssetTypeUpsert model)
        {
            var prefix = "Assets.PostAssetTypeAsync => ";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage));

                if (model.Class == AssetTypeClass.Glossary)
                {
                    model.Class = AssetTypeClass.BusinessAsset;
                }

                var governanceRoleReferenceListUid = SettingsRepository.GetSettingValue<Guid>(Setting.GovernanceRoleReferenceListUid);
                var EnableOrganizations = SettingsRepository.GetSettingValue<bool>(Setting.EnableOrganizations);


                var validator = new AssetTypeValidator(this.Company, governanceRoleReferenceListUid, EnableOrganizations);

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
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, AssetsApiMessages.UseAsTransformation, AssetTypeErrors.TransformationClassRestriction));

                if (model.AutoDisplayParent.HasValue && (!model.Class.AllowsAutoDisplayParent() || parentAssetType == null))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, AssetsApiMessages.AutoDisplayParent, AssetTypeErrors.AutoDisplayParentRestriction));

                if (model.CanEditParent.HasValue && ((model.Class != AssetTypeClass.BusinessAsset && model.Class != AssetTypeClass.TechnicalAsset) || parentAssetType == null))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, AssetsApiMessages.CanEditParent, AssetTypeErrors.CanEditParentClassRestriction));

                if (AssetRepository.IsReachedTransformationLimit(model))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, AssetsApiMessages.ReachedTransformationlimit, AssetTypeErrors.TransformationLimitExceeded));


                AssetType governanceRoleRefList = null;
                if (model.Class == AssetTypeClass.Diagram)
                {
                    governanceRoleRefList = Company.AssetTypes.FirstOrDefault(x => x.uid == governanceRoleReferenceListUid);
                    if (governanceRoleRefList == null)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, string.Format(AssetsApiMessages.GovernRoleReferListNotExists, governanceRoleReferenceListUid.ToString())));
                    }
                }


                AssetType assetType = null;
                var nameFriendlyName = "Name";
                var isNamePartOfKey = true;

                var insertStatus = AssetRepository.AddAssetType(model, assetType, parentAssetType, predicate, Company.CurrentResourceID, out nameFriendlyName, out isNamePartOfKey);
                if (insertStatus.Item1 != HttpStatusCode.OK)
                    return await Task.FromResult(errorMessageResponse(insertStatus.Item1, insertStatus.Item2, insertStatus.Item3));


                if (model.ObjectID > 0)
                {
                    if (model.Class != AssetTypeClass.Reference)
                    {
                        var nameFieldType = new FieldType
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
                            IsPartOfKey = isNamePartOfKey,
                            UpdatedBy = Company.CurrentResourceID
                        };

                        if (model.Class == AssetTypeClass.Diagram)
                        {
                            nameFieldType.ColumnOrder = 2;
                            nameFieldType.ShowIfEmpty = true;
                        }

                        Company.Add(nameFieldType);
                    }

                    if (model.Class == AssetTypeClass.Diagram)
                    {
                        Company.Add(new FieldType
                        {
                            ObjectID = model.ObjectID,
                            Object = model.Object,
                            IsListable = true,
                            IsRequired = true,
                            IsEditable = true,
                            FriendlyName = "Governance Role",
                            Name = "GovernanceRole",
                            ColumnOrder = 3,
                            Type = DataType.Lookup.ToString(),
                            IsDisplayable = true,
                            IsPartOfKey = false,
                            LookupObjectID = governanceRoleRefList.ObjectID,
                            LookupObjectType = SystemObjects.ReferenceItem.ToString(),
                            UpdatedBy = Company.CurrentResourceID,
                            ShowIfEmpty = true,
                            LookupDisplayFormat = "{Code}",
                            LookupEditFormat = "{Code}"
                        });

                        Company.Add(new FieldType
                        {
                            ObjectID = model.ObjectID,
                            Object = model.Object,
                            IsListable = true,
                            IsRequired = true,
                            IsEditable = true,
                            FriendlyName = "Step No",
                            Name = "StepNo",
                            ColumnOrder = 1,
                            Type = DataType.Decimal.ToString(),
                            IsDisplayable = true,
                            IsPartOfKey = false,
                            UpdatedBy = Company.CurrentResourceID,
                            ShowIfEmpty = true
                        });
                    }
                }

                assetType = AssetRepository.GetAssetTypeByModel(model);

                AssetRepository.UpsertAssetStyle(assetType.ID, model.IconStyle.ForeColor, model.IconStyle.BackColor, model.IconStyle.Icon, model.Name);

                if (assetType == null) return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidType, AssetTypeErrors.NotFoundGeneric));

                Company.CreateRollupPathChangedExecution(assetTypeId: assetType.ID);

                var result = new AssetTypeSuccess { Uid = assetType.uid, Message = AssetsApiMessages.AssetTypeCreatedMessage, Success = true };

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

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage));
            }
        }

        /// <summary>
        /// Get field types for the given asset type Uid
        /// </summary>
        /// <param name="artifactTypeID">The Uid of the asset type</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("artfactType/{artifactTypeID}"),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public AssetType GetArtifactTypeUidById(int artifactTypeID)
        {
            return this.AssetRepository.GetArtifactTypeByID(artifactTypeID);
        }

        /// <summary>
        /// Get Asset type object and object id for asset type Uid
        /// </summary>
        /// <param name="assetTypeUid">The Uid of the asset type</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("assetTypeLegacyData/{assetTypeUid}"),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<dynamic> GetArtifactTypeUidById(Guid assetTypeUid)
        {
            return await AssetRepository.GetAssetTypeObjectAndObjectId(assetTypeUid);
        }

        /// <summary>
        /// Updates an asset type based on the specific asset type unique identifier (Uid).
        /// </summary>
        /// <remarks>
        /// This endpoint can update the following asset type classes:  
        /// - BusinessAsset 
        /// - Model
        /// - Organization
        /// - Policy
        /// - Reference
        /// - Rule
        /// - TechnicalAsset 
        /// </remarks>
        /// <param name="model">The asset type model to update.</param>
        /// <returns></returns>
        [
            HttpPut,
            Route(""),
            SwaggerRequestExample(typeof(AssetTypeUpsert), typeof(AssetTypeUpdateExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Update asset type and success / failure message.", typeof(AssetTypeSuccess)),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset Type not found based on Uid provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Assets already exist with assigned parents. You may not change the parent of this asset type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "You have not provided a proper predicate based on its asset type class.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Display Format contains invalid field references.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request is badly formated or has failed validation.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Conflict, "If attempting to alter certain properties of a child asset type and there is a conflict within your Govern environment. For example, changing the predicate between a parent a child asset type", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutAssetTypeAsync(AssetTypeUpsert model)
        {
            var prefix = "Assets.PutAssetTypeAsync => ";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage));

                var govRoleUid = SettingsRepository.GetSettingValue<Guid>(Setting.GovernanceRoleReferenceListUid);
                var EnableOrganizations = SettingsRepository.GetSettingValue<bool>(Setting.EnableOrganizations);
                var validator = new AssetTypeValidator(this.Company, govRoleUid, EnableOrganizations);

                if (model.Class == AssetTypeClass.Glossary)
                {
                    model.Class = AssetTypeClass.BusinessAsset;
                }

                AssetType assetType = AssetRepository.GetAssetTypeByUidAndClass(model.Uid, model.Class);

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
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, AssetsApiMessages.UseAsTransformation, AssetTypeErrors.TransformationClassRestriction));

                if (AssetRepository.IsReachedTransformationLimit(model))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, AssetsApiMessages.ReachedTransformationlimit, AssetTypeErrors.TransformationLimitExceeded));

                if (model.AutoDisplayParent.HasValue && parentAssetType == null && (model.Class != AssetTypeClass.BusinessAsset && model.Class != AssetTypeClass.TechnicalAsset))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, AssetsApiMessages.AutoDisplayParent, AssetTypeErrors.AutoDisplayParentRestriction));

                bool isUseAsTransformationChanged = (!model.UseAsTransformation && assetType.UseAsTransformation) || (model.UseAsTransformation && !assetType.UseAsTransformation);
                if (isUseAsTransformationChanged && (assetType.Class == AssetTypeClass.BusinessAsset || assetType.Class == AssetTypeClass.TechnicalAsset))
                {
                    var IsTransformPredicateExists = await this.relationshipRepository.IsTransformPredicateExists(assetType.ID);
                    if (IsTransformPredicateExists)
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, AssetsApiMessages.TransormationReationExists, AssetTypeErrors.RelationshipExistsForAssetType));
                }

                var updateStatus = AssetRepository.UpdateAssetType(model, assetType, parentAssetType, predicate);
                if (updateStatus.Item1 != HttpStatusCode.OK)
                    return await Task.FromResult(errorMessageResponse(updateStatus.Item1, updateStatus.Item2, updateStatus.Item3));

                AssetRepository.UpsertAssetStyle(assetType.ID, model.IconStyle.ForeColor, model.IconStyle.BackColor, model.IconStyle.Icon, model.Name);

                //update affected display values
                Company.CreateOrUpdateTypeDisplayValuesAsync(model.ObjectID, model.Object.ToString());
                Company.CreateRollupPathChangedExecution(assetTypeId: assetType.ID);

                var result = new AssetTypeSuccess { Uid = model.Uid, Message = string.Format(ApiMessages.SucessfullyUpdated, model.Name), Success = true };

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

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage));
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
        /// <param name="useTempTablesForFieldValues">Optional query string parameter that allows you to specify false to preserve field values in a static table usually for troubleshooting.  The default value for this is true.</param>
        /// <param name="assets">The payload of your request.</param>        
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            Route("{assetTypeUid:Guid}"),
            SwaggerRequestExample(typeof(AssetInsert), typeof(AssetInsertsExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of bulk asset results, including any error messages.", typeof(List<DatabaseBulkAssetResult>)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add assets of this type.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostAssetsAsync(
            Guid assetTypeUid, 
            List<AssetInsert> assets, 
            bool triggersWorkflow = true, 
            bool lookupFieldsPassedByValue = false,
            bool useTempTablesForFieldValues = true,
            [SwaggerDescription(nameof(Swagger.Execution_ApplicationId))] string applicationId = null)
        {
            var prefix = "Assets.PostBulkAssetsAsync => ";

            try
            {
                if (applicationId != null && applicationId.Length > 200)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.ApplicationIdMaxLengthViolated);
                }

                AssetType assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(ActionApiMessages.AssetTypeNotFound, assetTypeUid.ToString())));

                if (!Company.HasAssetTypePermission(assetType.Object, assetType.ObjectID, Permission.AddAsset))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, AssetsApiMessages.AssetTypeAddAssetPermissionsDenied));
                }

                var EnableOrganizations = SettingsRepository.GetSettingValue<bool>(Setting.EnableOrganizations);

                if (assetType.Class == AssetTypeClass.Organization && !EnableOrganizations)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, $"{AssetTypeErrors.UnsupportedAssetClass}"));
                }


                if (assets == null)
                    assets = readRequestJsonContent<List<AssetInsert>>(Request).Result;

                if (assets == null || assets.Count == 0)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , ApiMessages.JSONValidMessage));

                if (assets.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , string.Format(AssetsApiMessages.RequestMaxAsset, MAX_SYNCHRONOUS_API_ITEM_COUNT, MAX_SYNCHRONOUS_API_ITEM_COUNT)));

                var execution = getApiExecution(assets.Count, new ApiExecutionFields_PostAssets { AssetTypeUid = assetTypeUid }, applicationId: applicationId);

                var results = AssetRepository.PostAssets(assets, assetType, execution, triggersWorkflow, lookupFieldsPassedByValue, useTempTablesForFieldValues);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }

            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { ApiMessages.EndpointMethod, prefix },
                    { AssetsApiMessages.AssetTypeUid, assetTypeUid.ToString() },
                    { AssetsApiMessages.AssetCount, $"{((assets != null) ? assets.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage));
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
        /// <param name="useTempTablesForFieldValues">Optional query string parameter that allows you to specify false to preserve field values in a static table usually for troubleshooting.  The default value for this is true.</param>
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutAssetsAsync(
            Guid assetTypeUid,
            List<AssetUpdate> assets,
            bool triggersWorkflow = true,
            bool lookupFieldsPassedByValue = false,
            bool useTempTablesForFieldValues = true,
            [SwaggerDescription(nameof(Swagger.Execution_ApplicationId))] string applicationId = null)
        {
            var prefix = "Assets.PutAssetsAsync => ";
            try
            {
                if (applicationId != null && applicationId.Length > 200)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.ApplicationIdMaxLengthViolated);
                }

                AssetType assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(ActionApiMessages.AssetTypeNotFound, assetTypeUid.ToString())));

                if (assets == null)
                    assets = readRequestJsonContent<List<AssetUpdate>>(Request).Result;

                if (assets == null || assets.Count == 0)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , ApiMessages.JSONValidMessage));

                if (assets.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , string.Format(AssetsApiMessages.RequestMaxAsset, MAX_SYNCHRONOUS_API_ITEM_COUNT, MAX_SYNCHRONOUS_API_ITEM_COUNT)));

                var execution = getApiExecution(assets.Count, new ApiExecutionFields_PutAssets { AssetTypeUid = assetTypeUid }, applicationId: applicationId);

                var results = AssetRepository.PutAssets(assets, assetType, execution, triggersWorkflow, lookupFieldsPassedByValue, useTempTablesForFieldValues);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { ApiMessages.EndpointMethod, prefix },
                    { AssetsApiMessages.AssetTypeUid, assetTypeUid.ToString() },
                    { AssetsApiMessages.AssetCount, $"{((assets != null) ? assets.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage));
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteAssetsAsync(
            Guid assetTypeUid, 
            AssetDeletes assets, 
            bool triggersWorkflow = true, 
            [SwaggerDescription(nameof(Swagger.Execution_ApplicationId))] string applicationId = null)
        {
            var prefix = "Assets.DeleteAssetsAsync => ";
            var errorMessage = "";

            try
            {
                if (applicationId != null && applicationId.Length > 200)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.ApplicationIdMaxLengthViolated);
                }

                AssetType assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(ActionApiMessages.AssetTypeNotFound, assetTypeUid.ToString())));

                if (assets == null)
                    assets = readRequestJsonContent<AssetDeletes>(Request).Result;

                if (assets == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , ApiMessages.JSONValidMessage));

                if (assets.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , string.Format(AssetsApiMessages.RequestMaxAsset, MAX_SYNCHRONOUS_API_ITEM_COUNT, MAX_SYNCHRONOUS_API_ITEM_COUNT)));

                var execution = getApiExecution(assets.Count, new ApiExecutionFields_DeleteAssets { AssetTypeUid = assetTypeUid }, applicationId: applicationId);
                List<DatabaseBulkAssetResult> results = AssetRepository.DeleteAsset(assets, assetType, execution, triggersWorkflow);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { ApiMessages.EndpointMethod, prefix },
                    { AssetsApiMessages.AssetTypeUid, assetTypeUid.ToString() },
                    { AssetsApiMessages.AssetCount, $"{((assets != null) ? assets.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage));
            }
        }

        /// <summary>
        /// Gets the score and the status of a Asset by its Uid
        /// </summary>
        /// <param name="assetUid">The asset Uid</param>
        /// <returns></returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route("GetUIDetails/{assetUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(Object)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public dynamic GetUIDetails(Guid assetUid)
        {
            //handle reference list items
            var assetType = Company.AssetTypes.FirstOrDefault(x => x.uid == assetUid);
            if (assetType != null)
            {
                return new
                {
                    assetType.Object,
                    assetType.ObjectID,
                    DisplayValue = assetType.Name,
                    AssetTypeUid = assetType.uid,
                    TypeName = assetType.Name
                };
            }
            return Company.Query<dynamic>($@"select Object,ObjectId,DisplayValue,lower(AssetTypeUid) as AssetTypeUid, TypeName from AssetDetail where uid = @assetUid", new { assetUid }, ApiTimeout).FirstOrDefault();
        }

        /// <summary>
        /// Gets the object and objectId of a Uid
        /// </summary>
        /// <param name="assetUid">The asset Uid</param>
        /// <returns></returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route("GetObjectDetailUIDetails/{assetUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(Object)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public dynamic GetObjectDetailUIDetails(Guid assetUid)
        {
            return Company.Query<dynamic>($@"select Object,ObjectId from [utility].[GetObjectObjectIdByUID](@assetUid)", new { assetUid }, ApiTimeout).FirstOrDefault();
        }

        /// <summary>
        /// Get field types for the given asset type Uid
        /// </summary>
        /// <param name="assetUid">The Uid of the asset type</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("searchDetails/{assetUid}"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(AssetsApiViewModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetAssetsSearchDetailsAsync(Guid assetUid)
        {
            var prefix = "Assets.GetAssetsSearchDetailsAsync => ";
            var errorMessage = "";
            AssetType type = new AssetType();
            Asset asset = new Asset();
            try
            {
                asset = AssetRepository.GetAssetByUID(assetUid);
                if (asset != null)
                {
                    var res = await AssetRepository.GetAssetDetails(asset) as object;
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, res)));

                }
                else
                {
                    type = AssetRepository.GetAssetTypeByUID(assetUid);
                    if (type == null)
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, string.Format(AssetsApiMessages.AssetAssetTypeNotFound, assetUid), errorMessage));

                    var res = await AssetRepository.GetAssetTypeDetails(type) as object;
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, res)));
                }

            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage));
            }
        }

        /// <summary>
        /// Retrieves a count of assets in the current environment.  Including the total asset count as well as a breakdown by asset class in the current environment.
        /// permissions are not factored into the return counts of assets and this endpoint requires administrator access.
        /// </summary>
        /// <returns>Returns a list of asset classes the count of assets in them as well as the total asset count in the environment.</returns>
        [
            HttpGet,
            Route("counts"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset type counts for current user.", typeof(AssetsCountModel)),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error indicating the user does not have permission to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetEnvironmentAssetCountsAsync()
        {
            var prefix = "Assets.GetEnvironmentAssetCountsAsync => ";
            var errorMessage = "";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return Request.CreateErrorResponse(HttpStatusCode.Forbidden, AssetsApiMessages.EnvironmentLevelAssetCountNotAllowed);
                ;
                return Request.CreateResponse(HttpStatusCode.OK, await AssetRepository.GetAssetsCounts());
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { ApiMessages.EndpointMethod, prefix }
                });

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// Get Count of asset for asset type Uid
        /// </summary>
        /// <param name="assetTypeUid">The Uid of the asset type</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("count/{assetTypeUid}"),
            SwaggerResponse(HttpStatusCode.OK, "An asset type count for current user.", typeof(List<AssetCountsModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid Asset Type Uid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset Type not found based on Uid provided.", typeof(ErrorResponse)),
        ]
        public async Task<HttpResponseMessage> GetAssetCountOfArtifactTypeUid(Guid assetTypeUid)
        {
            var prefix = "Assets.GetAssetCountOfArtifactTypeUid => ";
            var errorMessage = "";

            try
            {
                if (assetTypeUid == null || assetTypeUid == Guid.Empty)
                {
                        return ReturnApiError(HttpStatusCode.BadRequest, ActionApiMessages.AssetTypeUidIsNotValid);
                }
                else
                {
                    var assetType = this.AssetRepository.GetAssetTypeByUID(assetTypeUid);
                    if (assetType == null)
                    {
                        return ReturnApiError(HttpStatusCode.NotFound, string.Format(ActionApiMessages.AssetTypeNotFound, assetTypeUid.ToString()));
                    }
                }

                var assettypecount = await AssetRepository.GetAssetCountOfAssetTypeUid(assetTypeUid);
                return Request.CreateResponse(HttpStatusCode.OK, assettypecount);
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
        /// Retrieves a list of all asset types and asset counts for current user.
        /// </summary>
        /// <returns>Returns a list of asset type counts for current user.</returns>
        [
            HttpGet,
            Route("counts/byAssetType"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset type counts for current user.", typeof(List<AssetTypeCountModel>)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid Class name specified.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerParameter("Class", "Comma separated values of classes to filter by. Allowed values are BusinessAsset, TechnicalAsset, Model, Policy, Rule.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("returncount", "Allows you to include or exclude the count of the asset type. The default is true which means the count is included.", DataType = "boolean", ParameterType = "query", Required = false)
        ]
        public async Task<HttpResponseMessage> GetAssetTypeCountsAsync(Guid? assetTypeUid = null)
        {
            var prefix = "Assets.GetAssetTypeCountsAsync => ";
            var errorMessage = "";

            try
            {
                List<AssetTypeClass> classFilters = new List<AssetTypeClass>() {
                    AssetTypeClass.BusinessAsset,
                    AssetTypeClass.TechnicalAsset,
                    AssetTypeClass.Model,
                    AssetTypeClass.Policy,
                    AssetTypeClass.Rule,
                AssetTypeClass.Diagram
                };

                List<AssetTypeClass> allowedClasses = classFilters.Select(x => x).ToList();

                var param = Request.GetQueryNameValuePairs();
                if (param.Any(x => x.Key.ToLower() == "class"))
                {
                    var value = param.FirstOrDefault(x => x.Key.ToLower() == "class").Value;
                    var values = value.Split(',');
                    if (values.Count() > 0)
                    {
                        classFilters.Clear();
                        foreach (var cs in values.Select(x => x.Trim()))
                        {
                            if (Enum.TryParse(cs, true, out AssetTypeClass assetTypeClass))
                            {
                                if (!allowedClasses.Any(x => x == assetTypeClass))
                                {
                                    return ReturnApiError(HttpStatusCode.BadRequest, string.Format(AssetsApiMessages.ClassNotSupport, assetTypeClass.ToString()));
                                }
                                classFilters.Add(assetTypeClass);
                            }
                            else
                            {
                                return ReturnApiError(HttpStatusCode.BadRequest, string.Format(AssetsApiMessages.InvalidAssetTypeClass, cs));
                            }

                        }
                    }
                }

                var classes = await AssetRepository.GetAssetTypeCounts(classFilters.Select(x => (int)x).ToArray(), param, assetTypeUid);
                return Request.CreateResponse(HttpStatusCode.OK, classes);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { ApiMessages.EndpointMethod, prefix }
                });

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// Retrieves a list of all asset types and asset counts for current user.
        /// </summary>
        /// <returns>Returns a list of asset type counts for current user.</returns>
        [
            HttpGet,
            Route("{assetUid:Guid}/fields/{fieldApiName}"),
            SwaggerConsumes("application/json", "application/xml"),
            SwaggerProduces("application/json", "text/json", "application/xml", "text/xml", "application/octet-stream"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset type counts for current user.", typeof(List<dynamic>)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid Class name specified.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by AssetId.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_simpleFilter", "The text or phrase you want to find within fields. Filtering is done using 'Starts with' logic. Asterisk (*) symbol can be used as a wild card character to match any character.", DataType = "string", ParameterType = "query", Required = false),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetComplexFieldValueForAsset(Guid assetUid, string fieldApiName)
        {
            var prefix = "Assets.GetComplexFieldValueForAsset => ";
            var errorMessage = "";

            try
            {
                var qparams = Request.GetQueryNameValuePairs();
                var result = new Dictionary<string, object>();
                var asset = AssetRepository.GetAssetByUID(assetUid);
                int pageSize = 10;
                int pageNum = 1;
                string simpleFilter = string.Empty;
                string advancedFilter = string.Empty;

                if (asset == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(ActionApiMessages.AssetNotFound, assetUid.ToString())));
                }

                var fieldType = Company.FieldTypes.Where(x => x.AssetTypeID == asset.AssetTypeID && x.Name.ToLower().Trim() == fieldApiName.ToLower().Trim()).FirstOrDefault();
                if (fieldType == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(AssetsApiMessages.FieldTypeAssetNotFound, fieldApiName)));
                }

                if (!new string[] { "ComplexRelationLookup", "RefListRelationship", "OwnershipLookup" }.Contains(fieldType.Type))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format(AssetsApiMessages.FieldTypeNotSupport, fieldType.Type)));
                }


                bool useFriendlyNames = true;
                bool useUnflattedStructure = true;
                bool returnForUI = false;
                bool returnuseUidUrls = true;
                string orderBy = string.Empty;
                string direction = string.Empty;

                List<FieldType> fields = fieldsRepository.GetFieldDefinitionForComplexLookupFieldType(fieldType, assetUid);
                FieldTypeLookup ftl = Company.FieldTypeLookups.FirstOrDefault(x => x.FieldTypeID == fieldType.ID);

                List<dynamic> Values = new List<dynamic>();
                List<GridColumn> Columns = new List<GridColumn>();
                List<GridField> Fields = new List<GridField>();
                List<dynamic> scoringInfo = new List<dynamic>();

                int count = 0;
                var dbArgs = new DynamicParameters();

                if (qparams.Any(x => x.Key.ToLower() == "usefriendlynames"))
                {
                    if (!bool.TryParse(qparams.FirstOrDefault(x => x.Key.ToLower() == "usefriendlynames").Value.Trim().ToLower(), out useFriendlyNames))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, AssetsApiMessages.InvalidParameteruseuseFriendlyNames));
                    }
                }

                if (qparams.Any(x => x.Key.ToLower() == "useunflattedstructure"))
                {
                    if (!bool.TryParse(qparams.FirstOrDefault(x => x.Key.ToLower() == "useunflattedstructure").Value.Trim().ToLower(), out useUnflattedStructure))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, AssetsApiMessages.InvalidParameteruseUnflattedStructure));
                    }
                }

                if (qparams.Any(x => x.Key.ToLower() == "forui"))
                {
                    if (!bool.TryParse(qparams.FirstOrDefault(x => x.Key.ToLower() == "forui").Value.Trim().ToLower(), out returnForUI))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest,AssetsApiMessages.InvalidParameteforUI));
                    }
                }

                if (qparams.Any(x => x.Key.ToLower() == "useuidurls"))
                {
                    if (!bool.TryParse(qparams.FirstOrDefault(x => x.Key.ToLower() == "useuidurls").Value.Trim().ToLower(), out returnuseUidUrls))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, AssetsApiMessages.InvalidParameteruseUidUrls));
                    }
                }

                if (qparams.Any(x => x.Key.ToLower() == "_pagenum"))
                {
                    if (!int.TryParse(qparams.FirstOrDefault(x => x.Key.ToLower() == "_pagenum").Value.Trim().ToLower(), out pageNum))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, AssetsApiMessages.InvalidParameter_pageNum));
                    }
                }

                if (qparams.Any(x => x.Key.ToLower() == "_pagesize"))
                {
                    if (!int.TryParse(qparams.FirstOrDefault(x => x.Key.ToLower() == "_pagesize").Value.Trim().ToLower(), out pageSize))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, AssetsApiMessages.InvalidParameter_pageSize));
                    }
                }

                if (qparams.Any(x => x.Key.ToLower() == "simplefilter"))
                {
                    simpleFilter = qparams.FirstOrDefault(x => x.Key.ToLower() == "simplefilter").Value;
                }

                if (qparams.Any(x => x.Key.ToLower() == "filter"))
                {
                    advancedFilter = qparams.FirstOrDefault(x => x.Key.ToLower() == "filter").Value;
                }

                if (qparams.Any(x => x.Key.ToLower() == "_order"))
                {
                    orderBy = qparams.FirstOrDefault(x => x.Key.ToLower() == "_order").Value;
                }

                if (qparams.Any(x => x.Key.ToLower() == "_direction"))
                {
                    direction = qparams.FirstOrDefault(x => x.Key.ToLower() == "_direction").Value.ToLower();

                    if (!new string[] { "desc", "asc" }.Contains(direction))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.InvalidDirection));
                    }
                }

                if (!string.IsNullOrEmpty(orderBy))
                {
                    if (fieldType.Type == "OwnershipLookup")
                    {
                        List<string> allowedOrderFields = new List<string>()
                        {
                            "ResourceItemUrl","SecurityAssetName","Context","ResourceUid","ResponsibilityTypeName","ResourceName","SecurityAssetUid"
                        };

                        if (!allowedOrderFields.Select(x => x.ToLower()).Contains(orderBy.ToLower().Trim()))
                        {
                            return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, AssetsApiMessages.InvalidParameter_order));
                        }
                    }

                    if (fieldType.Type == "ComplexRelationLookup")
                    {
                        var definition = ftl.ParseComplexLookupDefinition();

                        var mappings = definition.GetFriendlyNamesMapping();

                        if (!mappings.ContainsKey(orderBy))
                        {
                            foreach (var item in mappings)
                            {
                                if (item.Value.ToLower() == orderBy.ToLower())
                                {
                                    orderBy = item.Key;
                                }
                            }

                            if (!mappings.ContainsKey(orderBy))
                                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, AssetsApiMessages.InvalidParameter_order));
                        }
                    }
                }

                var isStreamResponse = Request?.Headers?.Accept?.Any(a => a.MediaType == "application/octet-stream") ?? false;
                if (isStreamResponse)
                {
                    pageNum = 1;
                    pageSize = 10000;
                }

                dbArgs.Add("resourceId", Company.CurrentResourceID);
                dbArgs.Add("pageSize", pageSize);
                dbArgs.Add("pageNum", pageNum);
                dbArgs.Add("useUidUrls", returnuseUidUrls);
                dbArgs.Add("assetUid", assetUid);
                dbArgs.Add("object", asset.Object);
                dbArgs.Add("objectId", asset.ObjectID);
                dbArgs.Add("fieldTypeId", fieldType.ID);

                if (fieldType.Type == "ComplexRelationLookup")
                {
                    (Columns, Fields, Values, count, scoringInfo) =
                       await fieldsRepository.GetComplexRelationLookupGrid(ftl, fields, dbArgs, simpleFilter, advancedFilter, orderBy, direction);

                }

                if (fieldType.Type == "RefListRelationship")
                {
                    (Columns, Fields, Values, count) =
                       await fieldsRepository.GetRefListFromRelationshipGrid(fields, dbArgs, simpleFilter, advancedFilter, orderBy, direction);
                }

                if (fieldType.Type == "OwnershipLookup")
                {
                    (Columns, Fields, Values, count) =
                       await fieldsRepository.GetOwnershipLookupGrid(ftl, fields, dbArgs, simpleFilter, advancedFilter, orderBy, direction);
                }

                foreach (IDictionary<string, object> value in Values)
                {
                    foreach (var pair in value)
                    {
                        if (pair.Key.EndsWith("_assetPath"))
                        {
                            value[pair.Key] = WebUtility.HtmlDecode(pair.Value.ToString());
                        }
                    }
                }

                if (returnForUI || isStreamResponse)
                {
                    useFriendlyNames = useUnflattedStructure = false;
                }

                if (fieldType.Type == "ComplexRelationLookup")
                {
                    var definition = ftl.ParseComplexLookupDefinition();

                    if (useFriendlyNames)
                    {
                        CustomJSONContractResolver customContract = definition.GetFriendlyNameJSONContract();
                        var settings = new JsonSerializerSettings();
                        settings.ContractResolver = customContract;
                        Values = JsonConvert.DeserializeObject<JArray>(JsonConvert.SerializeObject(Values, settings)).ToObject<List<dynamic>>();

                        if (useUnflattedStructure)
                        {
                            List<dynamic> unflattened = definition.UnflattenJson(Values);
                            Values = unflattened;
                        }

                    }

                }

                if (isStreamResponse)
                {
                    string fileName = "Items";

                    if (fieldType != null)
                    {
                        fileName = fieldType.FriendlyName.GetSafeFilename();
                        fileName += " List";
                    }

                    if (fieldType.Type == "RefListRelationship")
                    {
                        var type = asset.Object;
                        var id = asset.ObjectID;
                        var intersect = Company.Filter<Intersect>(i => i.IntersectTypeID == fieldType.LookupObjectID.Value && ((i.Subject == type && i.SubjectID == id) || (i.Object == type && i.ObjectID == id))).FirstOrDefault();
                        if (intersect != null)
                        {
                            var referenceItemTypeID = (intersect.Subject == type && intersect.SubjectID == id) ? intersect.ObjectID : intersect.SubjectID;
                            var assetType = Company.Filter<AssetType>(x => x.Object == "ReferenceItemType" && x.ObjectID == referenceItemTypeID).FirstOrDefault();
                            if (assetType != null)
                            {
                                fileName = assetType.Name.GetSafeFilename();
                                fileName += " List";
                            }
                        }

                        //remove Color column from export
                        Columns = Columns.Where(c => c.columntype != "color" && c.datafield != "Color").ToList();
                        Fields = Fields.Where(f => f.type != "color" && f.name != "Color").ToList();
                    }

                    var document = new SLDocument();
                    document.RenameWorksheet(SLDocument.DefaultFirstSheetName, "Items");

                    int colIndex = 1;
                    for (int i = 0; i < Columns.Count; i++)
                    {
                        var colField = Columns[i].datafield;
                        var dataType = "string";

                        for (int k = 0; k < Fields.Count; k++)
                        {
                            var field = Fields[k];
                            if (field.name == colField)
                            {
                                dataType = field.type;
                                break;
                            }

                        }

                        document.SetCellValue(1, colIndex, Columns[i].text);

                        int rowIndex = 2;

                        for (int j = 0; j < Values.Count; j++)
                        {
                            var data = Values[j] as IDictionary<string, object>;
                            var value = data[colField];

                            SetCellValue(document, rowIndex, colIndex, dataType, value);

                            rowIndex++;
                        }
                        colIndex++;
                    }

                    var stream = new MemoryStream();
                    document.SaveAs(stream);
                    byte[] bytes = stream.ToArray();

                    var response = createFileResponseMessage(HttpStatusCode.OK, $"{fileName} {DateTime.Now.ToString("MMM dd yyyy")}.xlsx", bytes);
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(response));
                }
                else
                {

                    result.Add("pageSize", pageSize);
                    result.Add("pageNum", pageNum);
                    result.Add("total", count);

                    if (fieldType.Type == "RefListRelationship")
                    {
                        var type = asset.Object;
                        var id = asset.ObjectID;
                        var intersect = Company.Filter<Intersect>(i => i.IntersectTypeID == fieldType.LookupObjectID.Value && ((i.Subject == type && i.SubjectID == id) || (i.Object == type && i.ObjectID == id))).FirstOrDefault();
                        if (intersect != null)
                        {
                            var referenceItemTypeID = (intersect.Subject == type && intersect.SubjectID == id) ? intersect.ObjectID : intersect.SubjectID;
                            var assetType = Company.Filter<AssetType>(x => x.Object == "ReferenceItemType" && x.ObjectID == referenceItemTypeID).FirstOrDefault();
                            if (assetType != null)
                            {
                                result.Add("name", assetType.Name);
                                result.Add("description", assetType.Description);

                                if (returnForUI)
                                {
                                    var definition = JsonConvert.DeserializeObject<dynamic>(string.IsNullOrEmpty(fieldType.Definition) ? "{}" : fieldType.Definition);
                                    var showDescription = definition == null ? true : definition?.DisplayRefListDescription?.Value ?? true;

                                    result.Add("isReferenceListFromRelationship", true);
                                    result.Add("objectId", referenceItemTypeID);
                                    result.Add("fieldTypeId", fieldType.ID);
                                    result.Add("showDescription", showDescription);
                                    result.Add("url", $"/reference;referenceListId={assetType.uid.ToString().ToLower()}");
                                }
                            }
                        }
                    }

                    result.Add("items", Values);

                    if (returnForUI)
                    {
                        result.Add("Columns", Columns);
                        result.Add("Fields", Fields);
                        if (scoringInfo.Count > 0)
                        {
                            result.Add("ScoringInfo", scoringInfo);
                        }
                    }
                    var response = Request.CreateResponse(HttpStatusCode.OK, result);
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(response));
                }

            }
            catch (FilterExpressionParserException ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.FilterExpressionParseError, errorMessage));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { ApiMessages.EndpointMethod, prefix }
                });
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.InternalServerError, ApiMessages.UnknownError));
            }
        }

        /// <summary>
        /// Retrieves a list of possible owners for asset type.
        /// </summary>
        /// <returns>Returns a list of possible owners for asset type.</returns>
        /// <param name="assetTypeUid">The unique identifier of the asset type.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("{assetTypeUid:Guid}/possibleOwners"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset type counts for current user.", typeof(List<AssetTypePossibleOwnersModel>)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid Class name specified.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetPossibleOwnersByAssetTypeUid(Guid assetTypeUid)
        {
            var prefix = "Assets.GetPossibleOwnersByAssetTypeUid => ";
            var errorMessage = "";

            try
            {
                if (assetTypeUid == null || assetTypeUid == Guid.Empty)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ActionApiMessages.InvalidAssetTypeUid));
                }

                var assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);

                if (assetType == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(ActionApiMessages.AssetTypeNotFound, assetTypeUid.ToString())));
                }

                IEnumerable<dynamic> results = AssetRepository.GetPossibleOwnersForAssetType(assetType);

                return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { ApiMessages.EndpointMethod, prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError,ApiMessages.InternalServerError, errorMessage));
            }
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostBulkAssetsAsync(
            Guid assetTypeUid, 
            List<AssetInsert> assets, 
            bool triggersWorkflow = true, 
            [SwaggerDescription(nameof(Swagger.Execution_ApplicationId))] string applicationId = null)
        {
            var prefix = "Assets.PostBulkAssetsAsync => ";

            try
            {
                if (applicationId != null && applicationId.Length > 200)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.ApplicationIdMaxLengthViolated);
                }

                AssetType assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(ActionApiMessages.AssetTypeNotFound, assetTypeUid.ToString())));

                if (assets == null)
                    assets = readRequestJsonContent<List<AssetInsert>>(Request).Result;

                if (assets == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , ApiMessages.JSONValidMessage));

                var execution = getApiExecution(assets.Count, new ApiExecutionFields_PostAssets { AssetTypeUid = assetTypeUid }, applicationId: applicationId);

                ApiExecutionInfo executionInfo = await AssetRepository.PostBulkAssets(assets, execution, triggersWorkflow);

                var result = Request.CreateResponse(
                            HttpStatusCode.OK,
                            new ApiExecutionRecievedResponse
                            {
                                ExecutionID = executionInfo.ExecutionID,
                                Message = ApiMessages.ExecutionIDStatus,
                                Uri = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Host}/api/v2/assets/executions/{executionInfo.ExecutionID}/status"
                            });

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(result));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { ApiMessages.EndpointMethod, prefix },
                    { AssetsApiMessages.AssetTypeUid, assetTypeUid.ToString() },
                    { AssetsApiMessages.AssetCount, $"{((assets != null) ? assets.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage));
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutBulkAssetsAsync(
            Guid assetTypeUid, 
            List<AssetUpdate> assets,
            bool triggersWorkflow = true, 
            [SwaggerDescription(nameof(Swagger.Execution_ApplicationId))] string applicationId = null)
        {
            var prefix = "Assets.PutBulkAssetsAsync => ";
            try
            {
                if (applicationId != null && applicationId.Length > 200)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.ApplicationIdMaxLengthViolated);
                }

                AssetType assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(ActionApiMessages.AssetTypeNotFound, assetTypeUid.ToString())));

                if (assets == null)
                    assets = readRequestJsonContent<List<AssetUpdate>>(Request).Result;

                if (assets == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , ApiMessages.JSONValidMessage));

                var execution = getApiExecution(assets.Count, new ApiExecutionFields_PutAssets { AssetTypeUid = assetTypeUid }, applicationId: applicationId);
                var executionInfo = await AssetRepository.PutBulkAssets(assetTypeUid, assets, execution, triggersWorkflow);

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
                );
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { ApiMessages.EndpointMethod, prefix },
                    { AssetsApiMessages.AssetTypeUid, assetTypeUid.ToString() },
                    { AssetsApiMessages.AssetCount, $"{((assets != null) ? assets.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage));
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
        /// <param name="clearAllAssetsFromType">Optional query string parameter that allows you to remove all assets from the input asset type with cascade set to true;  the assets model should be null or an empty array.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
            Route("batch/{assetTypeUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution's unique identifier to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add assets of this type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteBulkAssetsAsync(
            Guid assetTypeUid, 
            AssetDeletes assets,
            bool clearAllAssetsFromType = false, 
            bool triggersWorkflow = true,
            [SwaggerDescription(nameof(Swagger.Execution_ApplicationId))] string applicationId = null)
        {
            var prefix = "Assets.DeleteBulkAssetsAsync => ";
            try
            {
                if (applicationId != null && applicationId.Length > 200)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.ApplicationIdMaxLengthViolated);
                }

                AssetType assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , string.Format(ActionApiMessages.AssetTypeNotFound, assetTypeUid.ToString())));

                if (assets == null && !clearAllAssetsFromType)
                    assets = readRequestJsonContent<AssetDeletes>(Request).Result;

                if ((assets == null && !clearAllAssetsFromType) || (assets != null && assets.Count == 0 && !clearAllAssetsFromType) || (assets != null && assets.Count > 0 && clearAllAssetsFromType))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest ,ApiMessages.JSONValidMessage));


                var execution = getApiExecution(assets != null ? assets.Count : 0, new ApiExecutionFields_DeleteAssets { AssetTypeUid = assetTypeUid }, applicationId: applicationId);

                var executionInfo = await AssetRepository.BulkDeleteAssets(assetTypeUid, assets, execution, clearAllAssetsFromType, triggersWorkflow);

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
                );
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { ApiMessages.EndpointMethod, prefix },
                    { AssetsApiMessages.AssetTypeUid, assetTypeUid.ToString() },
                    { AssetsApiMessages.AssetCount, $"{((assets != null) ? assets.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage));
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteBulkAssetTypesAsync(AssetTypeDeletes assetTypes)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, AssetsApiMessages.RemoveAssetTypeNotAllowed));

            var prefix = "Assets.DeleteBulkAssetTypesAsync => ";
            var errorMessage = "";

            try
            {
                var governanceRole = SettingsRepository.GetSettingValue<Guid>(Setting.GovernanceRoleReferenceListUid);
                foreach (var asset in assetTypes)
                {
                    if (governanceRole == asset.Uid)
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , string.Format(AssetsApiMessages.ReferenceUIDConfigureAsGovernRole, asset.Uid.ToString())));
                }

                if (assetTypes == null)
                    assetTypes = readRequestJsonContent<AssetTypeDeletes>(Request).Result;

                if (assetTypes == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , ApiMessages.JSONValidMessage));

                List<ApiExecutionFields_DeleteAssetTypes> typesForDelete = assetTypes.Select(x => new ApiExecutionFields_DeleteAssetTypes() { AssetTypeUid = x.Uid }).ToList();
                var execution = getApiExecution(assetTypes.Count, typesForDelete);

                ApiExecutionInfo executionInfo = await AssetRepository.DeleteBulkAssetTypes(assetTypes, execution);

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
                );
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { ApiMessages.EndpointMethod, prefix },
                    { AssetsApiMessages.AssetCount, $"{((assetTypes != null) ? assetTypes.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage));
            }
        }

        /// <summary>
        /// Removes a single asset type
        /// </summary>
        /// <param name="assetType">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
            Route("single"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution's unique identifier to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to delete this asset type is invalid, possibly due to an deletion already in progress.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to remove asset types.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> DeleteSingleAssetTypesAsync(AssetTypeSingleDelete assetType)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, AssetsApiMessages.RemoveAssetTypeNotAllowed));

            var prefix = "Assets.DeleteBulkAssetTypesAsync => ";
            var errorMessage = "";

            try
            {
                var governanceRole = SettingsRepository.GetSettingValue<Guid>(Setting.GovernanceRoleReferenceListUid);

                if (governanceRole == assetType.Uid)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , string.Format(AssetsApiMessages.ReferenceUIDConfigureAsGovernRole, assetType.Uid.ToString())));

                if (assetType == null)
                    assetType = readRequestJsonContent<AssetTypeSingleDelete>(Request).Result;

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , ApiMessages.JSONValidMessage));

                var type = AssetRepository.GetAssetTypeByUID(assetType.Uid);
                if (type == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , string.Format(ActionApiMessages.AssetTypeNotFound, assetType.Uid.ToString())));
                }

                string deletionInProgressQuery = @"SELECT count(*)
                                      FROM [api].[Execution]
                                      where Route = '/api/v2/assets/single'
                                      and completedon is null
                                      and Method = 'DELETE'
                                      and Fields = @fields";

                var fieldObj = new ApiExecutionFields_DeleteAssetTypes { AssetTypeUid = assetType.Uid };

                var res = Company.Query<int>(
                    deletionInProgressQuery,
                    new { fields = JsonConvert.SerializeObject(fieldObj) })
                    .FirstOrDefault();

                if (res > 0)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , string.Format(AssetsApiMessages.AssetTypeInProcessNotDelete, assetType.Uid.ToString())));
                }

                var execution = getApiExecution(1, fieldObj);
                var deletes = new AssetTypeDeletes();
                deletes.Add(new AssetTypeDelete() { Cascade = assetType.Cascade, ExecutionItemUid = Guid.NewGuid(), Uid = assetType.Uid });

                var deleteAssetTypesResults = AssetRepository.DeleteSingleAssetType(deletes, type, execution);
                Company.CreateRollupPathChangedExecution(assetTypeId: type.ID);

                return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                           deleteAssetTypesResults
                        )
                    )
                );
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { ApiMessages.EndpointMethod, prefix },
                    { AssetsApiMessages.AssetCount, $"{((assetType != null) ? 1 : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage));
            }
        }

        /// <summary>
        /// GETs the status of an execution record, including the results for the execution.
        /// </summary>
        /// <param name="executionID">The execution's unique identifier to retrieve status for.</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("executions/{executionID:Guid}/status"),
            SwaggerParameter("summaryOnly", "When true the results are omitted from the response. The default value is false.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "An execution status including a list of assets.", typeof(ApiExecutionStatusModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your status was not found.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetExecutionStatus(Guid executionID)
        {
            var prefix = "Assets.GetExecutionStatus => ";
            var errorMessage = "";
            var summaryOnly = false;
            var queryParams = Request.GetQueryNameValuePairs();


            try
            {
                if (queryParams.ToList().Any(x => x.Key.ToLower() == "summaryonly"))
                {
                    bool.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "summaryonly").Value, out summaryOnly);
                }

                var res = await AssetRepository.GetExecutionStatusModel(executionID, !summaryOnly);
                if (res == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , ApiMessages.ExecutionUIDNotFound));
                }
                return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            res as object
                        )
                    )
                );
            }
            catch (ArgumentException)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound , ApiMessages.ExecutionUIDNotFound));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { ApiMessages.EndpointMethod, prefix },
                    { AssetsApiMessages.ExecutionID, executionID.ToString() },
                    { AssetsApiMessages.ExecutionUid, executionID.ToString() }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage));
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
        /// <param name="assetTags">Collection of assets and tags to associate. Use TagUID or TagName to associate an asset with existing tag.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            Route("tags"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Creates association between an existing Asset and an existing tag, returns the UID of asset/tag association.", typeof(List<AssetTagSuccessApiModel>)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Asset / Tag Association failed. Tag field may not be assigned to Asset. ", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult PostAssetTag(List<AssetTagApiModel> assetTags)
        {
            List<AssetTagSuccessApiModel> resultList = new List<AssetTagSuccessApiModel>();
            Tag currentTag;
            foreach (var assetTagApi in assetTags)
            {
                AssetTagSuccessApiModel result;

                if (assetTagApi.TagUID != Guid.Empty && !string.IsNullOrEmpty(assetTagApi.TagName))
                {
                    result = new AssetTagSuccessApiModel()
                    {
                        Message = $"Only TagUid OR TagName can be specified.",
                        Success = false
                    };

                    resultList.Add(result);
                    continue;
                }

                if (assetTagApi.TagUID == Guid.Empty)
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
                        Message = $"Invalid TagUid provided, no tag exists with the specified uid.",
                        Success = false
                    };

                    if (!string.IsNullOrEmpty(assetTagApi.TagName))
                    {
                        result.Message = $"Invalid TagName provided, no tag exists with the specified Tag name.";
                    }


                    resultList.Add(result);
                    continue;
                }
                if (assetTagApi.AssetUID == Guid.Empty)
                {
                    result = new AssetTagSuccessApiModel()
                    {
                        Message = $"Invalid AssetUid provided, no asset exists with the specified uid.",
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

                var assetType = Company.Filter<AssetType>(x => x.ID == asset.AssetTypeID).FirstOrDefault();
                var fieldTypes = Company.FieldTypes.Where(f => f.AssetTypeID == assetType.ID);
                if (!fieldTypes.Any(x => x.Type.ToLower() == "tag"))
                {
                    result = new AssetTagSuccessApiModel()
                    {
                        Message = $"No Tag Fields found for AssetUID {assetTagApi.AssetUID}, Cannot add an association without a Tag field",
                        Success = false
                    };
                    resultList.Add(result);
                    continue;
                }

                if (this.tagRepository.DoesAssetTagExists(currentTag.ID, asset.ID))
                {
                    result = new AssetTagSuccessApiModel()
                    {
                        Message = $"TagUID {assetTagApi.TagUID} and AssetUID {assetTagApi.AssetUID} association  already exists, it is not valid to add a second association",
                        Success = false
                    };
                    resultList.Add(result);
                    continue;
                }
                if (!Company.HasAssetPermission(asset.Object, asset.ObjectID, Permission.ReadAsset))
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
                    return errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, AssetsApiMessages.AssetTagAssociationFailed);
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
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
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
                        Message = $"A non-admin user can only remove the tag (Uid:  {assetTagApi.TagUID}) association to an asset (Uid: {assetTagApi.AssetUID}) if they initially created the association for or they have edit rights to asset",
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
                        Message = string.Format(AssetsApiMessages.TagUIDAssetUIDNotExists, assetTagApi.TagUID.ToString(), assetTagApi.AssetUID.ToString()),
                        Success = false
                    };
                    resultList.Add(result);
                }

            }
            return ResponseMessage(Request.CreateResponse<List<AssetTagSuccessApiModel>>(HttpStatusCode.OK, resultList));

        }
        #endregion

        private SLDocument GetCustomExportSheet(AssetType assetType, AssetTypeExportTemplate template, List<FieldType> fieldsForCustomExport, AssetsApiViewModel results)
        {
            var data = results.items;
            if (!(template.IncludeFieldTypes == null || template.IncludeFieldTypes.Length <= 0))
            {
                var allFieldTypes = Company.FieldTypes.Where(x => x.AssetTypeID == assetType.ID);
                var fieldTypeList = template.IncludeFieldTypes;

                fieldsForCustomExport.Clear();

                //done this way to set order of fields in spreadsheet to the order specified in include fields.
                foreach (var fieldName in fieldTypeList)
                {
                    var field = allFieldTypes.FirstOrDefault(x => x.Name.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase));
                    if (field != null) fieldsForCustomExport.Add(field);
                }
            }
            SLDocument document = null;
            if (template.IncludeParent)
            {
                fieldsForCustomExport.Insert(0, new FieldType { Type = "string", Name = "ParentDisplayName", FriendlyName = "Parent" });
            }

            if (assetType.Class == AssetTypeClass.Rule)
            {
                fieldsForCustomExport.Add(new FieldType { Type = "string", Name = "RuleUID", FriendlyName = "Rule UID" });

                foreach (var item in data)
                {
                    item.RuleUID = item.AssetUid;
                }
            }

            if (template.IncludeUrl)
            {
                fieldsForCustomExport.Add(new FieldType { Type = "string", Name = "Url", FriendlyName = "Url" });

                foreach (var item in data)
                {
                    item.Url = "asset/" + item.AssetUid;
                }

            }

            switch (template.ExportViewType)
            {
                case core.enums.ExportView.None:
                    document = GenerateDefaultSpreadsheet(fieldsForCustomExport, data, template, "Items");
                    break;
                case core.enums.ExportView.Pivot:
                    document = GeneratePivotedSpreadsheet(fieldsForCustomExport, data, template, "Items");
                    break;
                case core.enums.ExportView.Grouped:
                    document = GenerateGroupedSpreadsheet(fieldsForCustomExport, data, template, "Items");
                    break;
                default:
                    throw new Exception(AssetsApiMessages.InvalidExportViewType);
            }

            return document;
        }


        /// <summary>
        /// Retrieves a list of all pre defined colors.
        /// </summary>
        /// <returns>Returns a list colors.</returns>
        [
            HttpGet,
            Route("colors"),
            SwaggerConsumes("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of all pre defined colors.", typeof(List<dynamic>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetColors()
        {
            var prefix = "Assets.GetPossibleOwnersByAssetTypeUid => ";
            var errorMessage = "";
            try
            {
                var results = await Company.QueryAsync<dynamic>(@"SELECT * FROM dbo.Color", ApiTimeout);
                return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results.Select(x => new { label = x.Name, value = x.Name, title = x.Value }))));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { ApiMessages.EndpointMethod, prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.InternalServerError, errorMessage));
            }
        }

        /// <summary>
        /// Retrieves a list of asset uids and paths for the given asset type.
        /// </summary>
        /// <returns>Returns a list of asset uids and paths.</returns>
        [
            HttpGet,
            Route("paths/{assetTypeUid}"),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 5000.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for. The default value is 1.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Whether or not to include the total count in the results, the default is true.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerConsumes("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset uids and paths. This is an admin only endpoint.", typeof(AssetPathResults)),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error indicating the user does not have permission to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error indicating the asset type for the given uid was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error indicating the request is invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetAssetUids(Guid assetTypeUid)
        {
            var prefix = "Assets.GetAssetUids => ";
            var errorMessage = "";
            var queryParams = Request.GetQueryNameValuePairs();

            const int maxPageSize = 100000;

            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage)));

            var assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);

            if (assetType == null)
                return await Task.FromResult(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, ActionApiMessages.InvalidAssetTypeUid)));

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_pagesize"))
            {
                if (int.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "_pagesize").Value, out int res))
                {
                    if (res > maxPageSize || res < 1)
                    {
                        return await Task.FromResult(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, AssetsApiMessages.PageSizeRange)));
                    }
                }
                else
                {
                    return await Task.FromResult(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, AssetsApiMessages.PageSizeNotNumber)));
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_pagenum"))
            {
                if (int.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "_pagenum").Value, out int res))
                {
                    if (res < 1)
                    {
                        return await Task.FromResult(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, AssetsApiMessages.InvalidPageNumberGT0)));
                    }
                }
                else
                {
                    return await Task.FromResult(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, AssetsApiMessages.InvalidPageNumber)));
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_includetotal"))
            {
                if (!bool.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "_includetotal").Value, out bool res))
                {
                    return await Task.FromResult(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, AssetsApiMessages.InvalidParameterIncludeTotal)));
                }
            }

            try
            {
                var results = await AssetRepository.GetAssetPaths(assetType, queryParams);
                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK, results as object);


                return await Task.FromResult<IHttpActionResult>(ResponseMessage(response));

            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { ApiMessages.EndpointMethod, prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.InternalServerError, errorMessage));
            }
        }


        /// <summary>
        /// Retrieves the details for the specified asset
        /// </summary>
        /// <param name="assetUid">The uid of the asset</param>
        /// <returns>Details for the specified asset</returns>
        [
            HttpGet,
            Route("asset/{assetUid}"),
            SwaggerConsumes("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "Details of the asset.", typeof(object)),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error indicating the user does not have permission to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error indicating the asset for the given uid was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetAsset(Guid assetUid)
        {
            var prefix = "Assets.GetAsset => ";

            try
            {
                var res = await AssetRepository.GetAssetSingle(assetUid);

                if (res == null)
                {
                    return await Task.FromResult(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format(ActionApiMessages.AssetNotFound, assetUid.ToString()))));
                }

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, res as object)));
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { ApiMessages.EndpointMethod, prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.InternalServerError, errorMessage));
            }


        }

        /// <summary>
        /// Request certification of the specified asset
        /// </summary>
        /// <param name="assetUid">The uid of the asset</param>
        /// <returns>API response for success or failure</returns>
        [
            HttpPost,
            Route("RequestCertification/{assetUid}"),
            SwaggerConsumes("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "Details of the asset.", typeof(object)),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error indicating the user does not have permission to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error indicating the asset for the given uid was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> RequestCertification(Guid assetUid)
        {
            var prefix = "Assets.RequestCertification => ";

            try
            {
                var asset = Company.Assets.Where(x => x.uid == assetUid).Include(x => x.AssetType).FirstOrDefault();

                if (asset == null) throw new NotFoundException("Asset");

                if (Enum.TryParse(asset.Object, out SystemObjects obj) && Enum.TryParse(asset.AssetType.Object, out SystemObjects objType))
                {
                    Company.RequestObjectCertification(obj, asset.ObjectID, objType, asset.AssetType.ObjectID);
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            new ApiStatusResponse() { Success = true, Message = AssetsApiMessages.RequestCreatedMsg, Uid = asset.uid })
                        )
                    );
                }
                else
                {
                    throw new NotFoundException(AssetsApiMessages.InvalidAsset);

                }



            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { ApiMessages.EndpointMethod, prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError,ApiMessages.InternalServerError, errorMessage));
            }


        }



        /// <summary>
        /// Retrieves an process diagram url for Task asset uid
        /// </summary>
        /// <param name="assetUid">The asset uid</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("{assetUid:Guid}/diagramUrl"),
            SwaggerConsumes("application/json"),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetProcessDiagramUrl(Guid assetUid)
        {
            if (assetUid == null)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, ActionApiMessages.InvalidAssetUid));

            var asset = AssetRepository.GetAssetByUID(assetUid);
            if (asset == null)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, string.Format(ActionApiMessages.AssetNotFound, assetUid.ToString())));

            var response = Company.GetDiagramUrlForDiagramAsset(assetUid);

            return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response)));

        }

        /// <summary>
        /// Retrieves a list of watchers for a given asset.
        /// </summary>
        /// <returns>Returns a list of watchers</returns>
        [
            HttpGet,
            Route("asset/{assetUid}/watchers"),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for. The default value is 1.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Whether or not to include the total count in the results, the default is true.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. Options are resourceId or name. By default the results are ordered by name.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerConsumes("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of watchers for a given asset.", typeof(AssetWatchers)),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error indicating the user does not have permission to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error indicating the request is invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetAssetWatchers(Guid assetUid)
        {
            var queryParams = Request.GetQueryNameValuePairs();

            string isValid = isPageSizeAndNumValid(queryParams);

            if (string.IsNullOrEmpty(isValid) && queryParams.Any(q => q.Key == "_order"))
            {
                string[] allowedValues = new string[] { "name", "resourceid" };
                var order = queryParams.ToList().FirstOrDefault(q => q.Key == "_order").Value.ToLower();
                if (!allowedValues.Contains(order))
                {
                    isValid = $"{order} is not a valid _order field";
                }
            }

            if (string.IsNullOrEmpty(isValid) && queryParams.Any(q => q.Key == "_direction"))
            {
                string[] allowedValues = new string[] { "asc", "desc" };
                var directionFilter = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction");

                if (!allowedValues.Contains(directionFilter.Value.Trim().ToLower()))
                {
                    isValid = "Invalid _direction provided";
                }
            }

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_includetotal"))
            {
                var val = queryParams.ToList().First(k => k.Key.ToLower() == "_includetotal");

                if (!bool.TryParse(val.Value, out _))
                {
                    isValid = "Invalid _includeTotal value passed in the request";
                }
            }

            var asset = AssetRepository.GetAssetByUID(assetUid);
            if (asset == null)
            {
                isValid = "The asset with uid specified does not exist.";
            }

            if (!string.IsNullOrEmpty(isValid))
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , isValid));
            }

            try
            {
                var results = await AssetRepository.GetAssetWatchers(assetUid, queryParams);
                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK, results);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage));
            }
        }

        /// <summary>
        /// Get count of assets being watched for each Asset Type.
        /// </summary>        
        /// <param name="resourceUid">Optional Uid of a resource. If provided returns count for that specific resource. If null count will be of all watchers.</param>    
        [
            HttpGet,
            Route("watchers/counts"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "List of Asset Types with count of watchers", typeof(List<AssetTypeWatchCountModel>)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid parameters provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetWatchCountByType(string resourceUid = null)
        {
            string resourceJoin = "";
            DynamicParameters dbArgs = new DynamicParameters();

            if (resourceUid != null)
            {
                if (!Guid.TryParse(resourceUid, out Guid rUid) || !Company.GlobalReportingResources.Any(u => u.Uid == rUid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest , AssetsApiMessages.InvalidResourceUID));
                }
                else
                {
                    resourceJoin = $@"INNER JOIN
                                      reporting.Global_Resource R on R.ResourceID = F.ResourceID and R.uid = @resourceUid";

                    dbArgs.Add("@resourceUid", rUid);
                }
            }

            var sql = $@"
                        SELECT AssetTypeName, AssetTypeUid, count(*) as [Count] FROM (
                        SELECT 
	                        ast.[Name] as AssetTypeName,
	                        ast.[uid] as AssetTypeUid,
	                        a.uid,
	                        f.ResourceID
                        FROM
	                        Follow f
	                        inner join
	                        AssetType ast on f.ObjectID = ast.ObjectID and f.ObjectType=ast.Object and f.FollowTypeID =3
	                        inner join 
	                        Asset a on a.AssetTypeID=ast.ID 
	                        {resourceJoin}
                        union
                        select 
	                        ast.[Name] as AssetTypeName,
	                        ast.[uid] as AssetTypeUid,
	                        a.uid,
	                        f.ResourceID
                        from 
	                        Follow f
	                        inner join
	                        Asset a on f.ObjectID = a.ObjectID and f.ObjectType=a.Object and f.FollowTypeID = 1
	                        inner join 
	                        AssetType ast on a.AssetTypeID=ast.ID
	                        {resourceJoin}
	                        ) watches
                        Group by 
	                        watches.AssetTypeUid, watches.AssetTypeName
                        order by AssetTypeName";

            var results = await Company.QueryAsync<AssetTypeWatchCountModel>(sql, dbArgs, ApiTimeout);

            var response = Request.CreateResponse(HttpStatusCode.OK, results);

            return await Task.FromResult<IHttpActionResult>(ResponseMessage(response));
        }

        /// <summary>
        /// Retrieves details about assets being watched for a given asset type.
        /// </summary>
        /// <returns>Returns a list of watched asset details</returns>        
        /// <param name="assetTypeUid">Uid of the asset type</param>
        [
            HttpGet,
            Route("{assetTypeUid:Guid}/watchers"),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for. The default value is 1.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Whether or not to include the total count in the results, the default is true.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. Options are resourceId, name, assetDisplayValue, governanceScore or dataQualityScore. By default the results are ordered by name.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("resourceUid", "Optional Uid of a resource. If provided returns assets relevant to that specific resource. If null asset details returned will be for all watchers.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerConsumes("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of watchers for a given asset.", typeof(WatchedAssetTypeDetailModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error indicating the request is invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetAssetTypeWatchDetails(Guid assetTypeUid)
        {
            var queryParams = Request.GetQueryNameValuePairs();

            string isValid = isPageSizeAndNumValid(queryParams);
            if (!string.IsNullOrEmpty(isValid))
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, isValid));
            }

            if (queryParams.Any(q => q.Key == "resourceUid"))
            {
                if (!Guid.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key == "resourceUid").Value.ToLower(), out Guid resourceUid) || !Company.GlobalReportingResources.Any(u => u.Uid == resourceUid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, string.Format(AssetTypeErrors.InvalidParameterProvided, "resourceUid")));
                }
            }

            if (queryParams.Any(q => q.Key == "_order"))
            {
                string[] allowedValues = new string[] { "name", "resourceid", "assetdisplayvalue", "governancescore", "dataqualityscore" };
                var order = queryParams.ToList().FirstOrDefault(q => q.Key == "_order").Value.ToLower();
                if (!allowedValues.Contains(order))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, string.Format(AssetsApiMessages.InvalidOrder, order )));
                }
            }

            if (string.IsNullOrEmpty(isValid) && queryParams.Any(q => q.Key == "_direction"))
            {
                string[] allowedValues = new string[] { "asc", "desc" };
                var directionFilter = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction");

                if (!allowedValues.Contains(directionFilter.Value.Trim().ToLower()))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, string.Format(AssetTypeErrors.InvalidParameterProvided, "_direction")));
                }
            }

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_includetotal"))
            {
                var val = queryParams.ToList().First(k => k.Key.ToLower() == "_includetotal");

                if (!bool.TryParse(val.Value, out _))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, string.Format(AssetTypeErrors.InvalidParameterProvided, "_includeTotal")));
                }
            }

            var assetType = AssetRepository.GetAssetTypeByUID(assetTypeUid);
            if (assetType == null)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.NotFoundBasedOnUid));
            }

            try
            {
                var results = await AssetRepository.GetWatchedAssetDetails(assetTypeUid, queryParams);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage));
            }
        }

        /// <summary>
        /// Return all assets from asset type formatted for dropdown list item list and used for lazy load
        /// </summary>
        /// <param name="assetTypeUid"></param>
        [
            HttpGet,
            ApiExplorerSettings(IgnoreApi = true),
            MapToApiVersion("2.0"),
            Route("lookupvalues/{assetTypeUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "true/false based on relationship exists on assettype.", typeof(bool)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
            ]
        public async Task<HttpResponseMessage> GetAssetLookupValues(Guid assetTypeUid, int? skip = null, int? take = 0, string filter = null)
        {
            var prefix = "Assets.GetAssetLookupValues => ";
            var errorMessage = "";

            try
            {

                var assetType = Company.AssetTypes.FirstOrDefault(x => x.uid == assetTypeUid);

                if (assetType.Object == "ReferenceItemType" && assetType.ObjectID == 0)
                {
                    //handle reference lists
                    IQueryable<AssetType> query = Company.AssetTypes.Where(x => x.Object == "ReferenceItemType" && x.ObjectID != 0);

                    if (!string.IsNullOrEmpty(filter))
                    {
                        query = query.Where(x => x.Name.Contains(filter));
                    }

                    var refListResults = query.OrderBy(x => x.Name)
                        .Skip(skip.Value)
                        .Take(take.Value)
                        .Select(x => new { value = x.uid, label = x.Name }).ToList();

                    return Request.CreateResponse(HttpStatusCode.OK, refListResults);
                }

                filter = "%" + (filter ?? "") + "%";
                var results = await Company.QueryAsync<dynamic>($@"exec [SimpleAssetSearch] @assetTypeId, @filter,@skip,@take", new { assetTypeId = assetType.ID, skip, take, filter });

                return Request.CreateResponse(HttpStatusCode.OK, results);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// Return all level names for asset type uid specified.
        /// </summary>
        /// <param name="assetTypeUid"></param>
        [
            HttpGet,
            ApiExplorerSettings(IgnoreApi = true),
            MapToApiVersion("2.0"),
            Route("{assetTypeUid}/levels"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "true/false based on relationship exists on assettype.", typeof(bool)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
            ]
        public async Task<HttpResponseMessage> GetAssetTypeLevels(Guid assetTypeUid)
        {
            var prefix = "Assets.GetAssetTypeLevels => ";
            var errorMessage = "";

            try
            {
                var query = $@"
drop table if exists #levelNameMapping
create table #levelNameMapping (Level int, Name nvarchar(250), Description nvarchar(4000))

declare @levelCount int = (select top 1 HierarchyMaximumDepth from AssetType at where at.uid = @assetTypeUid)
   while @levelCount > 0
    begin
        insert into #levelNameMapping (Level) values (@levelCount)

        set @levelCount = @levelCount - 1
    end

update T
set T.Name = ATL.Name,
    T.Description = ATL.Description
from #levelNameMapping T
inner join AssetType at on at.uid = @assetTypeUid
inner join AssetTypeLevel ATL on atl.assettypeid = at.id and atl.level = T.Level

select Level, ISNULL(Name,'Level '+ cast(Level as nvarchar(10))) as Name, Description from #levelNameMapping order by [Level] asc";

                var results = await Company.QueryAsync<dynamic>(query, new { assetTypeUid });

                return Request.CreateResponse(HttpStatusCode.OK, results);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// Return asset path for asset uid.
        /// </summary>
        /// <param name="assetUid"></param>
        [
            HttpGet,
            ApiExplorerSettings(IgnoreApi = true),
            MapToApiVersion("2.0"),
            Route("{assetUid}/path"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "true/false based on relationship exists on assettype.", typeof(bool)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
            ]
        public async Task<HttpResponseMessage> GetAssetPathByUid(Guid assetUid)
        {
            var prefix = "Assets.GetAssetPathByUid => ";
            var errorMessage = "";

            try
            {
                var query = $@" exec graph.UpdateAssetNode @assetuid,1
                        select DisplayPath from asset a
                           inner join graph.AssetNodeDisplayPath Node on Node.id = a.id
                         where a.uid = @assetuid
                        ";

                var results = await Company.QueryAsync<dynamic>(query, new { assetUid });

                return Request.CreateResponse(HttpStatusCode.OK, results);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }


        /// <summary>
        /// Gets the descendents for a given asset.
        /// </summary>
        /// <param name="assetUid">The unique identifier of an asset.</param>
        /// <returns>A list of descendent asset uids</returns>
        [
            HttpGet,
            Route("asset/{assetUid:Guid}/descendents"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(AssetDescendantsResults)),
            SwaggerProduces("application/json", "text/json", "application/xml", "text/xml", "application/octet-stream"),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error to indicate that your request to retrieve this asset is forbidden due to lack of permissions to view it.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),            
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 250. Maximum page size is 10,000", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Allows you to disable including the count of the total number of results across pages in the response.  The default is true meaning the total count is included.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_parentAssetUid", "Filter by provided asset Uid.", DataType = "string", ParameterType = "query", Required = false),
        ]
        public async Task<IHttpActionResult> GetAssetDescendents(Guid assetUid)
        {
            var prefix = "Assets.GetAssetDescendent => ";
            try
            {
                var queryParams = Request.GetQueryNameValuePairs();

                var validationResult = ValidateGetDescendentParameters(assetUid, queryParams);
                
                if (validationResult.StatusCode != HttpStatusCode.OK)
                {
                    return await Task.FromResult(errorMessageResponse(validationResult.StatusCode, validationResult.Error, validationResult.Message)).ConfigureAwait(false);
                }

                var results = await AssetRepository.GetAssetDescendants(assetUid, queryParams);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results));
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string> {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.InternalServerError, errorMessage)).ConfigureAwait(false);
            }
        }

        private WorkHttpStatus ValidateGetDescendentParameters(Guid assetUid, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var isValid = isPageSizeAndNumValid(queryParams);

            if (!string.IsNullOrEmpty(isValid))
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, isValid);
            }

            if(assetUid == Guid.Empty)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format(ApiMessages.InvalidAssetUid, assetUid.ToString()));
            }

            var asset = AssetRepository.GetAssetByUID(assetUid);

            if (asset == null)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format(ApiMessages.InvalidAssetUid, assetUid.ToString()));
            }

            if (queryParams.Any(qp => qp.Key.ToLower() == "_includetotal"))
            {
                if (!bool.TryParse(queryParams.FirstOrDefault(q => q.Key.ToLower() == "_includetotal").Value, out bool includeTotal))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.InvalidIncludeTotal);
                }
            }

            if (queryParams.Any(qp => qp.Key.ToLower() == "_parentassetuid"))
            {
                if (!Guid.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "_parentassetuid").Value, out Guid parentAssetUid) || parentAssetUid==Guid.Empty)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, AssetsApiMessages.InvalidParentAssetUid);
                }

                var parentAsset = AssetRepository.GetAssetByUID(parentAssetUid);

                if (parentAsset == null)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format(ApiMessages.InvalidAssetUid, assetUid.ToString()));
                }
            }

            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }
    }
}