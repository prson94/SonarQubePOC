using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using d360.core.enums;

using Microsoft.Owin;

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
				var acceptOrigin = context.Request.Headers["Origin"];

				if (companyID.HasValue)
				{
					isPreflight = context.Request.Method == "OPTIONS";

					string originsSetting = "";
					using (var ctx = CreateOwinCompanyContext(companyID.Value))
					{
						originsSetting = ctx.GetSettingValue<string>(Setting.AllowedOrigins);
					}

					List<string> allowedOrigins = new List<string> {
						"https://shell-dev.dis.cloud.precisely.services",
						"https://shell-qa.dis.cloud.precisely.services",
						"https://shell-stg.dis.cloud.precisely.com",
						"https://shell.dis.cloud.precisely.com",
						"https://cloud.precisely.com"
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
