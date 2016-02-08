using d360.core;
using System.Linq;
using System.Web.Mvc;
using d360.core.entities;
using d360.model;
using System.Collections.Generic;
using System.Runtime.Serialization;
using d360.core.enums;
using d360.web.Models;

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

        public JsonNetResult GetArtifact(int id, string search)
        {
            var items = Company.Query<dynamic>(@"
select  top 8 
        objectid as id, 
        c.name, 
        iconbackcolor as backColor, 
        iconforecolor as foreColor, 
        c.objecttypename as typeName, 
        c.url, 
        c.object as objectType 
from    cache.objectdetails c 
        inner join artifact a on a.artifacttypeid = @id and c.object = 'Artifact' and c.objectid = a.id 
where lower(c.name) like lower('%' + @search + '%') ", new { id, search });
            return new JsonNetResult { Data = items, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult GetRelationshipByTypes(string type1, string type2)
        {
            string t1 = type1 + '/' + type2;
            string t2 = type2 + '/' + type1;
            var items = Company.Query<dynamic>("select t.id as intersecttypeid,rr.intersecttyperoleid, rr.side1label, rr.side2label, t.name as [types] from intersecttype t left join intersecttyperolerelation rr on rr.intersecttypeid = t.id where lower(t.name) = lower(@t1) or lower(t.name) = lower(@t2); ", new { t1, t2 });

            return new JsonNetResult { Data = items, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult GetPredicateInfo(MapType type = MapType.Lineage)
        {
            var items = Company.Query<dynamic>("select id, name from predicate where type = @type", new { type = type});
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
    }
}
