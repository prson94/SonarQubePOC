using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.model;
using d360.web.Models;
using d360.web.Models.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpreadsheetLight;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    [RoutePrefix("relations"), Authorize]
    public class RelationsController : BaseController
    {
        #region DI

        public RelationsController(ICommunityContext community, ICompanyContext company)
            : base(community, company)
        { }

        #endregion
                
        #region Json

        [HttpGet, Route("GetPredicates")]
        public JsonNetResult GetPredicates()
        {
            var list = Company.Query<dynamic>(@"select ID as [value], Name as [text] from Predicate order by Name");
            return new JsonNetResult { Data = list, Formatting = Formatting.None };
        }

        [HttpGet, Route("GetPossibleRelationshipsObjectByIntersect"), NonNullableParameters]
        public JsonNetResult GetPossibleRelationshipsObjectByIntersect(int id)
        {
            var list = Company.Query<AllowedIntersectionType>("GetAllowedIntersectionTypesByIntersect @intersectID", new { intersectID = id }).ToList().Select(i => new 
            {                
                Title = i.TargetName,                
                IntersectTypeID = i.IntersectTypeID,
                ParentIntersectID = i.ParentIntersectID,
                ObjectType = i.TargetType
            });
            return new JsonNetResult { Data = list, Formatting = Newtonsoft.Json.Formatting.None };
        }
             
        [HttpGet, Route("ChildRelationshipsBySourceAndTarget"), NonNullableParameters]
        public JsonNetResult ChildRelationshipsBySourceAndTarget(SystemObjects s, int sID, SystemObjects t, int tID)
        {
            var sType = s.ToString();
            var tType = t.ToString();
            var sql = $@"
select T.Object,
		T.ObjectID,
		T.ObjectUrl,
		T.ObjectName,
		T.ObjectTypeName
from[Intersect] O
    inner join[IntersectDetail] T on (
                                       ( (O.Subject = @s and O.SubjectID = @sid) AND (O.Object = @o and O.ObjectID = @oid) ) OR
                                       ( (O.Subject = @o and O.SubjectID = @oid) AND (O.Object = @s and O.ObjectID = @sid) )
							        )
									and T.Subject = 'Intersect' and T.SubjectID = O.ID";

            return new JsonNetResult { Data = Company.Query<dynamic>(sql, new { s = new Dapper.DbString { Value = sType, IsAnsi = true, IsFixedLength = true, Length = 50 }, sid = sID, o = new Dapper.DbString { Value = tType.ToString(), IsAnsi = true, IsFixedLength = true, Length = 50 }, oid = tID }).OrderBy(i => i.ObjectTypeName).ThenBy(i => i.ObjectName), Formatting = Formatting.None };
        }


        #endregion
    }
}
