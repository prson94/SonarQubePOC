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
        public ProcessController(ICommunityContext community, ICompanyContext company, IAssetRepository assetRepository) : base(community, company)
        {
            this.AssetRepository = assetRepository;
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
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "The asset uid must be specified"));

            var asset = (await Company.QueryAsync<Asset>("select * from Asset where uid = @assetUid", new { assetUid })).FirstOrDefault();

            var sql = $@"
                        declare @assetTypeUid uniqueidentifier = (select at.uid from asset a 
	                        inner join assettype at on a.AssetTypeID = at.ID
                        where a.uid = @assetUid)

                        SELECT     A.[Name]
                                    ,ISNULL(A.[Description],'') as Description
                                    ,A.[Class] as ClassID
                                    ,A.[uid]
									,A.DisplayFormat
                                    ,A.FlowObjectType
                                    ,P.[Path]
                                    ,AT.Icon as Icon
                        FROM        AssetType A
                                    cross apply dbo.GetAssetTypeTextPathById(A.ID, ' / ') P
                                    left join [dbo].[AssetTypeStyle] AT on (A.ID = AT.ID)
									inner join IntersectTypeDetail itd on itd.ObjectUid = a.uid and itd.SubjectUid = @assetTypeUid and itd.predicateType = @predicateType
                        where       
						A.[State] = 1 and A.ObjectID != 0 and Class = 15
						order by Name ";

            var nodes = await Company.QueryAsync<dynamic>(sql, new { assetUid, predicateType = (int)PredicateType.Diagram });


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
        public async Task<IHttpActionResult> GetProcessDiagram(Guid assetUid)
        {
            var targetAsset = Company.Assets.FirstOrDefault(x => x.uid == assetUid);
            var model = new ProcessDiagramModel();

            if (string.IsNullOrEmpty(targetAsset.Diagram))
            {
                model.nodeDataArray = new List<NodeData>();
                model.linkDataArray = new List<LinkData>();
            }
            else
            {
                model = JsonConvert.DeserializeObject<ProcessDiagramModel>(targetAsset.Diagram);
            }
            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model));

        }

        /// <summary>
        /// Updates a process diagram for specific asset
        /// </summary>
        /// <param name="assetUid">The asset uid</param>
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
            var targetAsset = Company.Assets.FirstOrDefault(x => x.uid == assetUid);
            targetAsset.Diagram = JsonConvert.SerializeObject(model);
            Company.SaveChanges();

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, "Saved"));

            //work in progress
            var json = Company.Query<string>("select top 1 Process from Asset where uid=@uid", new { uid = assetUid }).FirstOrDefault();
            var existingProcess = string.IsNullOrEmpty(json) ? new ProcessDiagramModel() : JsonConvert.DeserializeObject<ProcessDiagramModel>(json);
            if (existingProcess.nodeDataArray == null)
                existingProcess.nodeDataArray = new List<NodeData>();
            if (existingProcess.linkDataArray == null)
                existingProcess.linkDataArray = new List<LinkData>();

            if ((model.nodeDataArray.Count == 0 || model.linkDataArray.Count == 0) && !string.IsNullOrEmpty(json))
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Model cannot be empty."));
            }

            foreach (var item in model.linkDataArray)
            {
                if (string.IsNullOrEmpty(item.from) || string.IsNullOrEmpty(item.to))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Link without from and to node detected."));
                }
            }


            if (model.nodeDataArray.GroupBy(x => x.AssetTypeUid.ToString().ToLower() + x["Name"].ToLower()).Select(x => new { x.Key, Count = x.Count() }).Any(x => x.Count > 1))
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Nodes withing same Task Type cannot have same name."));
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

            foreach (var items in toAdd.GroupBy(x => x.AssetTypeUid))
            {
                var at = Company.AssetTypes.FirstOrDefault(x => x.uid == items.Key);
                var execution = getApiExecution(items.Count(), new ApiExecutionFields_PostAssets { AssetTypeUid = at.uid });
                execution.Method = "POST";

                var inserts = new List<AssetInsert>();
                foreach (var asset in items)
                {
                    var newUid = Guid.NewGuid();
                    inserts.Add(new AssetInsert()
                    {
                        ExecutionItemUid = asset.AssetUid,
                        Fields = asset.CustomFields
                    });
                }
                var importResult = AssetRepository.PostAssets(inserts, at, execution);

                //update model records uid after insert is done
                foreach (var importRes in importResult)
                {
                    var node = model.nodeDataArray.FirstOrDefault(x => x.AssetUid == importRes.ExecutionItemUid);
                    node.UpdateAssetUid(importRes.uid);
                }
            }

            foreach (var items in toUpdate.GroupBy(x => x.AssetTypeUid))
            {
                var at = Company.AssetTypes.FirstOrDefault(x => x.uid == items.Key);
                var execution = getApiExecution(items.Count(), new ApiExecutionFields_PutAssets { AssetTypeUid = at.uid });
                execution.Method = "PUT";

                var updates = new List<AssetUpdate>();
                foreach (var asset in items)
                {
                    var newUid = Guid.NewGuid();
                    asset.UpdateAssetUid(newUid);
                    updates.Add(new AssetUpdate()
                    {
                        ExecutionItemUid = asset.AssetUid,
                        Uid = newUid,
                        Fields = asset.CustomFields
                    });
                }
                var updateResult = AssetRepository.PutAssets(updates, at, execution);

            }

            foreach (var items in toDelete.GroupBy(x => x.AssetTypeUid))
            {
                var at = Company.AssetTypes.FirstOrDefault(x => x.uid == items.Key);
                var execution = getApiExecution(items.Count(), new ApiExecutionFields_PutAssets { AssetTypeUid = at.uid });
                execution.Method = "DELETE";

                var deletes = new AssetDeletes();
                foreach (var asset in items)
                {
                    var newUid = Guid.NewGuid();
                    asset.UpdateAssetUid(newUid);
                    deletes.Add(new AssetDelete()
                    {
                        Cascade = true,
                        Uid = asset.AssetUid
                    });

                }
                var deleteResults = AssetRepository.DeleteAsset(deletes, at, execution);
            }


            Company.Query<bool>("update dbo.Asset set Process = @process where uid = @uid", new { process = JsonConvert.SerializeObject(model), uid = assetUid }).FirstOrDefault();

            var result = new { updated = toUpdate, added = toAdd, deleted = toDelete, updatedModel = model };

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result));
        }

    }
}
