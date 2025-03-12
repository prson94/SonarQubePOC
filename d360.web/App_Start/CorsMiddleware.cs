using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using d360.core;
using d360.core.enums;
using d360.model;
using Microsoft.Owin;
using repositories;

namespace d360.web
{
    public class CorsMiddleware : BaseMiddleware
    {
        public CorsMiddleware(Func<IDictionary<string, object>, Task> next): base(next)
        {
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);
            IOwinResponse response = context.Response;
            bool isPreflight = false;
            int? companyID = context.Request.Get<int?>("CompanyID");

			if (companyID.HasValue && context.Request.Headers.ContainsKey("Origin"))
			{
				var acceptOrigin = context.Request.Headers["Origin"].SanitizeHtml();

				if (companyID.HasValue)
				{
					isPreflight = context.Request.Method == "OPTIONS";

					string originsSetting = null;
					string corsKey = "CorsHeaders";

					if (Cache.ListItemExists<string, int>(corsKey, companyID ?? 0))
					{
						originsSetting = Cache.GetItemInListByID<string, int>(corsKey, companyID ?? 0);
					}
					else
					{
						var cmy = DependencyResolver.Current.GetService<ICommunity>();
						originsSetting = await cmy.ReadSettingValueAsync<string>(companyID ?? 0, Setting.AllowedOrigins);
						Cache.SetItemInListByID(corsKey, companyID ?? 0, originsSetting, true, 5);
					}

					List<string> allowedOrigins = new List<string> {
						"https://shell-dev.dis.cloud.precisely.services",
						"https://shell-qa.dis.cloud.precisely.services",
						"https://shell-stg.dis.cloud.precisely.com",
						"https://shell.dis.cloud.precisely.com",
						"https://cloud.precisely.com",
      						"https://cdn-dev.cloud.precisely.services",
	    					"https://cdn-stg.cloud.precisely.com",
	  					"https://cdn.cloud.precisely.com"
					};

					if (!string.IsNullOrEmpty(originsSetting))
					{
						allowedOrigins.AddRange(originsSetting.Split(',').Select(s => s.Trim()));
					}

					//the requested origin is allowed, set appropriate access headers
					if (allowedOrigins.Contains(acceptOrigin))
					{
						context.Response.OnSendingHeaders(s =>
						{
							var res = (IOwinResponse)s;

							res.Headers.Add("Access-Control-Allow-Origin", new string[] { acceptOrigin });
							res.Headers.Add("Access-Control-Allow-Methods", new string[] { "*" });
							res.Headers.Add("Access-Control-Allow-Headers", new string[] { "*" });

							//override status code if this is a valid preflight request
							if (isPreflight)
							{
								res.StatusCode = (int)HttpStatusCode.OK;
								res.ReasonPhrase = HttpStatusCode.OK.ToString();
							}

						}, response);
					}
				}
			}
			await Next(environment);
        }
    }
}
