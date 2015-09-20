using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Http;
using d360.services.interfaces;
using d360.extensions;
using d360.core;
using System.Net.Http;
using System.Runtime.Serialization;
using d360.core.entities;

namespace d360.api
{
    [RoutePrefix("lineage")]
    public class LineageController : BaseApiController
    {
        ILineageService LineageService;

        public LineageController(ILineageService lineageService, IAuthenticationSource authenticationSource)
        {
            LineageService = lineageService;
            AuthenticationSource = authenticationSource;
        }
    }
}
