using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.extensions;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using Resources;
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
using System.IO;

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
        IRelationshipRepository RelationshipRepository;
        IFieldsRepository FieldsRepository;
        IAssetRepository AssetRepository;

        public RelationshipsController(ICommunityContext community, ICompanyContext company, IQueueSource queueSource, IStorageProvider storage, IRelationshipRepository relationshipRepository, IFieldsRepository fieldsRepository, IAssetRepository assetRepository)
            : base(community, company)
        {
            QueueSource = queueSource;
            Storage = storage;
            this.RelationshipRepository = relationshipRepository;
            this.FieldsRepository = fieldsRepository;
            this.AssetRepository = assetRepository;
        }

        #endregion

        /// <summary>
        /// GET a list of predicates.
        /// </summary>
        /// <param name="PredicateUid">Filter by a predicate's unique identifier.</param>
        /// <param name="Type">Filter by a predicate's functional type.</param>
        /// <param name="Name">Filter by a predicate's Name.</param>
        /// <param name="Inverse">Filter by a predicate's Inverse.</param>
        /// <param name="IsUsed">Filter by a predicate's usage.</param>
        /// <returns>A list of predicates contained within your Govern environment.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("predicates"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of predicates.", typeof(PredicatesApiViewModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))
       ]
        public async Task<HttpResponseMessage> GetPredicatesAsync(Guid? PredicateUid = null, PredicateType? Type = null, string Name = null, string Inverse = null, bool? IsUsed = null)
        {
            var prefix = "Relationships.GetPredicatesAsync => ";
            var errorMessage = "";

            try
            {
                IEnumerable<PredicateApiViewModel> predicates = await RelationshipRepository.GetPredicates(PredicateUid, Type, Name, Inverse, IsUsed);

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
        /// Deletes a given set of predicates.
        /// </summary>
        /// <param name="predicates">The list of predicates for deletion.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            Route("predicates"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the DELETE request.", typeof(List<PredicateDeleteResult>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to delete predicates of this type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeletePredicates(PredicateDeletes predicates)
        {
            var prefix = "Relationships.DeletePredicate => ";
            var errorMessage = "";
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage));

                if (predicates == null)
                    predicates = readRequestJsonContent<PredicateDeletes>(Request, true).Result;

                if (predicates == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                if (predicates.Count == 0)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided any predicates to process in this request."));

                if (predicates.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"You may only provide a maximum of {MAX_SYNCHRONOUS_API_ITEM_COUNT} predicates in this request."));

                var execution = getApiExecution(predicates.Count);


                List<PredicateDeleteResult> results = RelationshipRepository.DeletePredicates(predicates, execution);
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
        /// Inserts or updates a given set of predicates.
        /// </summary>
        /// <remarks>
        /// You also have the option of providing a Uid for this new predicate, which helps when migrating between environments. 
        /// When updating an existing predicate, you must include its Uid in the request.
        /// </remarks>
        /// <param name="predicates">The list of predicates for insertion.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            Route("predicates"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(List<PredicateUpsertResult>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add predicates.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> UpsertPredicates(PredicateUpserts predicates)
        {
            var prefix = "Relationships.InsertPredicate => ";
            var errorMessage = "";
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage));

                if (predicates == null)
                    predicates = readRequestJsonContent<PredicateUpserts>(Request, true).Result;

                if (predicates == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                if (predicates.Count == 0)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided any predicates to process in this request."));

                if (predicates.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"You may only provide a maximum of {MAX_SYNCHRONOUS_API_ITEM_COUNT} predicates in this request."));

                foreach (var pred in predicates)
                {
                    if (pred.Name.Length > 100)
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Name must be less then 100 characters."));
                    if (pred.Inverse.Length > 250)
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Inverse must be less then 250 characters."));
                }

                var execution = getApiExecution(predicates.Count);


                List<PredicateUpsertResult> results = RelationshipRepository.UpsertPredicates(predicates, execution);
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
        /// GET a list of predicate functional types.
        /// </summary>
        /// <returns>A list of static predicate functional types contained within your Govern environment.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("predicates/types"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of predicate functional types.", typeof(List<PredicateTypeApiViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))
       ]
        public HttpResponseMessage GetPredicatesTypesAsync()
        {
            var prefix = "Relationships.GetPredicatesTypesAsync => ";
            var errorMessage = "";

            try
            {
                var lineageVersion = Community.GetCompanySettingByKey<int>("LineageVersion");

                var types = PredicateType.DataLineage.GetAsList()
                    .Where(i => i.LineageVersionsSupported.Contains(lineageVersion) && !i.Obsolete)
                    .Select(i => new PredicateTypeApiViewModel
                    {
                        Type = i.ID,
                        Name = i.Name,
                        Description = i.Description
                    })
                    .OrderBy(i => i.Name)
                    .ToList();
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
        /// GET a list of relationships.
        /// </summary>
        /// <param name="intersectTypeUid">Filter by an intersect's unique identifier.</param>
        /// <returns>A excel file containing relationships.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("export/{intersectTypeUid}"),
            FileDownload,
            SwaggerConsumes("application/vnd.ms-excel"), SwaggerProduces("application/octet-stream"),
            SwaggerResponse(HttpStatusCode.OK, "Exported relationships to Excel.", typeof(List<PredicateTypeApiViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))
        ]
        public IHttpActionResult ExportToExcel(string intersectTypeUid)
        {
            Guid guid = Guid.Parse(intersectTypeUid);
            int id = RelationshipRepository.GetIntersectTypeByUid(guid).ID;

            var customColumns = FieldsRepository.GetCustomFields(SystemObjects.IntersectType, id);
            IEnumerable<dynamic> models;

            if (customColumns.Count() > 0)
                models = RelationshipRepository.GetExportModelWithCustomFields(id, customColumns);
            else
                models = RelationshipRepository.GetExportModel(id);

            SLDocument document = GetDocumentFromModels(customColumns, models);

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

        private static SLDocument GetDocumentFromModels(IEnumerable<string> customColumns, IEnumerable<dynamic> models)
        {
            var document = new SLDocument();
            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, "Items");

            int index = 1;

            document.SetCellValue(1, index++, "Subject Type");
            document.SetCellValue(1, index++, "Subject Name");
            document.SetCellValue(1, index++, "Subject Type Name");
            document.SetCellValue(1, index++, "Predicate");
            document.SetCellValue(1, index++, "Object Type");
            document.SetCellValue(1, index++, "Object Name");
            document.SetCellValue(1, index++, "Object Type Name");

            document.SetCellValue(1, index++, "Relationship UID");
            document.SetCellValue(1, index++, "Intersect ID");
            document.SetCellValue(1, index++, "Subject UID");
            document.SetCellValue(1, index++, "Subject ID");
            document.SetCellValue(1, index++, "Object UID");
            document.SetCellValue(1, index++, "Object ID");

            foreach (var col in customColumns)
            {
                document.SetCellValue(1, index++, col);
            }

            int rowNumber = 1;
            foreach (var row in models)
            {
                index = 1;
                rowNumber++;

                document.SetCellValue(rowNumber, index++, (string)row.Subject);
                document.SetCellValue(rowNumber, index++, (string)row.SubjectName);
                document.SetCellValue(rowNumber, index++, (string)row.SubjectTypeName);
                document.SetCellValue(rowNumber, index++, (string)row.PredicateName);
                document.SetCellValue(rowNumber, index++, (string)row.Object);
                document.SetCellValue(rowNumber, index++, (string)row.ObjectName);
                document.SetCellValue(rowNumber, index++, (string)row.ObjectTypeName);

                document.SetCellValue(rowNumber, index++, (row.UID ?? "").ToString());
                document.SetCellValue(rowNumber, index++, (int)row.ID);
                document.SetCellValue(rowNumber, index++, row.SubjectUid.ToString());
                document.SetCellValue(rowNumber, index++, (int)row.SubjectID);
                document.SetCellValue(rowNumber, index++, row.ObjectUid.ToString());
                document.SetCellValue(rowNumber, index++, (int)row.ObjectID);



                foreach (var col in customColumns)
                {
                    var data = (IDictionary<string, object>)row;
                    document.SetCellValue(rowNumber, index++, (string)data[col]);
                }
            }

            return document;
        }

        /// <summary>
        /// GET a list of relationships.
        /// </summary>
        /// <remarks>
        /// In addition to the below query parameters a field name for the relationship type can be specified to filter by exact match. For example MyCustomField=someExactValue. 
        /// This must be used in conjunction with the RelationshipTypeUid query parameter.
        /// </remarks>
        /// <param name="State">Filter on the state, or status, of a relationship.</param>
        /// <returns></returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json", "application/octet-stream"),
            SwaggerResponse(HttpStatusCode.OK, "A list of relationships.", typeof(GetRelationshipsApiModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Object representing one of the query parameter values could not be found.", typeof(ErrorResponse)),
            SwaggerParameter("RelationshipTypeUid", "Filter by a relationship type's unique identifier. Using this parameter will also provide any field values for the relationships, if applicable.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("PredicateUid", "Filter by a predicate's unique identifier.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("SubjectUid", "Filter by a subject asset's unique identifier.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("ObjectUid", "Filter by an object asset's unique identifier.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "Allows for changing the current page of results you are requesting.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "Allows for changing the page size of results you are requesting. The maximum page size is 5000, the default is 250.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Allows you to disable including the count of the total number of results across pages in the response.  The default is true meaning the total count is included and if leave out this parameter.", DataType = "boolean", ParameterType = "query", Required = false),
       ]
        public async Task<HttpResponseMessage> GetRelationshipsAsync(State? State = null)
        {
            var prefix = "Relationships.GetRelationshipsAsync => ";
            var errorMessage = "";

            try
            {
                #region Validation
                var queryParams = Request.GetQueryNameValuePairs().ToList();
                var isStreamResponse = Request?.Headers?.Accept?.Any(a => a.MediaType == "application/octet-stream") ?? false;


                if (queryParams.Any(x => x.Key.ToLower() == "relationshiptypeuid"))
                {
                    Guid RelationshipTypeUid = Guid.Empty;
                    var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "relationshiptypeuid").Value;
                    Guid.TryParse(value, out RelationshipTypeUid);
                    if (RelationshipTypeUid == null || RelationshipTypeUid == Guid.Empty)
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest, $"Invalid Relationship Type Uid passed in the request");
                    }
                    else
                    {
                        if (!RelationshipRepository.AnyExists(RelationshipTypeUid))
                            return ReturnApiError(HttpStatusCode.NotFound, $"Relationship Type with Uid [{RelationshipTypeUid}] could not be found.");
                    }
                }

                if (queryParams.Any(x => x.Key.ToLower() == "predicateuid"))
                {
                    Guid PredicateUid = Guid.Empty;
                    var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "predicateuid").Value;
                    Guid.TryParse(value, out PredicateUid);
                    if (PredicateUid == null || PredicateUid == Guid.Empty)
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest, $"Invalid Predicate Uid passed in the request");
                    }
                    else
                    {
                        if (!RelationshipRepository.AnyPredicateExists(PredicateUid))
                            return ReturnApiError(HttpStatusCode.NotFound, $"Predicate with Uid [{PredicateUid}] could not be found.");
                    }
                }

                if (queryParams.Any(x => x.Key.ToLower() == "subjectuid"))
                {
                    Guid SubjectUid = Guid.Empty;
                    var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "subjectuid").Value;
                    Guid.TryParse(value, out SubjectUid);
                    if (SubjectUid == null || SubjectUid == Guid.Empty)
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest, $"Invalid Subject Uid passed in the request");
                    }
                    else
                    {
                        if (!AssetRepository.DoesAssetExists(SubjectUid))
                            return ReturnApiError(HttpStatusCode.NotFound, $"Subject with Uid [{SubjectUid}] could not be found.");
                    }
                }

                if (queryParams.Any(x => x.Key.ToLower() == "objectuid"))
                {
                    Guid ObjectUid = Guid.Empty;
                    var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "objectuid").Value;
                    Guid.TryParse(value, out ObjectUid);
                    if (ObjectUid == null || ObjectUid == Guid.Empty)
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest, $"Invalid Object Uid passed in the request");
                    }
                    else
                    {
                        if (!AssetRepository.DoesAssetExists(ObjectUid))
                            return ReturnApiError(HttpStatusCode.NotFound, $"Object with Uid [{ObjectUid}] could not be found.");
                    }
                }

                #endregion

                string isValid = isPageSizeAndNumValid(queryParams);

                if (!string.IsNullOrEmpty(isValid))
                {
                    return ReturnApiError(HttpStatusCode.BadRequest, isValid);
                }

                HttpResponseMessage response;

                if (isStreamResponse)
                {
                    var items = await RelationshipRepository.GetRelationshipsExcel(queryParams);

                    var stream = new MemoryStream();
                    items.SaveAs(stream);
                    byte[] bytes = stream.ToArray();
                    response = createFileResponseMessage(HttpStatusCode.OK, "GetRelationships.xlsx", bytes);
                    return response;
                }

                else
                {
                    var results = await RelationshipRepository.GetRelationships(queryParams);
                    response = Request.CreateResponse(HttpStatusCode.OK, results);
                    return response;
                }
                
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// GET a list of relationship uids and the uids for the subject / object items in a given relationship type if applicable.
        /// </summary>
        /// <remarks>
        /// Used to return just the relationship uid, subject uid and object uid for a given relationship type.  Objects that are not assets will not have subject / object uids returned.  This endpoint is limited to administrators only.  Other API endpoints can then be used to get further details.
        /// </remarks>        
        /// <returns></returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("uids/{RelationshipTypeUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of relationships.", typeof(RelationshipUidResult)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid parameters provided to the api endpoint.", typeof(ErrorResponse)),
            SwaggerParameter("_pageNum", "Allows for changing the current page of results you are requesting.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "Allows for changing the page size of results you are requesting. The default is 5000 and the maximum value is 100,000.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Allows you to disable including the count of the total number of results across pages in the response.  The default is true meaning the total count is included and if leave out this parameter.", DataType = "boolean", ParameterType = "query", Required = false),
       ]
        public async Task<HttpResponseMessage> GetRelationshipUidsAsync(Guid RelationshipTypeUid)
        {
            var prefix = "Relationships.GetRelationshipUidsAsync => ";
            var errorMessage = "";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return ReturnApiError(HttpStatusCode.Forbidden, "You are not authorized to perform this action.");
                }


                var queryParams = Request.GetQueryNameValuePairs().ToList();                
                long pageSize = 5000;
                long pageNum = 1;
                bool includeTotal = true;

                if (queryParams.Any(x => x.Key.ToLower() == "_pagenum"))
                {                    
                    var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_pagenum").Value;
                    if(!long.TryParse(value, out pageNum))                    
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest, $"Invalid _pageNum parameter passed in the request");
                    }                    
                }

                if (queryParams.Any(x => x.Key.ToLower() == "_pagesize"))
                {
                    var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_pagesize").Value;
                    if (!long.TryParse(value, out pageSize))
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest, $"Invalid _pageSize parameter passed in the request");
                    }       
                    
                    if(pageSize > 100000)
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest, $"Invalid _pageSize parameter passed in the request value is greater than the maximum supported value of 100,000.");
                    }

                    if(pageSize<= 0)
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest, $"Invalid _pageSize parameter passed in the request value is less than or equal to zero.");
                    }
                }

                if (queryParams.Any(x => x.Key.ToLower() == "_includetotal"))
                {
                    var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_includetotal").Value;
                    if (!bool.TryParse(value, out includeTotal))
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest, $"Invalid _includeTotal parameter passed in the request, value must be true false or not specified");
                    }
                }

                if(RelationshipTypeUid == Guid.Empty)
                {
                    return ReturnApiError(HttpStatusCode.BadRequest, $"Invalid relationship type uid specified.  Please specify a valid uid.");
                }

                int intersectTypeID = await (Company.QueryFirstOrDefaultAsync<int>("select id from [intersecttype] where [uid] = @uid", new { uid = RelationshipTypeUid }));

                if (intersectTypeID <= 0)
                {
                    return ReturnApiError(HttpStatusCode.BadRequest, $"Invalid relationship type uid specified.  Please specify a valid uid for an existing relationship type.");
                }

                var results = await RelationshipRepository.GetRelationshipsUids(intersectTypeID, pageSize, pageNum, includeTotal);
                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK, results );
                return response;


            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// Creates relationship types based on the provided subject, object and predicate properties.
        /// </summary>
        /// <remarks>
        /// You have the option of providing a Uid for each of the new relationship types. This is particularly useful in a migration scenario where you want to migrate a relationship type from one environment to another. The default is to not provide one, in which case a Uid will be automatically generated.
        /// </remarks>
        /// <param name="relationshiptypes">A list of relationship types you want to add.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            Route("types"),
            SwaggerRequestExample(typeof(RelationshipTypeInsert), typeof(RelationshipTypeInsertExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to create the relationship type", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A list of relationship types  uid, including any error / success messages.", typeof(List<RelationshipTypeResult>))
        ]
        public async Task<IHttpActionResult> PostRelationshipTypesAsync(List<RelationshipTypeInsert> relationshiptypes)
        {
            var prefix = "Relationships.PostRelationshipTypesAsync => ";
            var errorMessage = "";
            try
            {

                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not authorized to perform this action."));

                if (relationshiptypes == null)
                    relationshiptypes = readRequestJsonContent<List<RelationshipTypeInsert>>(Request).Result;

                if (relationshiptypes == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                if (relationshiptypes.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"You may only provide a maximum of {MAX_SYNCHRONOUS_API_ITEM_COUNT} relationship types in this request."));

                var execution = getApiExecution(relationshiptypes.Count);

                var results = RelationshipRepository.PostRelationshipTypes(relationshiptypes, execution);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Error", errorMessage));
            }
        }

        /// <summary>
        /// This endpoint is used to update an existing relationship types predicate or cardinality properties.
        /// </summary>
        /// <param name="relationshiptypes">A list of relationship types you want to update.</param>
        /// <returns>>An HTTP status code and message.</returns>
        [
           HttpPut,
           Route("types"),
           SwaggerRequestExample(typeof(RelationshipTypeUpdate), typeof(RelationshipTypeUpdateExample)),
           SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
           SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
           SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to update the relationship type", typeof(ErrorResponse)),
           SwaggerResponse(HttpStatusCode.OK, "A list of relationship types  uid, including any error / success messages.", typeof(List<RelationshipTypeResult>))
       ]
        public async Task<IHttpActionResult> PutRelationshipTypesAsync(List<RelationshipTypeUpdate> relationshiptypes)
        {
            var prefix = "Relationships.PutRelationshipTypesAsync => ";
            var errorMessage = "";
            try
            {

                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not authorized to perform this action."));

                if (relationshiptypes == null)
                    relationshiptypes = readRequestJsonContent<List<RelationshipTypeUpdate>>(Request).Result;

                if (relationshiptypes == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                if (relationshiptypes.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"You may only provide a maximum of {MAX_SYNCHRONOUS_API_ITEM_COUNT} relationship types in this request."));

                var execution = getApiExecution(relationshiptypes.Count);

                var results = RelationshipRepository.PutRelationshipTypes(relationshiptypes, execution);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Error", errorMessage));
            }
        }

        /// <summary>
        /// Removes the specified relationship types based on the provided relationship type Uid(s). When Cascade=true, the call deletes all relationships based on this type. When false, it triggers an error message stating that there are existing relationships.
        /// </summary>
        /// <param name="relationshiptypes">The list of relationship types for deletion.</param>
        /// <returns>>An HTTP status code and message.</returns>
        [
           HttpDelete,
           Route("types"),
           SwaggerRequestExample(typeof(RelationshipTypeDelete), typeof(RelationshipTypeDeleteExample)),
           SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
           SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
           SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to update the relationship type", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.OK, "A list of relationship types  uid, including any error / success messages.", typeof(List<RelationshipTypeResult>))
       ]
        public async Task<IHttpActionResult> DeleteRelationshipTypesAsync(List<RelationshipTypeDelete> relationshiptypes)
        {
            var prefix = "Relationships.DeleteRelationshipTypesAsync => ";
            var errorMessage = "";
            try
            {

                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not authorized to perform this action."));

                if (relationshiptypes == null)
                    relationshiptypes = readRequestJsonContent<List<RelationshipTypeDelete>>(Request).Result;

                if (relationshiptypes == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                if (relationshiptypes.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"You may only provide a maximum of {MAX_SYNCHRONOUS_API_ITEM_COUNT} relationship types in this request."));

                var execution = getApiExecution(relationshiptypes.Count);

                var results = RelationshipRepository.DeleteRelationshipTypes(relationshiptypes, execution);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Error", errorMessage));
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))
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

                var types = await RelationshipRepository.GetRelationshipTypes(queryParams);

                if (types == null) types = new List<IntersectTypeApiViewModel>(); // Will send back empty list, which matches expectation for API specification.

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
        /// Verify if the asset type has existing relationships or not
        /// </summary>
        /// <param name="assetTypeId"></param>
        /// <returns>true if relationship exists otherwise false</returns>
        [
            HttpGet,
            ApiExplorerSettings(IgnoreApi = true),
            MapToApiVersion("2.0"),
            Route("isTransformPredicateExists/{assetTypeId}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "true/false based on relationship exists on assettype.", typeof(bool)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))
            ]
        public async Task<HttpResponseMessage> IsTransformPredicateExists(int assetTypeId)
        {
            var prefix = "Relationships.IsTransformPredicateExists => ";
            var errorMessage = "";

            try
            {
                var result = await this.RelationshipRepository.IsTransformPredicateExists(assetTypeId);
                return Request.CreateResponse(HttpStatusCode.OK, result);
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))
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
            return RelationshipRepository.GetIntersectTypeById(id);
        }


        #region Bulk Relationships

        /// <summary>
        /// Takes a given set of relationships and inserts/updates them. Use this endpoint if you want to process under 250 items and need immediate results.
        /// </summary>
        /// <param name="intersectTypeUid">The unique identifier of the intersect type.</param>
        /// <param name="relationships">The payload of your request. Must include SubjectAssetUid and ObjectAssetUid.</param>
        /// <param name="triggerWorkflow">Set this flag to 'true' to trigger workflows with this action. If flag is not set, default value is false.</param>
        /// <param name="lookupFieldsPassedByValue">Optional query string parameter that allows you to pass list values numeric value instead of plain text value.  The default value for this is false.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            Route("{intersectTypeUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of bulk relationship results, including any error messages.", typeof(List<DatabaseBulkRelationshipResult>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostRelationshipsAsync(Guid intersectTypeUid, RelationshipInserts relationships, bool triggerWorkflow = false, bool lookupFieldsPassedByValue = false)
        {
            var prefix = "Relationships.PostRelationshipsAsync => ";
            var errorMessage = "";

            try
            {
                IntersectType intersectType = RelationshipRepository.GetIntersectTypeByUid(intersectTypeUid);

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
                    results = Company.ImportRelationships(execution, intersectType, relationships, 3600, triggerWorkflow, lookupFieldsPassedByValue);

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
        /// <param name="triggerWorkflow">Set this flag to 'true' to trigger workflows with this action. If flag is not set, default value is false.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            Route("batch/{intersectTypeUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution ID to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)), SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostBulkRelationshipsAsync(Guid intersectTypeUid, RelationshipInserts relationships, bool triggerWorkflow = false)
        {
            var prefix = "Relationships.PostBulkRelationshipsAsync => ";
            var errorMessage = "";

            try
            {
                IntersectType intersectType = RelationshipRepository.GetIntersectTypeByUid(intersectTypeUid);

                if (intersectType == null)
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Relationship Type with Uid {intersectTypeUid} could not be found.")));

                if (relationships == null)
                    relationships = readRequestJsonContent<RelationshipInserts>(Request, true).Result;

                if (relationships == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                ApiExecutionInfo executionInfo = await RelationshipRepository.BulkPostRelationships(intersectTypeUid, relationships, this.getApiExecution, triggerWorkflow);

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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetExecutionStatus(Guid executionUid)
        {
            var prefix = "Relationships.GetExecutionStatus => ";
            var errorMessage = "";

            try
            {
                var dbExecutionItem = AssetRepository.GetExecutionItemByUid(executionUid);

                if (dbExecutionItem == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", "Execution unique identifier not found."));
                }

                var info = new ApiExecutionInfo { CompanyID = Company.CurrentCompanyID, ExecutionID = executionUid };

                List<DatabaseBulkAssetResult> results = RelationshipRepository.GetBulkResults(info);

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
            catch (ArgumentException)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", "Execution unique identifier not found."));
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
        /// Removes a given set of relationships based on the specific relationship type Uid. This endpoint is meant for a greater number of items as it stores the relationship list for asynchronous or batch processing.
        /// </summary>
        /// <param name="intersectTypeUid">The unique identifier of the intersect type.</param>
        /// <param name="relationships">The payload of your request. Must include Uid.</param>
        /// <param name="triggerWorkflow">Set this flag to 'true' to trigger workflows with this action. If flag is not set, default value is false.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            Route("batch/{intersectTypeUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution ID to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)), SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteBulkRelationshipsAsync(Guid intersectTypeUid, RelationshipDeletes relationships, bool triggerWorkflow = false)
        {
            var prefix = "Relationships.DeleteBulkRelationshipsAsync => ";
            var errorMessage = "";

            try
            {
                IntersectType intersectType = RelationshipRepository.GetIntersectTypeByUid(intersectTypeUid);

                if (intersectType == null)
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Relationship Type with Uid {intersectTypeUid} could not be found.")));

                if (relationships == null)
                    relationships = readRequestJsonContent<RelationshipDeletes>(Request, true).Result;

                if (relationships == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                ApiExecutionInfo executionInfo = await RelationshipRepository.BulkDeleteRelationships(intersectTypeUid, relationships, this.getApiExecution, triggerWorkflow);

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
        /// Deletes a relationship and children of a given uid.
        /// </summary>
        /// <param name="intersectTypeUid">The unique identifier of the relationship type.</param>
        /// <param name="relationships">The list of relationships for deletions.</param>
        /// <param name="triggerWorkflow">Set this flag to 'true' to trigger workflows with this action. If flag is not set, default value is false.</param>
        /// <remarks>
        /// The "types/" prefix in the URI will be removed in a subsequent release. Please use the DELETE endpoint without this prefix.
        /// </remarks>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            Route("types/{intersectTypeUid}"),
            Route("{intersectTypeUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the DELETE request.", typeof(List<DatabaseBulkRelationshipResult>)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to delete relationship of this type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Error while processing request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteRelationships(Guid intersectTypeUid, RelationshipDeletes relationships, bool triggerWorkflow = false)
        {
            IntersectType intersectType = RelationshipRepository.GetIntersectTypeByUid(intersectTypeUid);

            if (intersectType == null)
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Relationship Type with Uid {intersectTypeUid} could not be found.")));

            if (relationships == null)
                relationships = readRequestJsonContent<RelationshipDeletes>(Request, true).Result;

            if (relationships == null)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

            if (relationships.Count == 0)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided any relationships to process in this request."));

            if (relationships.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"You may only provide a maximum of {MAX_SYNCHRONOUS_API_ITEM_COUNT} relationships in this request. Please call the BATCH API to submit more than {MAX_SYNCHRONOUS_API_ITEM_COUNT} items."));

            var execution = getApiExecution(relationships.Count, new ApiExecutionFields_DeleteRelationships { IntersectTypeUid = intersectTypeUid });

            Company.Add(execution);

            List<DatabaseBulkRelationshipResult> results = null;
            try
            {
                results = RelationshipRepository.DeleteRelationships(execution, intersectType, relationships, 3600, triggerWorkflow);

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


        /// <summary>
        /// GET a list of relationship types.
        /// </summary>
        /// <returns>A excel file containing relationships types.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            ApiExplorerSettings(IgnoreApi = true),
            Route("export/types"),
            FileDownload,
            SwaggerConsumes("application/vnd.ms-excel"), SwaggerProduces("application/vnd.ms-excel"),
            SwaggerResponse(HttpStatusCode.OK, "Exported realtionship types to Excel.", typeof(List<PredicateTypeApiViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> ExportTypesToExcel()
        {
            var queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add(new KeyValuePair<string, string>("state", "1"));
            var models = await Company.GetRelationshipTypes(queryParams);
            var document = new SLDocument();
            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, "Items");

            #region Create the list sheet

            #region Header

            int index = 1;
            document.SetCellValue(1, index++, "Subject");
            document.SetCellValue(1, index++, "Subject Class");
            document.SetCellValue(1, index++, "Predicate");
            document.SetCellValue(1, index++, "Object");
            document.SetCellValue(1, index++, "Object Class");
            document.SetCellValue(1, index++, "Relationship Type UID");
            document.SetCellValue(1, index++, "Relationship Type Id");

            #endregion

            int rowNumber = 1;
            foreach (var row in models)
            {
                index = 1;
                rowNumber++;
                document.SetCellValue(rowNumber, index++, row.Subject.Name);
                document.SetCellValue(rowNumber, index++, row.Subject.Class.ToString());
                document.SetCellValue(rowNumber, index++, row.Predicate.Name);
                document.SetCellValue(rowNumber, index++, row.Object.Name);
                document.SetCellValue(rowNumber, index++, row.Object.Class.ToString());
                document.SetCellValue(rowNumber, index++, row.Uid.ToString());
                document.SetCellValue(rowNumber, index++, row.Id);
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
                FileName = string.Format("Relationship Types {0}.xlsx", System.DateTime.Now.ToShortDateString())
            };
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");

            return ResponseMessage(result);
        }

    }

}
