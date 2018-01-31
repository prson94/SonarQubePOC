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
            var models = Company.Query<dynamic>($@"
select	A.ObjectID as ID,
        A.DisplayValue,
        A.DisplayValue as TextPath,
        P.SubjectID as ParentID,
        A.ID as AssetID
from	AssetDetail A
		outer apply (
					select	I.SubjectID
					from	[Intersect] I
                            inner join IntersectType IT on IT.ID = I.IntersectTypeID and I.Object = A.Object and I.ObjectID = A.ObjectID
							inner join [Predicate] P on P.ID = IT.PredicateID and P.Type = 4
					) P
where   A.Type = 'TaxonomyType' and A.TypeID = @id AND A.[State] = 1", new { id });

            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [HttpGet, Route("ModelHierarchyDetailed"), NonNullableParameters]
        public JsonNetResult ModelHierarchyDetailed(int id, bool stripHtml = false)
        {
            var joins = "";
            var columns = "";

            var fields = getFieldTypesByObjectType("TaxonomyType", id, true);

            // get the dynamic fields set as listable for this taxonomy
            getDynamicFieldJoinStatements(id, "Taxonomy", out joins, out columns, false, false, true, fields, "A.ObjectID");

            var sql = $@"
select	A.ObjectID as ID, P.SubjectID as ParentID, A.TypeID as TaxonomyTypeID, A.DisplayValue,
        A.ID as AssetID,  
        {columns} 
        case 
            when DC.ItemsCount > 0 then cast(1 as bit) 
            else cast(0 as bit) 
        end as HasChildren,
        L.Level 
from	AssetDetail A
        cross apply dbo.GetAssetLevelById(A.ID) L
        {joins} 
        CROSS APPLY (
            		select	count(1) as [ItemsCount]
            		from	[Intersect]
            		where	([Subject] = A.Object and SubjectID = A.ObjectID) OR ([Object] = A.Object and ObjectID = A.ObjectID)
            		) DC 
		outer apply (
					select	I.SubjectID
					from	[Intersect] I
                            inner join IntersectType IT on IT.ID = I.IntersectTypeID and I.Object = A.Object and I.ObjectID = A.ObjectID
							inner join [Predicate] P on P.ID = IT.PredicateID and P.Type = 4
					) P
where   A.Type = 'TaxonomyType' and A.TypeID = @id AND A.[State] = 1 and not exists (select 1 from AssetWithoutReadPermission RP where RP.ResourceID = {Company.CurrentResourceID} and RP.AssetID = A.ID)";

            var models = Company.Query<dynamic>(sql, new { type = SystemObjects.TaxonomyType.ToString(), id, pt = PredicateType.IntraTypeHierarchy });

            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        #endregion
    }
}