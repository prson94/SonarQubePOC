using System.Web.Mvc;
using d360.model;

namespace d360.web.Controllers
{
    [RoutePrefix("services/custom"), Authorize]
    public class CustomController : BaseController
    {
        #region DI

        public CustomController(CommunityContext community, CompanyContext company)
            : base(community, company)
        { }

        #endregion

        #region Json

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">The ID of the rule to retrieve attributes for.</param>
        /// <param name="typeID">The ID of the attribute type to get.</param>
        /// <returns></returns>
        [Route("rules/{id}/attributes/{typeID}"), HttpGet]
        public JsonNetResult GetAttributesByAttributeType(int id, int typeID)
        {
            var joins = "";
            var columns = "";
            getDynamicFieldJoinStatements(typeID, "Attribute", out joins, out columns);

            var querySql = string.Format(@"select A.ID, {0} T.Name
from	Attribute A 
inner join AttributeType T on T.ID = A.AttributeTypeID and T.ID = @typeID and A.ObjectType = 'Rule' and A.ObjectID = @id {1}", columns, joins);

            var sql = string.Format(@"select * from ({0}) A", querySql);

            var models = Company.Query<dynamic>(sql, new { id = id, typeID = typeID });
            return new JsonNetResult
            {
                Data = models
            };            
        }

         #endregion
    }
}