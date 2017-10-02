using d360.core;
using System.Linq;
using System.Web.Mvc;
using d360.core.entities;
using d360.model;
using System.Collections.Generic;
using d360.core.enums;
using d360.web.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data.Entity.Design.PluralizationServices;
using d360.web.Models.Attributes;
using Dapper;

namespace d360.web.Controllers
{
    [RoutePrefix("diagrams"), Authorize]
    public class DiagramsController : BaseController
    {
        #region DI

        public DiagramsController(CommunityContext community, CompanyContext company) : base(community, company) { }

        #endregion

        #region Model Diagram

        [Route("{id:int}/InformationCatalogDiagramData")]
        public JsonNetResult InformationCatalogDiagramData(int id)
        {
            return new JsonNetResult {
                Data = Company.Query<InformationCatalogDiagramDataItem>(QueryConstants.InformationCatalogDiagramData, new { id = id }).ToList(),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        List<InformationCatalogDiagramDataItem> loadInformationCatalogDiagramData(InformationCatalogDiagramDataItem model, List<InformationCatalogDiagramDataItem> rawItems)
        {
            if (rawItems.Any(i => (model != null) ? i.ParentID == model.ID : !i.ParentID.HasValue))
            {
                var list = new List<InformationCatalogDiagramDataItem>();
                foreach (var c in rawItems.Where(i => (model != null) ? i.ParentID == model.ID : !i.ParentID.HasValue).OrderBy(i => i.Name))
                {
                    c.Children = loadInformationCatalogDiagramData(c, rawItems);
                    list.Add(c);
                }
                return list;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Impact Analysis Diagram

        [Route("{type}/{id:int}/ImpactAnalysis")]
        public JsonNetResult ImpactAnalysis(SystemObjects type, int id)
        {

            var list = Company.Query<string>(QueryConstants.ImpactAnalysisDiagram, new {
                type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true },
                id
            });
            var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);

            var json = string.Join("", list);
            //var obj = (string.IsNullOrEmpty(json)) ? new JObject() : JObject.Parse(json);
            dynamic obj = JsonConvert.DeserializeObject(string.IsNullOrEmpty(json) ? "{}" : json);

            if (obj != null && obj.nodes != null)
                foreach(var node in obj.nodes)
                {
                    try
                    {
                        node.typeNamePlural.Value = pluralize.IsPlural(node.typeNamePlural.Value) ? node.typeNamePlural.Value : pluralize.Pluralize(node.typeNamePlural.Value);
                    }
                    catch { }
                }

            return new JsonNetResult
            {
                Data = obj,
                Formatting = Formatting.None
            };
        }

        [Route("{type}/{id:int}/ImpactAnalysisFusion")]
        public JsonNetResult ImpactAnalysisFusion(SystemObjects type, int id)
        {
            var list = Company.Query<string>(QueryConstants.ImpactAnalysisDiagramFusion, new
            {
                type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true },
                id
            });
            var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);

            var json = string.Join("", list);
            dynamic obj = JsonConvert.DeserializeObject(string.IsNullOrEmpty(json) ? "{}" : json);

            if (obj != null && obj.nodes != null)
                foreach (var node in obj.nodes)
                {
                    try
                    {
                        node.typeNamePlural.Value = pluralize.IsPlural(node.typeNamePlural.Value) ? node.typeNamePlural.Value : pluralize.Pluralize(node.typeNamePlural.Value);
                    }
                    catch { }
                }

            return new JsonNetResult
            {
                Data = obj,
                Formatting = Formatting.None
            };
        }
        #endregion

        #region Lineage Diagram

        [HttpGet, Route("{type}/{id:int}/lineage/{view:int}/{usageOnly:bool}")]
        public JsonNetResult GetLineageByObject(SystemObjects type, int id, int view, bool usageOnly)
        {
            var list = Company.Query<string>(@"exec GetLineage @type, @id, @view, @usageOnly", 
                new {
                    type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true },
                    id,
                    view,
                    usageOnly
                }
            ).ToList();

            var json = string.Join("", list);
            var obj = (string.IsNullOrEmpty(json)) ? new JObject() : JObject.Parse(json);

            return new JsonNetResult
            {
                Data = obj,
                Formatting = Formatting.None
            };
        }

        [HttpPost, Route("{type}/{id:int}/lineagepreview/{view:int}")]
        public JsonNetResult GetLineagePreview(SystemObjects type, int id, int view, LineagePreviewModel model)
        {

            LineageEditorModel businessModel = model.BusinessModel;
            LineageEditorTechnicalModel technicalModel = model.TechnicalModel;

            var parameters = new DynamicParameters();
            var dtBusiness = new System.Data.DataTable();
            var dtTechnical = new System.Data.DataTable();

            parameters.Add("type", type.ToString());
            parameters.Add("id", id);
            parameters.Add("usageOnly", false);
            parameters.Add("view", view);

            #region DT Columns

            dtBusiness.Columns.Add(new System.Data.DataColumn("ID"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("SourceIntersectID"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("SourceSubject"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("SourceSubjectID"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("SourceObject"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("SourceObjectID"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("TargetIntersectID"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("TargetSubject"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("TargetSubjectID"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("TargetObject"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("TargetObjectID"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("Deleting"));
            dtBusiness.Columns.Add(new System.Data.DataColumn("Adding"));

            dtTechnical.Columns.Add(new System.Data.DataColumn("ID"));
            dtTechnical.Columns.Add(new System.Data.DataColumn("MapItemID"));
            dtTechnical.Columns.Add(new System.Data.DataColumn("SourceFusionAttributeID"));
            dtTechnical.Columns.Add(new System.Data.DataColumn("TargetFusionAttributeID"));
            dtTechnical.Columns.Add(new System.Data.DataColumn("Deleting"));
            dtTechnical.Columns.Add(new System.Data.DataColumn("Adding"));

            #endregion
            if (businessModel != null)
            {
                if (businessModel.Adds != null)
                    foreach (var row in businessModel.Adds)
                        dtBusiness.Rows.Add(row.ID,
                            row.SourceIntersectID,
                            row.SourceSubject,
                            row.SourceSubjectID,
                            row.SourceObject,
                            row.SourceObjectID,
                            row.TargetIntersectID,
                            row.TargetSubject,
                            row.TargetSubjectID,
                            row.TargetObject,
                            row.TargetObjectID,
                            false,
                            true);

                if (businessModel.Deletes != null)
                    foreach (var row in businessModel.Deletes)
                        dtBusiness.Rows.Add(row.ID,
                            row.SourceIntersectID,
                            row.SourceSubject,
                            row.SourceSubjectID,
                            row.SourceObject,
                            row.SourceObjectID,
                            row.TargetIntersectID,
                            row.TargetSubject,
                            row.TargetSubjectID,
                            row.TargetObject,
                            row.TargetObjectID,
                            true,
                            false);
            }

            if (technicalModel != null)
            {
                if (technicalModel.Adds != null)
                    foreach (var row in technicalModel.Adds)
                        dtTechnical.Rows.Add(row.ID, row.MapItemID, row.SourceFusionAttributeID, row.TargetFusionAttributeID, false, true);

                if (technicalModel.Deletes != null)
                    foreach (var row in technicalModel.Deletes)
                        dtTechnical.Rows.Add(row.ID, row.MapItemID, row.SourceFusionAttributeID, row.TargetFusionAttributeID, true, false);
            }


            dtBusiness.SetTypeName("LineageTable");
            parameters.Add("rows", dtBusiness);

            dtTechnical.SetTypeName("LineageTechnicalTable");
            parameters.Add("technicalRows", dtTechnical);

            var list = Company.Query<string>("exec GetLineage @type, @id, @view, @usageOnly, @rows, @technicalRows", parameters);

            var json = string.Join("", list);
            var obj = (string.IsNullOrEmpty(json)) ? new JObject() : JObject.Parse(json);

            return new JsonNetResult
            {
                Data = obj,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }


        [HttpGet, Route("lineage/{type}/{id:int}")]
        public JsonNetResult GetLineageByObjectV2(string type, int id)
        {
            var list = Company.Query<string>(@"exec GetLineageV2 @type, @id",
                new
                {
                    type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true },
                    id
                }
            ).ToList();

            var json = string.Join("", list);
            var obj = (string.IsNullOrEmpty(json)) ? new JObject() : JObject.Parse(json);

            return new JsonNetResult
            {
                Data = obj,
                Formatting = Formatting.None
            };
        }

        [HttpPost, Route("lineage/save")]
        public JsonNetResult PostLineage(LineageEditorModelV2 model)
        {
            var nodeMappings = new Dictionary<string, int>();
            //var existing = Company.Query<string>(@"exec GetLineageV2 @type, @id", new { type = model.Focal, id = model.FocalID }).ToList();
            //var json = string.Join("", existing);
            //if (!string.IsNullOrEmpty(json))
            //{
            //    dynamic obj = JsonConvert.DeserializeObject(json);
                
            //    for(int i = 0; i < obj.nodes.Count; i++)
            //    {
            //        var existingNode = obj.nodes[i];
            //        var modelNode = model.Nodes.Where(n => n.Group == existingNode.group && n.Object == existingNode["object"] && n.ObjectID == existingNode.objectId).FirstOrDefault();
            //        if (modelNode != null)
            //        {
            //            if (modelNode.Key.StartsWith("-"))
            //                nodeMappings.Add(modelNode.Key, existingNode.key);

            //            model.Nodes.Remove(modelNode);
            //            existingNode.category = "remove";
            //        }
            //    }
            //}

            

            //var obj = (string.IsNullOrEmpty(json)) ? new JObject() : JObject.Parse(json);

            //create new maps
            var maps = model.Nodes.Where(n => n.IsGroup && n.Category == "map" && n.Key.StartsWith("-")).ToList();
            var mapMappings = new Dictionary<string, int>();

            maps.ForEach(m =>
            {
                var map = new Map
                {
                    MapTypeID = 1
                };

                Company.Maps.Add(map);
                Company.SaveChanges();

                //var node = model.Nodes.Find(n => n.Key == m.Key);
                //node.Key = "Map|" + map.ID.ToString();
                mapMappings.Add(m.Key, map.ID);
            });

            //create new intersects to nodes
            var nodes = model.Nodes.Where(n => (n.Category == "focal" || n.Category == "object") && n.Key.StartsWith("-")).ToList();
            //var nodeMappings = new Dictionary<string, int>();

            nodes.ForEach(n =>
            {
                var intersectType = Company.IntersectTypes.Where(i => i.Subject == "MapType" && i.SubjectID == 1 && i.Object == n.ObjectType && i.ObjectID == n.ObjectTypeID).FirstOrDefault();

                if (intersectType != null)
                {
                    var intersect = new Intersect
                    {
                        IntersectTypeID = intersectType.ID,
                        Subject = "Map",
                        SubjectID = mapMappings[n.Group],
                        Object = n.Object,
                        ObjectID = n.ObjectID,
                        CreatedBy = Company.CurrentResourceID,
                        Deleted = false
                    };

                    Company.Intersects.Add(intersect);
                    Company.SaveChanges();
                }
            });


            //create new intersects between maps
            var links = model.Links.Where(l => l.IntersectID == 0).ToList();

            links.ForEach(l =>
            {
                if (l.From.StartsWith("-"))
                    l.From = mapMappings[l.From].ToString();
                else
                    l.From = l.From.Split('|').Last();

                if (l.To.StartsWith("-"))
                    l.To = mapMappings[l.To].ToString();
                else
                    l.To = l.To.Split('|').Last();

                var intersectType = Company.IntersectTypes.Where(i => i.Subject == "MapType" && i.SubjectID == 1 && i.Object == "MapType" && i.ObjectID == 1).FirstOrDefault();
                int from = -1, to = -1;

                int.TryParse(l.From, out from);
                int.TryParse(l.To, out to);

                if (intersectType != null && from > -1 && to > -1)
                {
                    var intersect = new Intersect
                    {
                        IntersectTypeID = intersectType.ID,
                        Subject = "Map",
                        SubjectID = from,
                        Object = "Map",
                        ObjectID = to,
                        CreatedBy = Company.CurrentResourceID,
                        Deleted = false
                    };

                    Company.Intersects.Add(intersect);
                    Company.SaveChanges();
                }
            });

            return new JsonNetResult
            {
                Data = "success",
                Formatting = Formatting.None
            };
        }
        #endregion
    }
}
