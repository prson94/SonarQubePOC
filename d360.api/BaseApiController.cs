using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using d360.extensions;
using System.Net.Http;
using System.Net;
using d360.core;

namespace d360.api
{
    public class ApiClaimsAuthorize : Thinktecture.IdentityModel.Authorization.WebApi.ClaimsAuthorizeAttribute
    {
        public ApiClaimsAuthorize(SystemObjects type, string action): base(action, type.ToString())
        {}
    }

    public class QuerySettings
    {
        public int Skip { get; set; }
        public int Take { get; set; }
        
        public QuerySettings(HttpRequestMessage Request)
        {
            int skip = 0;
            int take = 25;
            var queryValues = Request.GetQueryNameValuePairs().Select(i => new { i.Key, i.Value }).ToList();
            if (queryValues.Any(i => i.Key == "skip")) int.TryParse(queryValues.First(i => i.Key == "skip").Value, out skip);
            if (queryValues.Any(i => i.Key == "take")) int.TryParse(queryValues.First(i => i.Key == "take").Value, out take);
            if (skip < 0) skip = 0;
            if (take < 0) take = 25;
            if (take > 250) take = 250;

            Skip = skip;
            Take = take;
        }
    }

    public class BaseApiController : ApiController
    {
        internal IAuthenticationSource AuthenticationSource;

        internal string GetFullErrorMessage(Exception ex)
        {
            return ex.Message + ((ex.InnerException != null) ? "(" + ex.InnerException.Message + ")" : ""); ;
        }

        internal int? ParseParentIDIfPresent()
        {
            int? parentID = null;
            var queryValues = Request.GetQueryNameValuePairs().Select(i => new { i.Key, i.Value }).ToList();
            if (queryValues.Any(i => i.Key == "parentID"))
            {
                int pID;
                if (int.TryParse(queryValues.First(i => i.Key == "parentID").Value, out pID))
                {
                    parentID = pID;
                }
            }
            return parentID;
        }
    }
}
