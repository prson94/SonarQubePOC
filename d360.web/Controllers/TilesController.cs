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
    [RoutePrefix("tiles"), Authorize]
    public class TilesController : BaseController
    {        
        #region DI

        public TilesController(CommunityContext community, CompanyContext company) 
            : base(community, company)
        { 
        }

        #endregion

        public JsonNetResult HomeSocial()
        {
            return new JsonNetResult { Data = Company.GetSocialDataForCurrentResource(), Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult GroupSocial(int id)
        {
            return new JsonNetResult { Data = Company.GetSocialDataForGroup(id), Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult ProfileSocial(int id)
        {
            return new JsonNetResult { Data = Company.GetSocialDataForResource(id), Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult RelationshipAggregates(SystemObjects type, int id)
        {
            return new JsonNetResult { Data = Company.GetAggregateRelationshipBreakdownsByObject(type, id), Formatting = Newtonsoft.Json.Formatting.None };
        }
    }
}