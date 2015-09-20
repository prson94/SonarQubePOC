using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using d360.web.Models;
using d360.web.Controllers;
using d360.core.entities.Views;
using d360.core.entities;
using d360.model;
using d360.services.interfaces;
//using AttributeRouting;
using System.Net;
using d360.core;
using d360.core.exceptions;

namespace d360.web.Controllers
{
//[RoutePrefix("lineage")]
    public class LineageController : BaseController
    {
        #region DI
        
        ILineageService LineageService;

        public LineageController(CommunityContext community,
            CompanyContext company, 
            ILineageService lineageService)
            : base(community, company)
        {
            LineageService = lineageService;
        }

        #endregion

        #region Json

        public JsonResult Links(SystemObjects type, int id, SystemObjects filter, int filterID)
        {
            var items = LineageService.GetRelationshipsBySource(type, id, filter, filterID);
            return Json(items, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}
