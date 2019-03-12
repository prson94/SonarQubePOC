using d360.extensions;
using d360.model;
using Microsoft.Web.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Dapper;
using System.Data.SqlClient;
using System.Data;
using System.Data.Entity;
using Swashbuckle.Swagger.Annotations;

namespace d360.web.Controllers.V2
{
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/profiles"),
        Authorize
    ]

    public class ProfilesController : BaseApiController
    {
        #region DI

        public ProfilesController
(CommunityContext community, CompanyContext company)
            : base(community, company)
        {

        }

        #endregion

        private static string DefaultImplementationName = "default";
    }


}
