using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Http;
using d360.services.interfaces;
using d360.extensions;

namespace d360.api
{
    [RoutePrefix("resources")]
    public class ResourcesController : BaseApiController
    {
        IResourceService ResourceService;

        public ResourcesController(IResourceService resourceService, IAuthenticationSource authenticationSource)
        {
            ResourceService = resourceService;
            AuthenticationSource = authenticationSource;
        }

        [Route("{id}")]
        public string Get(int id)
        {
            return "Hello, World! " + id;
        }
    }
}
