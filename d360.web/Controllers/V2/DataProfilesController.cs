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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers.V2
{
    [ApiVersion("2.0"), RoutePrefix("api/v{version:apiVersion}/dataprofiles"), Authorize]
    public class DataProfilesController : BaseV2ApiController
    {
        internal IDataProfileRepository DataProfiles;
        internal IAssetRepository AssetRepository;

        public DataProfilesController(ICommunityContext community, ICompanyContext company, IDataProfileRepository dataProfileRepository, IAssetRepository assetRepository, ISettingsRepository settingsRepository)
            : base(community, company, settingsRepository)
        {
            this.DataProfiles = dataProfileRepository;
            this.AssetRepository = assetRepository;
        }

        /// <summary>
        /// Retrieves Data Profile results for a given asset.
        /// </summary>
        /// <param name="assetUid">The unique identifier of an asset.</param>
        /// <returns>A list of Data Profile results</returns>
        [
            HttpGet,
            Route("{assetUid:Guid}"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(AssetDataProfilesApiViewModel)),
            SwaggerProduces("application/json", "text/json", "application/xml", "text/xml", "application/octet-stream"),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error to indicate that your request to retrieve this asset is forbidden due to lack of permissions to view it.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerParameter("_startDate", "Start date to get data profile data for. If _startDate and _endDate are not supplied the date defaults to the most recent date for the specified asset for which there is data. Otherwise the default is current date UTC.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_endDate", "End date to get data profile data for. If _startDate and _endDate are not supplied the date defaults to the most recent date for the specified asset for which there is data. Otherwise the default is current date UTC.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_includeChildAssets", " If true returns the data profile results for all descendant assets of the specified asset for the same date criteria.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 250. Maximum page size is 10,000", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Allows you to disable including the count of the total number of results across pages in the response.  The default is true meaning the total count is included.", DataType = "boolean", ParameterType = "query", Required = false),
        ]
        public async Task<IHttpActionResult> GetDataProfiles(Guid assetUid)
        {
            var prefix = "DataProfiles.GetDataProfiles => ";
            try
            {
                var queryParams = Request.GetQueryNameValuePairs();

                var validationResult = ValidateDataProfileGetParmeters(assetUid, queryParams);

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

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }

        private WorkHttpStatus ValidateDataProfileGetParmeters(Guid assetUid, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var isValid = isPageSizeAndNumValid(queryParams);

            if (!string.IsNullOrEmpty(isValid))
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, isValid);
            }

            var asset = AssetRepository.GetAssetByUID(assetUid);

            if (asset == null || (asset.AssetType.Class != AssetTypeClass.BusinessAsset && asset.AssetType.Class != AssetTypeClass.TechnicalAsset))
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format(ActionApiMessages.AssetUidIsNotValid, assetUid.ToString()));
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
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, DataProfileAPIMessages.InvalidStartDate);
                }

            }

            if (queryParams.Any(qp => qp.Key.ToLower() == "_enddate"))
            {

                if (!DateTime.TryParse(queryParams.FirstOrDefault(qp => qp.Key.ToLower() == "_enddate").Value, out DateTime endDate))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, DataProfileAPIMessages.InvalidEndDate);
                }
            }

            if (queryParams.Any(qp => qp.Key.ToLower() == "_includechildassets"))
            {
                if (!bool.TryParse(queryParams.FirstOrDefault(qp => qp.Key.ToLower() == "_includechildassets").Value, out bool includeTotal))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, DataProfileAPIMessages.InvalidInclChildAssets);
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
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format(DataProfileAPIMessages.MaxDataProfieldRequest, MAX_SYNCHRONOUS_API_ITEM_COUNT.ToString(), MAX_SYNCHRONOUS_API_ITEM_COUNT.ToString()))).ConfigureAwait(false);
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

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
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
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format( DataProfileAPIMessages.MaxDataProfieldRequest, MAX_SYNCHRONOUS_API_ITEM_COUNT.ToString(), MAX_SYNCHRONOUS_API_ITEM_COUNT.ToString()))).ConfigureAwait(false);
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

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Removes Data Profile results for a given asset. 
        /// </summary>
        /// <param name="assetUid">The unique identifier of an asset.</param>
        /// <param name="startDate">Start date of data profile data to be deleted.</param>
        /// <param name="endDate">End date of data profile data to be deleted.</param>
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
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format(ActionApiMessages.AssetUidIsNotValid, assetUid.ToString()))).ConfigureAwait(false);
                }

                var recordCount = Company.AssetDataProfile.Count(x => x.ID == asset.ID && x.ProfileSetDate >= startDate.Date && x.ProfileSetDate <= endDate.Date);

                if (recordCount > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest,ApiMessages.InvalidRequest , string.Format(DataProfileAPIMessages.MaxDataProfieldDelete, MAX_SYNCHRONOUS_API_ITEM_COUNT.ToString()))).ConfigureAwait(false);
                }

                if (startDate > endDate)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest,DataProfileAPIMessages.StartEndDateValidation)).ConfigureAwait(false);
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

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.InternalServerError, errorMessage)).ConfigureAwait(false);
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

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError , errorMessage)).ConfigureAwait(false);
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

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Provides support for deleting a large set of Data Profile records.
        /// </summary>
        /// <param name="models">Data Profile Delete request collection.</param>
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
                                Message = ApiMessages.ExecutionIDStatus,
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

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
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
            SwaggerProduces("application/json"),
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

                var validationResult = ValidateMatchAssetGetParameters(assetUid, similarType, queryParams);

                if (validationResult.StatusCode != HttpStatusCode.OK)
                {
                    return await Task.FromResult(errorMessageResponse(validationResult.StatusCode, validationResult.Error, validationResult.Message)).ConfigureAwait(false);
                }

                var results = await DataProfiles.GetMatchingAssets(assetUid, similarType, queryParams).ConfigureAwait(false);

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

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.InternalServerError, errorMessage)).ConfigureAwait(false);
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
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest,DataProfileAPIMessages.InvalidOrderConfidencePath)).ConfigureAwait(false);
                    }
                }

                if (string.IsNullOrEmpty(typeQualifier) || typeQualifier.Length > 200)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, DataProfileAPIMessages.TypeParameterInvalid)).ConfigureAwait(false);
                }
                if (minConfidence <= 0 || minConfidence > 1)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, DataProfileAPIMessages.MinConfidenceParaInvalid)).ConfigureAwait(false);
                }

                var results = await DataProfiles.GetAssetsByTypeQualifier(typeQualifier, minConfidence, queryParams).ConfigureAwait(false);

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

        public WorkHttpStatus ValidateDataProfileUpsertRequest(List<DataProfileUpsertModel> models, bool IsInsert)
        {
            if (models == null || models.Count == 0)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest,ApiMessages.ErrorInvalidDatasetMessage);
            }

            //Key Field Validation
            if (models.Any(dp => dp.profileSetDate == null || dp.assetUid == null))
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, BAD_REQUEST_GENERIC_MESSAGE);
            }
            var dupRecords = models.GroupBy(i => new { i.assetUid, i.profileSetDate }).Where(i => i.Count() > 1).Select(i => new { keyFields = i.Key, Count = i.Count() }).ToList();
            if (dupRecords.Any())
            {
                var ErrorMessage = $"Duplicate Records: {string.Join(", ", dupRecords.Select(i => $"(AssetUid: {i.keyFields.assetUid}, ProfileSetDate: {i.keyFields.profileSetDate.Date: yyyy-MM-dd})"))}. AssetUid and ProfileSetDate pairs are used as record identifiers and must be unique within a batch.";
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ErrorMessage);
            }

            List<ValidationResult> validationResults = new List<ValidationResult>();
            foreach (var model in models)
            {
                validationResults.Clear();
                Asset asset = AssetRepository.GetAssetByUID(model.assetUid);

                if (asset == null)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format(ActionApiMessages.AssetUidIsNotValid, model.assetUid.ToString()));
                }

                if (asset.AssetType.Class != AssetTypeClass.BusinessAsset && asset.AssetType.Class != AssetTypeClass.TechnicalAsset)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format(DataProfileAPIMessages.AssetClassNotSupportDataProfile, asset.AssetType.Class.ToString()));
                }

                var profileSetDate = model.profileSetDate.Date;                
                var recordExists = Company.AssetDataProfile.Any(x => x.AssetId == asset.ID && DbFunctions.TruncateTime(x.ProfileSetDate) == profileSetDate);
                //check insert
                if (recordExists && IsInsert)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format(DataProfileAPIMessages.ProfileSetDateExists, model.assetUid.ToString(), model.profileSetDate.Date.ToString("yyyy-MM-dd")));
                }
                //check update
                if (!recordExists && !IsInsert)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, string.Format(DataProfileAPIMessages.ProfileSetDateNotExists, model.assetUid.ToString(), model.profileSetDate.Date.ToString("yyyy-MM-dd")));
                }
                
                if(model.topK !=null && model.topK.Any(x=> x.Trim() == string.Empty))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, DataProfileAPIMessages.TopKNotEmpty);
                }

                if (model.bottomK != null && model.bottomK.Any(x => x.Trim() == string.Empty))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, DataProfileAPIMessages.BottomKNotEmpty);
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
    }
}
