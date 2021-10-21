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
using d360.model.validators;
using Resources;

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
        ISurveyApiModelValidator validator;
        public SurveysController(ICommunityContext community, ICompanyContext company, IAssetRepository assetRepository, ISettingsRepository settingsRepository, ISurveyRepository surveyRepository,
            ISurveyApiModelValidator validator)
            : base(community, company, settingsRepository)
        {
            this.AssetRepository = assetRepository;
            this.SurveyRepository = surveyRepository;
            this.validator = validator;
        }

        /// <summary>
        /// Returns survey results for a specific survey type Uid.          
        /// </summary>        
        /// <param name="surveyTypeUid">Uid of survey type</param>
        /// <returns>A list of survey results</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route("{surveyTypeUid}/results"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("AssetUid", "The Uid of a specific asset to return.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("AsOfDate", "Pull results up to a certain date.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "A full list of survey results.", typeof(SurveyApiResponseModel)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (Uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset not found based on Uid provided.", typeof(ErrorResponse)),

        ]
        public async Task<IHttpActionResult> GetSurveysResultsAsync(string surveyTypeUid)
        {
            var prefix = "Surveys.GetSurveysResultsAsync => ";
            var errorMessage = "";

            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden,  ApiMessages.AccessDenied));

            try
            {
                Guid surveyUid = Guid.Parse(surveyTypeUid);

                var survey = SurveyRepository.GetSurveyTypeByUid(surveyUid);
                if (survey == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(SurverysApiMessages.SurveyUidNotFound, surveyTypeUid))).ConfigureAwait(false);
                }
                var queryParams = Request.GetQueryNameValuePairs();

                string isValid = isPageSizeAndNumValid(queryParams);

                if (!string.IsNullOrEmpty(isValid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, isValid)).ConfigureAwait(false);
                }

                if (queryParams.Any(x => x.Key.ToLower() == "assetuid"))
                {
                    Guid uid = Guid.Parse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "assetuid").Value);

                    var asset = AssetRepository.GetAssetByUID(uid);
                    if (asset == null)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.AssetNotFound, uid.ToString()))).ConfigureAwait(false);
                    }
                }

                if (queryParams.Any(x => x.Key.ToLower() == "asofdate"))
                {
                    DateTime date = DateTime.MinValue;
                    var paramDate = queryParams.FirstOrDefault(x => x.Key.ToLower() == "asofdate").Value;
                    if (!DateTime.TryParse(paramDate, out date))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, SurverysApiMessages.InvalidValueAsOfDate)).ConfigureAwait(false);
                    }
                }


                var response = SurveyRepository.GetSurveysResult(surveyUid, queryParams);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response)));

            }

            catch (Exception ex)
            {
                HttpStatusCode errorCode = HttpStatusCode.InternalServerError;
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                string errorTitle = "Unknown error";

                if (ex is FormatException)
                {
                    errorMessage = errorMessage.Replace("Guid", "Uid");
                    errorCode = HttpStatusCode.BadRequest;
                    errorTitle = ApiMessages.InvalidRequest;
                }

                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(errorCode, errorTitle, errorMessage)).ConfigureAwait(false);
            }

        }


        /// <summary>
        /// Returns defined survey types.          
        /// </summary>        
        /// <returns>A list of survey types</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route("types"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("AssetTypeUid", "Asset type this survey is assigned", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("SurveyTypeUid", "This querystring parameter is optional and if specified it returns the properties of a single survey", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("HasResponses", "Default value(blank) returns all types. Set to true to return only those survey types to which users have responded, and set to false to return survey types without responses", DataType = "boolean", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by. The acceptable fields are Name, CreatedOn, UpdatedOn, ValidForDays and NumberOfResponses. By default the results are ordered by CreatedOn ascending. Fields ValidForDays and NumberOfResponses are sorted descending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "A full list of survey types.", typeof(SurveyTypeApiResponseModel)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (Uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset type not found based on Uid provided.", typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetSurveyTypes()
        {
            var prefix = "Surveys.GetSurveysTypesAsync => ";
            var errorMessage = "";


            try
            {

                var queryParams = Request.GetQueryNameValuePairs();
                string isValid = isPageSizeAndNumValid(queryParams);

                if (!string.IsNullOrEmpty(isValid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, isValid)).ConfigureAwait(false);
                }

                var status = validator.ValidateGetSurveyTypesRequest(queryParams);

                if (status != null)
                {
                    return await Task.FromResult(errorMessageResponse(status.StatusCode, status.Error, status.Message)).ConfigureAwait(false);
                }

                if (queryParams.Any(x => x.Key.ToLower() == "assettypeuid"))
                {
                    Guid uid = Guid.Parse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "assettypeuid").Value);

                    var assetType = AssetRepository.GetAssetTypeByUID(uid);
                    if (assetType == null)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.AssetTypeNotFound, uid.ToString()))).ConfigureAwait(false);
                    }
                }

                if (queryParams.Any(x => x.Key.ToLower() == "surveytypeuid"))
                {
                    Guid uid = Guid.Parse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "surveytypeuid").Value);

                    var surveyType = SurveyRepository.GetSurveyTypeByUid(uid);
                    if (surveyType == null)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(SurverysApiMessages.SurveyUidNotFound, uid.ToString()))).ConfigureAwait(false);
                    }
                }


                var response = SurveyRepository.GetSurveyTypes(queryParams);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response))).ConfigureAwait(false);

            }

            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                if(ex is FormatException)
                {
                    errorMessage = errorMessage.Replace("Guid", "Uid");
                }

                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }

        }

        /// <summary>
        /// Returns survey result summary for specific survey type Uid.        
        /// </summary>        
        /// <param name="surveyTypeUid">Uid of survey type</param>
        /// <returns>A list of survey summary results</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route("{surveyTypeUid}/results/summary"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("AssetUid", "The Uid of a specific asset to return.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("AsOfDate", "Pull results up to a certain date.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by. The acceptable fields are FirstRespondedOn, LastRespondedOn, NumberOfResponders. By default the results are ordered by FirstRespondedOn ascending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "A full list of survey results.", typeof(SurveyResultSummaryApiResponseModel)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (Uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset not found based on Uid provided.", typeof(ErrorResponse)),

        ]
        public async Task<IHttpActionResult> GetSurveysResultsSummaryAsync(string surveyTypeUid)
        {
            var prefix = "Surveys.GetSurveysResultsAsync => ";
            var errorMessage = "";

            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.AccessDenied));

            try
            {
                Guid surveyUid = Guid.Parse(surveyTypeUid);

                var survey = SurveyRepository.GetSurveyTypeByUid(surveyUid);
                if (survey == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(SurverysApiMessages.SurveyUidNotFound, surveyTypeUid))).ConfigureAwait(false);
                }

                var queryParams = Request.GetQueryNameValuePairs();

                string isValid = isPageSizeAndNumValid(queryParams);

                if (!string.IsNullOrEmpty(isValid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, isValid)).ConfigureAwait(false);
                }

                if (queryParams.Any(x => x.Key.ToLower() == "assetuid"))
                {
                    Guid uid = Guid.Parse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "assetuid").Value);

                    var asset = AssetRepository.GetAssetByUID(uid);
                    if (asset == null)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound,ApiMessages.NotFound, string.Format(ActionApiMessages.AssetNotFound, uid.ToString()))).ConfigureAwait(false);
                    }
                    if(asset.AssetType.Object != survey.Object || asset.AssetType.ObjectID != survey.ObjectID)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, SurverysApiMessages.AssetTypeNotSurvey)).ConfigureAwait(false);
                    }
                }

                if (queryParams.Any(x => x.Key.ToLower() == "asofdate"))
                {
                    DateTime date = DateTime.MinValue;
                    var paramDate = queryParams.FirstOrDefault(x => x.Key.ToLower() == "asofdate").Value;
                    if (!DateTime.TryParse(paramDate, out date))
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, SurverysApiMessages.InvalidValueAsOfDate)).ConfigureAwait(false);
                    }
                }

                var response = SurveyRepository.GetSurveyResultSummary(surveyUid, queryParams);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response)));

            }

            catch (Exception ex)
            {
                HttpStatusCode errorCode = HttpStatusCode.InternalServerError;
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                string errorTitle = ApiMessages.UnknownError;
                if (ex is FormatException)
                {
                    errorMessage = errorMessage.Replace("Guid", "Uid");
                    errorCode = HttpStatusCode.BadRequest;
                    errorTitle = ApiMessages.InvalidRequest;
                }


                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(errorCode, errorTitle, errorMessage));
            }

        }
        /// <summary>
        /// Removes a given set of survey results based on the provided input parameters.
        /// </summary>
        /// <remarks>
        /// An Administrator can remove any survey results.
        /// At least one of the following Parameter must be provided:
        /// SurveyTypeUid,
        /// ResourceUid,
        /// AssetUid
        /// </remarks>
        /// <returns>An HTTP status code and message.</returns>
        [HttpDelete,
            MapToApiVersion("2.0"),
            Route("results"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("SurveyTypeUid", "Remove results for a specific survey type.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("ResourceUid", "Remove results for a specific user.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("AssetUid", "Remove results for a specific asset.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("StartDateRange", "Remove results that were submitted starting on a specific date.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("EndDateRange", "Remove results that were submitted ending on a specific date.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "Count of survey results deleted.", typeof(SurveyAPIDeleteResultsResponseModel)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to delete survey result is invalid, must populate either SurveyTypeUid / ResourceUid / AssetUid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Survey Type with Uid {uid} not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "User with Uid {uid} not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset with Uid {uid} not found.", typeof(ErrorResponse)),
            ]
        public async Task<IHttpActionResult> DeleteSurveyResultsAsync()
        {
            var prefix = "Surveys.DeleteSurveyResultsAsync => ";
            var errorMessage = "";

            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.AccessDenied));
            }

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();

                if (!this.validator.IsRequiredGuidExistForDeleteSurveyResult(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.Invalid, SurverysApiMessages.SurveyResourceAssetUidPopulated)).ConfigureAwait(false);
                }

                if (!this.validator.IsValidSurveyType(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(SurverysApiMessages.SurveyUidNotFound, GetUidFromQueryParams(queryParams, "SurveyTypeUid")))).ConfigureAwait(false);
                }

                if (!this.validator.IsValidAsset(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.AssetNotFound, GetUidFromQueryParams(queryParams, "AssetUid")))).ConfigureAwait(false);
                }

                if (!this.validator.IsValidResource(queryParams))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.UserUidNotFound, GetUidFromQueryParams(queryParams, "ResourceUid")))).ConfigureAwait(false);
                }

                if (!this.validator.IsValidDate(queryParams, "StartDateRange"))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.Invalid, SurverysApiMessages.NotvalidStartDateRange)).ConfigureAwait(false);
                }

                if (!this.validator.IsValidDate(queryParams, "EndDateRange"))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.Invalid, SurverysApiMessages.NotavalidEndDateRange)).ConfigureAwait(false);
                }

                int count = this.SurveyRepository.DeleteSurveyResults(queryParams);
                var result = new SurveyAPIDeleteResultsResponseModel { Message = string.Format(SurverysApiMessages.ResultRemoved, count.ToString()), Success=true };
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result))).ConfigureAwait(false);

            }

            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
               
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }

        }

        /// <summary>
        /// Returns a randomly selected survey applicable to an asset
        /// </summary>
        /// <param name="assetUid">The asset the survey is for</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("{assetUid}"),
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "The survey type Uid and name.", typeof(SurveyAssetApiResponseModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request is invalid.", typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> GetAssetSurveyAsync(string assetUid)
        {
            var prefix = "Surveys.GetAssetSurveyAsync => ";
            string errorMessage;

            if (!Guid.TryParse(assetUid, out Guid parsedAssetUid))
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.Invalid, ActionApiMessages.InvalidAssetUid)).ConfigureAwait(false);
            }

            var asset = AssetRepository.GetAssetByUID(parsedAssetUid);

            if (asset == null)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.Invalid, ActionApiMessages.InvalidAssetUid)).ConfigureAwait(false);
            }

            try
            {
                var survey = await SurveyRepository.GetAssetSurvey(parsedAssetUid);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, survey))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");

                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError,ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Posts a set of survey results for a specific survey type and asset
        /// </summary>
        /// <param name="surveyTypeUid">Uid of the survey type</param>
        /// <param name="model">A list of responses to the survey questions, where the response is indicated by the number that was defined in the Question Options on the Configuration > Surveys page in the application.</param>
        /// <returns>A response code indicating the status of the request</returns>
        [
            HttpPost,
            Route("{surveyTypeUid}"),
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Created, "The survey results were created successfully"),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request is invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Survey Type for the provided uid was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Question Survey Type for the provided uid was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the Asset for the provided uid was not found.", typeof(ErrorResponse)),

        ]
        public async Task<IHttpActionResult> PostSurveyAsync(string surveyTypeUid, SurveyResultsApiModel model)
        {
            var prefix = "Surveys.PostSurveyAsync => ";
            string errorMessage;

            if (model == null || model.Questions == null || model.Questions.Count == 0)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ApiMessages.ErrorInvalidDatasetMessage)).ConfigureAwait(false);
            }

            if (!Guid.TryParse(surveyTypeUid, out Guid uid))
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, SurverysApiMessages.InvalidFormatSurveyTypeUid)).ConfigureAwait(false);
            }

            var surveyType = SurveyRepository.GetSurveyTypeByUid(uid);

            if (surveyType == null)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(SurverysApiMessages.SurveyUidNotFound, uid.ToString()))).ConfigureAwait(false);
            }


            foreach (var question in model.Questions)
            {
                var questionType = SurveyRepository.GetSurveyQuestionTypeByUid(question.SurveyQuestionUid);
                if (questionType == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(SurverysApiMessages.SurveyQuestionTypeUidNotFound, question.SurveyQuestionUid.ToString()))).ConfigureAwait(false);
                }

                var responses = await SurveyRepository.GetSurveyQuestionResponses(questionType.Uid);
                var invalidResponses = question.Responses.Where(r => !responses.Contains(r));

                if (invalidResponses.Any())
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.BadRequest, string.Format(SurverysApiMessages.SurveyQuestionTypeUidInvalid, question.SurveyQuestionUid.ToString(), string.Join(", ", invalidResponses)))).ConfigureAwait(false);
                }
            }

            var asset = AssetRepository.GetAssetByUID(model.AssetUid);

            if (asset == null)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ActionApiMessages.AssetNotFound, model.AssetUid.ToString()))).ConfigureAwait(false);
            }

            if (surveyType.Object != asset.AssetType.Object || surveyType.ObjectID != asset.AssetType.ObjectID)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.BadRequest, SurverysApiMessages.SurveyInvalidForAssetType)).ConfigureAwait(false);
            }

            try
            {
                await SurveyRepository.PostSurveyResults(model, asset, surveyType);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.Created))).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");

                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);

            }
        }

        private Guid GetUidFromQueryParams(IEnumerable<KeyValuePair<string, string>> queryParams, string parameterName)
        {
            Guid uid = Guid.Empty;

            if (queryParams.ToList().Any(q => q.Key.ToLower() == parameterName.ToLower()))
            {
                var uidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == parameterName.ToLower()).Value;
                if (!Guid.TryParse(uidString, out uid))
                {
                    uid = Guid.Empty;
                }

            }
            return uid;
        }

    }
}
