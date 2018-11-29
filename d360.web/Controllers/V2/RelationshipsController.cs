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
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "A list of relationship types, including types names of both the subject and object.", typeof(List<IntersectTypeApiViewModel>))
       ]
        public async Task<HttpResponseMessage> GetRelationshipTypesAsync()
        {
            var prefix = "Relationships.GetRelationshipTypesAsync => ";
            var errorMessage = "";

            try
            {
                var types = await Company.QueryAsync<IntersectTypeApiViewModel>(@"
select	I.Uid,
		P.Name as PredicateName,
		P.Inverse as PredicateInverse,
		P.[Type] as PredicateTypeID,
		I.SubjectUid,
		S.Class as SubjectClassID,
		case 
			when I.SubjectUid = '0000000A-0000-0000-0000-000000000009' then 'Reference List' 
			when I.SubjectUid = '00000001-0000-0000-0000-a00000000011' then 'User'
			when I.SubjectUid = '00000001-0000-0000-0000-a00000000012' then 'Group'
			else S.Name 
		end as SubjectTypeName,
		I.ObjectUid,
		O.Class as ObjectClassID,
		case 
			when I.ObjectUid = '0000000A-0000-0000-0000-000000000009' then 'Reference List' 
			when I.ObjectUid = '00000001-0000-0000-0000-a00000000011' then 'User'
			when I.ObjectUid = '00000001-0000-0000-0000-a00000000012' then 'Group'
			else O.Name 
		end as ObjectTypeName
from	IntersectType I
		left join [Predicate] P on P.ID = I.PredicateID
		left join AssetType S on S.uid = I.SubjectUid
		left join AssetType O on O.uid = I.ObjectUid
where	coalesce(S.uid, I.SubjectUid) is not null
		and coalesce(O.uid, I.ObjectUid) is not null");

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
        /// <param name="uid">The unique identifier of the relationship type.</param>
        /// <param name="relationships">The payload of your request. Must include SubjectUid and ObjectUid.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost, 
            MapToApiVersion("2.0"), 
            Route("{uid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),//, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "A list of bulk relationship results, including any error messages.", typeof(List<DatabaseBulkRelationshipResult>))
        ]
        public async Task<IHttpActionResult> PostRelationshipsAsync(Guid uid, RelationshipInserts relationships)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add/update relationships of this type.")));

            var prefix = "Relationships.PostRelationshipsAsync => ";
            var errorMessage = "";

            try
            {
                var intersectType = Company.Filter<IntersectType>(i => i.uid == uid).SingleOrDefault();

                if (intersectType == null)
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Relationship Type with UID {uid} could not be found.")));
                if (relationships == null)
                    relationships = readRequestJsonContent<RelationshipInserts>(Request).Result;

                var results = (Company.Database.Connection as SqlConnection).BulkRelationshipsImport(
                    QueueSource, 
                    Company.CurrentCompanyDomain, Company.CurrentCompanyID, Company.CurrentResourceID, 
                    intersectType,
                    relationships);

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
        /// <param name="uid">The unique identifier of the intersect type.</param>
        /// <param name="relationships">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            Route("batch/{uid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution ID to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostBulkRelationshipsAsync(Guid uid, RelationshipInserts relationships)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not allowed to add assets of this type."));

            var prefix = "Relationships.PostBulkRelationshipsAsync => ";
            var errorMessage = "";

            try
            {
                var intersectType = Company.Filter<IntersectType>(i => i.uid == uid).SingleOrDefault();

                if (intersectType == null)
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Relationship Type with UID {uid} could not be found.")));

                if (relationships == null)
                    relationships = readRequestJsonContent<RelationshipInserts>(Request).Result;

                var executionInfo = new ApiExecutionInfo
                {
                    CompanyID = Company.CurrentCompanyID,
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
                    Fields = JsonConvert.SerializeObject(new ApiExecutionFields_PostRelationships { IntersectTypeUid = uid })
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
        /// <param name="uid">The execution ID to retrieve status for.</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("executions/{uid}/status"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "An execution status including a list of relationships.", typeof(ApiExecutionStatusModel))
        ]
        public async Task<IHttpActionResult> GetExecutionStatus(Guid uid)
        {
            var prefix = "Relationships.GetExecutionStatus => ";
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
