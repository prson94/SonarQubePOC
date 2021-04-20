using d360.core.entities;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using Resources;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace d360.web.Controllers.V2
{
    [ApiVersion("2.0"), RoutePrefix("api/v{version:apiVersion}/dataprofiles"), Authorize]
    public class DataProfilesController : BaseV2ApiController
    {
        internal IDataProfileRepository DataProfiles;
        internal IAssetRepository AssetRepository;

        public DataProfilesController(ICommunityContext community, ICompanyContext company, IDataProfileRepository dataProfileRepository, IAssetRepository assetRepository)
            : base(community, company)
        {
            this.DataProfiles = dataProfileRepository;
            this.AssetRepository = assetRepository;
    }

        /// <summary>
        /// Provides support for adding a Data Profile records.
        /// </summary>
        /// <param name="models">Data Profile record collection.</param>
        /// <returns>Results response stating the success or failure or the request.</returns>
        [
            HttpPost,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Adding Data Profile Records.", typeof(DataProfileUpsertResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> PostDataProfiles(List<DataProfileUpsertModel> models)
        {
            var prefix = "DataProfiles.PostDataProfiles => ";
            var execution = getApiExecution(models.Count);

            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, NOT_AUTHORIZED_MESSAGE)).ConfigureAwait(false);
            }

            try
            {
                if (models.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, $"You may only provide a maximum of {MAX_SYNCHRONOUS_API_ITEM_COUNT} Data Profile records in this request. Please call the BATCH API to submit more than {MAX_SYNCHRONOUS_API_ITEM_COUNT} items.")).ConfigureAwait(false);
                }

                var validationResult =  ValidateDataProfileUpsertRequest(models, true);
                if(validationResult.StatusCode != HttpStatusCode.OK)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, validationResult.Message)).ConfigureAwait(false);
                }

                var results =  DataProfiles.UpsertDataProfiles(models, execution, true);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string> {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Provides support for Updating a Data Profile records.
        /// </summary>
        /// <param name="models">Data Profile record collection.</param>
        /// <returns>Results response stating the success or failure or the request.</returns>
        [
            HttpPut,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Updating Data Profile Records.", typeof(DataProfileUpsertResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, NOT_FOUND_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, BAD_REQUEST_GENERIC_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, NOT_AUTHORIZED_MESSAGE, typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> PutDataProfiles(List<DataProfileUpsertModel> models)
        {
            var prefix = "DataProfiles.PostDataProfiles => ";
            var execution = getApiExecution(models.Count);

            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, NOT_AUTHORIZED_MESSAGE)).ConfigureAwait(false);
            }

            try
            {
                if (models.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, $"You may only provide a maximum of {MAX_SYNCHRONOUS_API_ITEM_COUNT} Data Profile records in this request. Please call the BATCH API to submit more than {MAX_SYNCHRONOUS_API_ITEM_COUNT} items.")).ConfigureAwait(false);
                }

                var validationResult = ValidateDataProfileUpsertRequest(models, false);
                if (validationResult.StatusCode != HttpStatusCode.OK)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.BadRequest, validationResult.Message)).ConfigureAwait(false);
                }

                var results = DataProfiles.UpsertDataProfiles(models, execution, false);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results));
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string> {
                    { "Endpoint Method", prefix }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage)).ConfigureAwait(false);
            }
        }

        public WorkHttpStatus ValidateDataProfileUpsertRequest(List<DataProfileUpsertModel> models, bool IsInsert)
        {
            //Key Field Validation
            if (models.Any(dp => dp.profileSetDate == null || dp.assetUid == null))
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, BAD_REQUEST_GENERIC_MESSAGE);
            }
            var dupRecords = models.GroupBy(i => new { i.assetUid, i.profileSetDate }).Where(i => i.Count() > 1).Select(i => new { keyFields = i.Key, Count = i.Count() }).ToList();
            if (dupRecords.Any())
            {
                var ErrorMessage = $"Duplicate Records: {string.Join(", ", dupRecords.Select(i => $"(AssetUid: {i.keyFields.assetUid}, ProfileSetDate: {i.keyFields.profileSetDate.Date: yyyy-MM-dd})"))}. AssetUid and ProfileSetDate pairs are used as record identifiers and must be unique within a batch.";
                return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, ErrorMessage);
            }

            List<ValidationResult> validationResults = new List<ValidationResult>();
            foreach (var model in models)
            {
                validationResults.Clear();
                Asset asset = AssetRepository.GetAssetByUID(model.assetUid);

                if (asset == null)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, $"AssetUid {model.assetUid} is invalid");
                }

                var profileSetDate = model.profileSetDate.Date;                
                var recordExists = Company.AssetDataProfile.Any(x => x.AssetId == asset.ID && DbFunctions.TruncateTime(x.ProfileSetDate) == profileSetDate);
                //check insert
                if (recordExists && IsInsert)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, $"Record already exists for AssetUid {model.assetUid} and ProfileSetDate {model.profileSetDate.Date:yyyy-MM-dd}");
                }
                //check update
                if (!recordExists && !IsInsert)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, $"Record does not exist for AssetUid {model.assetUid} and ProfileSetDate {model.profileSetDate.Date:yyyy-MM-dd}");
                }                

                if (model.bottomK != null && model.bottomK.Count > 0)
                {
                    var bottomKValue = string.Join(",", model.bottomK);
                    if (bottomKValue.Length > 200)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, $"topK value must be less than 200 characters");
                    }
                }

                if (model.topK != null && model.topK.Count > 0)
                {
                    var topKValue = string.Join(",", model.topK);
                    if (topKValue.Length > 200)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, $"topK value must be less than 200 characters");
                    }
                }

                if (model.shapesDetail != null && model.shapesDetail.Count > 0)
                {
                    var shapesDetailValue = JsonConvert.SerializeObject(model.shapesDetail);
                    if (shapesDetailValue.Length > 200)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, $"shapesDetail value must be less than 200 characters");
                    }
                }

                if (model.cardinalityDetail != null && model.cardinalityDetail.Count > 0)
                {
                    var cardinalityDetailValue = JsonConvert.SerializeObject(model.cardinalityDetail);
                    if (cardinalityDetailValue.Length > 200)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, $"cardinalityDetail value must be less than 200 characters");
                    }
                }

                bool isValid = Validator.TryValidateObject(model, new ValidationContext(model, serviceProvider: null, items: null), validationResults, true);
                if (!isValid)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, ApiMessages.BadRequest, validationResults.First().ErrorMessage);
                }
            }
            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }
    }
}
