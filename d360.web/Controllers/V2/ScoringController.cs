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
        Authorize,
        ApiExplorerSettings(IgnoreApi = true)
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
                List<AllocationApiGetModel> allocations = ScoringRepository.GetAllocations(queryParams);

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

                AllocationApiGetModel allocation = ScoringRepository.PostAllocation(model, ref alloc);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Created, allocation));
            }
            catch
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
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"Allocation with same configuration already exists.");
                }

                bool hasActiveMeasures = ScoringRepository.HasActiveMeasures(alloc);
                if (hasActiveMeasures)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating allocation", $"Unfortunately you are unable to delete a score with measures defined.");
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
        public async Task<IHttpActionResult> ExportAllocationsToExcel()
        {
            var queryParams = Request.GetQueryNameValuePairs();
            queryParams = queryParams.Union(new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("_state", "1") });
            var models = ScoringRepository.GetAllocations(queryParams);
            var document = new SLDocument();
            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, "Items");

            #region Create the list sheet

            #region Header

            int index = 1;
            document.SetCellValue(1, index++, "Uid");
            document.SetCellValue(1, index++, "Asset Class");
            document.SetCellValue(1, index++, "Asset Type");
            document.SetCellValue(1, index++, "Asset Type Uid");
            document.SetCellValue(1, index++, "Score Type");

            #endregion

            int rowNumber = 1;
            foreach (var row in models)
            {
                index = 1;
                rowNumber++;
                document.SetCellValue(rowNumber, index++, row.uid.ToString());
                document.SetCellValue(rowNumber, index++, row.assetClassName.GetDisplayName());
                document.SetCellValue(rowNumber, index++, row.assetTypePath);
                document.SetCellValue(rowNumber, index++, row.assetTypeUid.ToString());
                document.SetCellValue(rowNumber, index++, row.scoreType.GetDisplayName());
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
        /// Exports the list of Rules.
        /// </summary>
        /// <param name="uid">The Uid of the Rule Type.</param>
        /// <returns>An excel sheet of the rules of the given rule type uid.</returns>
        [
            HttpGet,
            Route("exportRules/{uid}"),
            ApiExplorerSettings(IgnoreApi = true),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "Returns an excel sheet with all the rules.", typeof(List<Rule>)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetExportRules(string uid)
        {
            try
            {
                Guid guid = Guid.Empty;
                DynamicParameters dbArgs = new DynamicParameters();
                string selectSql = @"
                        SELECT 
		                        A.ID as 'ID',
		                        A.uid as 'AssetUid',
		                        AT.uid as 'AssetTypeUid',
		                        R.Threshold,
		                        A.UpdatedOn,
		                        A.CreatedOn,
                                'asset/' +  + CAST(A.uid as varchar(36)) as 'Url'
	                         ";

                string joinsSql = "  ";

                string whereSQL = "WHERE AT.uid = @uid";
                dbArgs.Add("uid", uid);

                List<string> fieldColumns = new List<string>();
                List<string> fieldJoins = new List<string>();

                var document = createExcelBaseDocument(null, "Items");
                if (!Guid.TryParse(uid, out guid) || guid == Guid.Empty) 
                {
                    return errorMessageResponse(
                        HttpStatusCode.BadRequest, 
                        "Invalid Guid", $"Please provide a valid Guid");
                }

                var typesToAvoid = new List<string>() {
                    DataType.Attribute.ToString(),
                    DataType.ComplexRelationLookup.ToString(),
                    DataType.DataTableSelect.ToString(),
                    DataType.FilteredLookup.ToString(),
                    DataType.OwnershipLookup.ToString()
                };
                var assetType = AssetRepository.GetAssetTypeByUID(guid);
                var fieldTypes = Company.Filter<FieldType>(i => i.Object == assetType.Object && i.ObjectID == assetType.ObjectID).ToList()
                                    .Where(x => !typesToAvoid.Contains(x.Type)).ToList();

                getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns);

                foreach (var col in fieldColumns)
                {
                    selectSql += "," + col;
                }

                foreach (var join in fieldJoins)
                {
                    joinsSql += join ;
                }
                var sql = $@"
                            {selectSql}
                            FROM dbo.[Rule] R
                                                                    LEFT JOIN dbo.RuleType RT on R.RuleTypeID = RT.ID
                                                                    INNER JOIN [dbo].Asset A on A.Object = 'Rule' and A.ObjectID = R.ID
                                                                    INNER JOIN [dbo].AssetType AT on AT.ID = a.AssetTypeID
                            {joinsSql} 
                            {whereSQL}";

                var results = await Company.QueryAsync<dynamic>(sql, new { uid });


                //set row headers and non dynamic field columns
                fieldTypes.Add(new FieldType { Type = "string", Name = "Url", FriendlyName = "Url" });
                fieldTypes.Insert(1, new FieldType { Type = "decimal", Name = "Threshold", FriendlyName = "Threshold" });
                fieldTypes.Insert(0, new FieldType { Type = "string", Name = "AssetTypeUid", FriendlyName = "Asset Type UID" });
                fieldTypes.Insert(0, new FieldType { Type = "Number", Name = "AssetUid", FriendlyName = "Asset UID" });
                fieldTypes.Insert(0, new FieldType { Type = "Number", Name = "ID", FriendlyName = "ID" });

                int index = 1;
                int rowNumber = 1;
                foreach (var field in fieldTypes)
                {
                    document.SetCellValue(1, index++, (string)field.FriendlyName);
                }
                //set values into columns
                foreach (var row in results)
                {
                    index = 1;
                    rowNumber++;

                    foreach (var field in fieldTypes)
                    {
                        var val = getRowFieldValue(row, field);
                        SetSpreadsheetValueFromField(document, rowNumber, index, field, val);
                        index++;
                    }
                }

                var stream = new MemoryStream();
                document.SaveAs(stream);
                var result = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(stream.GetBuffer())
                };
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.ms-excel");
                var response = ResponseMessage(result);

                return response;
            }
            catch(Exception ex)
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, "Error creating spreadsheet", $"{ex.Message}");
            }
        }
        private string getRowFieldValue(dynamic row, FieldType field)
        {
            if (field != null && field.ID > 0)
                return (((row as IDictionary<string, object>)[field.Name]) ?? "").ToString();
            else if (field != null && field.Name == "AssetTypeUid")
                return (string)((row as IDictionary<string, object>)["AssetTypeUid"]).ToString();
            else if (field != null && field.Name == "AssetUid")
                return (string)((row as IDictionary<string, object>)["AssetUid"]).ToString();
            else if (field != null && field.Name == "ID")
                return (row as IDictionary<string, object>)["ID"].ToString();
            else if (field != null && field.Name == "Threshold")
                return (string)((row as IDictionary<string, object>)["Threshold"].ToString());
            else if (field != null && field.Name == "Url")
                return (string)((row as IDictionary<string, object>)["Url"].ToString());
            return "";
        }

    }


}
