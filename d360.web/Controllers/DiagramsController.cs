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

        [HttpGet, Route("{type}/{id:int}/lineagenode")]
        public JsonNetResult GetLineageNodeDataForObject(string type, int id)
        {
            var sql = @"	select 
		                [name],
		                IconForeColor as foreColor,
		                IconBackColor as backColor,
		                [object],
		                objectId,
		                objectType,
		                objectTypeId,
		                objectTypeName
	                from cache.ObjectDetails
	                where [object] = @type and objectid = @id";

            return new JsonNetResult
            {
                Data = Company.Query<dynamic>(sql, new { type, id }).FirstOrDefault(),
                Formatting = Formatting.None
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
            var existingJson = Company.Query<string>(@"exec GetLineageV2 @type, @id", new { type = model.Focal, id = model.FocalID }).ToList();
            var json = string.Join("", existingJson);
            var existing = new LineageEditorModelV2();
            var maps = model.Nodes.Where(n => n.IsGroup && n.Category == "map").ToList();
            var transforms = model.Nodes.Where(n => n.IsGroup && n.Category == "transform").ToList();
            var objects = model.Nodes.Where(n => n.Group != null && (n.Category == "object" || n.Category == "focal")).ToList();

            List<string> errors = new List<string>();


            ////build existing lineage model so we can compare
            if (!string.IsNullOrEmpty(json))
            {
                dynamic obj = JsonConvert.DeserializeObject(json);

                if (obj.nodes != null)
                    for (int i = 0; i < obj.nodes.Count; i++)
                    {
                        var n = obj.nodes[i];
                        existing.Nodes.Add(new LineageNodeModel
                        {
                            Key = n.key,
                            Group = n.group,
                            IsGroup = n.isGroup,
                            Object = n["object"],
                            ObjectID = n.objectId,
                            Category = n.category,
                            IntersectTypeID = n.intersectTypeId == null ? 0 : n.intersectTypeId,
                            MapTypeTemplateID = n.MapTypeTempalteID
                        });
                    }

                if (obj.links != null)
                    for (int i = 0; i < obj.links.Count; i++)
                    {
                        var l = obj.links[i];
                        existing.Links.Add(new LineageLinkModel
                        {
                            From = l.from,
                            To = l.to,
                            IntersectID = l.intersectId
                        });
                    }
            }

            //add maps
            var mapMappings = new Dictionary<string, int>();
            maps.ForEach(m =>
            {
                if (m.Key.StartsWith("-"))
                {
                    var map = new Map();
                    map.MapTypeID = m.ObjectTypeID;
                    Company.Add(map);
                    Company.SaveChanges();
                    mapMappings.Add(m.Key, map.ID);
                }
            });

            //add objects
            objects.ForEach(n =>
            {
                if (n.Key.StartsWith("-"))
                {
                    int mapId = -1;
                    Map map;

                    if (n.Group.StartsWith("-"))
                    {
                        if (mapMappings.ContainsKey(n.Group))
                        {
                            mapId = mapMappings[n.Group];
                        }
                        else
                        {
                            errors.Add($"Map not found for node with key {n.Key}");
                            //exception map not found
                            return;
                        }

                    }
                    else if (n.Group.Contains("|"))
                    {
                        int.TryParse(n.Group.Split('|').Last(), out mapId);
                    }
                   
                    if (mapId > -1)
                    {
                        map = Company.GetById<Map>(mapId);
                        var intersectType = Company.IntersectTypes.Where(i => i.Subject == "MapType" && i.SubjectID == map.MapTypeID && i.Object == n.ObjectType && i.ObjectID == n.ObjectTypeID).FirstOrDefault();

                        if (intersectType != null)
                        {
                            var intersect = new Intersect();
                            intersect.IntersectTypeID = intersectType.ID;
                            intersect.Subject = "Map";
                            intersect.SubjectID = map.ID;
                            intersect.Object = n.Object;
                            intersect.ObjectID = n.ObjectID;

                            Company.Add(intersect);
                            Company.SaveChanges();
                        }
                        else
                        {
                            errors.Add($"Intersect type not found for node with key {n.Key}");
                            //exception intersecttype not found
                            return;
                        }
                    }
                    else
                    {
                        errors.Add($"Map not found for node with key {n.Key}");
                        //exception map not found
                        return;
                    }

                }
            });

            //add transforms
            var transformMappings = new Dictionary<string, int>();
            transforms.ForEach(t =>
            {
                if (t.Key.StartsWith("-"))
                {
                    var transform = new MapGroup();
                    transform.BusinessTransformation = t.BusinessTransformation;
                    transform.TechnicalTransformation = t.TechnicalTransformation;

                    Company.Add(transform);
                    Company.SaveChanges();
                    transformMappings.Add(t.Key, transform.ID);

                    //find associated children
                    var children = maps.Where(m => m.Group != null && transformMappings.ContainsKey(m.Group) && transform.ID == transformMappings[m.Group]).ToList();

                    children.ForEach(c =>
                    {
                        int mapId = -1;
                        if (c.Key.StartsWith("-"))
                        {
                            if (mapMappings.ContainsKey(c.Key))
                            {
                                mapId = mapMappings[c.Key];
                            }
                            else
                            {
                                errors.Add($"Could not find mapping for child map {c.Key}");
                                return;
                            }
                        }
                        else if (c.Key.Contains("|"))
                        {
                            int.TryParse(c.Key.Split('|').Last(), out mapId);
                        }
                        else
                        {
                            //error child key not found
                            errors.Add($"Could not find key for child map {c.Key}");
                            return;
                        }

                        if (mapId > -1)
                        {
                            var map = Company.GetById<Map>(mapId);
                            var groupItem = new MapGroupItem();
                            groupItem.MapGroupID = transform.ID;
                            groupItem.Object = "Map";
                            groupItem.ObjectID = map.ID;

                            Company.Add(groupItem);
                            Company.SaveChanges();
                        }
                        else
                        {
                            errors.Add("Map not found");
                            return;
                        }

                    });
                }
            });

            //find any extra objects and remove them
            var removingIntersectIds = new List<int>();
            var removingMapIds = new List<int>();
            var removingGroupIds = new List<int>();
            var removingGroupItems = new List<MapGroupItem>();

            existing.Nodes.Where(n => !n.Key.StartsWith("-") && (n.Category == "object" || n.Category == "focal")).ToList().ForEach(n =>
            {
                var node = model.Nodes.Where(m => m.Key == n.Key).FirstOrDefault();

                if (node != null)
                    return;

                //if we got this far the intersect needs to be removed
                //first we find the map

                int mapId = -1;
                Map map;

                if (n.Group.StartsWith("-"))
                {
                    if (mapMappings.ContainsKey(n.Group))
                    {
                        mapId = mapMappings[n.Group];
                    }
                    else
                    {
                        errors.Add($"Map not found for node with key {n.Key}");
                        //exception map not found
                        return;
                    }

                }
                else if (n.Group.Contains("|"))
                {
                    int.TryParse(n.Group.Split('|').Last(), out mapId);
                }

                if (mapId > -1)
                {
                    map = Company.GetById<Map>(mapId);
                    var intersect = Company.Intersects.Where(i => i.IntersectTypeID == n.IntersectTypeID && i.Subject == "Map" && i.SubjectID == map.ID).FirstOrDefault();

                    if (intersect != null)
                    {
                        removingIntersectIds.Add(intersect.ID);
                        return;
                    }
                    else
                    {
                        //could not find intersect
                        errors.Add($"Intersect not found for node with key {n.Key}");
                        return;
                    }
                }
                else
                {
                    //could not find map
                    errors.Add($"Map not found for node with key {n.Key}");
                    return;
                }
                    
            });

            //find any extra maps and remove them
            existing.Nodes.Where(n => !n.Key.StartsWith("-") && n.Category == "map").ToList().ForEach(m =>
            {
                var map = model.Nodes.Where(n => m.Key == n.Key).FirstOrDefault();

                if (map != null)
                    return;

                //we need to remove the map
                //first make sure there are no relationships to it

                //get the map id
                int mapId = -1;
                if (m.Key.StartsWith("-"))
                {
                    if (mapMappings.ContainsKey(m.Key))
                    {
                        mapId = mapMappings[m.Key];
                    }
                    else
                    {
                        errors.Add($"Map not found for node with key {m.Key}");
                        //exception map not found
                        return;
                    }

                }
                else if (m.Key.Contains("|"))
                {
                    int.TryParse(m.Key.Split('|').Last(), out mapId);
                }

                if (mapId > -1)
                {
                    var intersects = Company.Intersects.Where(i => i.Subject == "Map" && i.SubjectID == mapId).ToList();
                    removingIntersectIds.AddRange(intersects.Select(i => i.ID));
                    removingMapIds.Add(mapId);
                }

            });

            //find any extra transform and remove them
            existing.Nodes.Where(n => !n.Key.StartsWith("-") && n.Category == "transform").ToList().ForEach(t =>
            {
                var transform = model.Nodes.Where(n => n.Key == t.Key).FirstOrDefault();

                if (transform != null)
                {
                    //the transform does not need to be deleted but we still need to check the items
                    if (int.TryParse(transform.Key.Split('|').Last(), out int id))
                    {
                        var mapGroupItems = Company.MapGroupItems.Where(i => i.MapGroupID == id).ToList();
                        var mapGroupModelItems = model.Nodes.Where(n => n.Category == "map" && n.Group != null).ToList();
                        
                        if (mapGroupModelItems.Count < 1)
                        {
                            removingGroupIds.Add(id);
                        }

                        mapGroupItems.ForEach(m =>
                        {
                            var mapGroupItem = mapGroupModelItems.Where(i => i.Object == m.Object && i.ObjectID == m.ObjectID).FirstOrDefault();
                            if (mapGroupItem != null)
                                return;

                            removingGroupItems.Add(m);

                        });

                    }

                    return;

                }

                int transformId = -1;

                if (t.Key.StartsWith("-"))
                {
                    if (transformMappings.ContainsKey(t.Key))
                    {
                        transformId = mapMappings[t.Key];
                    }
                    else
                    {
                        errors.Add($"Transform not found for node with key {t.Key}");
                        //exception map not found
                        return;
                    }

                }
                else if (t.Key.Contains("|"))
                {
                    int.TryParse(t.Key.Split('|').Last(), out transformId);
                }

                if (transformId > -1)
                {
                    var mapGroup = Company.GetById<MapGroup>(transformId);
                    removingGroupIds.Add(mapGroup.ID);
                    var groupItems = Company.MapGroupItems.Where(i => i.MapGroupID == transformId).ToList();
                    removingGroupItems.AddRange(groupItems);
                }

            });


            Company.MapGroupItems.RemoveRange(removingGroupItems);
            Company.SaveChanges();

            removingGroupIds.ForEach(i =>
            {
                var mapGroup = Company.GetById<MapGroup>(i);
                if (mapGroup != null)
                    Company.MapGroups.Remove(mapGroup);
            });

            Company.SaveChanges();

            removingIntersectIds.ForEach(i =>
            {
                var intersect = Company.GetById<Intersect>(i);
                if (intersect != null)
                    Company.Intersects.Remove(intersect);
            });

            Company.SaveChanges();

            removingMapIds.ForEach(i =>
            {
                var map = Company.GetById<Map>(i);
                if (map != null)
                    Company.Maps.Remove(map);
            });

            Company.SaveChanges();

            model.Links.Where(l => l.IntersectID == 0).ToList().ForEach(l =>
            {
                int fromId = -1;
                int toId = -1;

                if (l.From.StartsWith("-"))
                {
                    if (mapMappings.ContainsKey(l.From))
                    {
                        fromId = mapMappings[l.From];
                    }
                    else
                    {
                        errors.Add($"Could not find source item for link {l.From}");
                        return;
                    }
                }
                else if (l.From.Contains("|") && int.TryParse(l.From.Split('|').Last(), out fromId))
                {
                    
                }
                else
                {
                    errors.Add($"Could not find source item for link {l.From}");
                    return;
                }

                if (l.To.StartsWith("-"))
                {
                    if (mapMappings.ContainsKey(l.To))
                    {
                        toId = mapMappings[l.To];
                    }
                    else
                    {
                        errors.Add($"Could not find source item for link {l.To}");
                        return;
                    }
                }
                else if (l.To.Contains("|") && int.TryParse(l.To.Split('|').Last(), out toId))
                {

                }
                else
                {
                    errors.Add($"Could not find source item for link {l.To}");
                    return;
                }


                if (fromId > -1 && toId > -1)
                {
                    var intersect = new Intersect();
                    //TODO: don't hardcode maptype 1
                    var intersectType = Company.IntersectTypes.Where(i => i.Subject == "MapType" && i.Object == "MapType" && i.SubjectID == 1 && i.ObjectID == 1).FirstOrDefault();

                    if(intersectType == null)
                    {
                        errors.Add($"Could not find a MapType to MapType intersect for ids {fromId} and {toId}");
                        return;
                    }

                    intersect.IntersectTypeID = intersectType.ID;
                    intersect.Subject = "Map";
                    intersect.Object = "Map";
                    intersect.SubjectID = fromId;
                    intersect.ObjectID = toId;

                    Company.Add(intersect);
                    Company.SaveChanges();
                }
            });

            existing.Links.Where(l => !l.From.StartsWith("-") && !l.To.StartsWith("-") && l.IntersectID > 0).ToList().ForEach(l =>
            {
                var link = model.Links.Where(i => i.IntersectID == l.IntersectID).FirstOrDefault();

                if (link != null)
                    return;

                var intersect = Company.GetById<Intersect>(l.IntersectID);

                if (intersect != null)
                {
                    Company.Intersects.Remove(intersect);
                }
            });

            Company.SaveChanges();


            return new JsonNetResult
            {
                Data = "success",
                Formatting = Formatting.None
            };
        }
        #endregion
    }
}
