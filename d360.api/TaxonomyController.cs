using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Http;
using d360.core;
using d360.services.interfaces;
using System.Net;
using System.Net.Http;
using d360.extensions;

namespace d360.api
{
    [RoutePrefix("taxonomies")]
    public class TaxonomyController : BaseApiController
    {
        ITaxonomyService TaxonomyService;

        public TaxonomyController(ITaxonomyService taxonomyService, IAuthenticationSource authenticationSource)
        {
            TaxonomyService = taxonomyService;
            AuthenticationSource = authenticationSource;
        }

        [Route(""), PermissionAuthorization]//(Permission.TaxonomyRead)
        public IQueryable<dynamic> GetTypes()
        {
            var items = TaxonomyService.GetTypes();

            return items.Select(i => new 
            {
                i.ID,
                i.Name,
                i.Description
            }).AsQueryable();
        }

        [Route("{typeID}"), PermissionAuthorization] //(Permission.TaxonomyRead)?parentID={parentID}
        public IQueryable<dynamic> GetAllByTypeAndParent(int typeID) //, int? parentID
        {
            int? parentID = ParseParentIDIfPresent();

            var items = TaxonomyService.GetAllByType(typeID).AsQueryable();
            items = (parentID.HasValue) ?
                items.Where(i => i.ParentID == parentID).OrderBy(i => i.Name).AsQueryable() :
                items.OrderBy(i => i.ParentID).ThenBy(i => i.Name).AsQueryable();//items.Where(i => i.ParentID == null);
            
            return items.Select(i => new
            {
                i.ID,
                i.ParentID,
                i.Name
            }).AsQueryable();
        }
    }
}
