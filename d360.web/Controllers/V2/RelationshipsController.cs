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
using Dapper;
using d360.model.helpers.filters;
using Newtonsoft.Json.Linq;

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

        public RelationshipsController(ICommunityContext community, ICompanyContext company, IQueueSource queueSource, IStorageProvider storage, IRelationshipRepository relationshipRepository, IFieldsRepository fieldsRepository, IAssetRepository assetRepository, ISettingsRepository settingsRepository) : base(community, company, settingsRepository)
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
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
        /// <remarks>
        /// It is important to note that the status of each delete is returned in the success property in the JSON response as it is possible for some predicates to be successfully removed while others may fail.
        /// </remarks>
        /// <param name="predicates">The list of predicates for deletion.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            Route("predicates"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the DELETE request.", typeof(List<PredicateDeleteResult>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "You are not allowed to delete predicates due to lack of administrative permissions.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeletePredicates(PredicateDeletes predicates)
        {
            var prefix = "Relationships.DeletePredicate => ";
            var errorMessage = "";
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage)).ConfigureAwait(false);
                }

                if (predicates == null)
                {
                    predicates = readRequestJsonContent<PredicateDeletes>(Request, true).Result;
                }

                if (predicates == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.JSONValidMessage)).ConfigureAwait(false);
                }

                if (predicates.Count == 0)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, RelationshipsApiMessages.PredicateRequired)).ConfigureAwait(false);
                }

                if (predicates.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(RelationshipsApiMessages.PredicateLimit, MAX_SYNCHRONOUS_API_ITEM_COUNT.ToString()))).ConfigureAwait(false);
                }

                var execution = getApiExecution(predicates.Count);


                List<PredicateDeleteResult> results = RelationshipRepository.DeletePredicates(predicates, execution);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results))).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage))).ConfigureAwait(false);
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add predicates.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> UpsertPredicates(PredicateUpserts predicates)
        {
            var prefix = "Relationships.InsertPredicate => ";
            var errorMessage = "";
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage)).ConfigureAwait(false);
                }

                if (predicates == null)
                {
                    predicates = readRequestJsonContent<PredicateUpserts>(Request, true).Result;
                }

                if (predicates == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.JSONValidMessage)).ConfigureAwait(false);
                }

                if (predicates.Count == 0)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest,RelationshipsApiMessages.PredicateRequired)).ConfigureAwait(false);
                }

                if (predicates.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(RelationshipsApiMessages.PredicateLimit, MAX_SYNCHRONOUS_API_ITEM_COUNT))).ConfigureAwait(false);
                }

                foreach (var pred in predicates)
                {
                    if (pred.Name.Length > 100)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, RelationshipsApiMessages.InvalidNameLength)).ConfigureAwait(false);
                    }
                    if (pred.Inverse.Length > 250)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, RelationshipsApiMessages.InvalidInverseLength)).ConfigureAwait(false);
                    }
                }

                var execution = getApiExecution(predicates.Count);


                List<PredicateUpsertResult> results = RelationshipRepository.UpsertPredicates(predicates, execution);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results))).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage))).ConfigureAwait(false);
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
       ]
        public HttpResponseMessage GetPredicatesTypesAsync()
        {
            var prefix = "Relationships.GetPredicatesTypesAsync => ";
            var errorMessage = "";

            try
            {
                var types = PredicateType.DataLineage.GetAsList()
                    .Where(i => !i.Obsolete)
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
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult ExportToExcel(string intersectTypeUid)
        {
            Guid guid = Guid.Parse(intersectTypeUid);

            var intersectType = RelationshipRepository.GetIntersectTypeByUid(guid);

            if (intersectType == null)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest,ApiMessages.BadRequest, string.Format(RelationshipsApiMessages.InvalidIntersectTypeUid, intersectTypeUid));
            }

            int id = intersectType.ID;

            var customColumns = FieldsRepository.GetCustomFields(SystemObjects.IntersectType, id);
            IEnumerable<dynamic> models;

            if (customColumns.Count() > 0)
            {
                models = RelationshipRepository.GetExportModelWithCustomFields(id, customColumns);
            }
            else
            {
                models = RelationshipRepository.GetExportModel(id);
            }

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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Object representing one of the query parameter values could not be found.", typeof(ErrorResponse)),
            SwaggerParameter("RelationshipTypeUid", "Filter by a relationship type's unique identifier. Using this parameter will also provide any field values for the relationships, if applicable.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("PredicateUid", "Filter by a predicate's unique identifier.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("SubjectUid", "Filter by a subject asset's unique identifier.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("ObjectUid", "Filter by an object asset's unique identifier.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "Allows for changing the current page of results you are requesting.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "Allows for changing the page size of results you are requesting. The maximum page size is 5000, the default is 250.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by Id.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Allows you to disable including the count of the total number of results across pages in the response.  The default is true meaning the total count is included and if leave out this parameter.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_includePath", "Includes Asset path values to both object and subject side.  The default is false meaning relationships will not return asset path.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_simpleFilter", "The text or phrase you want to find within the listable fields of a relationship. Filtering is done using 'Contains' logic. Asterisk (*) symbol can be used as a wild card character to match any character.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_filter", ADVANCED_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false),
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
                Guid RelationshipTypeUid = Guid.Empty;

                if (queryParams.Any(x => x.Key.ToLower() == "relationshiptypeuid"))
                {
                    var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "relationshiptypeuid").Value;
                    Guid.TryParse(value, out RelationshipTypeUid);
                    if (RelationshipTypeUid == null || RelationshipTypeUid == Guid.Empty)
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest, RelationshipsApiMessages.InvalidRelatioshipTypeUid);
                    }
                    else
                    {
                        if (!RelationshipRepository.AnyExists(RelationshipTypeUid))
                        {
                            return ReturnApiError(HttpStatusCode.NotFound, string.Format(ActionApiMessages.RelationShipTypeUidNotFound, RelationshipTypeUid.ToString()));
                        }
                    }
                }

                if (queryParams.Any(x => x.Key.ToLower() == "predicateuid"))
                {
                    Guid PredicateUid = Guid.Empty;
                    var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "predicateuid").Value;
                    Guid.TryParse(value, out PredicateUid);
                    if (PredicateUid == null || PredicateUid == Guid.Empty)
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest, RelationshipsApiMessages.InvalidPredicateUid);
                    }
                    else
                    {
                        if (!RelationshipRepository.AnyPredicateExists(PredicateUid))
                        {
                            return ReturnApiError(HttpStatusCode.NotFound, string.Format(RelationshipsApiMessages.PredicateUidNotFound, PredicateUid.ToString()));
                        }
                    }
                }

                if (queryParams.Any(x => x.Key.ToLower() == "subjectuid"))
                {
                    Guid SubjectUid = Guid.Empty;
                    var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "subjectuid").Value;
                    Guid.TryParse(value, out SubjectUid);
                    if (SubjectUid == null || SubjectUid == Guid.Empty)
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest, RelationshipsApiMessages.InvalidSubjectUid);
                    }
                    else
                    {
                        if (!AssetRepository.DoesAssetExists(SubjectUid))
                        {
                            var assetType = AssetRepository.GetAssetTypeByUID(SubjectUid);
                            if (assetType == null || assetType.Class != AssetTypeClass.Reference)
                            {
                                return ReturnApiError(HttpStatusCode.NotFound, string.Format(RelationshipsApiMessages.SubjectUidNotFound, SubjectUid.ToString()));
                            }
                        }
                    }
                }

                if (queryParams.Any(x => x.Key.ToLower() == "objectuid"))
                {
                    Guid ObjectUid = Guid.Empty;
                    var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "objectuid").Value;
                    Guid.TryParse(value, out ObjectUid);
                    if (ObjectUid == null || ObjectUid == Guid.Empty)
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest, RelationshipsApiMessages.InvalidObjectUid);
                    }
                    else
                    {
                        if (!AssetRepository.DoesAssetExists(ObjectUid))
                        {
                            var assetType = AssetRepository.GetAssetTypeByUID(ObjectUid);
                            if (assetType == null || assetType.Class != AssetTypeClass.Reference)
                            {
                                return ReturnApiError(HttpStatusCode.NotFound, string.Format(RelationshipsApiMessages.ObjectUidNotFound, ObjectUid.ToString()));
                            }
                        }
                    }
                }

                if (queryParams.Any(x => x.Key.ToLower() == "_direction"))
                {
                    var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_direction").Value.ToLower(System.Globalization.CultureInfo.InvariantCulture);

                    if (RelationshipTypeUid == Guid.Empty)
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest, RelationshipsApiMessages.DirectionAllowedForRelation);
                    }

                    if (!new[] { "asc", "desc" }.Contains(value))
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest, ApiMessages.InvalidDirection);
                    }
                }

                if (queryParams.Any(x => x.Key.ToLower() == "_order"))
                {
                    if (RelationshipTypeUid == Guid.Empty)
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest, RelationshipsApiMessages.OrderForRelation);
                    }
                    var orderValue = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_order").Value.ToLower(System.Globalization.CultureInfo.InvariantCulture);

                    var fieldTypes = Company.Query<string>("select F.Name from FieldType F inner join IntersectType I on F.Object = 'IntersectType' and I.ID = F.ObjectID and I.[Uid] = @relationshipTypeUid", new { RelationshipTypeUid }, ApiTimeout).ToList().Select(x => x.ToLower(System.Globalization.CultureInfo.InvariantCulture)).ToList();
                    fieldTypes.Add("object.[path]");
                    fieldTypes.Add("subject.[path]");

                    if (!fieldTypes.Contains(orderValue))
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest, RelationshipsApiMessages.InvalidOrderValue);
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
            catch (FilterExpressionParserException ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                return ReturnApiError(HttpStatusCode.BadRequest, errorMessage);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// Retrieves the details for the specified relationship.
        /// </summary>
        /// <param name="uid">The uid of a relationship to return.</param>
        /// <returns>Details for the specified relationship</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("relationship/{uid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A single relationships.", typeof(GetRelationshipSingleApiModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error indicating the asset for the given uid was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error indicating the user does not have permission to perform this action.", typeof(ErrorResponse))
       ]
        public async Task<HttpResponseMessage> GetRelationshipAsync(Guid uid)
        {
            var prefix = "Relationships.GetRelationshipAsync => ";
            var errorMessage = "";

            try
            {

                var intersect = Company.Intersects.FirstOrDefault(x => x.uid == uid);
                if (intersect == null || uid == Guid.Empty)
                {
                    return ReturnApiError(HttpStatusCode.NotFound, string.Format(RelationshipsApiMessages.RelationShipUidNotFound, uid.ToString()));
                }

                var hasObjectReadPermission = Company.HasAssetPermission(intersect.Object, intersect.ObjectID, Permission.ReadRelationships);
                var hasSubjectReadPermission = Company.HasAssetPermission(intersect.Subject, intersect.SubjectID, Permission.ReadRelationships);
                if (!hasObjectReadPermission || !hasSubjectReadPermission)
                {
                    return ReturnApiError(HttpStatusCode.Forbidden, RelationshipsApiMessages.ViewthisRelationNotAllowed);
                }

                var result = await RelationshipRepository.GetRelationship(uid);
                if (result == null)
                {
                    return ReturnApiError(HttpStatusCode.NotFound, string.Format(ApiMessages.InvalidGuid, uid.ToString()));
                }
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid parameters provided to the api endpoint.", typeof(ErrorResponse)),
            SwaggerParameter("_pageNum", "Allows for changing the current page of results you are requesting.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "Allows for changing the page size of results you are requesting. The default is 5000 and the maximum value is 100,000.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_includeTotal", "Allows you to disable including the count of the total number of results across pages in the response.  The default is true meaning the total count is included and if leave out this parameter.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_owner", "An optional exact match filter on the owner of the relationship.", DataType = "string", ParameterType = "query", Required = false),
       ]
        public async Task<HttpResponseMessage> GetRelationshipUidsAsync(Guid RelationshipTypeUid)
        {
            var prefix = "Relationships.GetRelationshipUidsAsync => ";
            var errorMessage = "";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return ReturnApiError(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedMessage);
                }


                var queryParams = Request.GetQueryNameValuePairs().ToList();
                long pageSize = 5000;
                long pageNum = 1;
                bool includeTotal = true;
                string owner = null;

                if (queryParams.Any(x => x.Key.ToLower() == "_pagenum"))
                {
                    var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_pagenum").Value;
                    if (!long.TryParse(value, out pageNum))
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest,  ApiMessages.Invalid_PageNum);
                    }
                }

                if (queryParams.Any(x => x.Key.ToLower() == "_pagesize"))
                {
                    var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_pagesize").Value;
                    if (!long.TryParse(value, out pageSize))
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest, ApiMessages.Invalid_PageSize);
                    }

                    if (pageSize > 100000)
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest,ApiMessages._PageSizeLimit);
                    }

                    if (pageSize <= 0)
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest, ApiMessages._PageSizePassedZeroLess);
                    }
                }

                if (queryParams.Any(x => x.Key.ToLower() == "_includetotal"))
                {
                    var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_includetotal").Value;
                    if (!bool.TryParse(value, out includeTotal))
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest, RelationshipsApiMessages.Invalid_includeTotal);
                    }
                }


                if (queryParams.Any(x => x.Key.ToLower() == "_owner"))
                {
                    owner = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_owner").Value;
                    if (owner.Length > 100)
                    {
                        return ReturnApiError(HttpStatusCode.BadRequest,RelationshipsApiMessages.Invalid_owner);
                    }
                }

                if (RelationshipTypeUid == Guid.Empty)
                {
                    return ReturnApiError(HttpStatusCode.BadRequest, RelationshipsApiMessages.PassedValidRelationTypeUid);
                }

                int intersectTypeID = await (Company.QueryFirstOrDefaultAsync<int>("select id from [intersecttype] where [uid] = @uid", new { uid = RelationshipTypeUid }));

                if (intersectTypeID <= 0)
                {
                    return ReturnApiError(HttpStatusCode.BadRequest, RelationshipsApiMessages.PassedValidForExistingRelationType);
                }

                var results = await RelationshipRepository.GetRelationshipsUids(intersectTypeID, pageSize, pageNum, includeTotal, owner);
                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK, results);
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
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
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage)).ConfigureAwait(false);
                }

                if (relationshiptypes == null)
                {
                    relationshiptypes = readRequestJsonContent<List<RelationshipTypeInsert>>(Request).Result;
                }

                if (relationshiptypes == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.JSONValidMessage)).ConfigureAwait(false);
                }

                if (relationshiptypes.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(RelationshipsApiMessages.RelationshipTypeLimit, MAX_SYNCHRONOUS_API_ITEM_COUNT.ToString()))).ConfigureAwait(false);
                }

                var execution = getApiExecution(relationshiptypes.Count);

                var results = RelationshipRepository.PostRelationshipTypes(relationshiptypes, execution);
                Company.CreateRollupPathChangedExecution();

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
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
           SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
           SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to update the relationship type", typeof(ErrorResponse)),
           SwaggerResponse(HttpStatusCode.OK, "A list of relationship types  uid, including any error / success messages.", typeof(List<RelationshipTypeResult>))
       ]
        public async Task<IHttpActionResult> PutRelationshipTypesAsync(List<RelationshipTypeUpdate> relationshiptypes)
        {
            var prefix = "Relationships.PutRelationshipTypesAsync => ";

            try
            {

                if (!Company.CurrentResourceIsAdmin)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, ApiMessages.EndpointNotAuthorizedMessage));
                }

                if (relationshiptypes == null)
                {
                    relationshiptypes = readRequestJsonContent<List<RelationshipTypeUpdate>>(Request).Result;
                }

                if (relationshiptypes == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.JSONValidMessage)).ConfigureAwait(false);
                }

                if (relationshiptypes.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(RelationshipsApiMessages.RelationshipTypeLimit, MAX_SYNCHRONOUS_API_ITEM_COUNT.ToString()))).ConfigureAwait(false);
                }

                var execution = getApiExecution(relationshiptypes.Count);

                var results = RelationshipRepository.PutRelationshipTypes(relationshiptypes, execution);
                Company.CreateRollupPathChangedExecution();

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
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
           SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
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
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, ApiMessages.EndpointNotAuthorizedHeading, NOT_AUTHORIZED_MESSAGE)).ConfigureAwait(false);
                }

                if (relationshiptypes == null)
                {
                    relationshiptypes = readRequestJsonContent<List<RelationshipTypeDelete>>(Request).Result;
                }

                if (relationshiptypes == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.JSONValidMessage)).ConfigureAwait(false);
                }

                if (relationshiptypes.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(RelationshipsApiMessages.RelationshipTypeLimit, MAX_SYNCHRONOUS_API_ITEM_COUNT.ToString()))).ConfigureAwait(false);
                }

                var execution = getApiExecution(relationshiptypes.Count);

                var results = RelationshipRepository.DeleteRelationshipTypes(relationshiptypes, execution);
                Company.CreateRollupPathChangedExecution();

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
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

                if (types == null)
                {
                    types = new List<IntersectTypeApiViewModel>(); // Will send back empty list, which matches expectation for API specification.
                }

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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
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
                    return ReturnApiError(HttpStatusCode.BadRequest, RelationshipsApiMessages.InvalidParameter);
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
        /// <param name="relationships">The payload of your request. Must include SubjectAssetUid and ObjectAssetUid. Uid is optional.</param>
        /// <param name="triggerWorkflow">Set this flag to 'true' to trigger workflows with this action. If flag is not set, default value is false.</param>
        /// <param name="lookupFieldsPassedByValue">Optional query string parameter that allows you to pass list values numeric value instead of plain text value.  The default value for this is false.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            Route("{intersectTypeUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of bulk relationship results, including any error messages.", typeof(List<DatabaseBulkRelationshipResult>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
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
                {
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format(ActionApiMessages.RelationShipTypeUidNotFound, intersectTypeUid.ToString())))).ConfigureAwait(false);
                }

                if (relationships == null)
                {
                    relationships = readRequestJsonContent<RelationshipInserts>(Request, true).Result;
                }

                if (relationships == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.JSONValidMessage));
                }

                if (relationships.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(RelationshipsApiMessages.MaxRelationShipLimit, MAX_SYNCHRONOUS_API_ITEM_COUNT, MAX_SYNCHRONOUS_API_ITEM_COUNT))).ConfigureAwait(false);
                }

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

                    // Quick sync of graph.
                    try
                    {
                        Company.SynchronizeExecutionRelationshipWithGraph(execution.ExecutionID);
                    }
                    catch
                    {
                        // Do nothing, as graph topic will eventually synch.
                    }

                }
                catch (Exception ex)
                {
                    string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                    execution.ErrorMessage = message;
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
        /// <param name="relationships">The payload of your request. Must include SubjectAssetUid and ObjectAssetUid. Uid is optional.</param>
        /// <param name="triggerWorkflow">Set this flag to 'true' to trigger workflows with this action. If flag is not set, default value is false.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            Route("batch/{intersectTypeUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution ID to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)), SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
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
                {
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format(ActionApiMessages.RelationShipTypeUidNotFound, intersectTypeUid.ToString())))).ConfigureAwait(false);
                }

                if (relationships == null)
                {
                    relationships = readRequestJsonContent<RelationshipInserts>(Request, true).Result;
                }

                if (relationships == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.JSONValidMessage));
                }

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
        /// <param name="executionID">The execution's unique identifier to retrieve status for.</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("executions/{executionID:Guid}/status"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerParameter("summaryOnly", "When true the results are omitted from the response. The default value is false.", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "An execution status including a list of relationships.", typeof(ApiExecutionStatusModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetExecutionStatus(Guid executionID)
        {
            var prefix = "Relationships.GetExecutionStatus => ";
            var errorMessage = "";
            var summaryOnly = false;
            var queryParams = Request.GetQueryNameValuePairs();

            try
            {
                if (queryParams.ToList().Any(x => x.Key.ToLower() == "summaryonly"))
                {
                    bool.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "summaryonly").Value, out summaryOnly);
                }

                var dbExecutionItem = AssetRepository.GetExecutionItemByUid(executionID);

                if (dbExecutionItem == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, ApiMessages.ExecutionUIDNotFound)).ConfigureAwait(false);
                }

                var info = new ApiExecutionInfo { CompanyID = Company.CurrentCompanyID, ExecutionID = executionID };

                List<DatabaseBulkAssetResult> results = null;
                bool finished = (dbExecutionItem.Processed + dbExecutionItem.Error) == dbExecutionItem.Total;

                if (!summaryOnly && finished)
                {
                    results = await RelationshipRepository.GetBulkResults(info);
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
            catch (ArgumentException)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound,ApiMessages.ExecutionUIDNotFound)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
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
                {
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format(ActionApiMessages.RelationShipTypeUidNotFound, intersectTypeUid.ToString())))).ConfigureAwait(false);
                }

                if (relationships == null)
                {
                    relationships = readRequestJsonContent<RelationshipDeletes>(Request, true).Result;
                }

                if (relationships == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.JSONValidMessage));
                }

                ApiExecutionInfo executionInfo = await RelationshipRepository.BulkDeleteRelationships(intersectTypeUid, relationships, this.getApiExecution, triggerWorkflow);

                return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            new ApiExecutionRecievedResponse
                            {
                                ExecutionID = executionInfo.ExecutionID,
                                Message = ApiMessages.ExecutionIDStatus,
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
        /// Deletes one or more relationships with the specified uid(s).
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to delete relationship of this type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteRelationships(Guid intersectTypeUid, RelationshipDeletes relationships, bool triggerWorkflow = false)
        {
            IntersectType intersectType = RelationshipRepository.GetIntersectTypeByUid(intersectTypeUid);

            if (intersectType == null)
            {
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format(ActionApiMessages.RelationShipTypeUidNotFound, intersectTypeUid.ToString())))).ConfigureAwait(false);
            }

            if (relationships == null)
            {
                relationships = readRequestJsonContent<RelationshipDeletes>(Request, true).Result;
            }

            if (relationships == null)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.JSONValidMessage));
            }

            if (relationships.Count == 0)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, RelationshipsApiMessages.RelationNotProvided)).ConfigureAwait(false);
            }

            if (relationships.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, string.Format(RelationshipsApiMessages.MaxRelationShipLimit, MAX_SYNCHRONOUS_API_ITEM_COUNT, MAX_SYNCHRONOUS_API_ITEM_COUNT))).ConfigureAwait(false);
            }

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

                // Quick sync of graph.
                try
                {
                    Company.SynchronizeExecutionRelationshipWithGraph(execution.ExecutionID);
                }
                catch
                {
                    // Do nothing, as graph topic will eventually sync.
                }
            }
            catch (Exception ex)
            {
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
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
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
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

        /// <summary>
        /// Retrieves a list of all relationship types and its counts for specific asset.
        /// </summary>
        /// <returns>A list of relationship counts per relationship type for an asset.</returns>
        [
            HttpGet,
            Route("counts/{assetUid}"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of relationship counts per relationship type for an asset.", typeof(List<AssetTypeCountModel>)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid Class name specified.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<HttpResponseMessage> GetRelationshipCounts(Guid assetUid)
        {
            var prefix = "Relationships.GetRelationshipCounts => ";
            var errorMessage = "";

            try
            {
                var asset = Company.Assets.FirstOrDefault(x => x.uid == assetUid);
                if (asset == null || !Company.HasAssetPermission(asset.ID, Permission.ReadRelationships))
                {
                    var assetType = Company.AssetTypes.FirstOrDefault(x => x.uid == assetUid);
                    if (assetType != null)
                    {
                        var refTypeCountSQL = @"drop table if exists #relationshipCountMap
                                create table #relationshipCountMap(IntersectTypeUid uniqueidentifier, IsSubject bit,Count int)

                                insert into #relationshipCountMap
                                select lower(it.uid), 0, count(*) from [Intersect] I
                                inner join IntersectType IT on IT.ID = I.IntersectTypeID
                                where I.Object = @object and I.ObjectID = @objectId
                                group by it.uid

                                insert into #relationshipCountMap
                                select lower(it.uid), 1, count(*) from [Intersect] I
                                inner join IntersectType IT on IT.ID = I.IntersectTypeID
                                where I.subject = @object and I.subjectid = @objectId
                                group by it.uid
                                select * from #relationshipCountMap";

                        var refListData = await Company.QueryAsync<RelationshipCountModel>(refTypeCountSQL, new { assetType.Object, assetType.ObjectID });
                        return Request.CreateResponse(HttpStatusCode.OK, refListData);

                    }



                    return Request.CreateResponse(HttpStatusCode.OK, new List<string>());
                }

                var countsSql = @"drop table if exists #relationshipCountMap
create table #relationshipCountMap(IntersectTypeUid uniqueidentifier, IsSubject bit,Count int)

;with cte as (select 
                        ae.IntersectTypeUid,
						1 as 'IsSubject',				
						a1.uid as 'SubjectUid',
						a2.uid as 'ObjectUid'
                        FROM 
                        graph.AssetNode A1,
                        graph.AssetEdge AE,
                        graph.AssetNode A2
                        WHERE MATCH(A1 - (AE) -> A2)
                        AND a1.uid = @assetuid
                        union
                        select
                        ae.IntersectTypeUid,
						0 as 'IsSubject',				
						a1.uid as 'SubjectUid',
						a2.uid as 'ObjectUid'
                        FROM 
                        graph.AssetNode A1,
                        graph.AssetEdge AE,
                        graph.AssetNode A2
                        WHERE MATCH(A1 <- (AE) - A2)
                        AND a1.uid = @assetuid)
						insert into #relationshipCountMap
						select IntersectTypeUid, IsSubject, count(*) as 'Count'
						from cte
						group by IntersectTypeUid,IsSubject

                        ;with cte as (select it.uid as 'IntersectTypeUid', 1 as 'IsSubject',count(*) as 'Count' 
                        from Asset a
                        inner join assettype at on at.id = a.assettypeid
                        inner join intersecttype it on it.subject = at.object and it.subjectid = at.objectid and it.object = 'ReferenceItemType'
                        inner join [Intersect] i on i.intersecttypeid = it.id and i.subject = a.object and i.subjectid = a.objectid
                        where a.uid = @assetuid
                        group by it.uid
                        union 
                        select it.uid as 'IntersectTypeUid', 0 as 'IsSubject',count(*) as 'Count' 
                        from Asset a
                        inner join assettype at on at.id = a.assettypeid
                        inner join intersecttype it on it.object = at.object and it.objectid = at.objectid and it.subject = 'ReferenceItemType'
                        inner join [Intersect] i on i.intersecttypeid = it.id and i.object = a.object and i.objectid = a.objectid
                        where a.uid = @assetuid
                        group by it.uid)
                        insert into #relationshipCountMap
                        select IntersectTypeUid, IsSubject,Count from cte

                        select * from #relationshipCountMap";

                var data = await Company.QueryAsync<RelationshipCountModel>(countsSql, new { assetUid });

                return Request.CreateResponse(HttpStatusCode.OK, data);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string> {
                    { "Endpoint Method", prefix }
                });

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// GET a list of relationship types for complex field.
        /// </summary>
        /// <param name="assetUid">Asset Uid.</param>
        /// <param name="fieldName">Field Api Name.</param>
        /// <returns>A list of predicates contained within your Govern environment.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("types/complexField/{assetUid}/{fieldName}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of predicates.", typeof(PredicatesApiViewModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
       ]
        public async Task<HttpResponseMessage> GetRelationshipTypesForComplexField(Guid assetUid, string fieldName = null)
        {
            var prefix = "Relationships.GetRelationshipTypesForComplexField => ";
            var errorMessage = "";

            try
            {
                var asset = AssetRepository.GetAssetByUID(assetUid);
                var assetType = Company.AssetTypes.FirstOrDefault(x => x.ID == asset.AssetTypeID);
                var fieldType = Company.FieldTypes.FirstOrDefault(x => x.AssetTypeID == assetType.ID && x.Name == fieldName);

                if (fieldType.Type != DataType.ComplexRelationLookup.ToString())
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new List<IntersectTypeApiViewModel>());
                }
                var ftl = Company.FieldTypeLookups.FirstOrDefault(x => x.FieldTypeID == fieldType.ID);
                var definition = ftl.ParseComplexLookupDefinition();

                var fields = FieldsRepository.GetFieldDefinitionForComplexLookupFieldType(fieldType, assetUid, true).ToList();

                List<Guid> relationshipTypes = new List<Guid>();

                foreach (var f in fields)
                {
                    if (f.Type == DataType.Relationship.ToString())
                    {
                        relationshipTypes.Add(Company.IntersectTypes.FirstOrDefault(x => x.ID == f.LookupObjectID).uid);
                    }
                }

                var dbArgs = new DynamicParameters();
                dbArgs.Add("uids", relationshipTypes);
                var sql = $@"
                select	I.Id,
                        I.Uid,
		                I.State as State,
                        coalesce(I.IsSystem, 0) as IsSystem,
		                P.UID as 'Predicate.Uid',
		                coalesce(P.[Type],0) as 'Predicate.Type',
		                coalesce(P.Name,'') as 'Predicate.Name',
		                coalesce(P.Inverse,'') as 'Predicate.Inverse',
		                S.Uid as 'Subject.Uid',		
		                coalesce(SP.[Path], S.Name) as 'Subject.Name',
		                coalesce(S.Class, 0) as 'Subject.Class',
		                I.SubjectCardinality as 'Subject.Cardinality',
		                O.Uid as 'Object.Uid',
		                coalesce(OP.[Path], O.Name)  as 'Object.Name',
		                coalesce(O.Class, 0) as 'Object.Class',
		                I.ObjectCardinality as 'Object.Cardinality'
                from	IntersectType I
		                left join [Predicate] P on P.ID = I.PredicateID

		                left join AssetType S on (S.Object = I.Subject and S.ObjectID = I.SubjectID)
                        outer apply dbo.GetAssetTypeTextPathById(S.ID, '/') SP
		
		                left join AssetType O on (O.Object = I.Object and O.ObjectID = I.ObjectID)
                        outer apply dbo.GetAssetTypeTextPathById(O.ID, '/') OP
                        where I.Uid in @uids
                        for json path";

                var models = await Company.GetDatabaseJsonAsObjectAsync<List<JObject>>(sql, dbArgs, ApiTimeout);

                if (models == null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new List<string>());
                }

                foreach (var item in models)
                {
                    var objectUid = Guid.Parse(item.GetValue("Object")["Uid"].ToString());
                    var subjectUid = Guid.Parse(item.GetValue("Subject")["Uid"].ToString());
                    foreach (var r in definition.Relations)
                    {
                        if (objectUid == r.AssetTypeUid)
                        {
                            item["SideOfRelationship"] = "Object";
                        }
                        else if (subjectUid == r.AssetTypeUid)
                        {
                            item["SideOfRelationship"] = "Subject";
                        }
                    }
                }
                return Request.CreateResponse(HttpStatusCode.OK, models);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }
    }

}
