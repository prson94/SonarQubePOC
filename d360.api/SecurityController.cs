using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Http;
using d360.extensions;
using d360.services.interfaces;

namespace d360.api
{
    [RoutePrefix("security")]
    public class SecurityController : BaseApiController
    {
        ISecurityService SecurityService;

        public SecurityController(ISecurityService securityService, IAuthenticationSource authenticationSource)
        {
            SecurityService = securityService;
            AuthenticationSource = authenticationSource;
        }

        [Route("{id}")]
        public string Get(int id)
        {
            return "Hello, World! " + id;
        }
    }
}
