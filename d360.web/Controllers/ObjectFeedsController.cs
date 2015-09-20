namespace d360.web.Controllers
{
    using d360.core;
    using System.Web.Mvc;
    using d360.core.entities;
    using d360.model;

    [RoutePrefix(""), Authorize]
    public class ObjectsController : BaseController
    {
        #region DI

        public ObjectsController(CommunityContext community, CompanyContext company)
            : base(community, company)
        { }

        #endregion

        [Route("{type}/{id:int}/reports/RelationshipWithContextAggregate")]
        public ActionResult RelationshipWithContextAggregate(SystemObjects type, int id)
        {
            return PartialView(new ObjectModel { ObjectID = id, ObjectType = type.ToString() });
        }
    }
}

namespace d360.web.Controllers
{
    using System.Linq;
    using d360.core;
    using System.Web.Http;
    using d360.model;
    using d360.core.entities.Views;
    using System.Web.Http.Description;


    [RoutePrefix(""), Authorize, ApiExplorerSettings(IgnoreApi = true)]
    public class ObjectFeedsController : BaseApiController
    {
        #region DI

        public ObjectFeedsController(CommunityContext community, CompanyContext company)
            : base(community, company)
        { 
        }

        #endregion

        [HttpGet, Route("{type}/{id:int}/reports/RelationshipWithContextAggregate/data")]
        public IQueryable<RelationshipWithContextAggregate> RelationshipWithContextAggregate(SystemObjects type, int id)
        {
            var sType = type.ToString();
            return Company.RelationshipWithContextAggregates.Where(i => i.ObjectType == sType && i.ObjectID == id);
        }
    }
}