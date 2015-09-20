using d360.core;
using d360.core.entities;
using d360.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    [RoutePrefix("tooltips"), Authorize]
    public class TooltipsController : BaseController
    {        
        #region DI

        public TooltipsController(CommunityContext community, CompanyContext company) 
            : base(community, company)
        { 
        }

        #endregion

        //public JsonResult YourFollowers()
        //{
        //    var models = Company.FollowDetails.Where(i => i.ObjectType == "Resource" && i.ObjectID == Company.CurrentResourceID);
        //    return Json(new { FollowerCount = fCount, GroupCount = gCount }, JsonRequestBehavior.AllowGet);
        //}

        [Route("{type}/{id:int}/redflags")]
        public ActionResult RedFlags(SystemObjects type, int id)
        {
            return PartialView(new ObjectModel { ObjectID = id, ObjectType = type.ToString() });
        }
    }
}