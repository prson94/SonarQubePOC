using System.Web.Mvc;
using d360.model;
using d360.web.Models.Attributes;

namespace d360.web.Controllers
{
    [RoutePrefix("groups"), Authorize]
    public class GroupsController : BaseController
    {
        #region DI

        public GroupsController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {
        }

        #endregion

        [Route("{id:int}/join"), HttpPost]
        public JsonResult JoinGroup(int id)
        { 
            return Json(new {
                title = "Request Sent!", message = "Sent request to join this group.", id = id
            });
        }
    }
}