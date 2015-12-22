using d360.core;
using System.Linq;
using System.Web.Mvc;
using d360.web.Models;
using d360.core.entities;
using d360.model;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Linq;
using System;
using Dapper;

namespace d360.web.Controllers
{
    [RoutePrefix("diagrams"), Authorize]
    public class DiagramsController : BaseController
    {
        #region DI

        public DiagramsController(CommunityContext community, CompanyContext company) : base(community, company) { }

        #endregion

        #region Diagram Tooltips

        public ContentResult DiagramRelationshipsTooltip(string type, int id)
        {
            var list = Company.Query<dynamic>(@"
select	    TargetTypeName,
		    TargetObject,
		    TargetObjectID,
		    TargetObjectName,
		    dbo.GenerateObjectUrl(TargetObject, TargetTypeID, TargetObjectID) as TargetUrl,
		    case Classification when 1 then 'Critical' else  'Normal' end as Classification
from		cache.Relationships
where		SourceObject = @type
			and SourceObjectID = @id
order by	TargetTypeName,
			TargetObjectName", new { type = type, id = id }).ToList();

            var html = @"
<div style=""max-height: 300px; overflow-y: scroll"">
<table class=""table-striped table-condensed"" style=""width:100%"">
<thead><th>Type</th><th>Name</th><th>Classification</th></thead>
<tbody>
";
            list.ForEach(i =>
            {

                html += string.Format(@"<tr>
<td>{0}</td>
<td><a href='{1}' data-context='Preview' data-type='{2}' data-id='{3}'>{4}</a></td>
<td>{5}</td>
</tr>", i.TargetTypeName, i.TargetUrl, i.TargetObject, i.TargetObjectID, i.TargetObjectName, i.Classification);
            });
            html += @"</tbody></table></div>";
            return Content(html);
        }

        #endregion

        #region Diagram Data

        #region Information Catalog Diagram

        [DataContract]
        public class InformationCatalogDiagramDataItem
        {
            [DataMember(Name = "key")]
            public int ID { get; set; }
            //[IgnoreDataMember]
            [DataMember(Name = "parent")]
            public int? ParentID { get; set; }
            [DataMember(Name = "name")]
            public string Name { get; set; }
            [DataMember(Name = "url")]
            public string Url { get; set; }
            [DataMember]
            public bool RelationshipsExist { get; set; }
            [DataMember(Name = "children")]
            public List<InformationCatalogDiagramDataItem> Children { get; set; }
        }

        public JsonNetResult InformationCatalogDiagramData(int id)
        {
            var query = Company.Query<InformationCatalogDiagramDataItem>(
@"with h as (
select		top 100 percent	
			T.ID,
			0 as ParentID,
			T.Name,
            dbo.GenerateObjectUrl('Taxonomy', T.TaxonomyTypeID, T.ID) as Url
from		Taxonomy T
where	    T.TaxonomyTypeID = @id
			and T.ParentID is null
order by	Name
union all
select		top 100 percent	
			C.ID,
			C.ParentID,
			C.Name,
            dbo.GenerateObjectUrl('Taxonomy', C.TaxonomyTypeID, C.ID) as Url
from		Taxonomy C
			inner join h on h.ID = C.ParentID
order by	C.Name
)
select	0 as ID, 
		null as ParentID,
		Name,
        dbo.GenerateObjectUrl('TaxonomyType', ID, ID) as Url,
        cast(0 as bit) as RelationshipsExist
from	TaxonomyType
where	ID = @ID
union
select	ID, 
		ParentID, 
		Name,
        Url,
        cast(R.RelationshipsExist as bit) as RelationshipsExist
from	h
        cross apply (
                    select  case 
                                when count(1) > 0 then 1
                                else 0
                            end as RelationshipsExist
                    from    IntersectNode N 
                    where   ObjectType = 'Taxonomy' and ObjectID = h.ID
                    ) R
", new { id = id }).ToList();
            //var rootModel = query.Single(i => i.ID == 0);
            //rootModel.Children = loadInformationCatalogDiagramData(rootModel, query);
            return new JsonNetResult {
                Data = query,//rootModel,
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


        #region Lineage/Environment Details Diagram

        [DataContract]
        public class LineageDiagramDataContext
        {
            [DataMember]
            public string Code { get; set; }

            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public string Lookup { get; set; }
        }

        [DataContract]
        public class LineageDiagramDataTechnicalRelationship
        {
            [DataMember]
            public string Type { get; set; }

            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public string Attribute { get; set; }

            [DataMember]
            public string Fusion { get; set; }

            [DataMember]
            public string Name { get; set; }

            [DataMember] //Not in query yet
            public string Url { get; set; }
        }

        [DataContract]
        public class LineageDiagramDataTransformation
        {
            [DataMember]
            public string Type { get; set; }

            [DataMember]
            public int ID { get; set; }

            [DataMember]
            public string Description { get; set; }
        }

        [DataContract]
        public class BaseDiagramItem
        {
            //public int IntersectID { get; set; }

            [DataMember]
            public int ID { get; set; }

            public int? ParentID { get; set; }

            [DataMember]
            public string ObjectType { get; set; }

            [DataMember]
            public int ObjectID { get; set; }

            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public string Type { get; set; }

            [DataMember]
            public string BackColor { get; set; }

            [DataMember]
            public string ForeColor { get; set; }

            [DataMember]
            public string Role { get; set; }

            [DataMember]
            public string Url { get; set; }

            public string TechnicalRelationships { get; set; }
            public string Contexts { get; set; }
            public string Transformations { get; set; }

            [DataMember(Name = "Contexts")]
            public List<LineageDiagramDataContext> ContextItems { get; set; }

            [DataMember(Name = "Relationships")]
            public List<LineageDiagramDataTechnicalRelationship> Relationships { get; set; }

            [DataMember(Name = "Transformations")]
            public List<LineageDiagramDataTransformation> TransformationItems { get; set; }
        }

        [DataContract]
        public class LineageDiagramItem : BaseDiagramItem
        {
            [DataMember]
            public List<LineageDiagramItem> children { get; set; }
        }

        [DataContract]
        public class EnvironmentDetailDiagramItem : BaseDiagramItem
        {
            [DataMember]
            public string AssigningItemType { get; set; }

            [DataMember]
            public int AssigningItemID { get; set; }

            [DataMember]
            public List<EnvironmentDetailDiagramItem> children { get; set; }
        }


        List<EnvironmentDetailDiagramItem> loadEnvironmentDetailDiagramChildren(List<EnvironmentDetailDiagramItem> items, EnvironmentDetailDiagramItem parent)
        {
            List<EnvironmentDetailDiagramItem> children = null;

            if (items.Any(i => i.ParentID == parent.ID))
            {
                children = new List<EnvironmentDetailDiagramItem>();
                foreach (var i in items.Where(i => i.ParentID == parent.ID).OrderBy(i => i.Name))
                {
                    loadLineageDiagramItem(i);

                    i.children = loadEnvironmentDetailDiagramChildren(items, i);

                    children.Add(i);
                }
            }

            return children;
        }

        List<LineageDiagramItem> loadLineageDiagramChildren(List<LineageDiagramItem> items, LineageDiagramItem parent)
        {
            List<LineageDiagramItem> children = null;

            if (items.Any(i => i.ParentID == parent.ID))
            {
                children = new List<LineageDiagramItem>();
                foreach (var i in items.Where(i => i.ParentID == parent.ID).OrderBy(i => i.Name))
                {
                    loadLineageDiagramItem(i);

                    i.children = loadLineageDiagramChildren(items, i);

                    children.Add(i);
                }
            }

            return children;
        }

        void loadLineageDiagramItem(BaseDiagramItem i)
        {
            XElement xml = null;

            if (i != null)
            {
                if (!string.IsNullOrEmpty(i.Contexts))
                {
                    xml = XElement.Parse(i.Contexts);
                    i.ContextItems = xml.Elements("context")
                        .Select(e => new LineageDiagramDataContext
                        {
                            Code = e.Attribute("code").Value,
                            Lookup = e.Attribute("lookup").Value,
                            Name = e.Attribute("name").Value
                        }).ToList();

                }

                if (!string.IsNullOrEmpty(i.TechnicalRelationships))
                {
                    xml = XElement.Parse(i.TechnicalRelationships);
                    i.Relationships = xml.Elements("relationship")
                        .Select(e => new LineageDiagramDataTechnicalRelationship
                        {
                            Attribute = e.Attribute("attribute").Value,
                            Fusion = e.Attribute("fusion").Value,
                            ID = int.Parse(e.Attribute("id").Value),
                            Name = e.Attribute("name").Value,
                            Type = e.Attribute("type").Value//,
                                                            //Url = e.Attribute("url").Value
                    }).ToList();

                }

                if (!string.IsNullOrEmpty(i.Transformations))
                {
                    xml = XElement.Parse(i.Transformations);
                    i.TransformationItems = xml.Elements("transformation")
                        .Select(e => new LineageDiagramDataTransformation
                        {
                            Description = e.Element("description").Value,
                            ID = int.Parse(e.Attribute("id").Value),
                            Type = e.Attribute("type").Value//,
                                                            //Url = e.Attribute("url").Value
                    }).ToList();
                }
            }
        }

        /// <summary>
        /// Gets the actual sources for the given relationship.
        /// </summary>
        /// <param name="id">The target IntersectID</param>
        /// <returns>JSON Data</returns>
        public JsonNetResult LineageDiagramData(int id)
        {
            var items = Company.Query<LineageDiagramItem>(
                    "EXEC GetLineageDiagramData @IntersectID",
                    new { IntersectID = id }
                ).ToList();

            LineageDiagramItem root = null;

            if (items != null)
            {
                if (items.Count > 0)
                {
                    root = items.SingleOrDefault(i => !i.ParentID.HasValue);
                    loadLineageDiagramItem(root);
                    root.children = loadLineageDiagramChildren(items, root);                
                }
            }

            if (root == null)
            {
                root = new LineageDiagramItem { Name = "No data", ID = 0 };
            }

            return new JsonNetResult { Data = root, Formatting = Newtonsoft.Json.Formatting.None };
        }

        /// <summary>
        /// Gets the ideal sources for the given relationship.
        /// </summary>
        /// <param name="id">The target IntersectID</param>
        /// <returns>JSON Data</returns>
        public JsonNetResult EnvironmentDetailsDiagramData(SystemObjects type, int id)
        {
            var items = Company.Query<EnvironmentDetailDiagramItem>(
                    "EXEC GetEnvironmentDetailsDiagramData @ObjectType, @ObjectID",
                    new { ObjectType = type.ToString(), ObjectID = id }
                ).ToList();

            var root = items.SingleOrDefault(i => !i.ParentID.HasValue);

            if (root != null)
            root.children = loadEnvironmentDetailDiagramChildren(items, root);

            return new JsonNetResult { Data = root, Formatting = Newtonsoft.Json.Formatting.None };
        }

        #endregion

        #endregion


        #region Lineage Go JS Test & Prototyping

        public class DiagramNode
        {
            public string ID { get; set; }
            public string Key { get; set; }
            public int ParentID { get; set; }
            public string ObjectType { get; set; }
            public string ObjectID { get; set; }
            public string TypeName { get; set; }
        }

        public class DiagramLink
        {
            public string From { get; set; }
            public string To { get; set; }
            public string Text { get; set; }
            public string Phrase { get; set; }
            public string PredicateName { get; set; }
            public int IntersectTypeID { get; set; }
            public int IntersectTypeRoleID { get; set; }
            public DiagramNode FromNode { get; set; }
            public DiagramNode ToNode { get; set; }
        }

        public class DiagramChanges
        {
            public List<DiagramLink> AddedLinks { get; set; }
            public List<ObjectModel> ExclusionObjects { get; set; }
            public List<DiagramNode> AllNodes { get; set; }
            public List<DiagramLink> AllLinks { get; set; }

        }

        public ActionResult LineageTest()
        {
            return View();
        }
        public ActionResult ModelTest()
        {
            return View();
        }
        public JsonNetResult GetDiagramDetails(SystemObjects type, int id)
        {
            var items = Company.Query<dynamic>(
                "EXEC GetEnvironmentDetailsDiagramData @ObjectType, @ObjectID",
                new { ObjectType = type.ToString(), ObjectID = id }
            ).ToList();

            return new JsonNetResult { Data = items, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult GetArtifactTypes()
        {
            var items = Company.Query<dynamic>("select id,name from artifacttype");
            return new JsonNetResult { Data = items, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult GetArtifact(int id, string search)
        {
            var items = Company.Query<dynamic>("select top 8 objectid as id, c.name, iconbackcolor as backColor, iconforecolor as foreColor, c.objecttypename as typeName, c.url, c.object as objectType from cache.objectdetails c join artifact a on a.artifacttypeid = @id and c.objectid = a.id where lower(c.name) like lower('%' + @search + '%') ", new { id, search });
            return new JsonNetResult { Data = items, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult GetRelationshipByTypes(string type1, string type2)
        {
            string t1 = type1 + '/' + type2;
            string t2 = type2 + '/' + type1;
            var items = Company.Query<dynamic>("select t.id as intersecttypeid,rr.intersecttyperoleid, rr.side1label, rr.side2label, t.name as [types] from intersecttype t left join intersecttyperolerelation rr on rr.intersecttypeid = t.id where lower(t.name) = lower(@t1) or lower(t.name) = lower(@t2); ", new { t1, t2 });

            return new JsonNetResult { Data = items, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult SaveChanges(DiagramChanges changes)
        {

            if (changes.AddedLinks == null)
                changes.AddedLinks = new List<DiagramLink>();
            foreach(DiagramLink l in changes.AddedLinks)
            {
                l.ToNode = changes.AllNodes.Where(n => n.Key == l.To).FirstOrDefault();
                l.FromNode = changes.AllNodes.Where(n => n.Key == l.From).FirstOrDefault();

                if (l.ToNode == null || l.FromNode == null)
                {
                    //TODO: error handling here
                }

                var r = Company.Query<dynamic>("EXEC AddMapRelationship @ResourceID, @Date, @ObjectType, @ObjectID, @Classification, @IntersectRole, @Description, @SubjectType, @SubjectID, @PredicateName, @PredicatePhrase"
                , new
                {
                    ResourceID = Company.CurrentResourceID,
                    Date = DateTime.UtcNow,
                    ObjectType = l.FromNode.ObjectType,
                    ObjectID = l.FromNode.ID,
                    Classification = (int?)null,
                    IntersectRole = (int?)null,
                    Description = (string)null,
                    SubjectType = l.ToNode.ObjectType,
                    SubjectID = l.ToNode.ID,
                    PredicateName = l.PredicateName,
                    PredicatePhrase = l.Phrase
                });

            }

            if (changes.ExclusionObjects == null)
                changes.ExclusionObjects = new List<ObjectModel>();
            foreach(ObjectModel d in changes.ExclusionObjects)
            {
                var z = Company.Query<dynamic>("EXEC [ExcludeMapIntersect] @ObjectType, @ObjectID",
                    new { ObjectType = d.ObjectType, ObjectID = d.ObjectID});
            }
            //TODO: return something useful here
            return null;
        }

        public JsonNetResult GetPredicateInfo()
        {
            var items = Company.Query<dynamic>("select pp.id, pp.predicateId, p.name, pp.phrase, cast(pp.ID as varchar(100)) + '_' + cast(p.ID as varchar(100)) as value, pp.Phrase + ' (' + p.Name + ') ' as displayName from predicatephrase pp join predicate p on p.id = pp.predicateid");
            return new JsonNetResult { Data = items, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult GetExclusionsByMapObject(SystemObjects type, int id)
        {
            var ids = Company.Query<int>("EXEC FindExcludeMapIntersect @ObjectType, @ObjectID"
                ,new { ObjectType = type.ToString(), ObjectID = id });
            return new JsonNetResult { Data = ids, Formatting = Newtonsoft.Json.Formatting.None };
        }
        #endregion

        #region Map Testing

        [HttpGet, Route("maps/{type}/{id:int}")]
        public ActionResult Map(string type, int id)
        {
            ViewBag.Type = type;
            ViewBag.ID = id;
            return View();
        }

        public class DbMapItem
        {
            public int ID { get; set; }
            public int MapID { get; set; }

            public string Sub { get; set; }
            public int SubID { get; set; }
            public string SubjectID { get; set; }
            public string Subject { get; set; }
            public string SubjectType { get; set; }
            public string SubjectBackColor { get; set; }
            public string SubjectForeColor { get; set; }

            public string Obj { get; set; }
            public int ObjID { get; set; }
            public string ObjectID { get; set; }
            public string Object { get; set; }
            public string ObjectType { get; set; }
            public string ObjectBackColor { get; set; }
            public string ObjectForeColor { get; set; }

            public string Predicate { get; set; }
            public bool Exclude { get; set; }
            public int IntersectMapID { get; set; }
        }

        public class JsonNodeItem
        {
            public string key { get; set; }
            public string obj { get; set; }
            public int objid { get; set; }
            public string type { get; set; }
            public string name { get; set; }
            public string back { get; set; }
            public string fore { get; set; }
            public bool exclude { get; set; }
            public int intersectMapId { get; set; }
        }

        public class JsonLinkItem
        {
            public string from { get; set; }
            public string frompid { get { return "OUT"; } }
            public string to { get; set; }
            public string text { get; set; }
        }

        [HttpGet, Route("maps/{type}/{id:int}.json")]
        public JsonNetResult MapJson(string type, int id)
        {
            //var list = Company.Query<DbMapItem>("GetMapDiagram @mapID", new { mapID = id }).ToList();
            var list = Company.Query<DbMapItem>("GetLineageDiagram @type, @id", new { type, id }).ToList();

            var nodes = new List<JsonNodeItem>();
            var links = new List<JsonLinkItem>();
            var mapId = (list.Count() == 0 ? 0 : list.First().MapID);

            list.ForEach(mapItem =>
            {
                if (!nodes.Any(i => i.key == mapItem.ObjectID))
                    nodes.Add(new JsonNodeItem { key = mapItem.ObjectID, obj = mapItem.Obj, objid = mapItem.ObjID, name = mapItem.Object, type = mapItem.ObjectType, back = mapItem.ObjectBackColor, fore = mapItem.ObjectForeColor, exclude = mapItem.Exclude, intersectMapId = mapItem.IntersectMapID });
                if (!nodes.Any(i => i.key == mapItem.SubjectID))
                    nodes.Add(new JsonNodeItem { key = mapItem.SubjectID, obj = mapItem.Sub, objid = mapItem.SubID, name = mapItem.Subject, type = mapItem.SubjectType, back = mapItem.SubjectBackColor, fore = mapItem.SubjectForeColor, exclude = mapItem.Exclude, intersectMapId = mapItem.IntersectMapID });
                links.Add(new JsonLinkItem { from = mapItem.SubjectID, to = mapItem.ObjectID, text = mapItem.Predicate });
            });

            return new JsonNetResult {
                Data = new { nodes, links, mapId },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #endregion
    }
}
