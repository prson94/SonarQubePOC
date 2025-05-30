using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.core.resources;
using d360.utils.excel;
using d360.web.Filters;
using d360.web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using repositories;
using SpreadsheetLight;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

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

        public DataProfilesController(
            ICoreComponentSet set,
            IAssetRepository assetRepository,
            IDataProfileRepository dataProfileRepository)
            : base(set)
        {
            AssetRepository = assetRepository;
            DataProfiles = dataProfileRepository;
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
        public async Task<IHttpActionResult> GetDataProfilesByAsset(Guid assetUid)
        {
			var queryParams = Request.GetQueryNameValuePairs();
			var isValid = isPageSizeAndNumValid(queryParams);
			var validationResult = await Catalog.ValidateDataProfileGetParameters(assetUid, queryParams, isValid);

			if (!validationResult.IsSuccess)
			{
				return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, Error.BadRequest, validationResult.Message)).ConfigureAwait(false);
			}


			var results = await Catalog.ReadDataProfilesAsync(assetUid, queryParams);
			return results.IsSuccess ? Ok(results.Data) : errorMessageResponse((HttpStatusCode)results.StatusCode, results.Message);
		}


		/// <summary>
		/// Retrieves Data Profile results for a identifier.
		/// </summary>
		/// <param name="profileIdentifier">The profile identifier.</param>
		/// <returns>A list of Data Profile results</returns>
		[
            HttpGet,
            Route("identifier/{profileIdentifier}"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(AssetDataProfilesApiViewModel)),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error to indicate that your request to retrieve this asset is forbidden due to lack of permissions to view it.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 250. Maximum page size is 10,000", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_assetUid", "Allows filtering results based on an asset uid", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Allows you to disable including the count of the total number of results across pages in the response.  The default is true meaning the total count is included.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_includeSamples", "If true returns the outlierDetail, topK, bottomK, cardinalityDetail, shapesDetail collections on the data profile results. The default is true meaning the collections will be included.", DataType = "boolean", ParameterType = "query", Required = false),
        ]
        public async Task<IHttpActionResult> GetDataProfilesByIdentifier(string profileIdentifier)
        {
			var queryParams = Request.GetQueryNameValuePairs();
			var isValid = isPageSizeAndNumValid(queryParams);
			var validationResult = await Catalog.ValidateDataProfileGetParameters(profileIdentifier, queryParams, isValid);

			if (!validationResult.IsSuccess)
			{
				return await Task.FromResult(errorMessageResponse((HttpStatusCode)validationResult.StatusCode, Error.BadRequest, validationResult.Message)).ConfigureAwait(false);
			}

			var results = await Catalog.ReadDataProfilesAsync(profileIdentifier, queryParams);
			return results.IsSuccess ? Ok(results.Data) : errorMessageResponse((HttpStatusCode)results.StatusCode, results.Message);
		}

        /// <summary>
        /// Retrieves list of unique series contained in an environment.
        /// </summary>
        /// <remarks>
        /// Results can be filtered using the _filter parameter and filter expressions are specified using field name, operator and value. For example city eq 'Redmond'.
        /// *  For comparison operators you can use eq (equal), ne (not equal), gt (greater than), ge (greater than or equal), lt (less than), le (less than or equal) and ct (contains) which allows usage of (*) symbol as wildcard
        ///     
        ///     Example :
        ///     - **Uid comparing operators ** (assetUid)
        ///         - Equals operator - assetUid eq 00000000-0000-0000-0000-000000000000
        ///     - **Comparison Operators**
        ///         - Equals operator - {fieldname} eq 'Data'
        ///         - Not equals operator - {fieldname} ne 'Data'
        ///         - Contains operator - {fieldname} ct 'Data'  
        ///         - Greater than operator - {fieldname} gt 99
        ///         - Greater than or equal operator - {fieldname} ge 99
        ///         - Less than operator - {fieldname} lt 99
        ///         - Less than or equal operator - {fieldname} le 99
        ///         - Not populated operator - {fieldname} eq null
        ///         - populated operator - {fieldname} ne null
        ///         - DateTime Is Before Operator - {DateTimeFieldName} lt 'YYYY-MM-DDTHH24:MI'
        ///         - DateTime Is After Operator - {DateTimeFieldName} gt 'YYYY-MM-DDTHH24:MI'
        ///         - DateTime Is Between Operator - ({DateTimeFieldName} ge 'YYYY-MM-DDTHH24:MI' and {DateTimeFieldName} le 'YYYY-MM-DDTHH24:MI')
        ///     
        ///     - **Logical Operators**
        ///         - Logical and - {fieldname} ge 00 and {fieldname} le 99
        ///         - Logical or - {fieldname} eq 'Data' or {fieldname} eq 'Data1'
        ///         
        ///     - **Profile Type**
        ///         - Full       -  {fieldname} eq 0
        ///         - Sample     -  {fieldname} eq 1
        ///         - Filtered   -  {fieldname} eq 2
        ///         
        /// If the requested content media type is "application/octet-stream", the response will be an Excel document with the asset audit data.
        /// </remarks>
        /// <returns>A list of unique series results</returns>
        [
            HttpGet,
            Route(""),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(AssetDataProfilesApiViewModel)),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error to indicate that your request to retrieve this asset is forbidden due to lack of permissions to view it.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),            
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 250. Maximum page size is 10,000", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Allows you to disable including the count of the total number of results across pages in the response.  The default is true meaning the total count is included.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_includeSamples", "If true returns the outlierDetail, topK, bottomK, cardinalityDetail, shapesDetail collections on the data profile results. The default is true meaning the collections will be included.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_filter", "The filter expression used to filter Profile data by assetUid (Uid),ProfileIdentifier (Text), profileSetDate (DateTime),typeQualifier (Text),type (Text),ftaVersion (Text),freshness (Number),ProfileSource (Text), ProfileSeries (Text) and ProfileType (Number) fields. Asterisk (*) symbol can be used as a wild card character to match any character.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("_includeChildAssets", " If true returns the data profile results for all descendant assets of the specified asset for the same date criteria.", DataType = "boolean", ParameterType = "query", Required = false),
			SwaggerParameter("_assetUid", "Allows filtering results based on an asset uid", DataType = "string", ParameterType = "query", Required = false),
		]
        public async Task<IHttpActionResult> GetDataProfiles()
        {
			var queryParams = Request.GetQueryNameValuePairs();

			var results = await Catalog.ReadDataProfilesAsync(queryParams);
			return results.IsSuccess ? Ok(results.Data) : errorMessageResponse((HttpStatusCode)results.StatusCode, results.Message);
		}

		/// <summary>
		/// Retrieves list of unique series contained in an environment.
		/// </summary>
		/// <remarks>
		/// Results can be filtered using the _filter parameter and filter expressions are specified using field name, operator and value. For example city eq 'Redmond'.
		/// *  For comparison operators you can use eq (equal), ne (not equal), gt (greater than), ge (greater than or equal), lt (less than), le (less than or equal) and ct (contains) which allows usage of (*) symbol as wildcard
		///     
		///     Example :
		///     - **Uid comparing operators ** (assetUid)
		///         - Equals operator - assetUid eq 00000000-0000-0000-0000-000000000000
		///     - **Comparison Operators**
		///         - Equals operator - {fieldname} eq 'Data'
		///         - Not equals operator - {fieldname} ne 'Data'
		///         - Contains operator - {fieldname} ct 'Data'  
		///         - Greater than operator - {fieldname} gt 99
		///         - Greater than or equal operator - {fieldname} ge 99
		///         - Less than operator - {fieldname} lt 99
		///         - Less than or equal operator - {fieldname} le 99
		///         - Not populated operator - {fieldname} eq null
		///         - populated operator - {fieldname} ne null
		///         - DateTime Is Before Operator - {DateTimeFieldName} lt 'YYYY-MM-DDTHH24:MI'
		///         - DateTime Is After Operator - {DateTimeFieldName} gt 'YYYY-MM-DDTHH24:MI'
		///         - DateTime Is Between Operator - ({DateTimeFieldName} ge 'YYYY-MM-DDTHH24:MI' and {DateTimeFieldName} le 'YYYY-MM-DDTHH24:MI')
		///     
		///     - **Logical Operators**
		///         - Logical and - {fieldname} ge 00 and {fieldname} le 99
		///         - Logical or - {fieldname} eq 'Data' or {fieldname} eq 'Data1'
		///         
		///     - **Profile Type**
		///         - Full       -  {fieldname} eq 0
		///         - Sample     -  {fieldname} eq 1
		///         - Filtered   -  {fieldname} eq 2
		///         
		/// If the requested content media type is "application/octet-stream", the response will be an Excel document with the asset audit data.
		/// </remarks>
		/// <returns>A list of unique series results</returns>
		[
			HttpGet,
			Route("series"),
			SwaggerResponse(HttpStatusCode.OK, "", typeof(ProfilesSeriesApiViewModel)),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Forbidden, "An error to indicate that your request to retrieve this asset is forbidden due to lack of permissions to view it.", typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerParameter("_filter", "The filter expression used to filter ProfileSeries by assetUid (Uid),profileSetDate (DateTime),typeQualifier (Text),type (Text),ftaVersion (Text),freshness (Number),ProfileSource (Text)and ProfileType (Number) fields. Asterisk (*) symbol can be used as a wild card character to match any character.", DataType = "string", ParameterType = "query", Required = false),
		]
		public async Task<IHttpActionResult> GetDataProfilesSeries()
		{
			var queryParams = Request.GetQueryNameValuePairs();

			var results = await Catalog.ReadDataProfilesSeriesAsyn(queryParams);
			return results.IsSuccess ? Ok(results.Data) : errorMessageResponse((HttpStatusCode)results.StatusCode, results.Message);
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
			var execution = getApiExecution(models.Count, action: ApiExecutionAction.PostDataProfile);

			var response = await Catalog.UpsertDataProfilesAsync(models, execution, true);
			if (response.IsSuccess)
			{
				return Ok(response.Data);
			}
			else
			{
				Log.LogError(exception: response.Ex, "Execution error: {ExecutionID}", execution.ExecutionID);
				return errorMessageResponse((HttpStatusCode)response.StatusCode, response.Message);
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
			var execution = getApiExecution(models.Count, action: ApiExecutionAction.PutDataProfile);

			var response = await Catalog.UpsertDataProfilesAsync(models, execution, false);
			if (response.IsSuccess)
			{
				return Ok(response.Data);
			}
			else
			{
				Log.LogError(exception: response.Ex, "Execution error: {ExecutionID}", execution.ExecutionID);
				return errorMessageResponse((HttpStatusCode)response.StatusCode, response.Message);
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
			ApiExplorerSettings(IgnoreApi = true)
		]
        public async Task<IHttpActionResult> DeleteDataProfiles(Guid assetUid, DateTime startDate, DateTime endDate, bool cascade)
        {
            var execution = getApiExecution(1, action: ApiExecutionAction.DeleteDataProfile);

			var response = await Catalog.RemoveDataProfileAsync(assetUid, startDate, endDate, execution, cascade);

			if (response.IsSuccess)
			{
				return Ok(response.Data.FirstOrDefault().DeletedCount);
			}
			else
			{
				Log.LogError(exception: response.Ex, "Execution error: {ExecutionID}", execution.ExecutionID);
				return errorMessageResponse((HttpStatusCode)response.StatusCode, response.Message);
			}
		}

		/// <summary>
		/// Removes Data Profile results for a given asset. 
		/// </summary>
		/// <param name="assetUid">The unique identifier of an asset.</param>
		/// <param name="_startDate">Start date of data profile data to be deleted. Expected date format is yyyy-MM-ddThh:mm:ss</param>
		/// <param name="_endDate">End date of data profile data to be deleted. Expected date format is yyyy-MM-ddThh:mm:ss</param>
		/// <param name="_cascade">True/false flag used to indicate if assets children should be deleted.</param>
		/// <returns>Results response with the count of records deleted.</returns>
		[
			HttpDelete,
			Route("{assetUID:Guid}"),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerParameter("_startDate", "Start date of data profile data to be deleted. If _startDate and _endDate are not supplied the date defaults to the most recent date for the specified asset for which there is data. Otherwise the default is before oldest profiling record.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("_endDate", "End date of data profile data to be deleted. If _startDate and _endDate are not supplied the date defaults to the most recent date for the specified asset for which there is data. Otherwise the default is after most recent profiling record.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("_cascade", "True/false flag used to indicate if assets children should be deleted.", DataType = "boolean", ParameterType = "query", Required = false),
			SwaggerResponse(HttpStatusCode.OK, "Count of Data Profile Records Deleted.", typeof(int)),
			SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
		]
		public async Task<IHttpActionResult> DeleteDataProfiles(Guid assetUid)
		{

			var queryParams = Request.GetQueryNameValuePairs();

			var execution = getApiExecution(1, action: ApiExecutionAction.DeleteDataProfile);

			var response = await Catalog.RemoveDataProfileAsync(assetUid, execution, queryParams);

			if (response.IsSuccess)
			{
				return Ok(response.Data.FirstOrDefault().DeletedCount);
			}
			else
			{
				Log.LogError(exception: response.Ex, "Execution error: {ExecutionID}", execution.ExecutionID);
				return errorMessageResponse((HttpStatusCode)response.StatusCode, response.Message);
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
            List<ValidationResult> validationResults = new List<ValidationResult>();
            foreach (var model in models)
            {
                bool isValid = Validator.TryValidateObject(model, new ValidationContext(model, serviceProvider: null, items: null), validationResults, true);
                if (!isValid)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, Error.BadRequest, validationResults.First().ErrorMessage)).ConfigureAwait(false);
                }
            }

            var execution = getApiExecution(models.Count, action: ApiExecutionAction.PostDataProfile);
            ApiExecutionInfo executionInfo = await DataProfiles.PostBatchDataProfiles(models, execution);
			return await sendExecutionProcessingResponse(executionInfo);
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
            List<ValidationResult> validationResults = new List<ValidationResult>();
            foreach (var model in models)
            {
                bool isValid = Validator.TryValidateObject(model, new ValidationContext(model, serviceProvider: null, items: null), validationResults, true);
                if (!isValid)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, Error.BadRequest, validationResults.First().ErrorMessage)).ConfigureAwait(false);
                }
            }

            var execution = getApiExecution(models.Count, action: ApiExecutionAction.PostDataProfile);
            ApiExecutionInfo executionInfo = await DataProfiles.PutBatchDataProfiles(models, execution);
			return await sendExecutionProcessingResponse(executionInfo);
        }

        /// <summary>
        /// Provides support for deleting a large set of Data Profile records.
        /// </summary>
        /// <param name="models">Data Profile Delete request collection. Note: Expected date format is yyyy-MM-dd</param>
        /// <returns>Results response containing the ExecutionID of the request.</returns>
        [
            HttpDelete,
            Route("batch"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution ID to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> DeleteBulkDataProfilesAsync(List<AssetDataProfileDeleteModel> models)
        {
			var execution = getApiExecution(models.Count, action: ApiExecutionAction.DeleteDataProfile);
            ApiExecutionInfo executionInfo = await DataProfiles.DeleteBatchDataProfiles(models, execution);
			return await sendExecutionProcessingResponse(executionInfo);
        }

        /// <summary>
        /// Retrieves a list of assets that match a given asset.
        /// </summary>
        /// <remarks>
        /// Advanced filtering is done using _filter parameter and filter expressions are specified using field name, operator and value. For example city eq 'Redmond'.
		/// *  For comparison operators you can use eq (equal), ne (not equal), gt (greater than), ge (greater than or equal), lt (less than), le (less than or equal), ct (contains) and nct (not contains) which allows usage of (*) symbol as wildcard
        ///     
        ///     Example :
        ///     
		///     - **Comparison Operators**
		///         - Equals operator - {fieldname} eq 'Data'
		///         - Not equals operator - {fieldname} ne 'Data'
		///         - Contains operator - {fieldname} ct 'Data'  
		///         - Greater than operator - {fieldname} gt 99
		///         - Greater than or equal operator - {fieldname} ge 99
		///         - Less than operator - {fieldname} lt 99
		///         - Less than or equal operator - {fieldname} le 99
		///         - Not populated operator - {fieldname} eq null
		///         - populated operator - {fieldname} ne null
		///         - AssetPath Contains - {AssetPathFieldName} ct 'APValue1'
		///         - AssetPath Does not contain - {AssetPathFieldName} nct 'APValue1'
		///         - AssetPath Is Operator - {AssetPathFieldName} eq 'APValue1'
		///         - AssetPath Is not Operator - {AssetPathFieldName} ne 'APValue1'
		///         - AssetPath Start with Operator - {AssetPathFieldName} ct 'APValue1*'
		///         - AssetPath End with Operator - {AssetPathFieldName} ct '*APValue1'
        ///     
		///     - **Logical Operators**
		///         - Logical and - {fieldname} ge 00 and {fieldname} le 99
		///         - Logical or - {fieldname} eq 'Data' or {fieldname} eq 'Data1'
		///         - Tag Contains Match Any (or) - (({TagFieldName} ct 'Data') or ({TagFieldName} ct 'Data1') or (...))
		///         - Tag Contains Match All(and) - (({TagFieldName} ct 'Data') and ({TagFieldName} ct 'Data1') and (...))
		///         - Tag Does not Contain Match Any(or) - (({TagFieldName} nct 'Data') or ({TagFieldName} nct 'Data1') or (...))
		///         - Tag Does not Contain Match All(and) - (({TagFieldName} nct 'Data') and ({TagFieldName} nct 'Data1') and (...))
		///         - AssetPath Contains (Match All(and)) - (({AssetPathFieldName} ct 'APValue1') and ({AssetPathFieldName} ct 'APValue2') )
		///         - AssetPath Contains (Match Any(or)) - (({AssetPathFieldName} ct 'APValue1') or ({AssetPathFieldName} ct 'APValue2'))
        /// </remarks>
        ///
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

            var queryParams = Request.GetQueryNameValuePairs();
            var isStreamResponse = Request?.Headers?.Accept?.Any(a => a.MediaType == "application/octet-stream") ?? false;

			var asset = await Catalog.GetAsset(assetUid);
			if (asset == null)
			{
				return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, Error.NotFound, string.Format(Error.AssetUidIsNotValid, assetUid))).ConfigureAwait(false);
			}

			if (!Company.HasAssetPermission(asset.Object, asset.ObjectID, Permission.ReadAsset))
			{
				return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, Error.NotFound, string.Format(Error.InvalidAssetUid, assetUid))).ConfigureAwait(false);
			}

			var isValid = isPageSizeAndNumValid(queryParams);

			if (!string.IsNullOrEmpty(isValid))
			{
				return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, Error.BadRequest, isValid)).ConfigureAwait(false);
			}

			var validationResult = await Catalog.ValidateMatchAssetGetParameters(assetUid, similarType, queryParams);

            if (!validationResult.IsSuccess)
            {
                return await Task.FromResult(errorMessageResponse((HttpStatusCode)validationResult.StatusCode, Error.BadRequest, validationResult.Message)).ConfigureAwait(false);
            }

            HttpResponseMessage response;

            if (isStreamResponse)
            {
                var results = await Catalog.GetMatchedAssetsForExport(assetUid, similarType, queryParams).ConfigureAwait(false);

				if (!results.IsSuccess)
				{
					return await Task.FromResult(errorMessageResponse((HttpStatusCode)results.StatusCode, Error.BadRequest, results.Message)).ConfigureAwait(false);
				}
				else
				{

					int pageNum = Company.ParsePageNumber(queryParams, 1);
					int pageSize = Company.ParsePageSize(queryParams, 200000);
					var assetPath = Catalog.ReadAssetPathsAssetUID(assetUid);

					SLDocument document = CreateResponseDocumentForExport(results.Data.ToList(), similarType, pageNum, pageSize);
					var stream = new MemoryStream();
					document.SaveAs(stream);
					byte[] bytes = stream.ToArray();
					var filename = $"Filtered {assetPath.Result[0][0]} {{0}} Fields List _{DateTime.Now:ddd MMM dd yyyy}_.xlsx";

					if (similarType.Equals("data", StringComparison.InvariantCultureIgnoreCase))
					{
						filename = string.Format(Label.MatchedAssetExportFileName, assetPath.Result[0][0], "Duplicate", DateTime.Now.ToString("ddd MMM dd yyyy"));
					}
					else
					{
						filename = string.Format(filename, "Similar");
					}
					response = createFileResponseMessage(HttpStatusCode.OK, filename, bytes);
				}
			}
            else
            {
				var results = await Catalog.ReadMatchingAssets(assetUid, similarType, queryParams);
				
				if (!results.IsSuccess)
				{
					return await Task.FromResult(errorMessageResponse((HttpStatusCode)results.StatusCode, Error.BadRequest, results.Message)).ConfigureAwait(false);
				}
				else
				{
					response = Request.CreateResponse(HttpStatusCode.OK, results.Data);
				}
			}

            return await Task.FromResult<IHttpActionResult>(ResponseMessage(response)).ConfigureAwait(false);
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
            var queryParams = Request.GetQueryNameValuePairs();

			var asset = await Catalog.GetAsset(assetUid);
			if (asset == null)
			{
				return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, Error.NotFound, string.Format(Error.AssetUidIsNotValid, assetUid))).ConfigureAwait(false);
			}

			if (!Company.HasAssetPermission(asset.Object, asset.ObjectID, Permission.ReadAsset))
			{
				return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, Error.NotFound, string.Format(Error.InvalidAssetUid, assetUid))).ConfigureAwait(false);
			}

			var isValid = isPageSizeAndNumValid(queryParams);

			if (!string.IsNullOrEmpty(isValid))
			{
				return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, Error.BadRequest, isValid)).ConfigureAwait(false);
			}
			
			var validationResult = await Catalog.ValidateMatchAssetGetParameters(assetUid, similarType, queryParams);

			if (!validationResult.IsSuccess)
			{
				return await Task.FromResult(errorMessageResponse((HttpStatusCode)validationResult.StatusCode, Error.BadRequest, validationResult.Message)).ConfigureAwait(false);
			}

            var results = await Catalog.ReadMatchingAssets(assetUid, similarType, queryParams, true).ConfigureAwait(false);

			return results.IsSuccess ? Ok(results.Data.total) : errorMessageResponse((HttpStatusCode)results.StatusCode, results.Message);
        }

        /// <summary>
        /// Retrieves a list of assets of the given type with the specified confidence.
        /// </summary>
        ///<remarks>
        /// Advanced filtering is done using _filter parameter and filter expressions are specified using field name, operator and value. For example city eq 'Redmond'.
		/// *  For comparison operators you can use eq (equal), ne (not equal), gt (greater than), ge (greater than or equal), lt (less than), le (less than or equal) and ct (contains) which allows usage of (*) symbol as wildcard
		///     
		///     Example :
		///     
		///     - **Comparison Operators**
		///         - Equals operator - {fieldname} eq 'Data'
		///         - Not equals operator - {fieldname} ne 'Data'
		///         - Contains operator - {fieldname} ct 'Data'  
		///         - Greater than operator - {fieldname} gt 99
		///         - Greater than or equal operator - {fieldname} ge 99
		///         - Less than operator - {fieldname} lt 99
		///         - Less than or equal operator - {fieldname} le 99
		///         - Not populated operator - {fieldname} eq null
		///         - populated operator - {fieldname} ne null
		///     
		///     - **Logical Operators**
		///         - Logical and - {fieldname} ge 00 and {fieldname} le 99
		///         - Logical or - {fieldname} eq 'Data' or {fieldname} eq 'Data1'
        /// </remarks>
        ///
        /// <param name="typeQualifier">Semantic Type to retrive results for.</param>
        /// <param name="minConfidence">Minimum Confidence that profile records must match or exceed.</param>
        /// <returns>A list of matching asset uids associated with asset paths and confidence</returns>
        [
            HttpGet,
            Route("type/{typeQualifier}/{minConfidence}"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(AssetDataProfileByTypeQualifierApiViewModel)),
            SwaggerProduces("application/json", "application/octet-stream"),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 250. Maximum page size is 10,000", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Allows you to disable including the count of the total number of results across pages in the response.  The default is true meaning the total count is included.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending and are sorted on the asset path value", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by. Allowed values are 'confidence', 'path' or 'assettypepath'. By default the results are ordered by asset path value.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_filter", "The filter expression used to filter assets by path, assetTypePath fields and/or when the outOfDate flag is true/false. Asterisk (*) symbol can be used as a wild card character to match any character.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_simpleFilter", "The text or phrase you want to find within path or assetTypePath fields. Filtering is done using 'Starts with' logic. Asterisk (*) symbol can be used as a wild card character to match any character.", DataType = "string", ParameterType = "query", Required = false),
        ]
        public async Task<IHttpActionResult> GetAssetsByTypeQualifier(string typeQualifier, decimal minConfidence)
        {
            var prefix = "DataProfiles.GetMatchingAssets => ";

            var queryParams = Request.GetQueryNameValuePairs();
            var isStreamResponse = Request?.Headers?.Accept?.Any(a => a.MediaType == "application/octet-stream") ?? false;
            var isValid = isPageSizeAndNumValid(queryParams, isStreamResponse);

            if (!string.IsNullOrEmpty(isValid))
            {
				return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, Error.BadRequest, isValid)).ConfigureAwait(false);
			}


			var response = await Catalog.ReadAssetsByTypeQualifier(typeQualifier, minConfidence, queryParams, isStreamResponse).ConfigureAwait(false);

			if (!response.IsSuccess)
			{
				return await Task.FromResult(errorMessageResponse((HttpStatusCode)response.StatusCode, Error.BadRequest, response.Message)).ConfigureAwait(false);
			}
			else
			{
				if (isStreamResponse)
				{
					var semantic = Company.Semantics.FirstOrDefault(x => x.Qualifier == typeQualifier);
					SLDocument document = CreateResponseDocumentForSemanticTypeAssetListExport(response.Data, semantic.Name);
					document.SelectWorksheet(Label.Common_ItemsSheetName);
					var stream = new MemoryStream();
					document.SaveAs(stream);

					var result = new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new ByteArrayContent(stream.GetBuffer())
					};
					result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
					{
						FileName = string.Format(Label.SemanticTypeAssetExportFilename, semantic.Name, DateTime.Now.ToString("ddd MMM dd yyyy"))
					};
					result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

					return ResponseMessage(result);
				}
				else
				{
					return Ok(response.Data);
				}
			}
		}

        /// <summary>
        /// Create the Excel document for export
        /// </summary>
        /// <returns>A spreadsheet populated with the details of the data profile results</returns>
        private SLDocument CreateResponseDocumentForExport(List<DataProfileExportModel> dataProfiles, string similarType, int pageNum, int pageSize)
        {
            SLDocument doc = new SLDocument();
            string assetSheetName = Label.AssetSheetName;
            string apiSheetName = Label.ApiSheetName;
            string matchType;

            if (similarType.Equals("data", StringComparison.InvariantCultureIgnoreCase))
            {
                matchType = Label.Duplicate;
            }
            else
            {
                matchType = Label.Similar;
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

            doc.SetCellValue(rowNumber, index++, Label.NameColumn);
            doc.SetCellValue(rowNumber, index++, Label.TagsColumn);
            doc.SetCellValue(rowNumber, index++, Label.AssetPathColumn);
            doc.SetCellValue(rowNumber, index++, Label.AssetTypePathColumn);
            doc.SetCellValue(rowNumber, index++, string.Format(Label.MatchedAssetNameColumn, matchType));
            doc.SetCellValue(rowNumber, index++, string.Format(Label.MatchedAssetTagsColumn, matchType));
            doc.SetCellValue(rowNumber, index++, string.Format(Label.MatchedAssetPathColumn, matchType));
            doc.SetCellValue(rowNumber, index++, string.Format(Label.MatchedAssetTypePathColumn, matchType));
            doc.SetCellValue(rowNumber, index++, Label.AssetUidColumn);
            doc.SetCellValue(rowNumber, index++, Label.AssetIdColumn);
            doc.SetCellValue(rowNumber, index++, Label.UrlColumn);
            doc.SetCellValue(rowNumber, index++, string.Format(Label.MatchedAssetUidColumn, matchType));
            doc.SetCellValue(rowNumber, index++, string.Format(Label.MatchedAssetIdColumn, matchType));
            doc.SetCellValue(rowNumber, index, string.Format(Label.MatchedAssetUrlColumn, matchType));

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
                    doc.SetCellValue(rowNumber, index, Error.TagFieldNotFound);
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
        ///  
        /// Advanced filtering is done using _filter parameter and filter expressions are specified using field name, operator and value. For example city eq 'Redmond'.
		/// *  For comparison operators you can use eq (equal), ne (not equal), gt (greater than), ge (greater than or equal), lt (less than), le (less than or equal) and ct (contains) which allows usage of (*) symbol as wildcard
		///     
		///     Example :
		///     
		///     - **Comparison Operators**
		///         - Equals operator - {fieldname} eq 'Data'
		///         - Not equals operator - {fieldname} ne 'Data'
		///         - Contains operator - {fieldname} ct 'Data'  
		///         - Greater than operator - {fieldname} gt 99
		///         - Greater than or equal operator - {fieldname} ge 99
		///         - Less than operator - {fieldname} lt 99
		///         - Less than or equal operator - {fieldname} le 99
		///         - Not populated operator - {fieldname} eq null
		///         - populated operator - {fieldname} ne null
		///     
		///     - **Logical Operators**
		///         - Logical and - {fieldname} ge 00 and {fieldname} le 99
		///         - Logical or - {fieldname} eq 'Data' or {fieldname} eq 'Data1'
        /// </remarks>
        /// <returns>A list of semantic types based on the provided filtering and sorting criteria.</returns>
        [
            HttpGet,
            Route("semantictypes"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json", "application/octet-stream"),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default (and maximum) value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the semantic types are ordered by Qualifier.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_simpleFilter", "The text or phrase you want to find within the listable fields of a semantic type. Filtering is done using 'Starts with' logic. Asterisk (*) symbol can be used as a wild card character to match any character.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_filter", ADVANCED_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("asOfEffectiveDate", "Assumed to be current UTC date if left empty, otherwise, gets semantic types as of the specified effective date, and nothing later. This is the parameter used to get prior versions.", DataType = "datetime", ParameterType = "query", Required = false),
            SwaggerParameter("_includeDisabled", " If true returns both enabled and disabled semantic types. Default value is true.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of semantic types.", typeof(GetSemantics)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetSemanticTypes(CancellationToken cancellationToken)
        {
            var queryParams = Request.GetQueryNameValuePairs();
            var isStreamResponse = Request?.Headers?.Accept?.Any(a => a.MediaType == "application/octet-stream") ?? false;
            string isValid = isPageSizeAndNumValid(queryParams, isStreamResponse);

            if (!string.IsNullOrEmpty(isValid))
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, Error.InvalidRequest, isValid)).ConfigureAwait(false);
            }

			var apiModels = await Catalog.ReadSemanticTypesAsync(queryParams); 
            HttpResponseMessage response;

            if (isStreamResponse)
            {
                bool includeDisabled = false;
                if (queryParams.Any(q => q.Key == "_includeDisabled"))
                {
                    var _includeDisabled = queryParams.ToList().FirstOrDefault(q => q.Key == "_includeDisabled").Value;                       

                    if (!bool.TryParse(_includeDisabled, out includeDisabled))
                    {
                        includeDisabled = false;
                    }
                }
				
                SLDocument document = CreateResponseDocumentForSemanticTypesExport(apiModels.Data, includeDisabled);
				return Excel(document, string.Format(Label.SemanticTypeExportFilename, DateTime.Now.ToString("ddd MMM dd yyyy")));
            }
            else
            {
                return Ok(apiModels.Data);
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
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the semantic types are ordered by Effective Date.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered descending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of semantic types.", typeof(List<GetSemantic>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetSemanticTypeVersions(string qualifier, CancellationToken cancellationToken)
        {
			var queryParams = Request.GetQueryNameValuePairs();
            var responseModels = await Catalog.GetSemanticVersionsByQualifierAsync(qualifier, queryParams, cancellationToken);
            return Ok(responseModels);
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
            return Ok(SemanticBaseType.LocalDate.GetAsList());
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
            var queryParams = Request.GetQueryNameValuePairs();
            var orderBy = "name";

            if (queryParams.Any(qp => qp.Key.ToLower() == "_orderby"))
            {
                orderBy = queryParams.FirstOrDefault(qp => qp.Key.ToLower() == "_orderby").Value;
            }

            return Ok(SemanticMatchType.Pattern.GetAsList(orderBy));
        }

        /// <summary>
        /// Gets a list of semantic type statuses.
        /// </summary>
        [
            HttpGet,
            Route("semantictypes/lookups/statuses"),
            SwaggerProduces("application/json", "application/octet-stream"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of semantic type statuses.", typeof(List<SemanticStatusInfo>)),
        ]
        public async Task<HttpResponseMessage> GetSemanticTypeStatuses()
        {
            var isExport = Request?.Headers?.Accept?.Any(a => a.MediaType == "application/octet-stream") ?? false;
            List<SemanticStatusInfo> statuses = SemanticStatus.Draft.GetAsList();
            HttpResponseMessage response;

            if (isExport)
            {
                var excelDocument = new ExcelDocument(string.Format(Label.SemanticTypeStatusExportFilename, DateTime.Now.ToString("ddd MMM dd yyyy")))
                {
                    new ExcelSheet(Label.Common_ItemsSheetName)
                    {
                        HeaderRows = {
                            new ExcelRow
                            {
								Label.NameColumn,
								Label.ColorColumn
                            }
                        },

                        ValueRows = statuses.Select(row => new ExcelRow
                        {
                            row.Name,
                            row.ColorName,
                        }).ToList(),
                    }
                };

                SLDocument document = excelDocument.ToSLDocument();
                document.SelectWorksheet(Label.Common_ItemsSheetName);
                var stream = new MemoryStream();
                document.SaveAs(stream);
                byte[] bytes = stream.ToArray();

                response = createFileResponseMessage(HttpStatusCode.OK, string.Format(Label.SemanticTypeStatusExportFilename, DateTime.Now.ToString("ddd MMM dd yyyy")), bytes);

            }
            else
            {
                response = Request.CreateResponse(HttpStatusCode.OK, SemanticStatus.Draft.GetAsList());
            }

            return response;
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
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse)),
			RequireAdminPermissions
		]
        public async Task<IHttpActionResult> PatchSemanticTypes(List<PatchSemantic> requestModels)
        {
            var responseModels = await Catalog.PatchSemanticsAsync(requestModels);
            return Ok(responseModels);
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
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse)),
			RequireAdminPermissions
		]
        public async Task<IHttpActionResult> PostSemanticTypes(List<PostSemantic> requestModels)
        {
            var responseModels = await Catalog.PostSemanticsAsync(requestModels);
            return Created("", responseModels);
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
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse)),
			RequireAdminPermissions
		]
        public async Task<IHttpActionResult> PutSemanticTypes(List<PutSemantic> requestModels)
        {
            var reponseModels = await Catalog.PutSemanticsAsync(requestModels);
            return Ok(reponseModels);
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
            SwaggerResponse(HttpStatusCode.OK, SUCCESS_MESSAGE, typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Your semantic type was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Conflict, "Request to remove this semantic type is invalid, possibly due to being used on one or more data profiles.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse)),
			RequireAdminPermissions
		]
        public async Task<IHttpActionResult> DeleteSemanticType(string qualifier)
        {
            var status = await Catalog.DeleteSemanticAsync(qualifier);

            return ResponseMessage(Request.CreateResponse(status, new ConfirmResponse { message = "Semantic type removed." }));
        }

		[
			HttpGet,
			Route("possibleCreators"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "A list of users who were creating Semantic Types."),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			ApiExplorerSettings(IgnoreApi = true)
		]
		public async Task<IHttpActionResult> GetPossibleCreators()
		{
			var result = await Catalog.GetPossibleCreators();
			return Ok(result);
		}

		[
			HttpGet,
			Route("possibleRedactors"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "A list of users who were editing Semantic Types."),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			ApiExplorerSettings(IgnoreApi = true)
		]
		public async Task<IHttpActionResult> GetPossibleRedactors()
		{
			var result = await Catalog.GetPossibleRedactors();
			return Ok(result);
		}

		/// <summary>
		/// Create the Excel document for export
		/// </summary>
		/// <returns>A spreadsheet populated with a list of the Semantic Types</returns>
		private SLDocument CreateResponseDocumentForSemanticTypesExport(PagedApiBaseViewModel<GetSemantic> semantics, bool includeDisabled = false)
        {
            ExcelRow HeaderRow = new ExcelRow
                        {
							Label.NameColumn,
							Label.QualifierColumn,
							Label.DescriptionColumn,
							Label.ThresholdColumn,
							Label.PriorityColumn,
							Label.StatusColumn,
							Label.SourceColumn,
							Label.MatchTypeColumn,
							Label.BaseTypeColumn,
							Label.JsonColumn,
							Label.HeaderFilterColumn,
							Label.HeaderFilterConfidenceColumn,
							Label.RegularExpressionColumn,
							Label.ValidValuesColumn,
							Label.InvalidValuesColumn,
							Label.MinimumSamplesColumn,
							Label.ValidLocalesColumn,
							Label.MinimumColumn,
							Label.MaximumColumn,
							Label.MinimumMaximumPresentColumn,

                        };
            if (includeDisabled)
            {
                HeaderRow.Add(Label.SemanticTypeDisabledColumn);
                HeaderRow.Add(Label.SemanticTypeEffectiveRangeColumn);
                HeaderRow.Add(Label.SemanticTypeUidColumn);
                HeaderRow.Add(Label.SemanticTypeURLColumn);
            }
            else
            {
                HeaderRow.Add(Label.SemanticTypeUidColumn);
                HeaderRow.Add(Label.SemanticTypeURLColumn);
            }
            var document = new ExcelDocument(string.Format(Label.SemanticTypeExportFilename, DateTime.Now.ToString("ddd MMM dd yyyy")))
            {


                new ExcelSheet(Label.Common_ItemsSheetName)
                {
                    HeaderRows = {
                        HeaderRow
                    },

                    ValueRows = semantics.items.Select(row => new ExcelRow
					{
						row.Name,
						row.Qualifier,
						row.Description,
						row.Threshold + "%",
						row.Priority,
						row.Status == SemanticStatus.InReview ? Label.SemanticStatusUnderReview : row.Status.ToString(),
						row.Source == SemanticSource.BuiltIn ? Label.SemanticSourceBuiltIn : Label.SemanticSourceUserDefined,
						parseMatchTypeForExport(row.MatchType),
						parseBaseTypeForExport(row.BaseType),
						row.JsonPayloadStructured != null ? JsonConvert.SerializeObject(row.JsonPayloadStructured, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }) : "",
						row.HeaderFilter,
						row.HeaderFilterConfidence.HasValue ? row.HeaderFilterConfidence.ToString() + "%" : "",
						row.RegularExpression,
						row.ValidValuesStructured != null ? string.Join(" | ", row.ValidValuesStructured) : "",
						row.InvalidValuesStructured != null ? string.Join(" | ", row.InvalidValuesStructured) : "",
						row.MinimumSamples.HasValue ? row.MinimumSamples.ToString() : "",
						row.ValidLocalesStructured != null ? string.Join(" | ", row.ValidLocalesStructured) : "",
						row.Minimum.ToString(),
						row.Maximum.ToString(),
						row.MinMaxPresent.ToString(),
						includeDisabled ? row.IsDisabled.ToString() : row.Uid.ToString(),
						includeDisabled ? string.Join("|", row.EffectiveDates.Select(d=>{ return $"{d.StartDate} - {d.EndDate}"; })) : $"semantics/{row.Uid}",
                        includeDisabled ? row.Uid.ToString() : "",
                        includeDisabled ? $"semantics/{row.Uid}" : ""
                    }).ToList(),
                },

                new ExcelSheet(Label.Common_ApiInfoSheetName)
                {
                    ValueRows =
                    {
                        new ExcelRow { Label.Common_PageSize, semantics.pageSize.ToString() },
                        new ExcelRow { Label.Common_PageNum, semantics.pageNum.ToString() },
                        new ExcelRow { Label.Common_Total, semantics.total.ToString() }
                    }
                }
            };

            return document.ToSLDocument();
        }

        private string parseMatchTypeForExport(SemanticMatchType matchType)
        {
            switch (matchType)
            {
                case SemanticMatchType.Advanced:
                    return Label.SemanticMatchTypeAdvanced;
                case SemanticMatchType.List:
                    return Label.SemanticMatchTypeList;
                case SemanticMatchType.Pattern:
                    return Label.SemanticMatchTypePattern;
                default:
                    return matchType.ToString();
            }
        }

        private string parseBaseTypeForExport(SemanticBaseType baseType)
        {
            switch (baseType)
            {
                case SemanticBaseType.Double:
                case SemanticBaseType.Long:
                    return string.Format(Label.SemanticBaseTypeNumber, baseType);
                case SemanticBaseType.Boolean:
                    return Label.SemanticBaseTypeBoolean;
                default:
                    return baseType.ToString();
            }
        }

        /// <summary>
        /// Create the Excel document for export
        /// </summary>
        /// <returns>A spreadsheet populated with a list of the Semantic Types</returns>
        private SLDocument CreateResponseDocumentForSemanticTypeAssetListExport(AssetDataProfileByTypeQualifierApiViewModel assets, string semanticName)
        {
            var document = new ExcelDocument(string.Format(Label.SemanticTypeAssetExportFilename, semanticName, DateTime.Now.ToString("ddd MMM dd yyyy")))
            {
                new ExcelSheet(Label.Common_ItemsSheetName)
                {
                    HeaderRows = {
                        new ExcelRow
                        {
							Label.AssetPathColumn,
							Label.AssetTypePathColumn,
							Label.AssetUidColumn,
							Label.AssetTypeUidColumn,
							Label.SemanticTypeUidColumn,
							Label.AssetUrlColumn,
							Label.SemanticTypeURLColumn
                        }
                    },

                    ValueRows = assets.items.Select(row => new ExcelRow
                    {
                        row.path,
                        row.assetTypePath,
                        row.uid.ToString(),
                        row.assetTypeUid.ToString(),
                        row.semanticTypeUid.ToString(),
                        $"asset/{row.uid}",
                        $"semantics/{row.semanticTypeUid}"
                    }).ToList(),
                },

                new ExcelSheet(Label.Common_ApiInfoSheetName)
                {
                    ValueRows =
                    {
                        new ExcelRow { Label.Common_PageSize, assets.pageSize.ToString() },
                        new ExcelRow { Label.Common_PageNum, assets.pageNum.ToString() },
                        new ExcelRow { Label.Common_Total, assets.total.ToString() }
                    }
                }
            };

            return document.ToSLDocument();
        }

		#endregion
	}
}
