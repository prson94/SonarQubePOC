using d360.core;
using d360.core.entities;
using d360.core.enums;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace d360.web
{
	public class CompanyIDCheckMiddleware : BaseMiddleware
	{
		public class cd
		{
			public bool AllowNewUserLogin { get; set; }

			public AuthenticationType AuthenticationType { get; set; }

			public int ClientID { get; set; }
			
			public int CompanyID { get; set; }

			public int DomainSettingID { get; set; }
			
			public string UrlPrefix { get; set; }

			public string PrimaryCompanyPrefix { get; set; }
		}

		public CompanyIDCheckMiddleware(Func<IDictionary<string, object>, Task> next): base(next)
		{
		}

		private async Task<cd> loadCachedItem(string host)
		{
			var key = "CompanyPrefixes";
			var tenant = Cache.GetItemInListByID<cd, string>(key, host);
			
			var keyInactive = "CompanyPrefixesInactive";
			var inactiveTenant = Cache.GetItemInListByID<string, string>(keyInactive, host);

			if (tenant == null && inactiveTenant == null)
			{
				using (var cnn = new SqlConnection(Config.GetValue<string>("ReadOnlyConnectionString")))
				{
					cnn.Open();
					tenant = await cnn.QuerySingleOrDefaultAsync<cd>(@"
select	top 1 s.AllowNewUserLogin,
		s.AuthenticationType, 
		e.ClientID, 
		s.CompanyID, 
		s.DomainSettingID, 
		s.UrlPrefix, 
		coalesce(p.UrlPrefix, s.UrlPrefix) as PrimaryCompanyPrefix
from	CompanyDomainSetting s
		inner join DomainSetting d on d.ID = s.DomainSettingID and s.UrlPrefix = @host
		left join CompanyDomainSetting p on p.CompanyID = s.CompanyID and p.IsPrimary = 1
		inner join Company e on e.ID = s.CompanyID and e.Status = 'Active'", new { host });
				}

				if (tenant != null)
				{
					Cache.SetItemInListByID(key, host, tenant, true, 10);
				}
				else 
				{
					Cache.SetItemInListByID(keyInactive, host, host, true, 10);
				}
			}

			return tenant;
		}

		public async Task Invoke(IDictionary<string, object> environment)
		{
			IOwinContext context = new OwinContext(environment);
			var host = context.Request.Headers["Host"].SanitizeHtml();
			try
			{
				
				bool searchHeaders = true;
				if (host.Contains(".data3sixty"))
				{
					host = host.Substring(0, host.IndexOf(".data3sixty")).ToLower();
					searchHeaders = false;
				}
				if (searchHeaders)
				{
					if (!string.IsNullOrEmpty(context.Request.Headers["CompanyID"]))
					{
						host = context.Request.Headers["CompanyID"].ToLower().SanitizeHtml();
					}
				}
				
				var tenant = await loadCachedItem(host);
				
				if (tenant != null)
				{
					context.Request.Set("AllowNewUserLogin", tenant.AllowNewUserLogin);
					context.Request.Set("AuthenticationType", tenant.AuthenticationType);
					context.Request.Set("CompanyDomain", tenant.UrlPrefix);
					context.Request.Set("PrimaryCompanyPrefix", tenant.PrimaryCompanyPrefix);
					context.Request.Set("ClientID", tenant.ClientID);
					context.Request.Set("CompanyID", tenant.CompanyID);
					context.Request.Set("DomainSettingID", tenant.DomainSettingID);
				}
				else
				{
					context.Response.ContentType = "text/html";
					context.Response.Write(
						string.Format(
							"<div style='" +
								"font-weight: bold; " +
								"background: linear-gradient(to bottom, rgb(167, 167, 167) 0%, #e4d6d600 75%);" +
								"height:100%;" +
								"width:100%;" +
								"margin: -8px;" +
								"position: absolute;'>" +
								"<div style='" +
									"width:600px;" +
									"height:200px;" +
									"margin-left:auto;" +
									"margin-right:auto;" +
									"background: #d2d2d2;" +
									"margin-top: 100px;" +
									"padding: 10px;" +
									"box-shadow: 2px 2px 10px 3px rgba(148,148,148,0.75);" +
									"text-align: center;" +
									"'>" +
									"<h1>Error locating company [{0}]</h1>" +
									"<p>Please check the url is correct or contact your Administrator.</p>" +
								"" +
								"</div>" +
							"</div>"
							, host));
					Trace.TraceWarning("Could not locate the company with host address of: {0}", host);
					return;
				}
			}
			catch (Exception e)
			{
				Log.LogError(e, $"Error checking company Ids in CompanyIDCheckMiddleware. For host {host}");
			}
			await Next(environment);
		}
	}
}
