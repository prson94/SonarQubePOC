using d360.core;
using System.Linq;
using System.Web.Mvc;
using d360.core.entities;
using d360.model;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.web.Controllers
{
    [RoutePrefix("diagrams"), Authorize]
    public class DiagramsController : BaseController
    {
        #region DI

        public DiagramsController(CommunityContext community, CompanyContext company) : base(community, company) { }

        #endregion

        #region Model Diagram

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

        #region Lineage Diagram Updating

        public class DiagramNode
        {
            public string ID { get; set; }
            public string Key { get; set; }
            public int ParentID { get; set; }
            public SystemObjects Type { get; set; }
            public string ObjectID { get; set; }
            public string TypeName { get; set; }
            public int IntersectMapID { get; set; }
        }

        public class DiagramLink
        {
            public string From { get; set; }
            public string To { get; set; }
            public string Text { get; set; }
            public int PredicateID { get; set; }
            public int IntersectTypeID { get; set; }
            public int IntersectTypeRoleID { get; set; }
            public DiagramNode FromNode { get; set; }
            public DiagramNode ToNode { get; set; }
        }

        public class DiagramChanges
        {
            public string TargetType { get; set; }
            public string TargetID { get; set; }
            public List<DiagramLink> AddedLinks { get; set; }
            public List<DiagramNode> DeletedNodes { get; set; }
            public List<DiagramNode> AllNodes { get; set; }
            public List<DiagramLink> AllLinks { get; set; }

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

        //public JsonNetResult SaveChanges(DiagramChanges changes)
        //{

        //    if (changes.AddedLinks == null)
        //        changes.AddedLinks = new List<DiagramLink>();
        //    if (changes.DeletedNodes == null)
        //        changes.DeletedNodes = new List<DiagramNode>();

        //    //foreach (DiagramLink l in changes.AddedLinks)
        //    //{
        //    //    l.ToNode = changes.AllNodes.Where(n => n.Key == l.To).FirstOrDefault();
        //    //    l.FromNode = changes.AllNodes.Where(n => n.Key == l.From).FirstOrDefault();

        //    //    if (l.ToNode == null || l.FromNode == null)
        //    //    {
        //    //        //TODO: error handling here
        //    //    }

        //    //    //var r = Company.Query<dynamic>("EXEC AddMapRelationship @ResourceID, @Date, @ObjectType, @ObjectID, @Classification, @IntersectRole, @Description, @SubjectType, @SubjectID, @PredicateID"
        //    //    //, new
        //    //    //{
        //    //    //    ResourceID = Company.CurrentResourceID,
        //    //    //    Date = DateTime.UtcNow,
        //    //    //    ObjectType = l.FromNode.Type.ToString(),
        //    //    //    ObjectID = l.FromNode.ID,
        //    //    //    Classification = (int?)null,
        //    //    //    IntersectRole = (int?)null,
        //    //    //    Description = (string)null,
        //    //    //    SubjectType = l.ToNode.Type.ToString(),
        //    //    //    SubjectID = l.ToNode.ID,
        //    //    //    PredicateID = l.PredicateID
        //    //    //});

        //    //}

        //    var intersects = new List<IntersectMap>();
        //    foreach (DiagramNode n in changes.DeletedNodes)
        //    {
        //        if (!Company.HasPermission(n.Type, n.IntersectMapID, Claim.Delete, ClaimObject.Relationship))
        //        {
        //            continue;
        //        }

        //        var model = Company.GetById<IntersectMap>(n.IntersectMapID);
        //        if (model != null)
        //        {
        //            Company.Delete(model);
        //        }
        //    }

        //    //if (changes.ExclusionObjects == null)
        //    //    changes.ExclusionObjects = new List<ObjectModel>();
        //    //foreach(ObjectModel d in changes.ExclusionObjects)
        //    //{
        //    //    var z = Company.Query<dynamic>("EXEC [ExcludeMapIntersect] @ObjectType, @ObjectID",
        //    //        new { ObjectType = d.ObjectType, ObjectID = d.ObjectID});
        //    //}
        //    //TODO: return something useful here
        //    return new JsonNetResult { Data = new { message = "Sources updated successfully." }, Formatting = Newtonsoft.Json.Formatting.None };
        //}

        public JsonNetResult GetPredicateInfo()
        {
            var items = Company.Query<dynamic>("select id, name from predicate");
            return new JsonNetResult { Data = items, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult UpdatePredicate(int intersectMapID, int predicateID)
        {
            var record = Company.GetById<IntersectMap>(intersectMapID);

            if (record != null)
                record.PredicateID = predicateID;

            Company.Update(record);

            return new JsonNetResult { Data = record, Formatting = Newtonsoft.Json.Formatting.None };
        }

        #endregion

        #region Lineage Diagram Data

        [HttpGet, Route("maps/{type}/{id:int}")]
        public ActionResult Map(string type, int id)
        {
            ViewBag.Type = type;
            ViewBag.ID = id;
            return View();
        }

        public class DbMapItem
        {
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

            public int Level { get; set; }

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
            public int level { get; set; }
            public bool exclude { get; set; }
            public int intersectMapId { get; set; }
            public int intersectId { get; set; }

            public override string ToString()
            {
                return level.ToString();
            }
        }

        public class JsonLinkItem
        {
            public int id { get; set; }
            public string from { get; set; }
            public string frompid { get { return "OUT"; } }
            public string to { get; set; }
            public string text { get; set; }
            public int predicateId { get; set; }
        }

        public class DiagramModel
        {
            public DiagramModel()
            {
                nodes = new List<JsonNodeItem>();
                links = new List<JsonLinkItem>();
            }

            public List<JsonNodeItem> nodes { get; set; }

            public List<JsonLinkItem> links { get; set; }
        }


        [HttpGet, Route("maps/{type}/{id:int}.json")]
        public JsonNetResult MapJson(string type, int id)
        {
            var list = Company.Query<DbMapItem>("GetLineageDiagram @type, @id", new { type, id }).ToList();

            var nodes = new List<JsonNodeItem>();
            var links = new List<JsonLinkItem>();

            list.ForEach(mapItem =>
            {
                if (!nodes.Any(i => i.key == mapItem.ObjectID))
                    nodes.Add(new JsonNodeItem { key = mapItem.ObjectID, obj = mapItem.Obj, objid = mapItem.ObjID, level = mapItem.Level, name = mapItem.Object, type = mapItem.ObjectType, back = mapItem.ObjectBackColor, fore = mapItem.ObjectForeColor, exclude = mapItem.Exclude, intersectMapId = mapItem.IntersectMapID });
                if (!nodes.Any(i => i.key == mapItem.SubjectID))
                    nodes.Add(new JsonNodeItem { key = mapItem.SubjectID, obj = mapItem.Sub, objid = mapItem.SubID, name = mapItem.Subject, type = mapItem.SubjectType, back = mapItem.SubjectBackColor, fore = mapItem.SubjectForeColor, exclude = mapItem.Exclude, intersectMapId = mapItem.IntersectMapID });
                links.Add(new JsonLinkItem { id = mapItem.IntersectMapID, from = mapItem.SubjectID, to = mapItem.ObjectID, text = mapItem.Predicate });
            });

            return new JsonNetResult {
                Data = new { nodes, links },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #endregion
    }
}
