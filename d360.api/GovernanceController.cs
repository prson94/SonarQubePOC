using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Http;
using d360.services.interfaces;
using d360.extensions;

namespace d360.api
{
    [RoutePrefix("governance")]
    public class GovernanceController : BaseApiController
    {
        IGovernanceService GovernanceService;

        public GovernanceController(IGovernanceService governanceService, IAuthenticationSource authenticationSource)
        {
            GovernanceService = governanceService;
            AuthenticationSource = authenticationSource;
        }

        [Route("{id}")]
        public string Get(int id)
        {
            return "Hello, World! " + id;
        }
    }
}
