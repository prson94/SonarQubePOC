using d360.core.entities;
using d360.model;
using d360.model.DataAccessLayer;
using d360.model.validators;
using d360.web.Filters;
using d360.web.Models;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Linq;
using d360.web.Models.Attributes;
using SpreadsheetLight;
using System.IO;
using Newtonsoft.Json;
using d360.core.entities.Process;
using Dapper;
using d360.core.enums;
using System.Data;
using Resources;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling tag management in Govern
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/connectorLabels"),
        Authorize,
        ApiExplorerSettings(IgnoreApi = true)
    ]
    public class ConnectorLabelsController : BaseV2ApiController
    {

        IConnectorLabelRepository ConnectorLabelRepository;

        public ConnectorLabelsController(ICoreComponentSet set, IConnectorLabelRepository connectorLabelRepository): base(set)
        {
            this.ConnectorLabelRepository = connectorLabelRepository;
        }


        /// <summary>
        /// Retrieves a list of available labels by search term
        /// </summary>
        /// <returns></returns>
        [
            HttpGet,
            Route("search"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "The list of connector labels."),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the request was not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An error to indicate an internal server error.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetLabels(string q = null, bool isExact = false, bool getUseCount = false, Guid? exceptUid = null)
        {
            string labelsSql;
            if (isExact)
            {
                labelsSql = $@"SELECT top 10 uid, Value
                                {(getUseCount ? ", Labels.cnt as UseCount" : "")}
                                  FROM [dbo].[ConnectorLabel] cl 
                                {(getUseCount ? "cross apply (select count(*) from ProcessExpandedData where LabelUid = cl.uid)Labels(cnt)" : "")}
                                where Value = @q and state = 1 
                                {(exceptUid.HasValue ? " and cl.uid <> @exceptUid" : "")}
                                order by Value";
            }
            else
            {
                if (!string.IsNullOrEmpty(q))
                {
                    q = $"%{q}%";
                }

                labelsSql = $@"SELECT top 10 uid, Value                                
                                    {(getUseCount ? ", Labels.cnt as UseCount" : "")}
                                  FROM [dbo].[ConnectorLabel] cl
                                {(getUseCount ? "cross apply (select count(*) from ProcessExpandedData where LabelUid = cl.uid)Labels(cnt)" : "")}
                                where state = 1 
                                {(!string.IsNullOrEmpty(q) ? " and Value like @q" : "")}
                                {(exceptUid.HasValue ? " and cl.uid <> @exceptUid" : "")}
                                order by Value";
            }
            var response = Company.Query<dynamic>(labelsSql, new { q, exceptUid }, ApiTimeout);

            return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response))).ConfigureAwait(false);

        }

        /// <summary>
        /// Retrieves a connector label usage by connector label uid
        /// </summary>
        /// <returns></returns>
        [
            HttpGet,
            Route("{labelUid:Guid}/usage"),
            SwaggerConsumes("application/json", "application/json", "application/octet-stream"),
            SwaggerResponse(HttpStatusCode.OK, "The list of connector labels."),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the request was not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An error to indicate an internal server error.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetUsage(Guid labelUid)
        {
            try
            {
                if (labelUid == null || labelUid == Guid.Empty)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ConnectorLabelAPIMessage.UidNotValid)).ConfigureAwait(false);
                }

                var label = Company.ConnectorLabels.FirstOrDefault(x => x.uid == labelUid);
                if (label == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, ApiMessages.NotFound, string.Format(ConnectorLabelAPIMessage.UidNotFound, labelUid.ToString()))).ConfigureAwait(false);
                }

                var isStreamResponse = Request?.Headers?.Accept?.Any(a => a.MediaType == "application/octet-stream") ?? false;
                IEnumerable<dynamic> response = ConnectorLabelRepository.GetConnectorLabelUsage(labelUid, Request.GetQueryNameValuePairs());

                if (isStreamResponse)
                {
                    (byte[] bytes, string filename) = ConnectorLabelRepository.GetExcelFromConnectorLabelUsage(label, response);
                    var fileResponse = createFileResponseMessage(HttpStatusCode.OK, $"{filename}.xlsx", bytes);
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(fileResponse)).ConfigureAwait(false);

                }
                else
                {
                    return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response)));
                }
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", "ConnectorLabel.GetUsage" },
                    { "LabelUid", labelUid.ToString() }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// Create or get label by label name
        /// Used by connector label autocomplete control in Process Designer
        /// </summary>
        /// <returns></returns>
        [
            HttpPost,
            Route("insertOrGet"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "The list of connector labels."),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the request was not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An error to indicate an internal server error.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> CreateOrGetLabel(ConnectorLabelPostModel label)
        {


            if (label == null || string.IsNullOrEmpty(label.Value) || label.Value.Trim() == "")
            {
                return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest,ConnectorLabelAPIMessage.LabelValieNotEmpty))).ConfigureAwait(false);
            }

            var labelValue = label.Value.Trim();
            var dbRecord = Company.ConnectorLabels.FirstOrDefault(x => x.Value.ToLower() == labelValue.ToLower());
            if (dbRecord != null)
            {
                return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, dbRecord))).ConfigureAwait(false);
            }


            if (labelValue.Length > 40)
            {
                return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest,ConnectorLabelAPIMessage.LabelMax40Char))).ConfigureAwait(false);
            }

            dbRecord = new ConnectorLabel();
            dbRecord.Value = labelValue;
            dbRecord.UpdatedBy = dbRecord.CreatedBy = Company.CurrentResourceID;
            dbRecord.UpdatedOn = dbRecord.CreatedOn = DateTime.UtcNow;

            Company.Add(dbRecord);

            return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, dbRecord)));

        }

        /// <summary>
        /// Retrieves a list of all connector labels.
        /// </summary>                
        [
            HttpGet, MapToApiVersion("2.0"), Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 250.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("uid", "The Uid of a specific connector label to return.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "A full list of connector labels.", typeof(List<ConnectorLabelApiModelWrapper>)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied"),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))

        ]
        public async Task<IHttpActionResult> Get()
        {
            try
            {
                var queryParams = Request.GetQueryNameValuePairs();

                string isValid = isPageSizeAndNumValid(queryParams);

                if (!string.IsNullOrEmpty(isValid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, isValid)).ConfigureAwait(false);
                }

                var res = await ConnectorLabelRepository.GetLabels(queryParams);

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, res));
            }
            catch (Exception ex)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, ConnectorLabelAPIMessage.ErrorGetLabels, ex.Message);

            }
        }

        /// <summary>
        /// Adds a connector label with the properties provided in the model.
        /// </summary>        
        /// <param name="model">The connector label to be created.</param>
        /// <returns>The created connector label.</returns>
        [
            HttpPost,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "The specified label was saved, returns the properties of the created connector label.", typeof(ConnectorLabelApiModel)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult PostTag(ConnectorLabelPostModel model)
        {
            if (model == null)
            {
                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ApiMessages.ErrorInvalidDatasetMessage));
            }

            ConnectorLabelApiModel result = new ConnectorLabelApiModel();
            try
            {
                ConnectorLabelValidator.ValidateForPost(model);

                //make sure no tag with the same name exists
                if (ConnectorLabelRepository.DoesLabelExists(model.Value))
                {
                    throw new ArgumentNullException(ConnectorLabelAPIMessage.LabelAlreadyExists);
                }


                result = ConnectorLabelRepository.CreateConnectorLabel(model);
            }
            catch (Exception e)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, ConnectorLabelAPIMessage.ErrorCreateLabel, e.Message);
            }

            return ResponseMessage(Request.CreateResponse<ConnectorLabelApiModel>(HttpStatusCode.OK, result));
        }



        /// <summary>
        /// Updates the specified connector label with the values provided in the model.
        /// </summary>
        /// <param name="labelUid">The Uid of the connector label to be updated.</param>        
        /// <param name="model">The new definition of the connector label to be used for the update.</param>
        /// <returns>A connector label model representing the updated connector label.</returns>
        [
            HttpPut,
            MapToApiVersion("2.0"),
            Route("{labelUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "The specified tag was updated, returns the properties of the created tag.", typeof(ConnectorLabelApiModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the tag was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, UNKNOWN_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult Put(Guid labelUid, ConnectorLabelPostModel model)
        {
            if (!ConnectorLabelRepository.DoesLabelExists(labelUid))
            {
                return errorMessageResponse(HttpStatusCode.NotFound, ConnectorLabelAPIMessage.ErrorUpdateLabel, string.Format(ConnectorLabelAPIMessage.UidNotFound, labelUid.ToString()));
            }


            ConnectorLabelApiModel result = new ConnectorLabelApiModel();
            try
            {

                ConnectorLabelValidator.ValidateForPut(labelUid, model);

                var existingLabel = Company.ConnectorLabels.FirstOrDefault(x => x.uid == labelUid);

                if (existingLabel == null)
                {
                    throw new ArgumentNullException(string.Format(ConnectorLabelAPIMessage.UidNotFound, labelUid.ToString()));
                }

                if (ConnectorLabelRepository.DoesLabelExists(labelUid, model))
                {
                    throw new ArgumentNullException(ConnectorLabelAPIMessage.LabelAlreadyExists);
                }

                result = ConnectorLabelRepository.UpdateConnectorLabel(labelUid, model, existingLabel);
            }
            catch (Exception e)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, ConnectorLabelAPIMessage.ErrorUpdateLabel, e.Message);
            }

            return ResponseMessage(Request.CreateResponse<ConnectorLabelApiModel>(HttpStatusCode.OK, result));
        }

        /// <summary>
        /// Deletes a connector label based on the provided Uid.
        /// </summary>
        /// <param name="labels">List of connector labels to be removed.</param>
        /// <returns>A status for the DELETE request.</returns>
        [
            HttpDelete,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the DELETE request.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the connector label provided is invalid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the connector label was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public IHttpActionResult DeleteByUid([FromBody]List<ConnectorLabelApiDeleteModel> labels)
        {

            foreach (var label in labels)
            {
                if (!ConnectorLabelRepository.DoesLabelExists(label.uid))
                {
                    return errorMessageResponse(HttpStatusCode.NotFound, ConnectorLabelAPIMessage.ErrorDeleteLabel, string.Format(ConnectorLabelAPIMessage.UidNotFound, label.uid.ToString()));
                }
            }

            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, ApiMessages.ForbiddenUserNotAuthorizedMessage));
            }

            try
            {
                if (!ConnectorLabelRepository.DeleteConnectorLabels(labels))
                {
                    return errorMessageResponse(HttpStatusCode.NotFound, ConnectorLabelAPIMessage.ErrorDeleteLabel, ConnectorLabelAPIMessage.LabelNotFound);
                }
            }
            catch (Exception ex)
            {
                return errorMessageResponse(HttpStatusCode.BadRequest, ConnectorLabelAPIMessage.ErrorDeleteLabel, ex.Message);

            }

            return successMessageResponse(HttpStatusCode.OK, ConnectorLabelAPIMessage.LabelRemoved, ConnectorLabelAPIMessage.LabelRemoveSucess);
        }

        /// <summary>
        /// GET a list of connector labels.
        /// </summary>
        /// <returns>A excel file containing connector labels.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            ApiExplorerSettings(IgnoreApi = true),
            Route("export"),
            FileDownload,
            SwaggerConsumes("application/vnd.ms-excel"), SwaggerProduces("application/vnd.ms-excel"),
            SwaggerResponse(HttpStatusCode.OK, "Exported connector labels to Excel.", typeof(List<ConnectorLabelApiModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> ExportToExcel()
        {

            var queryParams = Request.GetQueryNameValuePairs();

            var labels = await ConnectorLabelRepository.GetConnectorLabelsForExcel(queryParams);

            var document = new SLDocument();
            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, "Items");

            #region Create the list sheet

            #region Header

            int index = 1;
            document.SetCellValue(1, index++, "Name");
            document.SetCellValue(1, index++, "Use Count");
            document.SetCellValue(1, index++, "Created On");
            document.SetCellValue(1, index++, "Created By");
            document.SetCellValue(1, index++, "Updated On");
            document.SetCellValue(1, index++, "Updated By");
            document.SetCellValue(1, index++, "Label UID");

            #endregion

            int rowNumber = 1;
            foreach (var row in labels)
            {
                index = 1;
                rowNumber++;
                document.SetCellValue(rowNumber, index++, row.Value.ToString());
                document.SetCellValue(rowNumber, index++, row.UseCount.ToString());
                document.SetCellValue(rowNumber, index++, row.CreatedOn.ToString());
                document.SetCellValue(rowNumber, index++, row.CreatedBy.ToString());
                document.SetCellValue(rowNumber, index++, row.UpdatedOn.ToString());
                document.SetCellValue(rowNumber, index++, row.UpdatedBy.ToString());
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
                FileName = string.Format("Connector Labels {0}.xlsx", System.DateTime.Now.ToShortDateString())
            };
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");

            return ResponseMessage(result);
        }


        /// <summary>
        /// Consolidates connector lables
        /// </summary>
        /// <param name="parentUid">The unique identifier of the parent connector label.</param>        
        /// <param name="childrenUids">The list of children connector labels which we want to consolidate.</param>
        /// <returns>A status for the POST request.</returns>
        [
            HttpPost,
            Route("consolidate/{parentUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A message indicating the status of the POST request.", typeof(List<ConnectorLabelApiModel>)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the tag was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public IHttpActionResult ConsolidateLabels(string parentUid, List<string> childrenUids)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, ApiMessages.AccessDenied));
            }

            try
            {

                if (Guid.Parse(parentUid) == Guid.Empty)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ConnectorLabelAPIMessage.ErrorConsolidateLabel, string.Format(ApiMessages.CustomUidNotValid, parentUid));
                }

                foreach (var item in childrenUids)
                {
                    if (Guid.Parse(item) == Guid.Empty)
                    {
                        return errorMessageResponse(HttpStatusCode.BadRequest,ConnectorLabelAPIMessage.ErrorConsolidateLabel, string.Format(ApiMessages.CustomUidNotValid, item));
                    }
                }

                if (childrenUids.Contains(parentUid))
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ConnectorLabelAPIMessage.ErrorConsolidateLabel, ConnectorLabelAPIMessage.UnableIncludeParentinChildLabels);
                }
                var parentGuid = Guid.Parse(parentUid);

                var parentLabel = Company.ConnectorLabels.FirstOrDefault(x => x.uid == parentGuid);
                if (parentLabel == null)
                {
                    return errorMessageResponse(HttpStatusCode.BadRequest, ConnectorLabelAPIMessage.ErrorConsolidateLabel, string.Format(ConnectorLabelAPIMessage.UidNotFound, parentUid));
                }

                //Get diagram usage
                List<Guid> children = childrenUids.Select(x => Guid.Parse(x)).ToList();
                List<long> assetUids = Company.Query<long>($@"select a.id from processexpandeddata ped
                                            inner join asset a on a.uid = ped.diagramassetuid
                                            where labeluid in @children", new { children }).ToList();

                var processes = Company.AssetProcessDiagrams.AsNoTracking().Where(x => assetUids.Contains(x.AssetId)).ToList();

                if (Company.Database.Connection.State != ConnectionState.Open)
                {
                    Company.Connection.Open();
                }

                using (var trans = Company.Connection.BeginTransaction())
                {
                    var conn = trans.Connection;
                    try
                    {


                        foreach (var process in processes)
                        {
                            if (process.Diagram != null)
                            {
                                var model = JsonConvert.DeserializeObject<ProcessDiagramModel>(process.Diagram);
                                foreach (var link in model.linkDataArray.Where(x => x.labelUid.HasValue))
                                {
                                    if (children.Contains(link.labelUid.Value))
                                    {
                                        link.labelUid = parentGuid;
                                    }
                                }


                                conn.Execute($@"
                                update AssetProcessDiagram
                                    set Diagram = @updatedDiagram
                                where ID = @diagramId",
                                    new
                                    {
                                        diagramId = process.ID,
                                        updatedDiagram = JsonConvert.SerializeObject(model),
                                        resourceId = Company.CurrentResourceID
                                    }, transaction: trans);
                            }
                        }
                        conn.Execute($@"update ConnectorLabel set State = {(int)State.Deleted} where uid in @children", new { children }, transaction: trans);
                        trans.Commit();

                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            if (trans != null)
                            {
                                trans.Rollback();
                            }
                        }
                        catch
                        {
                        }

                        throw ex;
                    }
                }
                var result = ConnectorLabelAPIMessage.ConsolidateSucessfully;

                return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result));
            }
            catch (Exception ex)
            {
                return errorMessageResponse(HttpStatusCode.InternalServerError, ConnectorLabelAPIMessage.ErrorConsolidateLabel, ex.Message);

            }

        }


    }
}

