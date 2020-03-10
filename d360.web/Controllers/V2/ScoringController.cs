using d360.core;
using d360.core.entities;
using d360.core.entities.Scoring;
using d360.core.enums;
using d360.extensions;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Dapper;
using Microsoft.Web.Http;
using SpreadsheetLight;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling metrics and scoring for assets throughout your environment.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/scoring"),
        Authorize
    ]
    public class ScoringController : BaseV2ApiController
    {
        #region DI

        IAssetRepository AssetRepository;
        IScoringRepository ScoringRepository;
        public ScoringController(ICommunityContext community, ICompanyContext company, IQueueSource queueSource, IScoringRepository scoringRepository, IAssetRepository assetRepository)
            : base(community, company)
        {
            this.AssetRepository = assetRepository;
            this.ScoringRepository = scoringRepository;
        }

        #endregion



        /// <summary>
        /// Get a list of allocations.
        /// </summary>
        /// <returns>The allocation.</returns>
        [
            HttpGet,
            ApiExplorerSettings(IgnoreApi = true),
            Route("allocations"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "Returns the list of allocations.", typeof(List<AllocationApiGetModel>)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))
        ]
        public IHttpActionResult GetAllocations()
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Unauthorized, "Error retrieving allocations", "You are not authorized to perform this action.");
                }

                var queryParams = Request.GetQueryNameValuePairs();

                string errorMessage = string.Empty;

                List<AllocationApiGetModel> allocations = ScoringRepository.GetAllocations(queryParams, out errorMessage);

                if (!string.IsNullOrEmpty(errorMessage))
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error retrieving allocations", errorMessage);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, allocations));
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error retrieving allocations", $"An unknown error occured and has been logged for further investigation. Please try your request again later.");
            }
        }


        /// <summary>
        /// Creates allocation based on provided asset type uid and score type.
        /// </summary>
        /// <returns>The allocation.</returns>
        [
            HttpPost,
            ApiExplorerSettings(IgnoreApi = true),
            Route("allocations"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.Created, "Returns the corresponding allocation.", typeof(AllocationApiGetModel)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset type was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to insert this allocation is invalid, possibly due to an incorrectly formatted identifier (Uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))
        ]
        public IHttpActionResult PostAllocation(AllocationApiUpsertModel model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Unauthorized, "Error adding allocation", "You are not authorized to perform this action.");
                }

                if (model.assetTypeUid == null || model.assetTypeUid == Guid.Empty)
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error adding allocation", $"You have not provided valid assetTypeUid.");

                List<ScoreType> scoreTypes = new List<ScoreType>() { ScoreType.DataQuality, ScoreType.Governance, ScoreType.Perceptional };

                if (!scoreTypes.Contains(model.scoreType))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error adding allocation", $"You have not provided valid scoreType.");
                }

                var assetType = AssetRepository.GetAssetTypeByUID(model.assetTypeUid);

                List<AssetTypeClass> allowedClasses = ScoringRepository.AllowedClassesForScoreType();
                if (assetType == null)
                    return errorMessageResponse(HttpStatusCode.NotFound, "Error adding allocation", $"AssetType with uid {model.assetTypeUid} does not exist.");

                if (!allowedClasses.Contains(assetType.Class))
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error adding allocation", $"Asset type has invalid class.");

                ScoreTypeAllocation alloc = ScoringRepository.GetAllocationByModel(model);

                if (alloc != null && alloc.State == State.Active)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error adding allocation", $"Score Allocation already exists.");
                }

                if (model.scoreType == ScoreType.DataQuality && model.isExternallyCalculated == false)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error adding allocation", $"Data Quality Score Allocation cannot have isExternallyCalculated flag set to False.");
                }

                if (model.lowerThreshold == null)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"Lower threshold must be set.");
                }
                if (model.upperThreshold == null)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"Upper threshold must be set.");
                }
                if (model.lowerThreshold >= model.upperThreshold)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"Lower threshold must be smaller than Upper threshold.");
                }
                if (model.lowerThreshold <= 0 || model.upperThreshold <= 0 || model.upperThreshold > 100)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"Threshold values must be between 0 and 100.");
                }

                AllocationApiGetModel allocation = ScoringRepository.PostAllocation(model, ref alloc);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Created, allocation));
            }
            catch (Exception ex)
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error adding allocations", $"An unknown error occured and has been logged for further investigation. Please try your request again later.");
            }
        }

        /// <summary>
        /// Updates an existing allocation.
        /// </summary>
        /// <returns>The allocation.</returns>
        [
            HttpPut,
            ApiExplorerSettings(IgnoreApi = true),
            Route("allocations/{allocationUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding allocation.", typeof(AllocationApiGetModel)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your allocation was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to update this allocation is invalid, possibly due to an incorrectly formatted identifier (Uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))
        ]
        public IHttpActionResult PutAllocation(Guid allocationUid, AllocationApiUpsertModel model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Unauthorized, "Error updating allocation", "You are not authorized to perform this action.");
                }

                ScoreTypeAllocation alloc = ScoringRepository.GetAllocationByUid(allocationUid);

                if (alloc == null)
                    return errorMessageResponse(HttpStatusCode.NotFound, "Error updating allocation", $"Allocation with uid {allocationUid} does not exist.");

                if (model.assetTypeUid == null || model.assetTypeUid == Guid.Empty)
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"You have not provided valid assetTypeUid.");

                List<ScoreType> scoreTypes = new List<ScoreType>() { ScoreType.DataQuality, ScoreType.Governance, ScoreType.Perceptional };

                if (!scoreTypes.Contains(model.scoreType))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"You have not provided valid scoreType.");
                }

                var assetType = AssetRepository.GetAssetTypeByUID(model.assetTypeUid);

                List<AssetTypeClass> allowedClasses = ScoringRepository.AllowedClassesForScoreType();
                if (assetType == null)
                    return errorMessageResponse(HttpStatusCode.NotFound, "Error updating allocation", $"AssetType with uid {model.assetTypeUid} does not exist.");

                if (!allowedClasses.Contains(assetType.Class))
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"Asset type has invalid class.");

                bool alreadyExists = ScoringRepository.DoesAllocationExist(allocationUid, model);

                if (alreadyExists)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"Score Allocation already exists.");
                }
                
                bool hasActiveMeasures = ScoringRepository.HasActiveMeasures(alloc);
                bool canBeEdited = (model.assetTypeUid == alloc.AssetTypeUid
                                   && model.scoreType == alloc.ScoreType
                                   && model.isExternallyCalculated == alloc.IsExternallyCalculated)
                                   || !hasActiveMeasures;

                if (!canBeEdited)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"Unfortunately you are unable to update a scores Asset Type, Score Type or Externally calculated flag if score has active measures defined.");
                }

                if (model.scoreType == ScoreType.DataQuality && model.isExternallyCalculated == false)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"Data Quality Score Allocation cannot have isExternallyCalculated flag set to False.");
                }

                if (model.lowerThreshold == null)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"Lower threshold must be set.");
                }
                if (model.upperThreshold == null)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"Upper threshold must be set.");
                }
                if (model.lowerThreshold >= model.upperThreshold)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"Lower threshold must be smaller than Upper threshold.");
                }
                if (model.lowerThreshold <= 0 || model.upperThreshold <= 0 || model.upperThreshold > 100)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"Threshold values must be between 0 and 100.");
                }


                AllocationApiGetModel allocation = ScoringRepository.UpdateAllocation(model, alloc);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, allocation));
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error updating allocations", $"An unknown error occured and has been logged for further investigation. Please try your request again later.");
            }
        }

        /// <summary>
        /// Gets allocations.
        /// </summary>
        /// <returns>The metric.</returns>
        [
            HttpDelete,
            ApiExplorerSettings(IgnoreApi = true),
            Route("allocations/{allocationUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "Returns the corresponding metric.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your metric was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this metric is invalid, possibly due to an incorrectly formatted identifier (Uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))
        ]
        public IHttpActionResult DeleteAllocation(Guid allocationUid)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Unauthorized, "Error deleting allocation", "You are not authorized to perform this action.");
                }

                var alloc = ScoringRepository.GetAllocationByUid(allocationUid);

                if (alloc == null)
                    return errorMessageResponse(HttpStatusCode.NotFound, "Error deleting allocation", $"Allocation with uid {allocationUid} does not exist.");

                var hasActiveMeasures = ScoringRepository.HasActiveMeasures(alloc);
                if (hasActiveMeasures)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"Unfortunately you are unable to delete a score with measures defined.");
                }

                ScoringRepository.DeleteAllocation(alloc);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new ConfirmResponse() { message = "Allocation succesfully deleted!" }));
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error deleting allocations", $"An unknown error occured and has been logged for further investigation. Please try your request again later.");
            }
        }


        /// <summary>
        /// GET a list of relationship types.
        /// </summary>
        /// <returns>A excel file containing relationships types.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            ApiExplorerSettings(IgnoreApi = true),
            Route("export"),
            FileDownload,
            SwaggerConsumes("application/vnd.ms-excel"), SwaggerProduces("application/vnd.ms-excel"),
            SwaggerResponse(HttpStatusCode.OK, "Exported realtionship types to Excel.", typeof(List<PredicateTypeApiViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public IHttpActionResult ExportAllocationsToExcel()
        {
            var queryParams = Request.GetQueryNameValuePairs();
            queryParams = queryParams.Union(new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("_state", "1") });
            string error = string.Empty;
            var models = ScoringRepository.GetAllocations(queryParams, out error);
            var document = new SLDocument();
            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, "Items");

            #region Create the list sheet

            #region Header

            int index = 1;
            document.SetCellValue(1, index++, "Asset Class");
            document.SetCellValue(1, index++, "Asset Type");
            document.SetCellValue(1, index++, "Score Type");
            document.SetCellValue(1, index++, "Score Calculation");
            document.SetCellValue(1, index++, "Threshold - Poor");
            document.SetCellValue(1, index++, "Threshold - Average");
            document.SetCellValue(1, index++, "Threshold - Good");
            document.SetCellValue(1, index++, "Asset Type UID");
            document.SetCellValue(1, index++, "Score UID");

            #endregion

            int rowNumber = 1;
            foreach (var row in models)
            {
                index = 1;
                rowNumber++;

                document.SetCellValue(rowNumber, index++, row.assetClassName.GetDisplayName());
                document.SetCellValue(rowNumber, index++, row.assetTypePath);
                document.SetCellValue(rowNumber, index++, row.scoreType.GetDisplayName());
                document.SetCellValue(rowNumber, index++, row.isExternallyCalculated ? "External" : "Internal");
                document.SetCellValue(rowNumber, index++, $"0-{row.lowerThreshold}");
                document.SetCellValue(rowNumber, index++, $">{row.lowerThreshold}-{row.upperThreshold}");
                document.SetCellValue(rowNumber, index++, $">{row.upperThreshold}-100");
                document.SetCellValue(rowNumber, index++, row.assetTypeUid.ToString());
                document.SetCellValue(rowNumber, index++, row.uid.ToString());
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
        /// Get a list of asset types that have not been allocated to the provided score type.
        /// </summary>
        /// <param name="scoreType">The score type to get asset types with no allocations.</param>
        /// <returns>List of asset types that have not been allocated to the provided score type.</returns>
        [
            HttpGet,
            ApiExplorerSettings(IgnoreApi = true),
            Route("unallocatedAssetTypes/{scoreType}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "Returns a list of asset types that are not yet allocated to the score type provided.", typeof(List<AllocationApiGetUnallocatedAssetTypeModel>)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetUnallocatedAssetTypesForScoreType(string scoreType)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Unauthorized, "Error retrieving unallocated asset types", "You are not authorized to perform this action.");
                }

                if (!Enum.TryParse(scoreType, true, out ScoreType sc))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error retrieving unallocated asset types", $"Invalid score type: {scoreType} provided, please provide a valid score type.");
                }

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, await ScoringRepository.GetUnallocatedAssetTypes(sc)));
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error retrieving allocations", $"An unknown error occured and has been logged for further investigation. Please try your request again later.");
            }
        }

        /// <summary>
        /// Post externally calculated scores and measure results.
        /// </summary>
        /// <param name="model">The externally calculated score results to load.</param>
        /// <param name="scoreType">The score type of the score results. Valid values for scoreType are 1) DataQuality and 2) Governance. Either the numerical value or string value can be supplied</param>
        /// <returns>List of results.</returns>
        [
            HttpPost,
            Route("{scoreType}/externalresults"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "The list of results, containing any potential errors. A value of true for the IsSuccess property indicates that the metric was saved.", typeof(List<ExternalScoreResultsApiResultsModel>)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))
        ]
        public IHttpActionResult PostExternalResults(string scoreType, List<ExternalScoreResultsApiPostModel> model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return errorMessageResponse(HttpStatusCode.Unauthorized, "Error adding score results", "You are not authorized to perform this action.");
                }

                if (!Enum.TryParse(scoreType, true, out ScoreType scoreTypeEnum))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error adding score results", $"Invalid score type: {scoreType} provided, please provide a valid score type.");
                }

                var execution = getApiExecution(model.Count);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, ScoringRepository.PostExternalResults(scoreTypeEnum, model, execution)));
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error adding score results", $"An unknown error occured and has been logged for further investigation. Please try your request again later.");
            }
        }


        /// <summary>
        /// Post measure results to calculate a score internally.
        /// </summary>
        /// <param name="model">The score results to load.</param>
        /// <param name="scoreType">The score type of the score results. Valid values for scoreType are 1) DataQuality and 2) Governance. Either the numerical value or string value can be supplied</param>
        /// <returns>The results.</returns>
        [
            HttpPost,
            Route("{scoreType}/results"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "The list of staging results, containing any potential errors. A value of true for the IsSuccess property indicates that the metric was saved for further processing.", typeof(List<BulkMetricTemporaryTableModel>)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your score type was not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request model was invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))
        ]
        public IHttpActionResult PostScoreResults(string scoreType, List<ScoreResultApiPostModel> model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return errorMessageResponse(HttpStatusCode.Unauthorized, "Error adding score results", "You are not authorized to perform this action.");

                if (!Enum.TryParse(scoreType, true, out ScoreType scoreTypeEnum))
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error adding score results", $"Invalid score type: {scoreType} provided, please provide a valid score type.");

                if (model == null || model.Count < 1)
                    return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "You have submitted an invalid or empty data set. Please check your request and submit again."));


                var execution = getApiExecution(model.Count);
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, ScoringRepository.PostScoreResults(scoreTypeEnum, execution, model)));
            }
            catch
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error adding score results", $"An unknown error occured and has been logged for further investigation. Please try your request again later.");
            }
        }
    }


}
