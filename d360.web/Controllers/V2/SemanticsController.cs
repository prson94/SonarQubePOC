using d360.core;
using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.exceptions;
using d360.extensions;
using d360.model;
using d360.model.DataAccessLayer;
using d360.model.helpers.filters;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Dapper;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Resources;
using SpreadsheetLight;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// <summary>
    /// This service houses all endpoints handling semantics throughout your environment.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/semantics"),
        Authorize,
        StringEnumController
    ]
    public class SemanticsController : BaseV2ApiController
    {
        #region DI

        ISemanticsRepository SemanticsRepository;
        public SemanticsController(ICommunityContext community, ICompanyContext company, ISemanticsRepository semanticsRepository, ISettingsRepository settingsRepository)
            : base(community, company, settingsRepository)
        {
            this.SemanticsRepository = semanticsRepository;
        }

        #endregion


        /// <summary>
        /// Gets a list of semantics for use in data profiling.
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
        /// <returns>A list of semantics based on the provided filtering and sorting criteria.</returns>
        [
            HttpGet,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default (and maximum) value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the semantics are ordered by Qualifier.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_simpleFilter", "The text or phrase you want to find within the listable fields of a semantic. Filtering is done using 'Starts with' logic. Asterisk (*) symbol can be used as a wild card character to match any character.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_filter", ADVANCED_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("asOfEffectiveDate", "Assumed to be current UTC date if left empty, otherwise, gets semantics as of the specified effective date, and nothing later. This is the parameter used to get prior versions.", DataType = "datetime", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of semantics.", typeof(GetSemantics)),            
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetSemantics(CancellationToken cancellationToken)
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
                    "Error retrieving semantics",
                    ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        /// <summary>
        /// Gets a list of versions for a given semantic qualifier.
        /// </summary>
        /// <returns>A list of semantic versions.</returns>
        [
            HttpGet,
            Route("{qualifier}/versions"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the semantics are ordered by Qualifier.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of semantics.", typeof(List<GetSemantic>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetSemanticVersions(string qualifier, CancellationToken cancellationToken)
        {
            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var apiModels = await SemanticsRepository.GetSemanticVersionsByQualifierAsync(qualifier, queryParams, cancellationToken);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, apiModels));
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error retrieving semantics", ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        /// <summary>
        /// Gets a list of semantic base types.
        /// </summary>
        [
            HttpGet,
            Route("lookups/basetypes"),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of semantic base types.", typeof(List<SemanticBaseTypeInfo>)),
        ]
        public IHttpActionResult GetSemanticBaseTypes()
        {
            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, SemanticBaseType.LocalDate.GetAsList()));
        }

        /// <summary>
        /// Gets a list of semantic base types.
        /// </summary>
        [
            HttpGet,
            Route("lookups/matchtypes"),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of semantic match types.", typeof(List<SemanticMatchTypeInfo>)),
        ]
        public IHttpActionResult GetSemanticMatchTypes()
        {
            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, SemanticMatchType.Pattern.GetAsList()));
        }

        /// <summary>
        /// Gets a list of semantic statuses.
        /// </summary>
        [
            HttpGet,
            Route("lookups/statuses"),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of semantic statuses.", typeof(List<SemanticStatusInfo>)),
        ]
        public IHttpActionResult GetSemanticStatuses()
        {
            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, SemanticStatus.Draft.GetAsList()));
        }

        /// <summary>
        /// Selectively updates one or more semantics based on the fields provided. 
        /// If certain fields that make up a semantic are missing from your request payload, then those fields will not be updated.
        /// </summary>
        /// <remarks>
        /// For Built-in semantics, you may only update the following properties:
        ///  - **name**
        ///  - **description**
        ///
        /// Minimum and Maximum properties, if provided, must fall within the range: -999999999999.999999 to 999999999999.999999
        ///
        /// For a list of possible values for the following fields, check the relevant endpoint:
        ///  - **baseType** : /api/v2/semantics/lookups/basetypes
        ///  - **matchType** : /api/v2/semantics/lookups/matchtypes
        ///  - **status** : /api/v2/semantics/lookups/statuses
        /// </remarks>
        /// <returns>A list of semantics you updated.</returns>
        [
            HttpPatch,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(List<PatchSemantic>), typeof(PatchSemanticExample1)),
            SwaggerRequestExample(typeof(List<PatchSemantic>), typeof(PatchSemanticExample2)),
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding semantics.", typeof(List<GetSemantic>)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "One or more semantics were not found based on the provided qualifiers.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request to update these semantics is invalid, given the reason specified in the error message.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PatchSemantics(List<PatchSemantic> semantics)
        {
            const string ERROR_HEADING = "Error patching semantics";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Forbidden, ERROR_HEADING, ApiMessages.EndpointNotAuthorizedMessage);
                }

                var apiModels = await SemanticsRepository.PatchSemanticsAsync(semantics);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, apiModels));
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error updating semantics", ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }


        /// <summary>
        /// Creates one or more user-defined semantics.
        /// </summary>
        /// <remarks>
        /// For a list of possible values for the following fields, check the relevant endpoint:
        ///  - **baseType** : /api/v2/semantics/lookups/basetypes
        ///  - **matchType** : /api/v2/semantics/lookups/matchtypes
        ///  - **status** : /api/v2/semantics/lookups/statuses
        ///
        /// Minimum and Maximum properties, if provided, must fall within the range: -999999999999.999999 to 999999999999.999999
        /// </remarks>
        /// <returns>A list of field types corresponding to the given criteria, if any.</returns>
        [
            HttpPost,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(List<PostSemantic>), typeof(PostSemanticExample1)),
            SwaggerRequestExample(typeof(List<PostSemantic>), typeof(PostSemanticExample2)),
            SwaggerResponse(HttpStatusCode.Created, "Returns the corresponding semantics.", typeof(List<GetSemantic>)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request to insert these semantics is invalid, given the reason specified in the error message.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostSemantics(List<PostSemantic> semantics)
        {
            const string ERROR_HEADING = "Error adding semantics";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Forbidden, ERROR_HEADING, ApiMessages.EndpointNotAuthorizedMessage);
                }

                var models = await SemanticsRepository.PostSemanticsAsync(semantics);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Created, models));
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ERROR_HEADING, ApiMessages.UnknownErrorInvestigatingMessage);
            }
        }

        /// <summary>
        /// Updates one or more user-defined semantics. Built-in semantics may not be updated using this endpoint.
        /// </summary>
        /// <remarks>
        /// For a list of possible values for the following fields, check the relevant endpoint:
        ///  - **baseType** : /api/v2/semantics/lookups/basetypes
        ///  - **matchType** : /api/v2/semantics/lookups/matchtypes
        ///  - **status** : /api/v2/semantics/lookups/statuses
        ///
        /// Minimum and Maximum properties, if provided, must fall within the range: -999999999999.999999 to 999999999999.999999
        /// </remarks>
        /// <returns>A list of updated semantics.</returns>
        [
            HttpPut,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerRequestExample(typeof(List<PutSemantic>), typeof(PutSemanticExample1)),
            SwaggerRequestExample(typeof(List<PutSemantic>), typeof(PutSemanticExample2)),
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding semantics.", typeof(List<GetSemantic>)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "One or more semantics were not found based on the provided qualifiers.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Request to update these semantics is invalid, given the reason specified in the error message.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutSemantics(List<PutSemantic> semantics)
        {
            const string ERROR_HEADING = "Error updating semantics";

            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Forbidden, ERROR_HEADING, ApiMessages.EndpointNotAuthorizedMessage);
                }
                
                var models = await SemanticsRepository.PutSemanticsAsync(semantics);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, models));
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
        /// Deletes a semantic, provided it is not currently referenced in any asset data profiles.
        /// </summary>
        /// <remarks>
        /// This action will remove all versions of the semantic.
        /// </remarks>
        /// <returns>A confirmation response.</returns>
        [
            HttpDelete,
            Route("{qualifier}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Returns a success message.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Your semantic was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Conflict, "Request to remove this semantic is invalid, possibly due to being used on one or more data profiles.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteSemantic(string qualifier)
        {
            const string ERROR_HEADING = "Error deleting semantic";
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Forbidden, ERROR_HEADING, ApiMessages.EndpointNotAuthorizedMessage);
                }
                
                var status = await SemanticsRepository.DeleteSemanticAsync(qualifier);

                return ResponseMessage(Request.CreateResponse(status, new ConfirmResponse { message = "Semantic removed." }));
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
    }

    #region Request Examples

    public class PatchSemanticExample1 : IExamplesProvider
    {
        public object GetExamples()
        {
            return new List<PatchSemantic> {
                new PatchSemantic
                {
                    Qualifier = "EMAIL",
                    Name = "Email address",
                    Description = "A user's email address."
                }
            };
        }
    }

    public class PatchSemanticExample2 : IExamplesProvider
    {
        public object GetExamples()
        {
            return new List<PatchSemantic> {
                new PatchSemantic
                {
                    Qualifier = "EMAIL",
                    Name = "Email address",
                    Description = "A user's email address.",
                    RegularExpression = @"^$|\b([A-Za-z0-9'_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})\b"
                }
            };
        }
    }

    public class PostSemanticExample1 : IExamplesProvider
    {
        public object GetExamples()
        {
            return new List<PostSemantic> {
                new PostSemantic
                {
                    Qualifier = "EMAIL",
                    Name = "Email address",
                    Description = "A user's email address.",
                    MatchType = SemanticMatchType.Pattern,
                    RegularExpression = @"^$|\b([A-Za-z0-9'_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})\b"
                }
            };
        }
    }

    public class PostSemanticExample2 : IExamplesProvider
    {
        public object GetExamples()
        {
            return new List<PostSemantic> {
                new PostSemantic
                {
                    Qualifier = "ADV_Q",
                    Name = "Some advanced semantic",
                    Description = "An example that uses the advanced proeprty to send a custom object.",
                    JsonPayloadStructured = JObject.Parse("{clazz: \"namespace.classname\", custnum1: 12345 }")
                }
            };
        }
    }

    public class PutSemanticExample1 : IExamplesProvider
    {
        public object GetExamples()
        {
            return new List<PutSemantic> {
                new PutSemantic
                {
                    BaseType = SemanticBaseType.String,
                    MatchType = SemanticMatchType.List,
                    Qualifier = "NORTHEAST_STATES",
                    Name = "New England States",
                    Description = "A list of states in the New England region of the US.",
                    ValidValuesStructured = new List<string> { "CT", "MA", "ME", "NH", "RI", "VT" }
                }
            };
        }
    }

    public class PutSemanticExample2 : IExamplesProvider
    {
        public object GetExamples()
        {
            return new List<PutSemantic> {
                new PutSemantic
                {
                    Qualifier = "IPADDRESS.IPV6",
                    Name = "IP V6 Address",
                    Description = "Version 6 of an IP address.",
                    HeaderFilterStructured = new SemanticHeaderFilter { 
                        match = "all", 
                        values = new List<SemanticHeaderFilterValue> { new SemanticHeaderFilterValue { @operator = "eq", value = ".*(?i)(ip).*" } } 
                    },
                    HeaderFilterConfidence = 70
                }
            };
        }
    }

    #endregion
}
