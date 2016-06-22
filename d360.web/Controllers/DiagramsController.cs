using d360.core;
using System.Linq;
using System.Web.Mvc;
using d360.core.entities;
using d360.model;
using System.Collections.Generic;
using System.Runtime.Serialization;
using d360.core.enums;
using d360.web.Models;
using System;
using Newtonsoft.Json;

namespace d360.web.Controllers
{
    [RoutePrefix("diagrams"), Authorize]
    public class DiagramsController : BaseController
    {
        #region DI

        public DiagramsController(CommunityContext community, CompanyContext company) : base(community, company) { }

        #endregion

        #region Model Diagram

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

        #region Lineage Diagram

        public class LineageModel
        {
            public string Key { get; set; }

            public int ID { get; set; }
            public int MapID { get; set; }
            public int IntersectID { get; set; }
            public int IntersectTypeID { get; set; }
            public int IntersectRoleID { get; set; }
            public string IntersectRole { get; set; }
            public bool IsSource { get; set; }

            public string Subject { get; set; }
            public int SubjectID { get; set; }
            public string SubjectIconBackColor { get; set; }
            public string SubjectIconForeColor { get; set; }
            public string SubjectName { get; set; }

            public string Object { get; set; }
            public int ObjectID { get; set; }
            public string ObjectIconBackColor { get; set; }
            public string ObjectIconForeColor { get; set; }
            public string ObjectName { get; set; }

            public int Level { get; set; }

            public int RawSourceRuleCount { get; set; } = 0;
            public int SourceRuleCount { get; set; } = 0;
            public int RawMappingRuleCount { get; set; } = 0;
            public int LinkMappingRuleCount { get; set; } = 0;
            public int ChallengeCount { get; set; } = 0;
            public int OpenEventCount { get; set; } = 0;
            public int OpenIssueCount { get; set; } = 0;
            public int RawTransformationCount { get; set; } = 0;
            public int LinkTransformationCount { get; set; } = 0;
        }


        //void processSourceLevel(List<LineageModel> list, int id)
        //{

        //    var level = list.Single(i => i.ID == id && i.Type == "S").Level + 1;

        //    list.Where(i => i.ID == id && i.Type == "O").ToList().ForEach(i => {
        //        i.Level = level;
        //        processSourceLevel(list, i.O, i.OID, level);
        //    });
        //}
        //void processSourceLevel(List<LineageModel> list, string obj, int objID, int level)
        //{
        //    list.Where(i => i.O == obj && i.OID == objID && i.Type == "S" && i.Level == 0).ToList().ForEach(i => {
        //        i.Level = level;
        //        processSourceLevel(list, i.ID);
        //    });
        //}


        //public DiagramModel TraverseDiagram(DiagramModel model, JsonNodeItem start)
        //{
        //    var diagram = new DiagramModel();
        //    diagram.nodes.Add(start);

        //    //links to the right
        //    var links = model.links.Where(l => l.from == start.key).ToList();

        //    links.ForEach(l =>
        //    {
        //        diagram.links.Add(l);
        //        var node = model.nodes.Where(i => i.key == l.to).SingleOrDefault();
        //        if (node == null)
        //            return;

        //        var k = TraverseDiagram(model, node);
        //        diagram.nodes.AddRange(k.nodes);
        //        diagram.links.AddRange(k.links);
        //    });

        //    return diagram;
        //}

        //public DiagramModel MergeDiagram(DiagramModel model)
        //{
        //    var leadingNodes = model.nodes.Where(n => !model.links.Any(l => l.to == n.key)).ToList();
        //    var diagrams = new List<DiagramModel>();

        //    //get discrete diagrams
        //    leadingNodes.ForEach(n =>
        //    {
        //        var diagram = TraverseDiagram(model, n);
        //        diagrams.Add(diagram);

        //    });

        //    //pick the biggest
        //    var mainDiagram = diagrams.OrderByDescending(d => d.nodes.Count).FirstOrDefault();

        //    //now merge the smaller diagrams into the main one if possible
        //    foreach (DiagramModel dgm in diagrams)
        //    {
        //        if (dgm == mainDiagram)
        //            continue;

        //        var nodeList = dgm.nodes.OrderByDescending(n => n.level);

        //        foreach (JsonNodeItem n in nodeList)
        //        {

        //            var node = mainDiagram.nodes.OrderBy(k => k.level).Where(k => k.obj == n.obj && k.objid == n.objid).FirstOrDefault();
        //            if (node == null)
        //                continue;
        //            else
        //            {
        //                var leftLinks = dgm.links.Where(l => l.to == n.key);
        //                var rightLinks = dgm.links.Where(l => l.from == n.key);

        //                var nodeExists = false;

        //                if (mainDiagram.nodes.Any(k => k.key == n.key))
        //                {
        //                    //make sure we don't delete this node later
        //                    nodeExists = true;
        //                }

        //                //point affected links to mainDiagram node
        //                foreach (JsonLinkItem l in leftLinks)
        //                    l.to = node.key;
        //                foreach (JsonLinkItem l in rightLinks)
        //                    l.from = node.key;

        //                if (!nodeExists)
        //                    model.nodes.Remove(n);
        //            }
        //        }
        //    }

        //    return model;
        //}

        //void loadLinks(List<LineageModel> list, List<JsonLinkItem> links, List<int> mapIDs, List<int> processedMapIDs = null)
        //{
        //    if (processedMapIDs == null)
        //        processedMapIDs = new List<int>();

        //    mapIDs.ForEach(mapID =>
        //    {
        //        var sources = list.Where(i => i.MapID == mapID && i.IsSource);
        //        var targets = list.Where(i => i.MapID == mapID && !i.IsSource);
        //        foreach (var t in targets)
        //        {
        //            links.AddRange(
        //                sources.Select(s => new JsonLinkItem {
        //                    from = s.Key,
        //                    id = t.ID,
        //                    text = s.IntersectRole,
        //                    mappingRuleCount = t.LinkMappingRuleCount,
        //                    intersectRoleId = s.IntersectRoleID,
        //                    to = t.Key,
        //                    transformationCount = t.LinkTransformationCount
        //                })
        //            );
        //        }
        //    });
        //}


        [HttpGet, Route("{type}/{id:int}/lineage/{viewID:int}")]
        public JsonNetResult GetLineageByObject(SystemObjects type, int id, int viewID)
        {
            var list = Company.Query<LineageModel>(@"exec GetLineage @viewID, @type, @id", new { viewID, type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, id }).ToList();

            //list.Where(i => i.Level == 1).ToList().ForEach(i =>
            //{
            //    processSourceLevel(list, i.ID); //assumes type is "O"
            //});

            var model = new DiagramModel();

            list.ForEach(i =>
            {
                if (!model.nodes.Any(n => n.key == i.Key))
                {
                    model.nodes.Add(new JsonNodeItem
                    {
                        back = i.SubjectIconBackColor,
                        challengeCount = i.ChallengeCount,
                        fore = i.SubjectIconForeColor,
                        intersectId = i.IntersectID,
                        mapId = i.MapID,
                        key = i.Key,
                        level = i.Level,
                        mappingRuleCount = i.RawMappingRuleCount,
                        name = i.SubjectName,
                        obj = i.Subject,
                        objid = i.SubjectID,
                        objecttype = i.Subject,
                        objecttypeid = i.SubjectID,
                        openEventCount = i.OpenEventCount,
                        openIssueCount = i.OpenIssueCount,
                        sourceRuleCount = i.SourceRuleCount,
                        transformationCount = i.RawTransformationCount,
                        type = i.Subject
                    });
                }
            });

            var mapIDs = list.Select(i => i.MapID).Distinct().ToList();
            mapIDs.ForEach(mapID =>
            {
                var sources = list.Where(i => i.MapID == mapID && i.IsSource);
                var targets = list.Where(i => i.MapID == mapID && !i.IsSource);
                foreach (var t in targets)
                {
                    model.links.AddRange(
                        sources.Select(s => new JsonLinkItem
                        {
                            from = s.Key,
                            id = t.ID,
                            text = s.IntersectRole,
                            mappingRuleCount = t.LinkMappingRuleCount,
                            intersectRoleId = s.IntersectRoleID,
                            to = t.Key,
                            transformationCount = t.LinkTransformationCount
                        })
                    );
                }
            });

            //loadLinks(list, model.links);

            //Func<string, int, string, int> getTotalSourceRules = delegate (string obj, int objID, string currentType) {
            //    return list.Where(i => i.O == obj && i.OID == objID && i.Type == "O").Sum(i => i.RawSourceRuleCount);
            //};

            //Func<string, int, string, int> getTotalMappingRules = delegate (string obj, int objID, string currentType) {
            //    return list.Where(i => i.O == obj && i.OID == objID && i.Type == "O").Sum(i => i.RawMappingRuleCount);
            //};

            //var IDs = list.Select(i => i.ID).Distinct().ToList();
            //IDs.ForEach(m =>
            //{
            //    var s = list.Single(i => i.ID == m && i.Type == "S");
            //    var sKey = $"{s.Level}{s.O}{s.OID}";
            //    if (!model.nodes.Any(i => i.key == sKey))
            //        model.nodes.Add(new JsonNodeItem { key = sKey, level = s.Level, obj = s.O, objid = s.OID, name = s.ObjectName, type = s.TypeName, objecttype = s.ObjectType, objecttypeid = s.ObjectTypeID, back = s.BackColor, fore = s.ForeColor, intersectMapId = s.ID, intersectId = s.IntersectID, mappingRuleCount = s.RawMappingRuleCount, challengeCount = s.ChallengeCount, openEventCount = s.OpenEventCount, openIssueCount = s.OpenIssueCount }); //, sourceRuleCount = getTotal(s.O, s.OID, s.Type)

            //    var o = list.Single(i => i.ID == m && i.Type == "O");
            //    var oKey = $"{o.Level}{o.O}{o.OID}";
            //    if (!model.nodes.Any(i => i.key == oKey))
            //        model.nodes.Add(new JsonNodeItem { key = oKey, level = o.Level, obj = o.O, objid = o.OID, name = o.ObjectName, type = o.TypeName, objecttype = o.ObjectType, objecttypeid = o.ObjectTypeID, back = o.BackColor, fore = o.ForeColor, intersectMapId = o.ID, intersectId = o.IntersectID, sourceRuleCount = getTotalSourceRules(o.O, o.OID, o.Type), mappingRuleCount = o.RawMappingRuleCount, challengeCount = o.ChallengeCount, openEventCount = o.OpenEventCount, openIssueCount = o.OpenIssueCount });
            //    else
            //        model.nodes.First(i => i.key == oKey).sourceRuleCount = getTotalSourceRules(o.O, o.OID, o.Type);

            //    if (model.links.Any(i => i.from == sKey && i.to == oKey))
            //    {
            //        var existingLink = model.links.Single(i => i.from == sKey && i.to == oKey);
            //        existingLink.text += $", {s.Predicate}";
            //    }
            //    else
            //    {
            //        model.links.Add(new JsonLinkItem { id = s.ID, intersectTypeId = s.IntersectTypeID, from = sKey, to = oKey, text = s.Predicate, predicateId = s.PredicateID, mappingRuleCount = s.LinkMappingRuleCount, transformationCount = s.LinkTransformationCount });
            //    }
            //});

            //model = MergeDiagram(model);

            return new JsonNetResult
            {
                Data = new { model.nodes, model.links },
                Formatting = Formatting.None
            };
        }

        //public JsonNetResult ObjectsBySearch(SystemObjects type, int id, string search)
        //{
        //    search = '%' + search.Trim('%') + '%';
        //    var items = Company.Query<dynamic>(
        //        QueryConstants.LineageSearchQuery, 
        //        new {
        //            type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true } ,
        //            id,
        //            search
        //        }
        //    );
        //    return new JsonNetResult {
        //        Data = items,
        //        Formatting = Newtonsoft.Json.Formatting.None
        //    };
        //}

        //public JsonNetResult GetRelationshipByTypes(string type1, string type2)
        //{
        //    string t1 = type1 + '/' + type2;
        //    string t2 = type2 + '/' + type1;
        //    var items = Company.Query<dynamic>("select t.id as intersecttypeid,rr.intersecttyperoleid, rr.side1label, rr.side2label, t.name as [types] from intersecttype t left join intersecttyperolerelation rr on rr.intersecttypeid = t.id where lower(t.name) = lower(@t1) or lower(t.name) = lower(@t2); ", new { t1, t2 });
        //    return new JsonNetResult { Data = items, Formatting = Newtonsoft.Json.Formatting.None };
        //}

        //public JsonNetResult GetPredicateInfo(PredicateType type = PredicateType.Lineage)
        //{
        //    var items = Company.Query<dynamic>(QueryConstants.PredicateInfoByTypeList, new { type = type});
        //    return new JsonNetResult { Data = items, Formatting = Newtonsoft.Json.Formatting.None };
        //}

        //public JsonNetResult GetPredicateInfoByAllocation(int id)
        //{
        //    var items = Company.Query<dynamic>(
        //        QueryConstants.PredicateInfoByAllocationList, 
        //        new { id = id}
        //    );
        //    return new JsonNetResult { Data = items, Formatting = Newtonsoft.Json.Formatting.None };
        //}

        //public JsonNetResult GetPredicateInfoByTypes(string type1, string type2, int id1, int id2, PredicateType mapType)
        //{
        //    var intersectTypeID = Company.Query<int>("select IntersectTypeID from utility.RelationshipTypes where SourceObjectType = @s and SourceObjectID = @si and TargetObjectType = @t and TargetObjectID = @ti", new { s = new Dapper.DbString { IsAnsi = true, Value = type1 }, si = id1, t = new Dapper.DbString { IsAnsi = true, Value = type2 }, ti = id2 }).FirstOrDefault();

        //    var predicateTypeAssigned = false;
        //    if (intersectTypeID > 0)
        //    {
        //        predicateTypeAssigned = Company.Any<IntersectTypePredicate>(i => i.IntersectTypeID == intersectTypeID && i.PredicateType == mapType);
        //    }
        //    else
        //    {
        //        var intersectType = new IntersectType();
        //        intersectType.Nodes = new List<IntersectTypeNode>() {
        //            new IntersectTypeNode { ObjectType = type1, ObjectID = id1, Order = 1 },
        //            new IntersectTypeNode { ObjectType = type2, ObjectID = id2, Order = 2 }
        //        };
        //        Company.Add<IntersectType>(intersectType);
        //        intersectTypeID = intersectType.ID;
        //    }

        //    if (!predicateTypeAssigned)
        //    {
        //        Company.Add<IntersectTypePredicate>(new IntersectTypePredicate { IntersectTypeID = intersectTypeID, PredicateType = mapType });
        //    }

        //    var items = Company.Query<dynamic>(@"select p.id, p.name, p.type from predicate p
        //        join intersecttypepredicate t on t.predicatetype = p.type and t.intersecttypeid in 
        //        (
        //         select distinct n1.intersecttypeid from intersecttypenode n1
        //         join intersecttypenode n2 on n2.intersecttypeid = n1.intersecttypeid and n2.objectType = @type2  and n2.objectid = @id2 
        //         where n1.objectType = @type1 and n1.[order] != n2.[order] and n1.objectid = @id1
        //        )
        //        where p.type = @type",
        //        new { type1 = type1, type2 = type2, id1 = id1, id2 = id2, type = mapType});
        //    return new JsonNetResult { Data = items, Formatting = Newtonsoft.Json.Formatting.None };
        //}

        //public JsonNetResult UpdatePredicate(int intersectMapID, int predicateID)
        //{
        //    var record = Company.GetById<IntersectMap>(intersectMapID);

        //    if (record != null)
        //        record.PredicateID = predicateID;

        //    Company.Update(record);

        //    return new JsonNetResult { Data = record, Formatting = Newtonsoft.Json.Formatting.None };
        //}

        #endregion
    }
}
