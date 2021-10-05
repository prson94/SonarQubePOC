using d360.extensions.info;
using d360.model;
using d360.web.caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace d360.web
{
    public class BaseMiddleware
    {
        public CompanyContext CreateOwinCompanyContext(int companyId)
        {
            var sec = new UriSecurityContextProvider
            {
                CompanyID = companyId,
                ResourceID = 0,
                CompanyPrefix = "",
                IsAdministrator = false
            };
            var cache = new MemoryCachingProvider();
            var community = new CommunityContext(cache, null, sec);
            return new CompanyContext(community, cache, null, sec, null, false);
        }
    }
}