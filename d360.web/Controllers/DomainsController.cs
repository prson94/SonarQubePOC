using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using d360.web.Models;
using d360.core;
using d360.core.entities;
using System.Net;
using d360.model;

namespace d360.web.Controllers
{
    [RoutePrefix("domains"), Authorize]
    public class DomainsController : BaseController
    {
        #region DI

        public DomainsController(CommunityContext community, CompanyContext company): base(community, company)
        {
        }

        #endregion

        public JsonNetResult Hierarchy(int id)
        {
            var dt = Company.GetById<DomainType>(id, i => i.Domains, i => i.Groups);

            var list = new List<DomainHierarchyItem>();

            foreach (var o in dt.Groups.OrderBy(o => o.Name))
            {
                list.Add(new DomainHierarchyItem 
                {
                    HierarchyID = string.Format("DomainGroup|{0}", o.ID),
                    ID = o.ID,
                    Name = o.Name,
                    ParentHierarchyID = null,
                    Type = "DomainGroup"
                });
            }

            foreach (var o in dt.Domains.OrderBy(o => o.Name))
            {
                list.Add(new DomainHierarchyItem
                {
                    HierarchyID = string.Format("Domain|{0}", o.ID),
                    ID = o.ID,
                    Name = o.Name,
                    ParentHierarchyID = string.Format("DomainGroup|{0}", o.DomainGroupID),
                    Type = "Domain"
                });
            }

            return new JsonNetResult
            {
                Data = list,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }
    }
}
