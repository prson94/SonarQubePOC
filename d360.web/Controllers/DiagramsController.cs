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

        #region Impact Analysis Diagram

        public JsonNetResult ImpactAnalysis(SystemObjects type, int id)
        {
            var sql = @"
	declare @links table ([from] varchar(250), [to] varchar(250), [text] varchar(50), intersectid int)
	declare @nodes table ([key] varchar(250), obj varchar(50), [objid] int, typeName nvarchar(250), name nvarchar(500), back varchar(7), fore varchar(7), [predicate] nvarchar(250), intersectid int)
	
	insert into @nodes
		select	D.Object + cast(D.ObjectID as varchar),
				D.Object,
				D.ObjectID,
				D.ObjectTypeName,
				D.TextPath,
				D.IconBackColor,
				D.IconForeColor,
				case 
					when I.Subject = @type and I.SubjectID = @id then coalesce(P.Name, 'uses')
					else coalesce(P.Inverse, 'used in')
				end as [Predicate],
				I.ID
		from	[Intersect] I
				inner join cache.ObjectDetails D on 
									D.Object = case 
												when I.Subject = @type and I.SubjectID = @id then I.Object
												else I.Subject
											   end 
									and
									D.ObjectID = case 
												when I.Subject = @type and I.SubjectID = @id then I.ObjectID
												else I.SubjectID
											   end
				inner join IntersectType T on T.ID = I.IntersectTypeID
				left join [Predicate] P on P.ID = T.PredicateID
		where	( 
					(I.Subject = @type and I.SubjectID = @id) OR 
					(I.Object = @type and I.ObjectID = @id)  
				)
	
	insert into @links
		select	@type + cast(@id as varchar),
				[key],
				[predicate],
				[intersectid]
		from	@nodes


	insert into @nodes
		select	D.Object + cast(D.ObjectID as varchar),
				D.Object,
				D.ObjectID,
				D.ObjectTypeName,
				D.TextPath,
				D.IconBackColor,
				D.IconForeColor,
				null,
				null
		from	cache.ObjectDetails D
		where	Object = @type and ObjectID = @id

	select	(
			select * from @links for json path			
			) as 'links',
			(
			select * from @nodes for json path			
			) as 'nodes'
	for json path, WITHOUT_ARRAY_WRAPPER";

            var list = Company.Query<string>(sql, new {
                type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true },
                id
            });

            var json = string.Join("", list);
            var obj = (string.IsNullOrEmpty(json)) ? new JObject() : JObject.Parse(json);

            return new JsonNetResult
            {
                Data = obj,
                Formatting = Formatting.None
            };
        }

        #endregion

        #region Lineage Diagram

        [HttpGet, Route("{type}/{id:int}/lineage/{view:int}")]
        public JsonNetResult GetLineageByObject(SystemObjects type, int id, int view)
        {
            var list = Company.Query<string>(@"exec GetLineage @type, @id, @view", 
                new {
                    type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true },
                    id,
                    view
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

        #region Old stuff to remove once we switch to new lineage editor

        public JsonNetResult ObjectsBySearch(SystemObjects type, int id, string search)
        {
            search = '%' + search.Trim('%') + '%';
            var items = Company.Query<dynamic>(
                QueryConstants.LineageSearchQuery,
                new
                {
                    type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true },
                    id,
                    search
                }
            );
            return new JsonNetResult
            {
                Data = items,
                Formatting = Formatting.None
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
            var items = Company.Query<dynamic>(QueryConstants.PredicateInfoByTypeList, new { type = type });
            return new JsonNetResult { Data = items, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult GetPredicateInfoByAllocation(int id)
        {
            var items = Company.Query<dynamic>(
                QueryConstants.PredicateInfoByAllocationList,
                new { id = id }
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
                new { type1 = type1, type2 = type2, id1 = id1, id2 = id2, type = mapType });
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

        #endregion
    }
}
