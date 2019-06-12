using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.extensions;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using SpreadsheetLight;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling glossary-related data such as artifacts and models.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/relationships"),
        Authorize,
        StringEnumController
    ]
    public class RelationshipsController : BaseV2ApiController
    {
        #region DI

        IQueueSource QueueSource;
        IStorageProvider Storage;

        public RelationshipsController(ICommunityContext community, ICompanyContext company, IQueueSource queueSource, IStorageProvider storage)
            : base(community, company)
        {
            QueueSource = queueSource;
            Storage = storage;
        }

        #endregion

        /// <summary>
        /// GET a list of predicates.
        /// </summary>
        /// <param name="PredicateUid">Filter by an predicate's unique identifier.</param>
        /// <param name="Type">Filter by a predicate's functional type.</param>
        /// <param name="Name">Filter by an predicate's Name.</param>
        /// <param name="Inverse">Filter by an predicate's Inverse.</param>
        /// <returns>A list of predicates contained within your Govern environment.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("predicates"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of predicates.", typeof(PredicatesApiViewModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
       ]
        public async Task<HttpResponseMessage> GetPredicatesAsync(Guid? PredicateUid = null, core.enums.PredicateType? Type = null, string Name = null, string Inverse = null)
        {
            var prefix = "Relationships.GetPredicatesAsync => ";
            var errorMessage = "";

            try
            {
                var predicates = await Company.QueryAsync<PredicateApiViewModel>("select Uid, Name, Inverse, IsSystem, [Type] from [Predicate] order by [Type], Name");

                #region Where clause action

                if (PredicateUid.HasValue)
                {
                    predicates = predicates.Where(i => i.Uid == PredicateUid.Value);
                }

                if (Type.HasValue)
                {
                    predicates = predicates.Where(i => i.Type == Type.Value);
                }

                if (!string.IsNullOrEmpty(Name) && !string.IsNullOrWhiteSpace(Name))
                {
                    Name = Name.Trim().ToLower();
                    predicates = predicates.Where(i => i.Name.ToLower() == Name);
                }

                if (!string.IsNullOrEmpty(Inverse) && !string.IsNullOrWhiteSpace(Inverse))
                {
                    Inverse = Inverse.Trim().ToLower();
                    predicates = predicates.Where(i => i.Inverse.ToLower() == Inverse);
                }

                #endregion

                return Request.CreateResponse(HttpStatusCode.OK, predicates);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// GET a list of predicate functional types.
        /// </summary>
        /// <returns>A list of static predicate functional types contained within your Govern environment.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("predicates/types"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of predicate functional types.", typeof(List<PredicateTypeApiViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
       ]
        public HttpResponseMessage GetPredicatesTypesAsync()
        {
            var prefix = "Relationships.GetPredicatesTypesAsync => ";
            var errorMessage = "";

            try
            {
                var types = PredicateType.DataLineage.GetAsList().Select(i => new PredicateTypeApiViewModel
                {
                    Type = i.ID,
                    Name = i.Name,
                    Description = i.Description
                }).OrderBy(i => i.Name).ToList();
                return Request.CreateResponse(HttpStatusCode.OK, types);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }


        #region Endpoints
        /// <summary>
        /// GET a list of relationships.
        /// </summary>
        /// <param name="intersectTypeUid">Filter by an intersect's unique identifier.</param>
        /// <returns>A excel file containing relationships.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("export/{intersectTypeUid}"),
            FileDownload,
            SwaggerConsumes("application/vnd.ms-excel"), SwaggerProduces("application/vnd.ms-excel"),
            SwaggerResponse(HttpStatusCode.OK, "Exported realtionships to Excel.", typeof(List<PredicateTypeApiViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public IHttpActionResult ExportToExcel(string intersectTypeUid)
        {

            var customColumns = Company.Query<string>(
                @"select distinct  f.FriendlyName   as Name from fieldtype f  
				inner join IntersectType IT on IT.uid = @uid
				 where f.[object] = 'IntersectType' and f.objectid = IT.Id", new { uid = intersectTypeUid });

            string customColumnTableSQL = string.Empty;
            string customColumnValuesSQL = string.Empty;

            if (customColumns.Count() > 0)
            {
                customColumnTableSQL = @"DROP TABLE IF EXISTS tempdb.dbo.#TempFieldTable

                            create table #TempFieldTable
                            (
                                ObjectId int, 
                                FriendlyName Varchar(250), 
                                FormattedValue varchar(250),
	                            Id int
                            )

                            insert into #TempFieldTable
                            select f2.ObjectID, f.FriendlyName,FormattedValue ,f2.id
                            from fieldtype f  
                            inner join field f2 on f2.fieldtypeid = f.id 
                            where f.[object] = 'IntersectType'";
                foreach (var item in customColumns)
                {
                    customColumnValuesSQL += $",(Select FormattedValue from #TempFieldTable where ObjectId = I.Id and FriendlyName = '" + item.CleanForSql() + "') as '" + item.CleanForSql() + "'";
                }

            }


            var models = Company.Query<dynamic>(
                customColumnTableSQL +
                @"select  I.ID as ID, 
		                    S.Object as Subject,
		                    S.ObjectId as SubjectID,
		                    SVal.DisplayValue as SubjectName,
		                    ST.Name as SubjectTypeName,
		                    P.Name as PredicateName,
		                    O.Object as Object,
		                    O.ObjectId as ObjectID,
							OVal.DisplayValue as ObjectName,
		                    OT.Name as ObjectTypeName
                            " + customColumnValuesSQL + @"
							from 
	                        [Intersect] I
	                        inner join IntersectType T on T.ID = I.IntersectTypeID
	                        left outer join [Predicate] P on P.ID = T.PredicateID
	                        inner join Asset S on S.Object = I.Subject and S.ObjectID = I.SubjectID
	                        inner join AssetType ST on ST.ID = S.AssetTypeID
	                        inner join Asset O on O.Object = I.Object and O.ObjectID = I.ObjectID
	                        inner join AssetType OT on OT.ID = O.AssetTypeID
							cross apply dbo.GetAssetDisplayValueById(S.ID) as SVal
							cross apply dbo.GetAssetDisplayValueById(O.ID) as OVal

	                        where T.uid in (@Uid)", new { Uid = intersectTypeUid });

            var document = new SLDocument();
            document.AddWorksheet("Items");

            #region Create the list sheet

            #region Header

            int index = 1;
            document.SetCellValue(1, index++, "Intersect ID");
            document.SetCellValue(1, index++, "Subject Type");
            document.SetCellValue(1, index++, "Subject ID");
            document.SetCellValue(1, index++, "Subject Name");
            document.SetCellValue(1, index++, "Subject Type Name");
            document.SetCellValue(1, index++, "Predicate");
            document.SetCellValue(1, index++, "Object Type");
            document.SetCellValue(1, index++, "Object ID");
            document.SetCellValue(1, index++, "Object Name");
            document.SetCellValue(1, index++, "Object Type Name");

            #endregion

            int rowNumber = 1;
            foreach (var row in models)
            {
                index = 1;
                rowNumber++;
                document.SetCellValue(rowNumber, index++, (int)row.ID);
                document.SetCellValue(rowNumber, index++, (string)row.Subject);
                document.SetCellValue(rowNumber, index++, (int)row.SubjectID);
                document.SetCellValue(rowNumber, index++, (string)row.SubjectName);
                document.SetCellValue(rowNumber, index++, (string)row.SubjectTypeName);
                document.SetCellValue(rowNumber, index++, (string)row.PredicateName);
                document.SetCellValue(rowNumber, index++, (string)row.Object);
                document.SetCellValue(rowNumber, index++, (int)row.ObjectID);
                document.SetCellValue(rowNumber, index++, (string)row.ObjectName);
                document.SetCellValue(rowNumber, index++, (string)row.ObjectTypeName);

                foreach (var col in customColumns)
                {
                    var data = (IDictionary<string, object>)row;
                    document.SetCellValue(rowNumber, index++, (string)data[col]);
                }
            }

            #endregion

            var stream = new System.IO.MemoryStream();
            document.SaveAs(stream);

            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(stream.GetBuffer())
            };
            result.Content.Headers.ContentLength = stream.Length;
            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = $"Relationship Type Items {DateTime.Now.ToShortDateString()}.xlsx"
            };
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");

            return ResponseMessage(result);
        }
        #endregion


        /// <summary>
        /// GET a list of relationships.
        /// </summary>
        /// <remarks>
        /// In addition to the below query parameters a field name for the relationship type can be specified to filter by exact match. For example MyCustomField=someExactValue. 
        /// This must be used in conjunction with the RelationshipTypeUid query parameter.
        /// </remarks>
        /// <param name="RelationshipTypeUid">Filter by an relationship type's unique identifier. Using this parameter will also provide any field values for the relationships, if applicable.</param>
        /// <param name="PredicateUid">Filter by an predicate's unique identifier.</param>
        /// <param name="SubjectUid">Filter by a subject asset's unique identifier.</param>
        /// <param name="ObjectUid">Filter by an object asset's unique identifier.</param>
        /// <param name="State">Filter on the state, or status, of a relationship.</param>
        /// <param name="_pageNum">Allows for changing the current page of results you are requesting.</param>
        /// <param name="_pageSize">Allows for changing the page size of results you are requesting. The maximum page size is 250.</param>
        /// <returns></returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of relationships.", typeof(GetRelationshipsApiModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Object representing one of the query parameter values could not be found.", typeof(ErrorResponse))
       ]
        public async Task<HttpResponseMessage> GetRelationshipsAsync(Guid? RelationshipTypeUid = null, Guid? PredicateUid = null, Guid? SubjectUid = null, Guid? ObjectUid = null, core.enums.State? State = null, int? _pageSize = null, int? _pageNum = null)
        {
            var prefix = "Relationships.GetRelationshipsAsync => ";
            var errorMessage = "";

            try
            {
                #region Validation

                if (RelationshipTypeUid.HasValue)
                {
                    if (!Company.Any<IntersectType>(i => i.uid == RelationshipTypeUid.Value)) return ReturnApiError(HttpStatusCode.NotFound, $"Relationship Type with Uid [{RelationshipTypeUid.Value}] could not be found.");
                }

                if (PredicateUid.HasValue)
                {
                    if (!Company.Any<Predicate>(i => i.UID == PredicateUid.Value)) return ReturnApiError(HttpStatusCode.NotFound, $"Predicate with Uid [{PredicateUid.Value}] could not be found.");
                }

                if (SubjectUid.HasValue)
                {
                    if (!Company.Any<Asset>(i => i.uid == SubjectUid.Value)) return ReturnApiError(HttpStatusCode.NotFound, $"Subject with Uid [{SubjectUid.Value}] could not be found.");
                }

                if (ObjectUid.HasValue)
                {
                    if (!Company.Any<Asset>(i => i.uid == ObjectUid.Value)) return ReturnApiError(HttpStatusCode.NotFound, $"Object with Uid [{ObjectUid.Value}] could not be found.");
                }

                #endregion

                var queryParams = Request.GetQueryNameValuePairs().ToList();
                var items = await Company.GetRelationships(queryParams);
                return Request.CreateResponse(HttpStatusCode.OK, items);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// GET a list of relationship types.
        /// </summary>
        /// <param name="AssetTypeUid">Allows for filtering by an asset type's unique identifier, looking at the subject or object type.</param>
        /// <param name="PredicateUid">Allows for filtering of relationship types by predicate unique identifier.</param>
        /// <param name="State">Allows for filtering by the relationship type's state.</param>
        /// <returns></returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("types"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of relationship types, including types names of both the subject and object.", typeof(List<IntersectTypeApiViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
       ]
        public async Task<HttpResponseMessage> GetRelationshipTypesAsync(Guid? PredicateUid = null, Guid? AssetTypeUid = null, core.enums.State? State = null)
        {
            var prefix = "Relationships.GetRelationshipTypesAsync => ";
            var errorMessage = "";

            try
            {
                List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();

                if (AssetTypeUid.HasValue)
                {
                    queryParams.Add(new KeyValuePair<string, string>("AssetTypeUid", AssetTypeUid.Value.ToString()));
                }
                if (PredicateUid.HasValue)
                {
                    queryParams.Add(new KeyValuePair<string, string>("PredicateUid", PredicateUid.Value.ToString()));
                }
                if (State.HasValue)
                {
                    queryParams.Add(new KeyValuePair<string, string>("State", State.ToString()));
                }

                var types = await Company.GetRelationshipTypes(queryParams);

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
        /// <param name="id">The legacy type ID of the asset type.</param>
        /// <param name="type">The legacy object type of the asset type (ArtifactType, FusioAttributeType, TaxonomyType, etc.).</param>
        /// <returns></returns>
        [
            HttpGet,
            ApiExplorerSettings(IgnoreApi = true),
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
                SystemObjects systemType;
                if (Enum.TryParse(type, out systemType))
                {
                    var types = await Company.GetActiveIntersectTypesByObjectType(id, systemType);
                    return Request.CreateResponse(HttpStatusCode.OK, types);
                }
                else
                {
                    return ReturnApiError(HttpStatusCode.BadRequest, "The type parameter is invalid.");
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        [Route("types/{id:int}"), HttpGet, ApiExplorerSettings(IgnoreApi = true)]
        public IQueryable<IntersectType> GetIntersectType(int id)
        {
            return Company.Filter<IntersectType>(i => i.ID == id);
        }

        #region Bulk Relationships

        /// <summary>
        /// Takes a given set of relationships and inserts/updates them. Use this endpoint if you want to process under 250 items and need immediate results.
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
                    relationships = readRequestJsonContent<RelationshipInserts>(Request, true).Result;

                if (relationships == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                if (relationships.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"You may only provide a maximum of {MAX_SYNCHRONOUS_API_ITEM_COUNT} relationships in this request. Please call the BATCH API to submit more than {MAX_SYNCHRONOUS_API_ITEM_COUNT} items."));

                var execution = getApiExecution(relationships.Count, new ApiExecutionFields_PostRelationships { IntersectTypeUid = intersectTypeUid });
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
                    relationships = readRequestJsonContent<RelationshipInserts>(Request, true).Result;

                if (relationships == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

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

                var execution = getApiExecution(relationships.Count, new ApiExecutionFields_PostRelationships { IntersectTypeUid = intersectTypeUid });
                execution.ExecutionID = executionInfo.ExecutionID;
                Company.Add(execution);


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

        /// <summary>
        /// Deletes a relationship and children of a given uid.
        /// </summary>
        /// <param name="intersectTypeUid">The unique identifier of the relationship type.</param>
        /// <param name="relationships">The list of relationships for deletions.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            Route("types/{intersectTypeUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the DELETE request.", typeof(List<RelationshipDeleteApiStatus>)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to delete relationship of this type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteRelationship(Guid intersectTypeUid, RelationshipDeletes relationships)
        {
            var prefix = "Relationships.DeleteRelationship => ";
            var errorMessage = "";



            try
            {
                var intersectType = Company.IntersectTypes.FirstOrDefault(x => x.uid == intersectTypeUid);
                if (intersectType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Relationship Type with Uid {intersectTypeUid} could not be found."));

                bool hasRight = Company.CurrentResourceIsAdmin;

                if (!hasRight)
                {
                    hasRight = Company.HasAssetTypePermission(intersectType.Object, intersectType.ObjectID, Permission.ModifyAsset)
                           && Company.HasAssetTypePermission(intersectType.Subject, intersectType.SubjectID, Permission.ModifyAsset)
                           && Company.HasAssetTypePermission(intersectType.Object, intersectType.ObjectID, Permission.ModifyRelationships)
                           && Company.HasAssetTypePermission(intersectType.Subject, intersectType.SubjectID, Permission.ModifyRelationships);
                }

                if (!hasRight)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not allowed to delete relationships of this type."));
                }

                if (relationships == null || relationships.Count == 0)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad request", "You have not provided a valid JSON structure for this request.!"));
                }

                foreach (var item in relationships)
                {
                    if (item.Uid == null || item.Uid == Guid.Empty)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad request", "You have not provided a valid GUID!"));
                    }
                }



                StringBuilder relationshipUids = new StringBuilder();
                foreach (var rel in relationships)
                {
                    relationshipUids.Append($"('{rel.Uid.ToString()}')");
                    if (rel != relationships.Last())
                        relationshipUids.Append(",");
                }

                //Get Intersect ID for delete and union all child Intersects
                var getRelationshipIDsForDeleting = @"declare @relationships table(uid uniqueidentifier)
                                                      insert into @relationships values " + relationshipUids.ToString() + @"
                                                   
                                                      declare @results table(ID int, Uid uniqueidentifier,ParentUid uniqueidentifier )
                                                      ;WITH ITS AS
                                                          (  
                                                            SELECT T.*, REL.uid as ParentUid
                                                            FROM [Intersect] as T
                                                              inner join @relationships REL on REL.uid = T.uid
                                                            WHERE T.uid in (select uid from @relationships)  AND T.IntersectTypeID = @intersectTypeId
                                                      )
                                                      insert into @results (ID, Uid, ParentUid) select ID, Uid, ParentUid from ITS
                                                   
                                                      ;WITH CHILDREN AS
                                                       (
                                                        SELECT I.ID, I.uid, RES.ParentUid FROM [Intersect] AS I
	                                                      inner join @results RES on (I.Subject = 'Intersect' and I.SubjectID = RES.ID) OR (I.Object = 'Intersect' and I.ObjectID = RES.ID)
                                                        )
                                                      insert into @results (ID, Uid, ParentUid) select ID, Uid, ParentUid from CHILDREN
                                                   
                                                      select * from @results";

                var forDeleteCheck = await Company.QueryAsync<dynamic>(getRelationshipIDsForDeleting, new { intersectTypeId = intersectType.ID });


                List<int> parentRelationships = new List<int>();
                List<int> childrenRelationships = new List<int>();

                var response = new List<RelationshipDeleteApiStatus>();
                foreach (var rel in relationships)
                {
                    var status = new RelationshipDeleteApiStatus();
                    status.Uid = rel.Uid;
                    status.Message = "Relationship deleted";

                    var deleteItems = forDeleteCheck.Where(x => x.ParentUid == rel.Uid);
                    if (deleteItems.Count() == 0)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Relationship with Uid {rel.Uid} could not be found."));
                    }

                    if (deleteItems.Count() > 1 && !rel.Cascade)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad request", $"Relationship with Uid {rel.Uid} have child relationships. Use Cascade = true to delete all child relationships."));
                    }

                    status.Success = true;


                    foreach (var item in deleteItems)
                    {
                        if (rel.Uid == Guid.Parse(item.Uid.ToString()))
                            parentRelationships.Add(int.Parse(item.ID.ToString()));
                        else
                            childrenRelationships.Add(int.Parse(item.ID.ToString()));
                    }

                    response.Add(status);
                }

                Company.Delete<Intersect>(x => childrenRelationships.Contains(x.ID));
                Company.Delete<Intersect>(x => parentRelationships.Contains(x.ID));

                return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            response
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

    }

}
