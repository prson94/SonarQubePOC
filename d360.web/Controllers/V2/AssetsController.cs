using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.extensions;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using Microsoft.Web.Http;
using Newtonsoft.Json;
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

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling glossary-related data such as artifacts and models.
    /// </summary>
    [ 
        ApiVersion("2.0"), 
        RoutePrefix("api/v{version:apiVersion}/assets"), 
        Authorize
    ]
    public class AssetsController : BaseApiController
    {
        #region DI

        IQueueSource QueueSource;
        IStorageProvider Storage;

        public AssetsController(CommunityContext community, CompanyContext company, IStorageProvider storage, IQueueSource queueSource)
            : base(community, company)
        {
            QueueSource = queueSource;
            Storage = storage;
        }

        #endregion

        #region utils

        private async Task<T> readRequestJsonContent<T>(HttpRequestMessage request)
        {
            string json = "";

            if (request.Content.IsMimeMultipartContent())
            {
                var streamProvider = new MultipartMemoryStreamProvider();
                await request.Content.ReadAsMultipartAsync(streamProvider);

                json = await streamProvider.Contents.Single().ReadAsStringAsync();
            }
            else
            {
                json = await request.Content.ReadAsStringAsync();
            }

            return JsonConvert.DeserializeObject<T>(json);
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
            SwaggerResponse(HttpStatusCode.OK, "A list of asset type classes.", typeof(List<AssetTypeClassInfo>))
        ]
        public HttpResponseMessage GetAssetTypeClassesAsync()
        {
            var prefix = "Assets.GetAssetTypeClassesAsync => ";
            var errorMessage = "";

            try
            {
                var classes = AssetTypeClass.Glossary.GetAsList();
                return Request.CreateResponse(HttpStatusCode.OK, classes);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// GET a list of asset types.
        /// </summary>
        /// <returns></returns>
        [
            HttpGet, 
            Route("types"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset types.", typeof(List<AssetTypeApiViewModel>))
        ]
        public async Task<HttpResponseMessage> GetAssetTypesAsync()
        {
            var prefix = "Assets.GetAssetTypesAsync => ";
            var errorMessage = "";

            try
            {
                var assetTypes = await Company.QueryAsync<AssetTypeApiViewModel>(@"
SELECT		A.[Name]
			,A.[Description]
			,A.[Class] as ClassID
			,A.[Notes]
			,A.[uid],
			P.[Path]
FROM		AssetType A
			cross apply dbo.GetAssetTypeTextPathById(A.ID, ' / ') P
where		A.[State] = 1
order by	P.[Path]
");

                return Request.CreateResponse(HttpStatusCode.OK, assetTypes);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// Adds a given set of assets based on the specific asset type Uid. Use this endpoint if you want to process under 200 items and need immediate results.
        /// </summary>
        /// <param name="uid">The unique identifier of the asset type.</param>
        /// <param name="assets">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost, 
            Route("{uid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of bulk asset results, including any error messages.", typeof(List<DatabaseBulkAssetResult>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostAssetsAsync(Guid uid, AssetInserts assets)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not allowed to add assets of this type."));

            var prefix = "Assets.PostBulkAssetsAsync => ";
            var errorMessage = "";

            try
            {
                var assetType = Company.Filter<AssetType>(i => i.uid == uid).SingleOrDefault();

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with UID {uid} could not be found."));

                if (assets == null)
                    assets = readRequestJsonContent<AssetInserts>(Request).Result;

                var results = (Company.Database.Connection as SqlConnection).InsertAssets(
                    QueueSource, 
                    Company.CurrentCompanyDomain, 
                    Company.CurrentCompanyID, 
                    Company.CurrentResourceID, 
                    assetType, 
                    assets
                );
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Updates a given set of assets based on the specific asset type Uid. Use this endpoint if you want to process under 200 items and need immediate results.
        /// </summary>
        /// <param name="uid">The unique identifier of the asset type.</param>
        /// <param name="assets">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPut,
            Route("{uid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of bulk asset results, including any error messages.", typeof(List<DatabaseBulkAssetResult>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutAssetsAsync(Guid uid, AssetUpdates assets)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not allowed to update assets of this type."));

            var prefix = "Assets.PutAssetsAsync => ";
            var errorMessage = "";

            try
            {
                var assetType = Company.Filter<AssetType>(i => i.uid == uid).SingleOrDefault();

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with UID {uid} could not be found."));

                if (assets == null)
                    assets = readRequestJsonContent<AssetUpdates>(Request).Result;

                var results = (Company.Database.Connection as SqlConnection).UpdateAssets(
                    QueueSource, 
                    Company.CurrentCompanyDomain, 
                    Company.CurrentCompanyID, 
                    Company.CurrentResourceID, 
                    assetType, 
                    assets
                );
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        #region Batch

        /// <summary>
        /// Adds a given set of assets based on the specific asset type Uid. This endpoint is meant for a greater number of items as it stores the asset list for asynchronous or batch processing.
        /// </summary>
        /// <param name="uid">The unique identifier of the asset type.</param>
        /// <param name="assets">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            Route("batch/{uid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution ID to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostBulkAssetsAsync(Guid uid, AssetInserts assets)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not allowed to add assets of this type."));

            var prefix = "Assets.PostBulkAssetsAsync => ";
            var errorMessage = "";

            try
            {
                var assetType = Company.Filter<AssetType>(i => i.uid == uid).SingleOrDefault();

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with UID {uid} could not be found."));

                if (assets == null)
                    assets = readRequestJsonContent<AssetInserts>(Request).Result;

                var executionInfo = new ApiExecutionInfo
                {
                    CompanyID = Company.CurrentCompanyID,
                    CompanyDomainPrefix = Company.CurrentCompanyDomain,
                    ExecutionID = Guid.NewGuid(),
                    Action = ApiExecutionAction.PostAssets
                };

                // Save to storage container.
                Storage.CreateFolder(executionInfo.StorageFolder);
                Storage.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(assets));

                // Save to queue.
                await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo);

                // Save to the database.
                Company.Add(new ApiExecution
                {
                    ExecutionID = executionInfo.ExecutionID,
                    Error = 0,
                    Processed = 0,
                    Total = assets.Count,
                    StartedOn = DateTime.UtcNow,
                    ResourceID = Company.CurrentResourceID,
                    Fields = JsonConvert.SerializeObject(new ApiExecutionFields_PostAssets { AssetTypeUid = uid })
                });

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
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Updates a given set of assets based on the specific asset type Uid. This endpoint is meant for a greater number of items as it stores the asset list for asynchronous or batch processing.
        /// </summary>
        /// <param name="uid">The unique identifier of the asset type.</param>
        /// <param name="assets">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPut,
            Route("batch/{uid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution ID to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutBulkAssetsAsync(Guid uid, AssetUpdates assets)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not allowed to update assets of this type."));

            var prefix = "Assets.PutBulkAssetsAsync => ";
            var errorMessage = "";

            try
            {
                var assetType = Company.Filter<AssetType>(i => i.uid == uid).SingleOrDefault();

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with UID {uid} could not be found."));

                if (assets == null)
                    assets = readRequestJsonContent<AssetUpdates>(Request).Result;

                var executionInfo = new ApiExecutionInfo
                {
                    CompanyID = Company.CurrentCompanyID,
                    CompanyDomainPrefix = Company.CurrentCompanyDomain,
                    ExecutionID = Guid.NewGuid(),
                    Action = ApiExecutionAction.PutAssets
                };

                // Save to storage container.
                Storage.CreateFolder(executionInfo.StorageFolder);
                Storage.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(assets));

                // Save to queue.
                await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo);

                // Save to the database.
                Company.Add(new ApiExecution
                {
                    ExecutionID = executionInfo.ExecutionID,
                    Error = 0,
                    Processed = 0,
                    Total = assets.Count,
                    StartedOn = DateTime.UtcNow,
                    ResourceID = Company.CurrentResourceID,
                    Fields = JsonConvert.SerializeObject(new ApiExecutionFields_PostAssets { AssetTypeUid = uid })
                });

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
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// GETs the status of an execution record, including the results for the execution.
        /// </summary>
        /// <param name="uid">The execution ID to retrieve status for.</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("executions/{uid}/status"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "An execution status including a list of assets.", typeof(ApiExecutionStatusModel))
        ]
        public async Task<IHttpActionResult> GetExecutionStatus(Guid uid)
        {
            var prefix = "Assets.GetExecutionStatus => ";
            var errorMessage = "";

            try
            {
                var dbExecutionItem = Company.Filter<ApiExecution>(i => i.ExecutionID == uid).SingleOrDefault();

                if (dbExecutionItem == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", "Execution ID not found."));
                }

                var info = new ApiExecutionInfo { CompanyID = Company.CurrentCompanyID, ExecutionID = uid };

                List<DatabaseBulkAssetResult> results = null;
                try
                {
                    var resultsJson = Storage.GetFileContentsAsString(info.StorageFolder, info.ResponseFileName);
                    results = JsonConvert.DeserializeObject<List<DatabaseBulkAssetResult>>(resultsJson);
                }
                catch
                {
                }

                var statusModel = new ApiExecutionStatusModel {
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
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        #endregion
    }
}
