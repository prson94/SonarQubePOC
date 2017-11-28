using System.Linq;
using System.Web.Mvc;
using d360.core.entities;
using d360.model;
using d360.web.Models.Attributes;
using System.Web;
using d360.core;
using d360.core.enums;

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
$@"select	T.ID,
            T.DisplayValue,
            T.TextPath,
            P.SubjectID as ParentID,
            A.ID as AssetID, 
			case  when DC.ItemsCount > 0 then cast(1 as bit) else cast(0 as bit) end as HasChildren		 
	from	Taxonomy T
            inner join Asset A on A.Object = 'Taxonomy' and A.ObjectID = T.ID 
			CROSS APPLY (
				select	count(1) as [ItemsCount]
				from	[Intersect]
				where	([Subject] = 'Taxonomy' and SubjectID = T.ID) OR ([Object] = 'Taxonomy' and ObjectID = T.ID)
				) DC
		    outer apply (
					    select	I.SubjectID
					    from	[Intersect] I
                                inner join IntersectType IT on IT.ID = I.IntersectTypeID and I.Object = 'Taxonomy' and I.ObjectID = T.ID
							    inner join [Predicate] P on P.ID = IT.PredicateID and P.Type = 4
					    ) P
		    left join AssetWithoutReadPermission RP on RP.ResourceID = {Company.CurrentResourceID} and RP.AssetID = A.ID 
where T.TaxonomyTypeID = @id AND T.Visible = 1 and RP.AssetID is null
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

            var sql = $@"
select	A.ID, P.SubjectID as ParentID, A.TaxonomyTypeID, A.[Level], TD.DisplayValue,
        OA.ID as AssetID,  
        {columns} 
        case 
            when DC.ItemsCount > 0 then cast(1 as bit) 
            else cast(0 as bit) 
        end as HasChildren 
from	Taxonomy A 
        inner join Asset OA on OA.Object = 'Taxonomy' and OA.ObjectID = A.ID and A.TaxonomyTypeID = @id AND A.Visible = 1 
        left join dbo.GetAssetDisplayValue() TD on TD.ID = OA.ID
        {joins} 
        CROSS APPLY (
            		select	count(1) as [ItemsCount]
            		from	[Intersect]
            		where	([Subject] = 'Taxonomy' and SubjectID = A.ID) OR ([Object] = 'Taxonomy' and ObjectID = A.ID)
            		) DC 
		outer apply (
					select	I.SubjectID
					from	[Intersect] I
                            inner join IntersectType IT on IT.ID = I.IntersectTypeID and I.Object = 'Taxonomy' and I.ObjectID = A.ID
							inner join [Predicate] P on P.ID = IT.PredicateID and P.Type = 4
					) P
		left join AssetWithoutReadPermission RP on RP.ResourceID = {Company.CurrentResourceID} and RP.AssetID = OA.ID 
where   RP.AssetID is null 
order by A.[Level], A.DisplayValue";

            //NEW LOGIC BELOW. USE EVENTUALLY.
            //var sql = "";

            //var statements = Company.GenerateAssetTypeSql(SystemObjects.TaxonomyType, id, PredicateType.IntraTypeHierarchy, out sql, false);

            //var joins = string.Join(" ", statements.Where(i => !(!i.IsListable && i.SortOrder != 0)).Select(i => i.JoinStatement));
            //var columns = string.Join(", ", statements.Where(i => i.IsListable).OrderBy(i => i.ColumnOrder).Select(i => i.ColumnStatement));
            //if (!string.IsNullOrEmpty(columns)) columns += ", ";
            //var sorts = string.Join(", ", statements.Where(i => !(!i.IsListable && i.SortOrder != 0)).OrderBy(i => i.SortOrder).Select(i => i.SortStatement));
            //if (!string.IsNullOrEmpty(sorts)) sorts = "order by " + sorts;
            //sql = string.Format(sql, columns, joins, sorts);

            var models = Company.Query<dynamic>(sql, new { type = SystemObjects.TaxonomyType.ToString(), id, pt = PredicateType.IntraTypeHierarchy });

            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        #endregion
    }
}