using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using Resources;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using SpreadsheetLight;
using d360.model.helpers.filters;
using d360.core.exceptions;
using System.Threading;

namespace d360.web.Controllers.V2
{
    [
        ApiVersion("2.0"), 
        RoutePrefix("api/v{version:apiVersion}/dataprofiles"), 
        Authorize,
        StringEnumController
    ]
    public class DataProfilesController : BaseV2ApiController
    {
        internal IAssetRepository AssetRepository;
        internal IDataProfileRepository DataProfiles;
        private ISemanticsRepository SemanticsRepository;

        public DataProfilesController(
            ICoreComponentSet set, 
            IAssetRepository assetRepository, 
            IDataProfileRepository dataProfileRepository,
            ISemanticsRepository semanticsRepository)
            : base(set)
        {
            this.AssetRepository = assetRepository;
            this.DataProfiles = dataProfileRepository;
            this.SemanticsRepository = semanticsRepository;
        }

        #region Core Data Profile Endpoints

        /// <summary>
        /// Retrieves Data Profile results for a given asset.
        /// </summary>
        /// <param name="assetUid">The unique identifier of an asset.</param>
        /// <returns>A list of Data Profile results</returns>
        [
            HttpGet,
            Route("{assetUid:Guid}"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(AssetDataProfilesApiViewModel)),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error to indicate that your request to retrieve this asset is forbidden due to lack of permissions to view it.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerParameter("_startDate", "Start date to get data profile data for. If _startDate and _endDate are not supplied the date defaults to the most recent date for the specified asset for which there is data. Otherwise the default is current date UTC.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_endDate", "End date to get data profile data for. If _startDate and _endDate are not supplied the date defaults to the most recent date for the specified asset for which there is data. Otherwise the default is current date UTC.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_includeChildAssets", " If true returns the data profile results for all descendant assets of the specified asset for the same date criteria.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 250. Maximum page size is 10,000", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Allows you to disable including the count of the total number of results across pages in the response.  The default is true meaning the total count is included.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_includeSamples", "If true returns the outlierDetail, topK, bottomK, cardinalityDetail, shapesDetail collections on the data profile results. The default is true meaning the collections will be included.", DataType = "boolean", ParameterType = "query", Required = false),
        ]
        public async Task<IHttpActionResult> GetDataProfiles(Guid assetUid)
        {
            var prefix = "DataProfiles.GetDataProfiles => ";
            try
            {
                var queryParams = Request.GetQueryNameValuePairs();

                var validationResult = ValidateDataProfileGetParameters(assetUid, queryParams);

                if (validationResult.StatusCode != HttpStatusCode.OK)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, validationResult.Message)).ConfigureAwait(false);
                }

                var results = await DataProfiles.GetDataProfiles(assetUid, queryParams);

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

        private WorkHttpStatus ValidateDataProfileGetParameters(Guid assetUid, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var isValid = isPageSizeAndNumValid(queryParams);

            if (!string.IsNullOrEmpty(isValid))
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, isValid);
            }

            var asset = AssetRepository.GetAssetByUID(assetUid);

            if (asset == null || (asset.AssetType.Class != AssetTypeClass.BusinessAsset && asset.AssetType.Class != AssetTypeClass.TechnicalAsset))
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest,string.Format(ApiMessages.InvalidAssetUid,assetUid.ToString()));
            }

            if (isValid.Length > 0)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, isValid);
            }            

            if (queryParams.Any(qp => qp.Key.ToLower() == "_includetotal"))
            {
                if (!bool.TryParse(queryParams.FirstOrDefault(q => q.Key.ToLower() == "_includetotal").Value, out bool includeTotal))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.InvalidIncludeTotal);
                }
            }

            if (queryParams.Any(qp => qp.Key.ToLower() == "_startdate"))
            {

                if (!DateTime.TryParse(queryParams.FirstOrDefault(qp => qp.Key.ToLower() == "_startdate").Value, out DateTime endDate))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.InvalidStartDate);
                }

            }

            if (queryParams.Any(qp => qp.Key.ToLower() == "_enddate"))
            {

                if (!DateTime.TryParse(queryParams.FirstOrDefault(qp => qp.Key.ToLower() == "_enddate").Value, out DateTime endDate))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.InvalidEndDate);
                }
            }

            if (queryParams.Any(qp => qp.Key.ToLower() == "_includechildassets"))
            {
                if (!bool.TryParse(queryParams.FirstOrDefault(qp => qp.Key.ToLower() == "_includechildassets").Value, out bool includeTotal))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.Invalid_includeChildAssetsProvided);
                }
            }

            if (queryParams.Any(qp => qp.Key.ToLower() == "_includesamples"))
            {
                if (!bool.TryParse(queryParams.FirstOrDefault(q => q.Key.ToLower() == "_includesamples").Value, out bool includeTotal))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format(ApiMessages.InvalidParameterMessage, queryParams.FirstOrDefault(q => q.Key.ToLower() == "_includesamples").Value, "_includeSamples"));
                }
            }

            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }

        /// <summary>
        /// Provides support for adding Data Profile records.
        /// </summary>
        /// <param name="models">Data Profile record collection.</param>
        /// <returns>Results response stating the success or failure or the request.</returns>
        [
            HttpPost,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Adding Data Profile Records.", typeof(List<DataProfileUpsertResponse>)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> PostDataProfiles(List<DataProfileUpsertModel> models)
        {
            var prefix = "DataProfiles.PostDataProfiles => ";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, NOT_AUTHORIZED_MESSAGE)).ConfigureAwait(false);
                }

                var validationResult = ValidateDataProfileUpsertRequest(models, true);
                if (validationResult.StatusCode != HttpStatusCode.OK)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, validationResult.Message)).ConfigureAwait(false);
                }

                if (models.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest,string.Format(DataProfileAPIMessages.DataProfileRecordsLimit,MAX_SYNCHRONOUS_API_ITEM_COUNT.ToString(),MAX_SYNCHRONOUS_API_ITEM_COUNT.ToString()))).ConfigureAwait(false);
                }

                var execution = getApiExecution(models.Count);

                var results = DataProfiles.UpsertDataProfiles(models, execution, true);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string> {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError,ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Provides support for updating Data Profile records.
        /// </summary>
        /// <param name="models">Data Profile record collection.</param>
        /// <returns>Results response stating the success or failure or the request.</returns>
        [
            HttpPut,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Updating Data Profile Records.", typeof(List<DataProfileUpsertResponse>)),
            SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> PutDataProfiles(List<DataProfileUpsertModel> models)
        {
            var prefix = "DataProfiles.PutDataProfiles => ";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, NOT_AUTHORIZED_MESSAGE)).ConfigureAwait(false);
                }

                var validationResult = ValidateDataProfileUpsertRequest(models, false);
                if (validationResult.StatusCode != HttpStatusCode.OK)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, validationResult.Message)).ConfigureAwait(false);
                }

                if (models.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest,string.Format(DataProfileAPIMessages.DataProfileRecordsLimit,MAX_SYNCHRONOUS_API_ITEM_COUNT.ToString(),MAX_SYNCHRONOUS_API_ITEM_COUNT.ToString()))).ConfigureAwait(false);
                }

                var execution = getApiExecution(models.Count);

                var results = DataProfiles.UpsertDataProfiles(models, execution, false);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string> {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError,ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Removes Data Profile results for a given asset. 
        /// </summary>
        /// <param name="assetUid">The unique identifier of an asset.</param>
        /// <param name="startDate">Start date of data profile data to be deleted. Expected date format is yyyy-MM-dd</param>
        /// <param name="endDate">End date of data profile data to be deleted. Expected date format is yyyy-MM-dd</param>
        /// <param name="cascade">True/false flag used to indicate if assets children should be deleted.</param>
        /// <returns>Results response with the count of records deleted.</returns>
        [
            HttpDelete,
            Route("{assetUID:Guid}/{startDate}/{endDate}/{cascade}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Count of Data Profile Records Deleted.", typeof(int)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> DeleteDataProfiles(Guid assetUid, DateTime startDate, DateTime endDate, bool cascade)
        {
            var prefix = "DataProfiles.PostDataProfiles => ";
            var execution = getApiExecution(1);

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, NOT_AUTHORIZED_MESSAGE)).ConfigureAwait(false);
                }

                Asset asset = AssetRepository.GetAssetByUID(assetUid);

                if (asset == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest,string.Format(ApiMessages.InvalidAssetUid,assetUid.ToString()))).ConfigureAwait(false);
                }

                var recordCount = Company.AssetDataProfile.Count(x => x.ID == asset.ID && x.ProfileSetDate >= startDate.Date && x.ProfileSetDate <= endDate.Date);

                if (recordCount > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest,ApiMessages.InvalidRequest,string.Format(DataProfileAPIMessages.DataProfileDeleteMaxLimit,MAX_SYNCHRONOUS_API_ITEM_COUNT.ToString()))).ConfigureAwait(false);
                }

                if (startDate > endDate)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.StartEndDateValidation)).ConfigureAwait(false);
                }

                var results = DataProfiles.DeleteDataProfiles(asset, startDate, endDate, execution, cascade);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results.FirstOrDefault().DeletedCount));
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string> {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError,ApiMessages.InternalServerError, errorMessage)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Provides support for adding a large set of Data Profile records.
        /// </summary>
        /// <param name="models">Data Profile record collection.</param>
        /// <returns>Results response containing the ExecutionID of the request.</returns>
        [
            HttpPost,
            Route("batch"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution ID to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> PostBulkDataProfilesAsync(List<DataProfileUpsertModel> models)
        {
            var prefix = "DataProfiles.PostBulkDataProfilesAsync => ";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, NOT_AUTHORIZED_MESSAGE)).ConfigureAwait(false);
                }

                List<ValidationResult> validationResults = new List<ValidationResult>();
                foreach (var model in models)
                {
                    bool isValid = Validator.TryValidateObject(model, new ValidationContext(model, serviceProvider: null, items: null), validationResults, true);
                    if (!isValid)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, validationResults.First().ErrorMessage)).ConfigureAwait(false);
                    }
                }

                var execution = getApiExecution(models.Count);

                ApiExecutionInfo executionInfo = await DataProfiles.PostBatchDataProfiles(models, execution);

                var result = Request.CreateResponse(
                            HttpStatusCode.OK,
                            new ApiExecutionRecievedResponse
                            {
                                ExecutionID = executionInfo.ExecutionID,
                                Message = "Now processing request. Please check back with this ExecutionID for status.",
                                Uri = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Host}/api/v2/executions/{executionInfo.ExecutionID}"
                            });

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(result)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string> {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError,ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Provides support for updating a large set of Data Profile records.
        /// </summary>
        /// <param name="models">Data Profile record collection.</param>
        /// <returns>Results response containing the ExecutionID of the request.</returns>
        [
            HttpPut,
            Route("batch"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution ID to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> PutBulkDataProfilesAsync(List<DataProfileUpsertModel> models)
        {
            var prefix = "DataProfiles.PutBulkDataProfilesAsync => ";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, NOT_AUTHORIZED_MESSAGE)).ConfigureAwait(false);
                }

                List<ValidationResult> validationResults = new List<ValidationResult>();
                foreach (var model in models)
                {
                    bool isValid = Validator.TryValidateObject(model, new ValidationContext(model, serviceProvider: null, items: null), validationResults, true);
                    if (!isValid)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, validationResults.First().ErrorMessage)).ConfigureAwait(false);
                    }
                }

                var execution = getApiExecution(models.Count);

                ApiExecutionInfo executionInfo = await DataProfiles.PutBatchDataProfiles(models, execution);

                var result = Request.CreateResponse(
                            HttpStatusCode.OK,
                            new ApiExecutionRecievedResponse
                            {
                                ExecutionID = executionInfo.ExecutionID,
                                Message = "Now processing request. Please check back with this ExecutionID for status.",
                                Uri = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Host}/api/v2/executions/{executionInfo.ExecutionID}"
                            });

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(result)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string> {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError,ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Provides support for deleting a large set of Data Profile records.
        /// </summary>
        /// <param name="models">Data Profile Delete request collection. Note: Expected date format is yyyy-MM-dd</param>
        /// <returns>Results response containing the ExecutionID of the request.</returns>
        [
            HttpDelete,
            Route("batch"),
            SwaggerRequestExample(typeof(AssetInsert), typeof(AssetInsertsExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution ID to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> DeleteBulkDataProfilesAsync(List<AssetDataProfileDeleteModel> models)
        {
            var prefix = "DataProfiles.DeleteBulkDataProfilesAsync => ";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                { 
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, NOT_AUTHORIZED_MESSAGE)).ConfigureAwait(false);
                }

                var execution = getApiExecution(models.Count);

                ApiExecutionInfo executionInfo = await DataProfiles.DeleteBatchDataProfiles(models, execution);

                var result = Request.CreateResponse(
                            HttpStatusCode.OK,
                            new ApiExecutionRecievedResponse
                            {
                                ExecutionID = executionInfo.ExecutionID,
                                Message = "Now processing request. Please check back with this ExecutionID for status.",
                                Uri = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Host}/api/v2/executions/{executionInfo.ExecutionID}"
                            });

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(result)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string> {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError,ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Retrieves a list of assets that match a given asset.
        /// </summary>
        /// <param name="assetUid">The unique identifier of an asset.</param>
        /// <param name="similarType">Type of signature to match, Data or Structure.</param>
        /// <returns>A list of matching asset uids associatedwith asset paths</returns>
        [
            HttpGet,            
            Route("{assetUid:Guid}/similar/{similarType}/"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(AssetDataProfilesMatchingAssetsApiViewModel)),
            SwaggerProduces("application/json", "application/octet-stream"),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that a record could not be found based on the supplied Uid, possibly due to an incorrectly formatted identifier (uid) or when a data profile record does not exist for the supplied asset.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),            
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 250. Maximum page size is 10,000", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Allows you to disable including the count of the total number of results across pages in the response.  The default is true meaning the total count is included.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending and are sorted on the asset path value", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_simpleFilter", "The text or phrase you want to find within path field or tags. Filtering is done using 'Starts with' logic. Asterisk (*) symbol can be used as a wild card character to match any character.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_filter", ADVANCED_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by. Allowed values are 'path' and 'tags'. By default the results are ordered by asset path value.", DataType = "string", ParameterType = "query", Required = false),
        ]
        public async Task<IHttpActionResult> GetMatchingAssets(Guid assetUid, string similarType)
        {
            var prefix = "DataProfiles.GetMatchingAssets => ";

            try
            {                
                var queryParams = Request.GetQueryNameValuePairs();

                var isStreamResponse = Request?.Headers?.Accept?.Any(a => a.MediaType == "application/octet-stream") ?? false;

                var validationResult = ValidateMatchAssetGetParameters(assetUid, similarType, queryParams);

                if (validationResult.StatusCode != HttpStatusCode.OK)
                {
                    return await Task.FromResult(errorMessageResponse(validationResult.StatusCode, validationResult.Error, validationResult.Message)).ConfigureAwait(false);
                }                

                HttpResponseMessage response;

                if (isStreamResponse)
                {
                    var results = await DataProfiles.GetMatchedAssetsForExport(assetUid, similarType, queryParams).ConfigureAwait(false);

                    int pageNum = Company.ParsePageNumber(queryParams, 1);
                    int pageSize = Company.ParsePageSize(queryParams, 200000);
                    var assetPath = AssetRepository.GetAssetPath(assetUid);

                    SLDocument document = CreateResponseDocumentForExport(results.ToList(), similarType, pageNum, pageSize);
                    var stream = new MemoryStream();
                    document.SaveAs(stream);
                    byte[] bytes = stream.ToArray();
                    var filename = $"Filtered {assetPath.Result[0].Key[0]} {{0}} Fields List _{DateTime.Now:ddd MMM dd yyyy}_.xlsx";
                   
                    if (similarType.Equals("data", StringComparison.InvariantCultureIgnoreCase)){
                        filename = string.Format(DataProfileAPIMessages.MatchedAssetExportFileName, assetPath.Result[0].Key[0], "Duplicate", DateTime.Now.ToString("ddd MMM dd yyyy"));
                    }
                    else
                    {
                        filename = string.Format(filename, "Similar");
                    }

                    response = createFileResponseMessage(HttpStatusCode.OK, filename, bytes);                    
                }
                else
                {
                    var results = await DataProfiles.GetMatchingAssets(assetUid, similarType, queryParams).ConfigureAwait(false);
                    response = Request.CreateResponse(HttpStatusCode.OK, results);
                }

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(response)).ConfigureAwait(false);
            }
            catch (FilterExpressionParserException ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.FilterExpressionParseError, errorMessage)).ConfigureAwait(false);
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

        /// <summary>
        /// Retrieves the count of assets that match a given asset.
        /// </summary>
        /// <param name="assetUid">The unique identifier of an asset.</param>
        /// <param name="similarType">Type of signature to match, Data or Structure.</param>
        /// <returns>A count of assets that match the given asset</returns>
        [
            HttpGet,
            Route("{assetUid:Guid}/similar/{similarType}/count"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(long)),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that a record could not be found based on the supplied Uid, possibly due to an incorrectly formatted identifier (uid) or when a data profile record does not exist for the supplied asset.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerParameter("_simpleFilter", "The text or phrase you want to find within fields. Filtering is done using 'Starts with' logic. Asterisk (*) symbol can be used as a wild card character to match any character.", DataType = "string", ParameterType = "query", Required = false),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetMatchingAssetCount(Guid assetUid, string similarType)
        {
            var prefix = "DataProfiles.GetMatchingAssetCount => ";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();

                var validationResult = ValidateMatchAssetGetParameters(assetUid, similarType, queryParams);

                if (validationResult.StatusCode != HttpStatusCode.OK)
                {
                    return await Task.FromResult(errorMessageResponse(validationResult.StatusCode, validationResult.Error, validationResult.Message)).ConfigureAwait(false);
                }

                var results = await DataProfiles.GetMatchingAssets(assetUid, similarType, queryParams, true).ConfigureAwait(false);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results.total));                
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string> {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError,ApiMessages.InternalServerError, errorMessage)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Retrieves a list of assets of the given type with the specified confidence.
        /// </summary>
        /// <param name="typeQualifier">Semantic Type to retrive results for.</param>
        /// <param name="minConfidence">Minimum Confidence that profile records must match or exceed.</param>
        /// <returns>A list of matching asset uids associated with asset paths and confidence</returns>
        [
            HttpGet,
            Route("type/{typeQualifier}/{minConfidence}"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(AssetDataProfileByTypeQualifierApiViewModel)),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),            
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 250. Maximum page size is 10,000", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Allows you to disable including the count of the total number of results across pages in the response.  The default is true meaning the total count is included.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending and are sorted on the asset path value", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by. Allowed values are 'confidence' and 'path'. By default the results are ordered by asset path value.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_filter", ADVANCED_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_simpleFilter", "The text or phrase you want to find within path or assetTypePath fields. Filtering is done using 'Starts with' logic. Asterisk (*) symbol can be used as a wild card character to match any character.", DataType = "string", ParameterType = "query", Required = false),
        ]
        public async Task<IHttpActionResult> GetAssetsByTypeQualifier(string typeQualifier, Decimal minConfidence)
        {
            var prefix = "DataProfiles.GetMatchingAssets => ";

            try {
                var queryParams = Request.GetQueryNameValuePairs();

                var isValid = isPageSizeAndNumValid(queryParams);
                if (!string.IsNullOrEmpty(isValid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, isValid)).ConfigureAwait(false);
                }

                if (queryParams.Any(qp => qp.Key.ToLower() == "_direction"))
                {
                    string[] allowedValues = new[] { "asc", "desc" };
                    var directionFilter = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction").Value.Trim().ToLower();

                    if (!allowedValues.Contains(directionFilter))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.InvalidDirection)).ConfigureAwait(false);
                    }
                }

                if (queryParams.Any(qp => qp.Key.ToLower() == "_order"))
                {
                    string[] allowedValues = new[] { "confidence", "path" };
                    var directionFilter = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_order").Value.Trim().ToLower();

                    if (!allowedValues.Contains(directionFilter))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, DataProfileAPIMessages.OrderInvalid)).ConfigureAwait(false);
                    }
                }

                if (string.IsNullOrEmpty(typeQualifier) || typeQualifier.Length > 200)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, DataProfileAPIMessages.TypeQualifierInvalid)).ConfigureAwait(false);
                }
                if (minConfidence <= 0 || minConfidence > 1)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, DataProfileAPIMessages.MinConfidenceInvalid)).ConfigureAwait(false);
                }

                var results = await DataProfiles.GetAssetsByTypeQualifier(typeQualifier, minConfidence, queryParams).ConfigureAwait(false);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results));
            }
            catch (FilterExpressionParserException ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.FilterExpressionParseError, errorMessage)).ConfigureAwait(false);
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

        public WorkHttpStatus ValidateDataProfileUpsertRequest(List<DataProfileUpsertModel> models, bool IsInsert)
        {
            if (models == null || models.Count == 0)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.JSONValidMessage);
            }

            //Key Field Validation
            if (models.Any(dp => dp.profileSetDate == null || dp.assetUid == null))
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, BAD_REQUEST_GENERIC_MESSAGE);
            }
            var dupRecords = models.GroupBy(i => new { i.assetUid, i.profileSetDate }).Where(i => i.Count() > 1).Select(i => new { keyFields = i.Key, Count = i.Count() }).ToList();
            if (dupRecords.Any())
            {
                var ErrorMessage = string.Format(DataProfileAPIMessages.DuplicateRecordBatchProfile, string.Join(", ", dupRecords.Select(i => $"(AssetUid: {i.keyFields.assetUid}, ProfileSetDate: {i.keyFields.profileSetDate.Date: yyyy-MM-dd})")));
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ErrorMessage);
            }

            List<ValidationResult> validationResults = new List<ValidationResult>();
            foreach (var model in models)
            {
                validationResults.Clear();
                Asset asset = AssetRepository.GetAssetByUID(model.assetUid);

                if (asset == null)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest,string.Format(ApiMessages.InvalidAssetUid,model.assetUid.ToString()));
                }

                if (asset.AssetType.Class != AssetTypeClass.BusinessAsset && asset.AssetType.Class != AssetTypeClass.TechnicalAsset)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest,string.Format(DataProfileAPIMessages.ProfilingNotSupportAssetClass,asset.AssetType.Class.ToString()));
                }

                var profileSetDate = model.profileSetDate.Date;                
                var recordExists = Company.AssetDataProfile.Any(x => x.AssetId == asset.ID && DbFunctions.TruncateTime(x.ProfileSetDate) == profileSetDate);
                //check insert
                if (recordExists && IsInsert)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest,string.Format(DataProfileAPIMessages.ProfileRecordAlreadyExists,model.assetUid.ToString(),model.profileSetDate.Date.ToString("yyyy-MM-dd")));
                }
                //check update
                if (!recordExists && !IsInsert)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format(DataProfileAPIMessages.AssetUidProfileSetDateRecordNotfound,model.assetUid.ToString(),model.profileSetDate.Date.ToString("yyyy-MM-dd")));
                }

                if (model.topK !=null && model.topK.Any(x=> x.Trim() == string.Empty))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest,DataProfileAPIMessages.ElementTopKNotEmpty);
                }

                if (model.bottomK != null && model.bottomK.Any(x => x.Trim() == string.Empty))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest,DataProfileAPIMessages.ElementBottomKNotEmpty);
                }

                bool isValid = Validator.TryValidateObject(model, new ValidationContext(model, serviceProvider: null, items: null), validationResults, true);
                if (!isValid)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, validationResults.First().ErrorMessage);
                }
            }
            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }        

        private WorkHttpStatus ValidateMatchAssetGetParameters(Guid assetUid, string similarType, IEnumerable<KeyValuePair<string, string>> queryParams)
        {            
            var asset = AssetRepository.GetAssetByUID(assetUid);            

            if (asset == null || (asset.AssetType.Class != AssetTypeClass.BusinessAsset && asset.AssetType.Class != AssetTypeClass.TechnicalAsset))
            {
                return new WorkHttpStatus(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ApiMessages.InvalidAssetUid, assetUid));
            }

            if (!Company.HasAssetPermission(asset.Object, asset.ObjectID, Permission.ReadAsset))
            {
                return new WorkHttpStatus(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ApiMessages.InvalidAssetUid, assetUid));
            }

            var isValid = isPageSizeAndNumValid(queryParams);

            if (!string.IsNullOrEmpty(isValid))
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, isValid);
            }

            if (queryParams.Any(qp => qp.Key.ToLower() == "_includetotal"))
            {
                if (!bool.TryParse(queryParams.FirstOrDefault(q => q.Key.ToLower() == "_includetotal").Value, out bool includeTotal))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.InvalidIncludeTotal);
                }
            }

            if (queryParams.Any(qp => qp.Key.ToLower() == "_order"))
            {
                string[] allowedValues = new[] { "path", "tags" };
                var directionFilter = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_order").Value.Trim().ToLower();

                if (!allowedValues.Contains(directionFilter))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, DataProfileAPIMessages.InvalidOrder);
                }
            }

            if (queryParams.Any(qp => qp.Key.ToLower() == "_direction"))
            {
                string[] allowedValues = new [] { "asc", "desc" };
                var directionFilter = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction").Value.Trim().ToLower();

                if (!allowedValues.Contains(directionFilter))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.InvalidDirection);
                }
            }

            if (similarType != null)
            {
                string[] allowedValues = new [] { "structure", "data" };

                if (!allowedValues.Contains(similarType.ToLowerInvariant()))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format(DataProfileAPIMessages.InvalidSimilarType, similarType));
                }
            }
            else
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, DataProfileAPIMessages.RequiredSimilarType);
            }

            AssetDataProfile dataprofile = Company.AssetDataProfile.Where(x => x.AssetId == asset.ID).OrderByDescending(x => x.ProfileSetDate).FirstOrDefault();
            if (dataprofile == null || similarType.ToLowerInvariant() == "structure" && dataprofile.StructureSignature == null || similarType.ToLowerInvariant() == "data" && dataprofile.DataSignature == null)
            {
                return new WorkHttpStatus(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(DataProfileAPIMessages.NoSimilarTypeForAssetUid, similarType, assetUid));
            }

            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }

        /// <summary>
        /// Create the Excel document for export
        /// </summary>
        /// <returns>A spreadsheet populated with the details of the data profile results</returns>
        private SLDocument CreateResponseDocumentForExport(List<DataProfileExportModel> dataProfiles, string similarType, int pageNum, int pageSize)
        {                                  
            SLDocument doc = new SLDocument();
            string assetSheetName = DataProfileAPIMessages.AssetSheetName;
            string apiSheetName = DataProfileAPIMessages.ApiSheetName;
            string matchType;

            if (similarType.Equals("data", StringComparison.InvariantCultureIgnoreCase))
            {
                matchType = DataProfileAPIMessages.Duplicate;
            }
            else
            {
                matchType = DataProfileAPIMessages.Similar;
            }

            doc.RenameWorksheet(SLDocument.DefaultFirstSheetName, assetSheetName);

            doc.AddWorksheet(apiSheetName);
            doc.SelectWorksheet(apiSheetName);

            doc.SetCellValue(1, 1, "pageSize");
            doc.SetCellValue(1, 2, pageSize);
            doc.SetCellValue(2, 1, "pageNum");
            doc.SetCellValue(2, 2, pageNum);

            doc.SelectWorksheet(assetSheetName);

            #region Create the list sheet

            SLStyle noTagFieldStyle = new SLStyle();
            noTagFieldStyle.Font.FontColor = System.Drawing.ColorTranslator.FromHtml("#a0a3ad");

            #region Header
            int index = 1;
            int rowNumber = 1;

            doc.SetCellValue(rowNumber, index++, DataProfileAPIMessages.NameColumn);
            doc.SetCellValue(rowNumber, index++, DataProfileAPIMessages.TagsColumn);
            doc.SetCellValue(rowNumber, index++, DataProfileAPIMessages.AssetPathColumn);
            doc.SetCellValue(rowNumber, index++, DataProfileAPIMessages.AssetTypePathColumn);
            doc.SetCellValue(rowNumber, index++, string.Format(DataProfileAPIMessages.MatchedAssetNameColumn, matchType));
            doc.SetCellValue(rowNumber, index++, string.Format(DataProfileAPIMessages.MatchedAssetTagsColumn, matchType));
            doc.SetCellValue(rowNumber, index++, string.Format(DataProfileAPIMessages.MatchedAssetPathColumn, matchType));
            doc.SetCellValue(rowNumber, index++, string.Format(DataProfileAPIMessages.MatchedAssetTypePathColumn, matchType));
            doc.SetCellValue(rowNumber, index++, DataProfileAPIMessages.AssetUidColumn);
            doc.SetCellValue(rowNumber, index++, DataProfileAPIMessages.AssetIdColumn);
            doc.SetCellValue(rowNumber, index++, DataProfileAPIMessages.AssetUrlColumn);
            doc.SetCellValue(rowNumber, index++, string.Format(DataProfileAPIMessages.MatchedAssetUidColumn, matchType));
            doc.SetCellValue(rowNumber, index++, string.Format(DataProfileAPIMessages.MatchedAssetIdColumn, matchType));
            doc.SetCellValue(rowNumber, index, string.Format(DataProfileAPIMessages.MatchedAssetUrlColumn, matchType));

            #endregion
            #region Body
            foreach (var row in dataProfiles)
            {
                index = 1;
                rowNumber++;
                doc.SetCellValue(rowNumber, index++, row.AssetPath.Split('>').Last());
                doc.SetCellValue(rowNumber, index++, row.AssetTags);
                doc.SetCellValue(rowNumber, index++, row.AssetPath);
                doc.SetCellValue(rowNumber, index++, row.AssetTypePath);
                doc.SetCellValue(rowNumber, index++, row.MatchedAssetPath.Split('>').Last());
               
                if (row.hasTagField)
                {
                    doc.SetCellValue(rowNumber, index++, row.MatchedAssetTags);                    
                }
                else
                {
                    doc.SetCellValue(rowNumber, index, DataProfileAPIMessages.TagFieldNotFound);
                    doc.SetCellStyle(rowNumber, index++, noTagFieldStyle);
                }
                
                doc.SetCellValue(rowNumber, index++, row.MatchedAssetPath);
                doc.SetCellValue(rowNumber, index++, row.MatchedAssetTypePath);                
                doc.SetCellValue(rowNumber, index++, row.AssetUid.ToString());
                doc.SetCellValue(rowNumber, index++, row.AssetID);
                doc.SetCellValue(rowNumber, index++, $"asset/{row.AssetUid}");
                doc.SetCellValue(rowNumber, index++, row.MatchedAssetUid.ToString());
                doc.SetCellValue(rowNumber, index++, row.MatchedAssetID);
                doc.SetCellValue(rowNumber, index, $"asset/{row.MatchedAssetUid}");                
                
            }
            doc.AutoFitColumn(1, 14);
            #endregion
            #endregion
            return doc;
        }

        #endregion

        #region Semantic Types

        /// <summary>
        /// Gets a list of semantic types for use in data profiling.
        /// </summary>
        /// <remarks>
        /// You may using the `_filter` parameter with the following fields:
        ///  - **name**
        ///  - **description**
        ///  - **qualifier**
        ///  - **status**
        ///  - **source**
        ///  - **threshold**
        ///  - **priority**
        ///  - **baseType**
        ///  - **effectiveDate**
        /// </remarks>
        /// <returns>A list of semantic types based on the provided filtering and sorting criteria.</returns>
        [
            HttpGet,
            Route("semantictypes"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default (and maximum) value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the semantic types are ordered by Qualifier.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_simpleFilter", "The text or phrase you want to find within the listable fields of a semantic type. Filtering is done using 'Starts with' logic. Asterisk (*) symbol can be used as a wild card character to match any character.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_filter", ADVANCED_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("asOfEffectiveDate", "Assumed to be current UTC date if left empty, otherwise, gets semantic types as of the specified effective date, and nothing later. This is the parameter used to get prior versions.", DataType = "datetime", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of semantic types.", typeof(GetSemantics)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetSemanticTypes(CancellationToken cancellationToken)
        {
            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var apiModels = await SemanticsRepository.GetSemanticsAsync(queryParams, cancellationToken);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, apiModels));
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch (FilterExpressionParserException ex)
            {
                throw new GenericException(HttpStatusCode.BadRequest, "Invalid Filter Configuration", ex.Message);
            }
            catch
            {
                return errorMessageResponse(
                    HttpStatusCode.InternalServerError,
                    "Error retrieving semantic types",
                    ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        /// <summary>
        /// Gets a list of versions for a given semantic type qualifier.
        /// </summary>
        /// <returns>A list of semantic type versions.</returns>
        [
            HttpGet,
            Route("semantictypes/{qualifier}/versions"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the semantic types are ordered by Qualifier.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered descending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of semantic types.", typeof(List<GetSemantic>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetSemanticTypeVersions(string qualifier, CancellationToken cancellationToken)
        {
            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var responseModels = await SemanticsRepository.GetSemanticVersionsByQualifierAsync(qualifier, queryParams, cancellationToken);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, responseModels));
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error retrieving semantic types", ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        /// <summary>
        /// Gets a list of semantic base types.
        /// </summary>
        [
            HttpGet,
            Route("semantictypes/lookups/basetypes"),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of semantic base types.", typeof(List<SemanticBaseTypeInfo>)),
        ]
        public IHttpActionResult GetSemanticTypeBaseTypes()
        {
            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, SemanticBaseType.LocalDate.GetAsList()));
        }

        /// <summary>
        /// Gets a list of semantic type base types.
        /// </summary>
        [
            HttpGet,
            Route("semantictypes/lookups/matchtypes"),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of semantic type match types.", typeof(List<SemanticMatchTypeInfo>)),
        ]
        public IHttpActionResult GetSemanticTypeMatchTypes()
        {
            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, SemanticMatchType.Pattern.GetAsList()));
        }

        /// <summary>
        /// Gets a list of semantic type statuses.
        /// </summary>
        [
            HttpGet,
            Route("semantictypes/lookups/statuses"),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of semantic type statuses.", typeof(List<SemanticStatusInfo>)),
        ]
        public IHttpActionResult GetSemanticTypeStatuses()
        {
            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, SemanticStatus.Draft.GetAsList()));
        }

        /// <summary>
        /// Selectively updates one or more semantic types based on the fields provided. 
        /// If certain fields that make up a semantic type are missing from your request payload, then those fields will not be updated.
        /// </summary>
        /// <remarks>
        /// For Built-in semantic types, you may only update the following properties:
        ///  - **name**
        ///  - **description**
        ///
        /// Minimum and Maximum properties, if provided, must fall within the range: -999999999999.999999 to 999999999999.999999
        ///
        /// For a list of possible values for the following fields, check the relevant endpoint:
        ///  - **baseType** : /api/v2/dataprofiles/semantictypes/lookups/basetypes
        ///  - **matchType** : /api/v2/dataprofiles/semantictypes/lookups/matchtypes
        ///  - **status** : /api/v2/dataprofiles/semantictypes/lookups/statuses
        /// </remarks>
        /// <returns>A list of semantic types you updated.</returns>
        [
            HttpPatch,
            Route("semantictypes"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(List<PatchSemantic>), typeof(PatchSemanticExample1)),
            SwaggerRequestExample(typeof(List<PatchSemantic>), typeof(PatchSemanticExample2)),
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding semantic types.", typeof(List<GetSemantic>)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "One or more semantic types were not found based on the provided qualifiers.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request to update these semantic types is invalid, given the reason specified in the error message.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PatchSemanticTypes(List<PatchSemantic> requestModels)
        {
            const string ERROR_HEADING = "Error patching semantic types";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Forbidden, ERROR_HEADING, ApiMessages.EndpointNotAuthorizedMessage);
                }

                var responseModels = await SemanticsRepository.PatchSemanticsAsync(requestModels);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, responseModels));
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error updating semantic types", ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }


        /// <summary>
        /// Creates one or more user-defined semantic types.
        /// </summary>
        /// <remarks>
        /// For a list of possible values for the following fields, check the relevant endpoint:
        ///  - **baseType** : /api/v2/dataprofiles/semantictypes/lookups/basetypes
        ///  - **matchType** : /api/v2/dataprofiles/semantictypes/lookups/matchtypes
        ///  - **status** : /api/v2/dataprofiles/semantictypes/lookups/statuses
        ///
        /// Minimum and Maximum properties, if provided, must fall within the range: -999999999999.999999 to 999999999999.999999
        /// </remarks>
        /// <returns>A list of field types corresponding to the given criteria, if any.</returns>
        [
            HttpPost,
            Route("semantictypes"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(List<PostSemantic>), typeof(PostSemanticExample1)),
            SwaggerRequestExample(typeof(List<PostSemantic>), typeof(PostSemanticExample2)),
            SwaggerResponse(HttpStatusCode.Created, "Returns the corresponding semantic types.", typeof(List<GetSemantic>)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request to insert these semantic types is invalid, given the reason specified in the error message.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostSemanticTypes(List<PostSemantic> requestModels)
        {
            const string ERROR_HEADING = "Error adding semantic types";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Forbidden, ERROR_HEADING, ApiMessages.EndpointNotAuthorizedMessage);
                }

                var responseModels = await SemanticsRepository.PostSemanticsAsync(requestModels);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Created, responseModels));
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ERROR_HEADING, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        /// <summary>
        /// Updates one or more user-defined semantic types. Built-in semantic types may not be updated using this endpoint.
        /// </summary>
        /// <remarks>
        /// For a list of possible values for the following fields, check the relevant endpoint:
        ///  - **baseType** : /api/v2/dataprofiles/semantictypes/lookups/basetypes
        ///  - **matchType** : /api/v2/dataprofiles/semantictypes/lookups/matchtypes
        ///  - **status** : /api/v2/dataprofiles/semantictypes/lookups/statuses
        ///
        /// Minimum and Maximum properties, if provided, must fall within the range: -999999999999.999999 to 999999999999.999999
        /// </remarks>
        /// <returns>A list of updated semantic types.</returns>
        [
            HttpPut,
            Route("semantictypes"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(List<PutSemantic>), typeof(PutSemanticExample1)),
            SwaggerRequestExample(typeof(List<PutSemantic>), typeof(PutSemanticExample2)),
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding semantic types.", typeof(List<GetSemantic>)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "One or more semantic types were not found based on the provided qualifiers.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request to update these semantic types is invalid, given the reason specified in the error message.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutSemanticTypes(List<PutSemantic> requestModels)
        {
            const string ERROR_HEADING = "Error updating semantic types";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Forbidden, ERROR_HEADING, ApiMessages.EndpointNotAuthorizedMessage);
                }

                var reponseModels = await SemanticsRepository.PutSemanticsAsync(requestModels);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, reponseModels));
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ERROR_HEADING, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        /// <summary>
        /// Deletes a semantic type, provided it is not currently referenced in any asset data profiles.
        /// </summary>
        /// <remarks>
        /// This action will remove all versions of the semantic type.
        /// </remarks>
        /// <returns>A confirmation response.</returns>
        [
            HttpDelete,
            Route("semantictypes/{qualifier}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Returns a success message.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Your semantic type was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Conflict, "Request to remove this semantic type is invalid, possibly due to being used on one or more data profiles.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteSemanticType(string qualifier)
        {
            const string ERROR_HEADING = "Error deleting semantic type";
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Forbidden, ERROR_HEADING, ApiMessages.EndpointNotAuthorizedMessage);
                }

                var status = await SemanticsRepository.DeleteSemanticAsync(qualifier);

                return ResponseMessage(Request.CreateResponse(status, new ConfirmResponse { message = "Semantic type removed." }));
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ERROR_HEADING, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        #endregion
    }
}