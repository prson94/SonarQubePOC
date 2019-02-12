using d360.core;
using d360.core.entities;
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
        RoutePrefix("api/v{version:apiVersion}/relationships"), Authorize]
    public class RelationshipsController : BaseApiController
    {
        #region DI

        IQueueSource QueueSource;
        IStorageProvider Storage;

        public RelationshipsController(CommunityContext community, CompanyContext company, IQueueSource queueSource, IStorageProvider storage)
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
        /// GET a list of relationship types.
        /// </summary>
        /// <returns></returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("types"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of relationship types, including types names of both the subject and object.", typeof(List<IntersectTypeApiViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
       ]
        public async Task<HttpResponseMessage> GetRelationshipTypesAsync()
        {
            var prefix = "Relationships.GetRelationshipTypesAsync => ";
            var errorMessage = "";

            try
            {
                var types = await Company.GetActiveIntersectTypes();

                return Request.CreateResponse(HttpStatusCode.OK, types);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }
        
        /// <summary>
        /// GET a list of relationship types using an ID and a Type.
        /// </summary>
        /// <returns></returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("types/{id}/{type}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of relationship types by a given Type and Id, including types names of both the subject and object.", typeof(List<IntersectTypeApiViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
       ]
        public async Task<HttpResponseMessage> GetRelationshipTypesAsync(int id, string type)
        {
            var prefix = "Relationships.GetRelationshipTypesAsync => ";
            var errorMessage = "";

            try
            {
                var types = await Company.GetActiveIntersectTypes(id, type);

                return Request.CreateResponse(HttpStatusCode.OK, types);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        #region Bulk Relationships

        /// <summary>
        /// Takes a given set of relationships and bulk inserts/updates them.
        /// </summary>
        /// <param name="intersectTypeUid">The unique identifier of the intersect type.</param>
        /// <param name="relationships">The payload of your request. Must include SubjectAssetUid and ObjectAssetUid.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            Route("{intersectTypeUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of bulk relationship results, including any error messages.", typeof(List<DatabaseBulkRelationshipResult>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add assets of this type.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostRelationshipsAsync(Guid intersectTypeUid, RelationshipInserts relationships)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add/update relationships of this type.")));

            var prefix = "Relationships.PostRelationshipsAsync => ";
            var errorMessage = "";

            try
            {
                var intersectType = Company.Filter<IntersectType>(i => i.uid == intersectTypeUid).SingleOrDefault();

                if (intersectType == null)
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Relationship Type with Uid {intersectTypeUid} could not be found.")));
                if (relationships == null)
                    relationships = readRequestJsonContent<RelationshipInserts>(Request).Result;

                var execution = new ApiExecution
                {
                    ExecutionID = Guid.NewGuid(),
                    Error = 0,
                    Processed = 0,
                    Total = relationships.Count,
                    StartedOn = DateTime.UtcNow,
                    ResourceID = Company.CurrentResourceID,
                    Fields = JsonConvert.SerializeObject(new ApiExecutionFields_PostRelationships { IntersectTypeUid = intersectTypeUid })
                };
                Company.Add(execution);

                List<DatabaseBulkRelationshipResult> results = null;
                try
                {
                    results = Company.ImportRelationships(execution, intersectType, relationships);

                    // Close execution record.
                    execution.Processed = results.Count;
                    execution.Error = results.Count(i => !i.Success);
                    execution.CompletedOn = DateTime.UtcNow;
                    Company.Update(execution);
                }
                catch (Exception ex)
                {
                    execution.ErrorMessage = ex.GetFullExceptionData(false);
                    execution.CompletedOn = DateTime.UtcNow;
                    Company.Update(execution);
                }

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage)));
            }
        }


        /// <summary>
        /// Inserts or updates a given set of relationships based on the specific relationship type Uid. This endpoint is meant for a greater number of items as it stores the relationship list for asynchronous or batch processing.
        /// </summary>
        /// <param name="intersectTypeUid">The unique identifier of the intersect type.</param>
        /// <param name="relationships">The payload of your request. Must include SubjectAssetUid and ObjectAssetUid.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            Route("batch/{intersectTypeUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution ID to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)), SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add assets of this type.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostBulkRelationshipsAsync(Guid intersectTypeUid, RelationshipInserts relationships)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not allowed to add assets of this type."));

            var prefix = "Relationships.PostBulkRelationshipsAsync => ";
            var errorMessage = "";

            try
            {
                var intersectType = Company.Filter<IntersectType>(i => i.uid == intersectTypeUid).SingleOrDefault();

                if (intersectType == null)
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Relationship Type with Uid {intersectTypeUid} could not be found.")));

                if (relationships == null)
                    relationships = readRequestJsonContent<RelationshipInserts>(Request).Result;

                var executionInfo = new ApiExecutionInfo
                {
                    CompanyID = Company.CurrentCompanyID,
                    ResourceID = Company.CurrentResourceID,
                    CompanyDomainPrefix = Company.CurrentCompanyDomain,
                    ExecutionID = Guid.NewGuid(),
                    Action = ApiExecutionAction.PostRelationships
                };

                Storage.CreateFolder(executionInfo.StorageFolder);
                Storage.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(relationships));

                await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo);

                Company.Add(new ApiExecution
                {
                    ExecutionID = executionInfo.ExecutionID,
                    Error = 0,
                    Processed = 0,
                    Total = relationships.Count,
                    StartedOn = DateTime.UtcNow,
                    ResourceID = Company.CurrentResourceID,
                    Fields = JsonConvert.SerializeObject(new ApiExecutionFields_PostRelationships { IntersectTypeUid = intersectTypeUid })
                });

                return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            new ApiExecutionRecievedResponse
                            {
                                ExecutionID = executionInfo.ExecutionID,
                                Message = "Now processing request. Please check back with this ExecutionID for status.",
                                Uri = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Host}/api/v2/relationships/executions/{executionInfo.ExecutionID}/status"
                            }
                        )
                    )
                );
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage)));
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
            SwaggerResponse(HttpStatusCode.OK, "An execution status including a list of relationships.", typeof(ApiExecutionStatusModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetExecutionStatus(Guid executionUid)
        {
            var prefix = "Relationships.GetExecutionStatus => ";
            var errorMessage = "";

            try
            {
                var dbExecutionItem = Company.Filter<ApiExecution>(i => i.ExecutionID == executionUid).SingleOrDefault();

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
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        #endregion

    }
}
