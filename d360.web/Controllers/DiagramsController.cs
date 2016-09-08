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

        #endregion
    }
}
