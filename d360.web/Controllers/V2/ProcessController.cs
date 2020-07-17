using d360.core.entities;
using d360.model;
using Microsoft.Web.Http;
using System;
using System.Web.Http;
using d360.core;
using System.Linq;
using System.Data.SqlClient;
using d360.core.enums;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using d360.web.Filters;
using Swashbuckle.Swagger.Annotations;
using d360.web.Models;
using System.Web.Http.Description;
using System.Security.Cryptography;
using System.Text;
using d360.core.entities.Views;
using d360.model.DataAccessLayer;
using d360.core.entities.Graph;
using System.Data;
using Dapper;
using Newtonsoft.Json.Linq;
using d360.core.entities.Process;
using SpreadsheetLight;
using System.IO;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service supports all asset browser functionality for Lineage version 3.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/process"),
        Authorize,
        StringEnumController
    ]
    public class ProcessController : BaseV2ApiController
    {
        IAssetRepository AssetRepository;
        IProcessRepository ProcessRepository;

        public ProcessController(ICommunityContext community, ICompanyContext company, IAssetRepository assetRepository, IProcessRepository processRepository) : base(community, company)
        {
            this.AssetRepository = assetRepository;
            this.ProcessRepository = processRepository;
        }

        /// <summary>
        /// Returns a list of available colors for Governance Roles
        /// </summary>
        /// <returns></returns>
        [
            HttpGet,
            Route("governanceRoleColors"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "The list of available diagram types.", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the request was not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An error to indicate an internal server error.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetAvailableColorsForDiagramNodes()
        {
            var governanceRoleUid = Community.GetCompanySettingByKey<Guid>("GovernanceRoleReferenceListUid");
            var results = await Company.QueryAsync<dynamic>($@"
                select a.ObjectID, a.Object, JSON_VALUE(ACJ.ColorJson, '$.Value') as Value, adv.DisplayValue 
                    from assettype at
	                inner join asset a on a.AssetTypeID = at.ID
                    inner join dbo.GetAssetDisplayValue() adv on adv.id = a.id
                    outer apply dbo.GetAssetColorJsonById(A.Id) ACJ
                where at.uid = @governanceRoleUid", new { governanceRoleUid });

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results));
        }

        /// <summary>
        /// Returns a list of available process diagram nodes for the current asset
        /// </summary>
        /// <param name="assetUid">The asset uid</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("{assetUid:Guid}/availableNodes"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "The list of available diagram types.", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the request was not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An error to indicate an internal server error.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> AvailableDiagramNodesForAsset(Guid assetUid)
        {
            if (assetUid == null)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "The asset uid must be specified."));

            var asset = AssetRepository.GetAssetByUID(assetUid);
            if (asset == null)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "The asset with uid specified does not exist."));

            IEnumerable<dynamic> nodes = await ProcessRepository.GetAvailableDiagramNodesForAsset(assetUid);

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, nodes));
        }

        /// <summary>
        /// Retrieves a process diagram for specific asset
        /// </summary>
        /// <param name="assetUid">The asset uid</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("{assetUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "The list of update model.", typeof(ProcessDiagramModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the request was not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An error to indicate an internal server error.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public IHttpActionResult GetProcessDiagram(Guid assetUid)
        {
            if (assetUid == null)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "The asset uid must be specified."));

            var asset = AssetRepository.GetAssetByUID(assetUid);
            if (asset == null)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "The asset with uid specified does not exist."));

            ProcessDiagramModel model = ProcessRepository.GetAssetsProcessDiagram(assetUid);
            var assetDetail = Company.GetAssetDetail(asset.ID);

            var result = new { model, assetDetail };
            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result));

        }

        /// <returns></returns>
        /// <summary>
        /// Updates a process diagram for specific asset
        /// </summary>
        /// <param name="assetUid">The asset uid</param>
        /// <param name="model"></param>
        /// <returns></returns>
        [
            HttpPut,
            Route("{assetUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "The list of update model.", typeof(ProcessDiagramModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the request was not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An error to indicate an internal server error.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> UpdateProcessDiagram(Guid assetUid, ProcessDiagramModel model)
        {

            try
            {
                var targetAsset = Company.Assets.FirstOrDefault(x => x.uid == assetUid);
                ProcessDiagramModel existingProcess = ProcessRepository.GetAssetsProcessDiagram(assetUid);




                foreach (var item in model.linkDataArray)
                {
                    if (item.from == Guid.Empty || item.to == Guid.Empty)
                    {
                        throw new Exception("Link without from and to node detected.");
                    }
                }

                foreach (var node in model.nodeDataArray)
                {
                    if (!model.linkDataArray.Any(x => x.from == node.AssetUid || x.to == node.AssetUid))
                    {
                        throw new Exception("All nodes must be linked.");
                    }
                }

                if (model.nodeDataArray.GroupBy(x => x["Name"].ToLower()).Select(x => new { x.Key, Count = x.Count() }).Any(x => x.Count > 1))
                {
                    throw new Exception("Nodes withing same Task Type cannot have same name.");
                }
                List<NodeData> toAdd = new List<NodeData>();
                List<NodeData> toUpdate = new List<NodeData>();
                List<NodeData> toDelete = new List<NodeData>();

                foreach (var exNode in existingProcess.nodeDataArray)
                {
                    if (!model.nodeDataArray.Any(x => x.AssetUid == exNode.AssetUid))
                    {
                        toDelete.Add(exNode);
                    }
                }

                foreach (var node in model.nodeDataArray)
                {
                    var uid = node["key"];
                    var existsingNode = existingProcess.nodeDataArray.FirstOrDefault(x => x.AssetUid == node.AssetUid);
                    if (existsingNode == null)
                    {
                        toAdd.Add(node);
                    }
                    else
                    {
                        if (existsingNode.GetHash() != node.GetHash())
                        {
                            toUpdate.Add(node);
                        }
                    }
                }

                List<UpsertModel> upsertModels = new List<UpsertModel>();
                foreach (var item in toAdd.GroupBy(x => x.AssetTypeUid))
                {
                    var umItem = new UpsertModel();
                    umItem.AssetTypeUid = item.Key;
                    umItem.Assets = new List<UpsertAsset>();
                    foreach (var a in item.Select(x => x))
                    {
                        umItem.Assets.Add(new UpsertAsset()
                        {
                            ExternalKey = a.AssetUid,
                            Uid = null,
                            Fields = a.CustomFields
                        });
                    }
                    upsertModels.Add(umItem);
                }
                foreach (var item in toUpdate.GroupBy(x => x.AssetTypeUid))
                {
                    var umItem = new UpsertModel();
                    umItem.AssetTypeUid = item.Key;
                    umItem.Assets = new List<UpsertAsset>();
                    foreach (var a in item.Select(x => x))
                    {
                        umItem.Assets.Add(new UpsertAsset()
                        {
                            ExternalKey = a.AssetUid,
                            Uid = a.AssetUid,
                            Fields = a.CustomFields
                        });
                    }
                    upsertModels.Add(umItem);

                }

                var validationRes = AssetRepository.ValidateAssetUpsertModel(upsertModels);
                if (validationRes.Count > 0)
                {
                    return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new { hasError = true, errors = validationRes })));
                }

                var totalCount = toAdd.Count + toDelete.Count + toUpdate.Count;
                var execution = getApiExecution(totalCount);

                validationRes = ProcessRepository.UpdateProcessDiagram(execution, model, toAdd, toUpdate, toDelete, targetAsset.ID);

                if (validationRes.Count > 0)
                {
                    return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new { hasError = true, errors = validationRes })));
                }


                var result = new { updated = toUpdate.Count, added = toAdd.Count, deleted = toDelete.Count };
                return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result)));

            }
            catch (Exception ex)
            {
                var err = new List<ValidationError>();
                err.Add(new ValidationError() { Error = ex.Message });
                return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new
                {
                    hasError = true,
                    errors = err
                })));
            }

        }
        /// <summary>
        /// Retrieves an excel export of diagram for specific asset
        /// </summary>
        /// <param name="assetUid">The asset uid</param>
        /// <returns></returns>
        [
            HttpPost,
            Route("export/{assetUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "The list of update model.", typeof(ProcessDiagramModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the request was not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An error to indicate an internal server error.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetProcessDiagramExport(Guid assetUid)
        {
            if (assetUid == null)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "The asset uid must be specified."));

            var asset = AssetRepository.GetAssetByUID(assetUid);
            if (asset == null)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "The asset with uid specified does not exist."));
            string result = await Request.Content.ReadAsStringAsync();

            result = result.Replace("data:image/png;base64,", "");
            byte[] image = Convert.FromBase64String(result);

            byte[] bytes = await ProcessRepository.GetDiagramExcel(asset, image);

            var detail = Company.GetAssetDetail(asset.ID);


            var response = createFileResponseMessage(HttpStatusCode.OK, $"{detail.DisplayValue} {DateTime.Now.ToString("MMM dd yyyy")}.xlsx", bytes);
            return await Task.FromResult<IHttpActionResult>(ResponseMessage(response));

        }

        /// <summary>
        /// Retrieves an badges for process diagram
        /// </summary>
        /// <param name="assetUid">The asset uid</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("{assetUid:Guid}/badges"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "The list of update model.", typeof(ProcessDiagramModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the request was not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An error to indicate an internal server error.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetProcessDiagramBadges(Guid assetUid)
        {
            if (assetUid == null)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "The asset uid must be specified."));

            var asset = AssetRepository.GetAssetByUID(assetUid);
            if (asset == null)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "The asset with uid specified does not exist."));

            IEnumerable<dynamic> response = ProcessRepository.GetDiagramAssetBadges(assetUid);

            return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response)));

        }


    }
}
