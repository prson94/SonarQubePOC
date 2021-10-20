using d360.core.entities;
using d360.model;
using Microsoft.Web.Http;
using System;
using System.Web.Http;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using d360.web.Filters;
using Swashbuckle.Swagger.Annotations;
using d360.web.Models;
using System.Web.Http.Description;
using d360.model.DataAccessLayer;
using d360.core.entities.Process;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using d360.core.enums;

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
        readonly IAssetRepository AssetRepository;
        readonly IProcessRepository ProcessRepository;

        public ProcessController(CoreComponentSet set, IAssetRepository assetRepository, IProcessRepository processRepository) : base(set)
        {
            this.AssetRepository = assetRepository;
            this.ProcessRepository = processRepository;
        }

        /// <summary>
        /// Returns a list of available colors for Governance Roles
        /// </summary>
        /// <param name="assetUid">The asset uid</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("{assetUid:Guid}/governanceRoleColors"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "The list of available diagram types.", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the request was not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An error to indicate an internal server error.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetAvailableColorsForDiagramNodes(Guid assetUid)
        {
            var governanceRoleUid = SettingsRepository.GetSettingValue<Guid>(Setting.GovernanceRoleReferenceListUid);
            var results = await Company.QueryAsync<dynamic>($@"
                    drop table if exists #govRoles
                    create table #govRoles(
	                    GovRoleUid uniqueidentifier
                    )
                    insert into #govRoles 
                    select @governanceRoleUid

                    insert into #govRoles
                    select distinct GOV.uid from asset a
	                    inner join AssetType AT on At.id = a.AssetTypeID
	                    inner join IntersectType It on it.Subject = at.Object and it.SubjectID = at.ObjectID
	                    inner join Predicate P on it.PredicateID = p.ID and p.Type = 15
	                    inner join AssetType Task on Task.Object = it.Object and task.ObjectID = it.ObjectID
	                    inner join FieldType FT on FT.Object = task.object and ft.objectid = task.objectid and ft.Name ='GovernanceRole'
	                    inner join AssetType GOV on GOV.ObjectId = FT.LookupObjectId and gov.Object ='ReferenceItemType'
	                    where a.uid = @assetuid

                     select a.ObjectID, a.Object, JSON_VALUE(ACJ.ColorJson, '$.Value') as Value, adv.DisplayValue 
                                        from #govRoles gov
					                    inner join AssetType at on at.uid = gov.GovRoleUid
	                                    inner join asset a on a.AssetTypeID = at.ID
                                        inner join dbo.GetAssetDisplayValue() adv on adv.id = a.id
                                        outer apply dbo.GetAssetColorJsonByColor(A.Color) ACJ
                    ", new { governanceRoleUid, assetUid }
                     , ApiTimeout);

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
            {
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "The asset uid must be specified."));
            }

            var asset = AssetRepository.GetAssetByUID(assetUid);
            if (asset == null)
            {
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "The asset with uid specified does not exist."));
            }

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
            {
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "The asset uid must be specified."));
            }

            var asset = AssetRepository.GetAssetByUID(assetUid);
            if (asset == null)
            {
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "The asset with uid specified does not exist."));
            }

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
        /// <param name="sourceAssetUid">If uid passed, current assets process diagram will be replaced with all its fields, relationships and tags</param>
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
        public async Task<IHttpActionResult> UpdateProcessDiagram(Guid assetUid, ProcessDiagramModel model, Guid? sourceAssetUid = null)
        {
            try
            {
                Asset targetAsset;
                Asset sourceAsset = null;
                List<ProcessDiagramCopyRelationshipModel> copyRelationshipModel = null;
                List<ProcessDiagramCopyRelationshipModel> rejectedRelationsipsCopy = null;
                List<ProcessDiagramCopyMapper> pdCopyMapper = null;

                bool isDiagramReplace = false;

                bool validateFields = true;
                if (sourceAssetUid.HasValue)
                {
                    validateFields = false;
                }

                bool isModelEmpty = model.linkDataArray == null && model.linkFromPortIdProperty == null && model.linkToPortIdProperty == null && model.nodeDataArray == null;
                if (!sourceAssetUid.HasValue && isModelEmpty)
                {
                    throw new Exception("Model cannot be empty.");
                }

                if (sourceAssetUid.HasValue && !isModelEmpty)
                {
                    throw new Exception("When using copy/replace option with sourceAssetUid model must be empty.");
                }

                targetAsset = Company.Assets.FirstOrDefault(x => x.uid == assetUid);

                if (sourceAssetUid.HasValue)
                {
                    isDiagramReplace = true;
                    sourceAsset = Company.Assets.FirstOrDefault(x => x.uid == sourceAssetUid);
                    if (sourceAsset == null)
                    {
                        throw new Exception("sourceAssetUid is invalid or asset does not exist.");
                    }

                    if (sourceAsset.ID == targetAsset.ID)
                    {
                        throw new Exception("Source and target asset cannot be same.");
                    }
                    if (sourceAsset.AssetTypeID != targetAsset.AssetTypeID)
                    {
                        throw new Exception("Source and target asset types must be same.");
                    }

                    model = ProcessRepository.GetAssetsProcessDiagram(sourceAssetUid.Value);

                    copyRelationshipModel = Company.Query<ProcessDiagramCopyRelationshipModel>(@"
                                                            drop table if exists #assets
                                                            create table #assets(assetUid uniqueidentifier)

                                                            insert into #assets
	                                                            select fromuid as assetuid from processexpandeddata pxd
	                                                            where pxd.diagramassetuid = @assetuid
	                                                            union
	                                                            select touid as assetuid from processexpandeddata pxd
	                                                            where pxd.diagramassetuid = @assetuid


                                                            select ass.assetUid as keyUid, I.Id as IntersectId, 'Object' as Location, it.SubjectCardinality, it.ObjectCardinality from #assets ass
                                                                 inner join Asset a on a.uid = ass.assetuid
                                                                 inner join [Intersect] i on i.object = a.object and i.objectid = a.objectid
	                                                             inner join [IntersectType] it on i.IntersectTypeID = it.ID
                                                             union
                                                             select ass.assetUid as keyUid, I.Id as IntersectId, 'Subject' as Location, it.SubjectCardinality, it.ObjectCardinality from #assets ass
                                                                 inner join Asset a on a.uid = ass.assetuid
                                                                 inner join [Intersect] i on i.subject = a.object and i.subjectid = a.objectid
	                                                             inner join [IntersectType] it on i.IntersectTypeID = it.ID
                                                            ", new { assetUid = sourceAsset.uid }).ToList();

                    rejectedRelationsipsCopy = copyRelationshipModel.Where(x => x.ObjectCardinality == 1 || x.SubjectCardinality == 1).ToList();
                    copyRelationshipModel = copyRelationshipModel.Where(x => x.ObjectCardinality == 2 && x.SubjectCardinality == 2).ToList();

                    pdCopyMapper = new List<ProcessDiagramCopyMapper>();
                    //Invalidate all previous keys
                    foreach (var node in model.nodeDataArray)
                    {
                        var newKey = Guid.NewGuid();
                        var currentKey = node.AssetUid;
                        pdCopyMapper.Add(new ProcessDiagramCopyMapper{ oldUid = node.AssetUid, keyUid = newKey });
                        foreach (var rel in copyRelationshipModel.Where(x => x.keyUid == currentKey))
                        {
                            rel.keyUid = newKey;
                        }

                        foreach (var link in model.linkDataArray)
                        {
                            if (link.from == currentKey)
                            {
                                link.from = newKey;
                            }

                            if (link.to == currentKey)
                            {
                                link.to = newKey;
                            }
                        }

                        node["key"] = newKey.ToString();
                    }

                }




                if (!Company.HasAssetPermission(targetAsset.ID, core.enums.Permission.EditAsset))
                {
                    var err = new List<ValidationError>
                    {
                        new ValidationError { Error = "You are not authorized to edit this process diagram" }
                    };
                    return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        hasError = true,
                        errors = err
                    }))).ConfigureAwait(false);
                }

                ProcessDiagramModel existingProcess = ProcessRepository.GetAssetsProcessDiagram(assetUid);
                foreach (var item in model.linkDataArray)
                {
                    if (item.from == Guid.Empty || item.to == Guid.Empty)
                    {
                        throw new Exception("Link without from and to node detected.");
                    }
                }

                //clear label values, we need to save only uids
                foreach (var link in model.linkDataArray)
                {
                    link.label = null;
                }

                foreach (var node in model.nodeDataArray)
                {
                    if (!model.linkDataArray.Any(x => x.from == node.AssetUid || x.to == node.AssetUid))
                    {
                        return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            hasError = true,
                            errors = new List<ValidationError>
                            {
                                new ValidationError(){
                                AssetTypeUid = Guid.Empty,
                                AssetUid = Guid.Empty,
                                ErrorType = "Custom",
                                Error = "All nodes within diagram must be linked."
                                }
                            }
                        })));
                    }
                }

                var duplicates = model.nodeDataArray.GroupBy(x => x["Name"].ToLower()).Select(x => new { x.Key, Items = x }).Where(x => x.Items.Count() > 1).ToList();

                if (duplicates.Count > 0)
                {
                    List<ValidationError> err = new List<ValidationError>();
                    foreach (var item in duplicates)
                    {
                        var data = item.Items.FirstOrDefault();
                        err.Add(new ValidationError { ErrorType = "CustomUniqueName", AssetTypeUid = data.AssetTypeUid, AssetUid = data.AssetUid, Error = item.Items.Count() + " items have the same name '" + data["Name"] + "'" });
                    }
                    return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new { hasError = true, errors = err }))).ConfigureAwait(false);

                }
                List<NodeData> toAdd = new List<NodeData>();
                List<NodeData> toUpdate = new List<NodeData>();
                List<NodeData> toDelete = new List<NodeData>();

                foreach (var exNode in existingProcess.nodeDataArray)
                {
                    if (!model.nodeDataArray.Any(x => x.AssetUid == exNode.AssetUid) && exNode.IsNodeValid)
                    {
                        toDelete.Add(exNode);
                    }
                }

                foreach (var node in model.nodeDataArray)
                {                    
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


                //Check for asset type, if asset type is deleted dont copy nodes and links
                if (isDiagramReplace)
                {
                    if (toAdd.Any(n => !n.HasAssetType))
                    {
                        toAdd.ForEach(node =>
                        {
                            if (!node.HasAssetType)
                            {
                                model.linkDataArray = model.linkDataArray.Where(x => x.from != node.AssetUid && x.to != node.AssetUid).ToList();
                                model.nodeDataArray = model.nodeDataArray.Where(x => x.AssetUid != node.AssetUid).ToList();
                            }
                        });

                        toAdd = toAdd.Where(x => x.HasAssetType).ToList();
                    }
                }

                foreach (var item in toAdd.GroupBy(x => x.AssetTypeUid))
                {
                    var umItem = new UpsertModel();
                    umItem.AssetTypeUid = item.Key;
                    umItem.Assets = new List<UpsertAsset>();
                    foreach (var a in item.Select(x => x))
                    {
                        umItem.Assets.Add(new UpsertAsset
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

                var validationRes = AssetRepository.ValidateAssetUpsertModel(upsertModels, validateFields, true);
                if (validationRes.Count > 0)
                {
                    return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new { hasError = true, errors = validationRes }))).ConfigureAwait(false);
                }

                var totalCount = toAdd.Count + toDelete.Count + toUpdate.Count;
                var execution = getApiExecution(totalCount);

                if (sourceAsset != null)
                {
                    execution.Fields = JsonConvert.SerializeObject(new { SourceAssetUid = sourceAsset.uid });
                }

                validationRes = ProcessRepository.UpdateProcessDiagram(execution, model,
                    toAdd, toUpdate, toDelete,
                    targetAsset.ID, isDiagramReplace,
                    copyRelationshipModel, pdCopyMapper);

                if (validationRes.Count > 0)
                {
                    return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new { hasError = true, errors = validationRes })));
                }


                var result = new { updated = toUpdate.Count, added = toAdd.Count, deleted = toDelete.Count, warnings = rejectedRelationsipsCopy != null ? rejectedRelationsipsCopy : null };
                return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result))).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                var err = new List<ValidationError>
                {
                    new ValidationError { Error = ex.Message }
                };
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
            return await Task.FromResult<IHttpActionResult>(ResponseMessage(response)).ConfigureAwait(false);

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

            return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response))).ConfigureAwait(false);

        }

        /// <summary>
        /// Retrieves an direct link for process diagram
        /// </summary>
        /// <param name="assetUid">The asset uid</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("urlByDiagramAsset/{assetUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "Url of diagram asset", typeof(string)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetProcessDiagramUrl(Guid assetUid)
        {
            if (assetUid == null)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "The asset uid must be specified."));

            var asset = AssetRepository.GetAssetByUID(assetUid);
            if (asset == null)
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "The asset with uid specified does not exist."));

            Guid baseAssetUid = Company.Query<Guid>(@"select top 1 diagramassetuid from processexpandeddata where fromuid = @assetUid or touid = @assetUid", new { assetUid }).FirstOrDefault();
            string url = $"sidebar/visualization/browser/{baseAssetUid.ToString()}/Process/{assetUid}";
            return await Task.FromResult(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, url))).ConfigureAwait(false);

        }

        /// <summary>
        /// Returns a list of available copy options for current diagram
        /// </summary>
        /// <param name="assetUid">The asset uid</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("{assetUid:Guid}/importOptions"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "The list of available diagram types.", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the request was not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An error to indicate an internal server error.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetCopyOption(Guid assetUid)
        {

            var asset = Company.Assets.FirstOrDefault(x => x.uid == assetUid);

            var results = await Company.QueryAsync<dynamic>($@"
                    select a.uid,
	                    graph.GetPath(an.Segments, ' > ', ' / ') as assetPath,
	                    P.Path as typePath
	                    from asset a
	                        inner join AssetProcessDiagram apd on a.ID = apd.AssetID
	                        inner join graph.assetnode an on an.uid = a.uid
	                        inner join AssetType at on at.id = @assettypeid
	                        cross apply dbo.GetAssetTypeTextPathById(AT.ID, ' > ') P
                        where a.AssetTypeID = @assetTypeId and apd.Diagram is not null and a.uid <> @currentAssetuid
                        order by graph.GetPath(an.Segments, ' > ', ' / ')
                    ", new { currentAssetUid = asset.uid, assetTypeId = asset.AssetTypeID });

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results));
        }

        /// <summary>
        /// Returns a list of relationships that cannot be copied due to a cardinality
        /// </summary>
        /// <param name="targetAssetUid">The asset uid</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("ignoredCopyRelationships/{targetAssetUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"), //, "application/xml"
            SwaggerResponse(HttpStatusCode.OK, "The list of available diagram types.", typeof(ApiStatusResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that the asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "An error to indicate that you are not authorized to perform this action.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that the request was not valid.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An error to indicate an internal server error.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetIgnoredRelationships(Guid targetAssetUid)
        {
            var results = await Company.QueryAsync<dynamic>($@";with assets as (
                    select diagramassetuid as uid, FromUid as duid from processexpandeddata where diagramassetuid = @targetassetuid
                    union
                    select diagramassetuid as uid, ToUid as duid from processexpandeddata where diagramassetuid = @targetassetuid)
                    select 
                        assets.uid,
	                    utility.GetAssetDisplayValue(a.id) as 'FlowObject',
	                    utility.GetAssetDisplayValue(a2.id) as 'RelatedAsset'
                     from assets
	                    inner join asset a on a.uid = assets.duid
	                    inner join [intersect] i on i.subject = a.object and i.subjectid = a.objectid
	                    inner join intersecttype it on i.intersecttypeid = it.id
	                    inner join asset a2 on a2.Object = i.Object and a2.ObjectID = i.ObjectID
                    where it.objectcardinality = 1 or it.SubjectCardinality = 1
                    union
                    select 
	                    assets.uid,
	                    utility.GetAssetDisplayValue(a.id) as 'FlowObject',
	                    utility.GetAssetDisplayValue(a2.id) as 'RelatedAsset'
                     from assets
	                    inner join asset a on a.uid = assets.duid
	                    inner join [intersect] i on i.object = a.object and i.objectid = a.objectid
	                    inner join intersecttype it on i.intersecttypeid = it.id
	                    inner join asset a2 on a2.Object = i.subject and a2.ObjectID = i.subjectid
                    where it.objectcardinality = 1 or it.SubjectCardinality = 1
                    ", new { targetAssetUid });

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results));
        }
    }
}
