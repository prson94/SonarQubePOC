using System.Linq;
using System.Web.Mvc;
using d360.core.entities;
using d360.model;
using d360.web.Models.Attributes;
using System.Web;

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
            var models = Company.Query<dynamic>(
@"select	T.ID,
            T.DisplayValue,
            T.TextPath,
            T.ParentID,
            A.ID as AssetID, 
			case  when DC.ItemsCount > 0 then cast(1 as bit) else cast(0 as bit) end as HasChildren		 
	from	Taxonomy T
            inner join Asset A on A.Object = 'Taxonomy' and A.ObjectID = T.ID 
			CROSS APPLY (
				select	count(1) as [ItemsCount]
				from	[Intersect]
				where	([Subject] = 'Taxonomy' and SubjectID = T.ID) OR ([Object] = 'Taxonomy' and ObjectID = T.ID)
				) DC
where T.TaxonomyTypeID = @id AND T.Visible = 1 
order by T.[Level]", new { id = id });

            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [HttpGet, Route("ModelHierarchyDetailed"), NonNullableParameters]
        public JsonNetResult ModelHierarchyDetailed(int id, bool stripHtml = false)
        {
            var joins = "";
            var columns = "";

            var fields = getFieldTypesByObjectType("TaxonomyType", id, true);

            // get the dynamic fields set as listable for this taxonomy
            getDynamicFieldJoinStatements(id, "Taxonomy", out joins, out columns, false, false, true, fields);

            var sql = string.Format(@"
select	T.*, 
        A.ID as AssetID,  
        {0} 
        case 
            when DC.ItemsCount > 0 then cast(1 as bit) 
            else cast(0 as bit) 
        end as HasChildren 
from	Taxonomy T 
        inner join Asset A on A.Object = 'Taxonomy' and A.ObjectID = T.ID 
        {1} 
        CROSS APPLY (
		            select	count(1) as [ItemsCount]
		            from	[Intersect]
		            where	([Subject] = 'Taxonomy' and SubjectID = T.ID) OR ([Object] = 'Taxonomy' and ObjectID = T.ID)
		            ) DC 
where T.TaxonomyTypeID = @id AND T.Visible = 1 
order by T.[Level], T.DisplayValue", columns, joins);

            var models = Company.Query<dynamic>(sql, new { id = id });

            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        #endregion
    }
}