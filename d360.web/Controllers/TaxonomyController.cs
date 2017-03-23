using System.Linq;
using System.Web.Mvc;
using d360.core.entities;
using d360.model;
using d360.web.Models.Attributes;

namespace d360.web.Controllers
{
    [RoutePrefix("internal/taxonomy"), Authorize]
    public class TaxonomyController : BaseController
    {
        #region DI

        public TaxonomyController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {
        }

        #endregion

        #region JSON

        [HttpGet, Route("ModelHierarchy"), NonNullableParameters]
        public JsonNetResult ModelHierarchy(int id)
        {
            var models = Company.Query<TaxonomyDetail>(
@"select	T.*,
			case  when DC.ItemsCount > 0 then cast(1 as bit) else cast(0 as bit) end as HasChildren		 
	from	Taxonomy T
			CROSS APPLY (
				select	count(1) as [ItemsCount]
				from	[Intersect]
				where	([Subject] = 'Taxonomy' and SubjectID = T.ID) OR ([Object] = 'Taxonomy' and ObjectID = T.ID)
				) DC
where T.TaxonomyTypeID = @id AND T.Visible = 1", new { id = id }).Select(i => new { i.HasChildren, i.ID, i.Name, i.ParentID });

            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [HttpGet, Route("ModelHierarchyDetailed"), NonNullableParameters]
        public JsonNetResult ModelHierarchyDetailed(int id)
        {
            var models = Company.Query<TaxonomyDetail>(
@"select	T.*,
			case  when DC.ItemsCount > 0 then cast(1 as bit) else cast(0 as bit) end as HasChildren		 
	from	Taxonomy T
			CROSS APPLY (
				select	count(1) as [ItemsCount]
				from	[Intersect]
				where	([Subject] = 'Taxonomy' and SubjectID = T.ID) OR ([Object] = 'Taxonomy' and ObjectID = T.ID)
				) DC
where T.TaxonomyTypeID = @id AND T.Visible = 1", new { id = id }).Select(i => new { i.HasChildren, i.ID, i.Name, i.ParentID, i.Description, i.Level });

            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        #endregion
    }
}