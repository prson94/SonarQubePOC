using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using d360.core.entities.SurveyModels;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling tag management in Govern
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/survey"),
        Authorize,
        ApiExplorerSettings(IgnoreApi = false)
    ]
    public class SurveysController : BaseV2ApiController
    {
        IAssetRepository AssetRepository;
        ISurveyRepository SurveyRepository;
        public SurveysController(ICommunityContext community, ICompanyContext company, IAssetRepository assetRepository, ISurveyRepository surveyRepository)
            : base(community, company)
        {
            this.AssetRepository = assetRepository;
            this.SurveyRepository = surveyRepository;
        }

        /// <summary>
        /// Returns all survey results defined in Govern.          
        /// </summary>        
        /// <param name="surveyTypeUid">Uid of survey type</param>
        /// <returns>A list of survey results</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route("{surveyTypeUid}/results"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("AssetUid", "The uid of a specific asset to return.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("AsOfDate", "Pull results up to a certain date.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "A full list of survey results.", typeof(SurveyApiResponseModel)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset not found based on Uid provided.", typeof(ErrorResponse)),

        ]
        public async Task<IHttpActionResult> GetSurveysResultsAsync(string surveyTypeUid)
        {
            var prefix = "Surveys.GetSurveysResultsAsync => ";
            var errorMessage = "";

            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            try
            {
                Guid surveyUid = Guid.Parse(surveyTypeUid);

                var survey = SurveyRepository.GetSurveyTypeByUid(surveyUid);
                if (survey == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Survey Type with Uid {surveyTypeUid} not found."));
                }
                var queryParams = Request.GetQueryNameValuePairs();

                if (queryParams.Any(x => x.Key.ToLower() == "assetuid"))
                {
                    Guid uid = Guid.Parse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "assetuid").Value);

                    var asset = AssetRepository.GetAssetByUID(uid);
                    if (asset == null)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset with Uid {uid} not found."));
                    }
                }


                var response = SurveyRepository.GetSurveysResult(surveyUid, queryParams);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response)));

            }

            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }

        }


        /// <summary>
        /// Returns all survey types defined in Govern.          
        /// </summary>        
        /// <returns>A list of survey types</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route("types"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("AssetTypeUid", "Asset type this survey is assigned", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("HasResponses", "Return results that has responses", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by CreatedBy.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "A full list of tags.", typeof(SurveyTypeApiResponseModel)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset type not found based on Uid provided.", typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetSurveyTypes()
        {
            var prefix = "Surveys.GetSurveysResultsAsync => ";
            var errorMessage = "";

            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            try
            {

                var queryParams = Request.GetQueryNameValuePairs();
                if (queryParams.Any(x => x.Key.ToLower() == "assettypeuid"))
                {
                    Guid uid = Guid.Parse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "assettypeuid").Value);

                    var assetType = AssetRepository.GetAssetTypeByUID(uid);
                    if (assetType == null)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset type with Uid {uid} not found."));
                    }
                }

                var response = SurveyRepository.GetSurveyTypes(queryParams);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response)));

            }

            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }

        }

        /// <summary>
        /// Returns survey result summary for specific survey type uid defined in Govern.          
        /// </summary>        
        /// <param name="surveyTypeUid">Uid of survey type</param>
        /// <returns>A list of survey summary results</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route("{surveyTypeUid}/results/summary"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("AssetUid", "The uid of a specific asset to return.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("AsOfDate", "Pull results up to a certain date.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by CreatedBy.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "A full list of survey results.", typeof(SurveyApiResponseModel)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset not found based on Uid provided.", typeof(ErrorResponse)),

        ]
        public async Task<IHttpActionResult> GetSurveysResultsSummaryAsync(string surveyTypeUid)
        {
            var prefix = "Surveys.GetSurveysResultsAsync => ";
            var errorMessage = "";

            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            try
            {
                Guid surveyUid = Guid.Parse(surveyTypeUid);

                var survey = SurveyRepository.GetSurveyTypeByUid(surveyUid);
                if (survey == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Survey Type with Uid {surveyTypeUid} not found."));
                }

                var queryParams = Request.GetQueryNameValuePairs();

                if (queryParams.Any(x => x.Key.ToLower() == "assetuid"))
                {
                    Guid uid = Guid.Parse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "assetuid").Value);

                    var assetType = AssetRepository.GetAssetByUID(uid);
                    if (assetType == null)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset with Uid {uid} not found."));
                    }
                }

                var response = SurveyRepository.GetSurveyResultSummary(surveyUid, queryParams);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response)));

            }

            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }

        }

    }
}
