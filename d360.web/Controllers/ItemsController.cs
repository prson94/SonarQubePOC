using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Http;
using d360.core.entities;
using d360.model.interfaces;
using System.Web.Http.Controllers;
using AttributeRouting;

namespace d360.web.Controllers
{
    [RoutePrefix("items")]
    public class ItemsController : ApiController
    {
        public IDomainListItemRepository Repo { get; set; }

        public ItemsController()
        {
        }

        public IQueryable<DomainListItem> Get(int id)
        {
            return Repo.Filter(i => i.DomainListID == id).AsQueryable();
        }
    }
}
