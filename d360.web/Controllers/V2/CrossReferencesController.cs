using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

using d360.core.entities;
using d360.core.queue;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;

using Microsoft.Web.Http;

using Resources;

using Swashbuckle.Swagger.Annotations;

namespace d360.web.Controllers.V2
{
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/crossreferences"), Authorize
    ]
    public class CrossReferencesController : BaseV2ApiController
    {
        private const int DEFAULT_DELETE_TIMEOUT = 90;
        private readonly ICrossReferencesRepository crossReferencesRepository;
        private readonly IAssetRepository assetRepository;

        #region DI

        public CrossReferencesController(ICoreComponentSet set, ICrossReferencesRepository crossReferencesRepository, IAssetRepository assetRepository)
            : base(set)
        {
            this.crossReferencesRepository = crossReferencesRepository;
            this.assetRepository = assetRepository;
        }

        #endregion

        /// <summary>
        /// Returns all asset cross references.  Optional parameters are also supported, in the case where the optional parameters are specified only records matching that criteria are returned.
        /// </summary>
        /// <returns>An array of cross references</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A full list of asset cross references.", typeof(List<AssetCrossReference>)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(List<AssetCrossReference>)),
            SwaggerParameter("_assetUid", "The asset UID of the cross reference record.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_externalId", "The external ID of the cross reference record(s) to request.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_dataSource", "The data source of the cross reference record(s) to request.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_type", "The type of the cross reference record(s) to request.", DataType = "string", ParameterType = "query", Required = false),
        ]
        public async Task<HttpResponseMessage> Get()
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage));
            }

            var queryParams = Request.GetQueryNameValuePairs();
            var assetCrossReferences = await crossReferencesRepository.GetCrossReferences(queryParams);

            return Request.CreateResponse(assetCrossReferences);
        }

        /// <summary>
        /// Returns asset cross references for the specified asset based on its unique identifier.
        /// </summary>
        /// <param name="assetUid">The unique identifier of the asset.</param>
        /// <returns>An array of matching cross references.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("{assetUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset cross references based on the public unique identifier (assetUid) of the asset.", typeof(List<AssetCrossReference>)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(List<AssetCrossReference>))
        ]
        public async Task<HttpResponseMessage> GetByUid(string assetUid)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage));
            }

            var result = await crossReferencesRepository.GetByAssetUid(assetUid);

            return Request.CreateResponse(result);
        }

        /// <summary>
        /// Returns asset cross references for the specified type and external id.
        /// </summary>
        /// <param name="type">The type of the asset cross reference.</param>
        /// <param name="externalId">The external Id of asset cross reference.</param>
        /// <returns>An array of matching cross reference.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("{type}/{externalId}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset cross references based on the external type and identifier of the asset.", typeof(List<AssetCrossReference>)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(List<AssetCrossReference>))
        ]
        public async Task<HttpResponseMessage> GetByTypeID(string type, string externalId)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage));
            }

            var result = await crossReferencesRepository.GetCrossReferenceByTypeId(type, externalId);

            return Request.CreateResponse(result);
        }

        /// <summary>
        /// Returns asset cross references for the specified type.
        /// </summary>
        /// <param name="type">The type of the asset cross reference.</param>
        /// <returns>An array of matching cross references.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("type/{type}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset cross references based on the external type.", typeof(List<AssetCrossReference>)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(List<AssetCrossReference>))
        ]
        public async Task<HttpResponseMessage> GetByType(string type)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage));
            }

            var result = await crossReferencesRepository.GetCrossReferenceByType(type);

            return Request.CreateResponse(result);
        }

        /// <summary>
        /// Returns asset cross references for the specified data source.
        /// </summary>
        /// <param name="dataSource">The dataSource of asset cross reference.</param>
        /// <returns>An array of matching cross references.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("datasource/{dataSource}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset cross references based on the data source.", typeof(List<AssetCrossReference>)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "Access Denied", typeof(List<AssetCrossReference>))
        ]
        public async Task<HttpResponseMessage> GetByDataSource(string dataSource)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage));
            }

            var result = await crossReferencesRepository.GetCrossReferenceByDataSource(dataSource);

            return Request.CreateResponse(result);
        }

        /// <summary>
        /// Creates a new asset cross reference.  If an asset cross reference exists already an error is returned.  ExternalID and DataSource fields have a limit of 250 ASCII characters each.  Type has a limit of 50 ASCII characters.
        /// </summary>
        /// <param name="model">The asset cross references model.</param>
        /// <returns>The model of the created asset cross reference. If item already exists http confict is returned.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            Route(""),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotAcceptable, "Asset cross reference model does not contain required fields.", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.Conflict, "Asset cross reference already exists.", typeof(AssetCrossReference))
        ]
        public async Task<AssetCrossReference> Post(AssetCrossReference model)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.AccessDenied));
            }

            //validate the model input
            if (string.IsNullOrEmpty(model.DataSource) || string.IsNullOrEmpty(model.ExternalID) || string.IsNullOrEmpty(model.Type))
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, ApiMessages.ModelNotContainFields));
            }

            //check if the item already exists   
            bool exists = await crossReferencesRepository.XrefExists(model);

            if (exists)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, ApiMessages.AssetCorssRefAlreadyExists));
            }

            //create the new record
            int res = await crossReferencesRepository.CreateNewCrossReference(model);

            if (res <= 0)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, ApiMessages.AssetCorssRefAlreadyExists));
            }

            return model;
        }

        /// <summary>
        /// Creates new asset cross references.  If an asset cross reference exists already an error is returned.  ExternalID and DataSource fields have a limit of 250 ASCII characters each.  Type has a limit of 50 ASCII characters.
        /// </summary>
        /// <param name="models">List of asset cross references.</param>
        /// <returns>List of created asset cross references. If any item already exists an HTTP Confict is returned.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            Route("bulk"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(List<AssetCrossReference>)),
            SwaggerResponse(HttpStatusCode.Conflict, "One or more asset cross references already exist.", typeof(List<AssetCrossReference>))
        ]
        public async Task<IHttpActionResult> PostBulk(List<AssetCrossReference> models)
        {
            var prefix = "CrossReferences.PostBulk => ";
            string errorMessage;

            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage));
            }

            try
            {
                var execution = getApiExecution(models.Count);
                var results = crossReferencesRepository.PostBulkCrossReference(models, execution);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>()
                {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Updates the specified asset cross reference.  In order to update an asset cross reference record you must pass in the uid, datasource and type values for an existing cross reference item.  If you have special characters in your datasource, or type values use the PUT endpoint that only requires the uid in the URL.
        /// </summary>
        /// <param name="uid">The unique identifier of the asset cross reference.</param>
        /// <param name="dataSource">Asset cross reference datasource</param>
        /// <param name="type">Asset cross reference type</param>
        /// <param name="externalId">Asset cross reference externalId</param>
        /// <param name="model">Asset cross reference model</param>
        /// <returns>Http Status code OK if asset cross reference was updated, Http Status code of Not Found if item could not be updated.</returns>
        [
            HttpPut,
            MapToApiVersion("2.0"),
            Route("{uid:Guid}/{dataSource}/{type}/{externalId}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotAcceptable, "Model does not contain required fields.", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference))
        ]
        public async Task<HttpResponseMessage> Put(Guid uid, string dataSource, string type, string externalId, AssetCrossReference model)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.AccessDenied));
            }

            //validate the model input
            if (string.IsNullOrEmpty(dataSource) || string.IsNullOrEmpty(type) || string.IsNullOrEmpty(externalId))
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, ApiMessages.AssetCrossReferenceNotHaveRequiredField));
            }

            //create the new record
            int res = await crossReferencesRepository.PutCrossReference(uid, dataSource, type, model);

            if (res > 0)
            {
                return Request.CreateResponse(HttpStatusCode.OK); // updated
            }

            return Request.CreateResponse(HttpStatusCode.NotFound); // nothing updated
        }

        /// <summary>
        /// Updates the specified asset cross reference.  In order to update an asset cross reference record you must pass in the uid, datasource and type values for an existing cross reference item.
        /// </summary>
        /// <param name="uid">The unique identifier of the asset cross reference.</param>        
        /// <param name="model">Asset cross reference model</param>
        /// <returns>Http Status code OK if asset cross reference was updated, Http Status code of Not Found if item could not be updated.</returns>
        [
            HttpPut,
            MapToApiVersion("2.0"),
            Route("{uid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotAcceptable, "Model does not contain required fields.", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference))
        ]
        public async Task<HttpResponseMessage> Put(Guid uid, AssetCrossReference model)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.AccessDenied));
            }

            //validate the model input
            if (string.IsNullOrEmpty(model.DataSource) || string.IsNullOrEmpty(model.Type) || string.IsNullOrEmpty(model.ExternalID))
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, ApiMessages.AssetCrossReferenceNotHaveRequiredField));
            }

            //create the new record
            int res = await crossReferencesRepository.PutCrossReference(uid, model);

            if (res > 0)
            {
                return Request.CreateResponse(HttpStatusCode.OK); // updated
            }

            return Request.CreateResponse(HttpStatusCode.NotFound); // nothing updated
        }

        /// <summary>
        /// Deletes all asset cross reference records by the specified unique identifier.  Asset cross reference records with the same uid and different datasource and or type will also be deleted.
        /// </summary>
        /// <param name="uid">The unique identifier of the asset cross reference.</param>
        /// <returns>Http Status code OK if item was deleted, Http Status code of Not Found if item could not be deleted.</returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            Route("{uid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference))
        ]
        public async Task<HttpResponseMessage> DeleteByUid(Guid uid)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.ForbiddenUserNotAuthorizedMessage));
            }

            //deletes the new record
            int res = await crossReferencesRepository.DeleteCrossReferenceByUid(uid);

            if (res > 0)
            {
                return Request.CreateResponse(HttpStatusCode.OK); // deleted
            }

            return Request.CreateResponse(HttpStatusCode.NotFound); // nothing deleted
        }

        /// <summary>
        /// Deletes any asset cross references with the specified datasource and type.
        /// </summary>
        /// <param name="dataSource">Asset cross reference datasource</param>
        /// <param name="type">Asset cross reference type</param>
        /// <returns>Http Status code OK if asset cross references were deleted, Http Status code of Not Found if item could not be deleted></returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            Route("{dataSource}/{type}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotAcceptable, "Request does not contain required parameters datasource and type.", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference))
        ]
        public async Task<HttpResponseMessage> DeleteByDataSource(string dataSource, string type)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.AccessDenied));
            }

            if (string.IsNullOrEmpty(dataSource) || string.IsNullOrEmpty(type))
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, ApiMessages.RequestMissingDatasourceType));
            }

            //deletes the new record
            int res = await crossReferencesRepository.DeleteCrossReferenceByDataSource(dataSource, type, GetTimeoutFromQueryString());

            if (res > 0)
            {
                return Request.CreateResponse(HttpStatusCode.OK); // deleted
            }

            return Request.CreateResponse(HttpStatusCode.NotFound); // nothing deleted
        }

        /// <summary>
        /// Deletes any asset cross references with the specified type.
        /// </summary>
        /// <param name="type">Asset cross reference type.</param>
        /// <returns>Http Status code OK if assset cross references were deleted, Http Status code of Not Found if item could not be deleted</returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            Route("type/{type}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotAcceptable, "Request does not contain required parameter type.", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference))
        ]
        public async Task<HttpResponseMessage> DeleteByType(string type)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.AccessDenied));
            }

            if (string.IsNullOrEmpty(type))
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, ApiMessages.RequestMissingType));
            }

            //deletes the new record
            int res = await crossReferencesRepository.DeleteCrossReferenceByType(type, GetTimeoutFromQueryString());

            if (res > 0)
            {
                return Request.CreateResponse(HttpStatusCode.OK); // deleted
            }

            return Request.CreateResponse(HttpStatusCode.NotFound); // nothing deleted
        }

        /// <summary>
        /// Deletes any asset cross references with the specified datasource.
        /// </summary>
        /// <param name="dataSource">Asset cross reference datasource.</param>
        /// <returns>Http Status code OK if asset cross references were deleted, Http Status code of Not Found if item could not be deleted</returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            Route("dataSource/{dataSource}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotAcceptable, "Request does not contain required parameter dataSource.", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference))
        ]
        public async Task<HttpResponseMessage> DeleteByDataSource(string dataSource)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.AccessDenied));
            }

            if (string.IsNullOrEmpty(dataSource))
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, ApiMessages.RequestMissingDatasource));
            }

            //deletes the new record
            int res = await crossReferencesRepository.DeleteCrossReferenceByDataSource(dataSource, GetTimeoutFromQueryString());

            if (res > 0)
            {
                return Request.CreateResponse(HttpStatusCode.OK); // deleted
            }

            return Request.CreateResponse(HttpStatusCode.NotFound); // nothing deleted
        }

        /// <summary>
        /// Creates new asset cross references.  This endpoint is meant for batch processing.
        /// </summary>
        /// <param name="crossReferences">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            Route("batch"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution ID to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add Asset Cross Reference", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))

        ]
        public async Task<IHttpActionResult> PostBatchCrossReferenceAsync(List<AssetCrossReference> crossReferences)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.ForbiddenUserNotAuthorizedMessage)).ConfigureAwait(false);
            }

            var prefix = "CrossReferences.PostBatchCrossReferenceAsync => ";
            string errorMessage;
            try
            {
                if (crossReferences == null)
                {
                    crossReferences = readRequestJsonContent<List<AssetCrossReference>>(Request).Result;
                }

                if (crossReferences == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.ErrorInvalidDatasetMessage)).ConfigureAwait(false);
                }

                var execution = getApiExecution(crossReferences.Count);

                ApiExecutionInfo executionInfo = await crossReferencesRepository.PostBatchCrossReference(crossReferences, execution);

                var result = Request.CreateResponse(
                                 HttpStatusCode.OK,
                                new ApiExecutionRecievedResponse
                                {
                                    ExecutionID = executionInfo.ExecutionID,
                                    Message = ApiMessages.ExecutionIDStatus,
                                    Uri = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Host}/api/v2/crossreferences/executions/{executionInfo.ExecutionID}/status"
                                });

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(result)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                   { "CrossReferencesCount", $"{((crossReferences != null) ? crossReferences.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
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
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json", "application/xml"),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "An execution status including a list of Asset Cross References.", typeof(BulkAssetCrossReferenceResult)),
            SwaggerResponse(HttpStatusCode.NotFound, "Execution unique identifier not found.", typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetExecutionStatus(Guid executionID)
        {
            var prefix = "CrossReferences.GetExecutionStatus => ";
            string errorMessage;

            try
            {
                ApiExecution execution = assetRepository.GetExecutionItemByUid(executionID);

                if (execution == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ApiMessages.ExecutionIDNotFound)).ConfigureAwait(false);
                }

                var bulkResult = crossReferencesRepository.GetExecutionStatus(execution);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, bulkResult))).ConfigureAwait(false);
            }
            catch (ArgumentException)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ApiMessages.ExecutionIDNotFound)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "ExecutionUid", executionID.ToString() }, //left to avoid breaking change
                    { "ExecutionID", executionID.ToString() }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }

        private int GetTimeoutFromQueryString()
        {
            var queryParams = Request.GetQueryNameValuePairs();
            var timeout = DEFAULT_DELETE_TIMEOUT;

            queryParams.ToList().ForEach(q =>
            {
                var key = q.Key.ToLower();

                if (key.StartsWith("_"))
                {
                    switch (key)
                    {
                        case "_timeout":
                            if (int.TryParse(q.Value, out timeout))
                            {
                                if (timeout < 1)
                                {
                                    timeout = 30;// min timeout
                                }
                            }
                            break;
                    }
                }
            });

            return timeout;
        }

    }
}
