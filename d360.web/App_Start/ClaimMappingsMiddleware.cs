using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using d360.core;
using d360.core.entities;
using d360.model;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Owin;

namespace d360.web
{
	public class ClaimMappingsMiddleware : BaseMiddleware
	{
		private readonly int CLAIMS_CACHE_MINUTES = 15;

		public ClaimMappingsMiddleware(Func<IDictionary<string, object>, Task> next): base(next) { }

		public async Task Invoke(IDictionary<string, object> environment)
		{
			IOwinContext context = new OwinContext(environment);
			var host = context.Request.Headers["Host"];

			try
			{
				var urlPrefix = context.Request.Get<string>("CompanyDomain");
				var companyId = context.Request.Get<int>("CompanyID");

				var key = $"{companyId}_{urlPrefix}_ClaimMappings";
				var mappings = Cache.GetItem<List<ClaimMapping>>(key);

				if (mappings == null)
				{
					using (var cnn = new SqlConnection(ConfigurationManager.AppSettings[constants.COMMUNITYDB_APPSETTING]))
					{
						await cnn.OpenIfClosed();
						mappings = (await cnn.QueryAsync<ClaimMapping>(@"exec GetClaimMappings @companyId, @urlPrefix", new { companyId, urlPrefix })).ToList();
					}

					Cache.SetItem(key, mappings, true, CLAIMS_CACHE_MINUTES);
				}
			}
			catch (Exception e)
			{
				Log.LogError(e, $"Error getting claims map in ClaimMappingsMiddleware. For host {host}");
			}

			await Next(environment);
		}
	}
}
