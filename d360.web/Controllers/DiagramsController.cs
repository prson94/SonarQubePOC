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

        #region Lineage Diagram Updating

        public JsonNetResult ObjectsBySearch(SystemObjects type, int id, string search)
        {
            search = '%' + search.Trim('%') + '%';
            var items = Company.Query<dynamic>(
                QueryConstants.LineageSearchQuery, 
                new {
                    type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true } ,
                    id,
                    search
                }
            );
            return new JsonNetResult {
                Data = items,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        public JsonNetResult GetRelationshipByTypes(string type1, string type2)
        {
            string t1 = type1 + '/' + type2;
            string t2 = type2 + '/' + type1;
            var items = Company.Query<dynamic>("select t.id as intersecttypeid,rr.intersecttyperoleid, rr.side1label, rr.side2label, t.name as [types] from intersecttype t left join intersecttyperolerelation rr on rr.intersecttypeid = t.id where lower(t.name) = lower(@t1) or lower(t.name) = lower(@t2); ", new { t1, t2 });
            return new JsonNetResult { Data = items, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult GetPredicateInfo(PredicateType type = PredicateType.Lineage)
        {
            var items = Company.Query<dynamic>(QueryConstants.PredicateInfoByTypeList, new { type = type});
            return new JsonNetResult { Data = items, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult GetPredicateInfoByAllocation(int id)
        {
            var items = Company.Query<dynamic>(
                QueryConstants.PredicateInfoByAllocationList, 
                new { id = id}
            );
            return new JsonNetResult { Data = items, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult GetPredicateInfoByTypes(string type1, string type2, int id1, int id2, PredicateType mapType)
        {
            var intersectTypeID = Company.Query<int>("select IntersectTypeID from utility.RelationshipTypes where SourceObjectType = @s and SourceObjectID = @si and TargetObjectType = @t and TargetObjectID = @ti", new { s = new Dapper.DbString { IsAnsi = true, Value = type1 }, si = id1, t = new Dapper.DbString { IsAnsi = true, Value = type2 }, ti = id2 }).FirstOrDefault();

            var predicateTypeAssigned = false;
            if (intersectTypeID > 0)
            {
                predicateTypeAssigned = Company.Any<IntersectTypePredicate>(i => i.IntersectTypeID == intersectTypeID && i.PredicateType == mapType);
            }
            else
            {
                var intersectType = new IntersectType();
                intersectType.Nodes = new List<IntersectTypeNode>() {
                    new IntersectTypeNode { ObjectType = type1, ObjectID = id1, Order = 1 },
                    new IntersectTypeNode { ObjectType = type2, ObjectID = id2, Order = 2 }
                };
                Company.Add<IntersectType>(intersectType);
                intersectTypeID = intersectType.ID;
            }

            if (!predicateTypeAssigned)
            {
                Company.Add<IntersectTypePredicate>(new IntersectTypePredicate { IntersectTypeID = intersectTypeID, PredicateType = mapType });
            }

            var items = Company.Query<dynamic>(@"select p.id, p.name, p.type from predicate p
                join intersecttypepredicate t on t.predicatetype = p.type and t.intersecttypeid in 
                (
	                select distinct n1.intersecttypeid from intersecttypenode n1
	                join intersecttypenode n2 on n2.intersecttypeid = n1.intersecttypeid and n2.objectType = @type2  and n2.objectid = @id2 
	                where n1.objectType = @type1 and n1.[order] != n2.[order] and n1.objectid = @id1
                )
                where p.type = @type",
                new { type1 = type1, type2 = type2, id1 = id1, id2 = id2, type = mapType});
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
