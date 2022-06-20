using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

using d360.core;
using d360.core.entities;
using Dapper;

using Microsoft.Owin;

namespace d360.web
{
	public class ClaimMappingsMiddleware : BaseMiddleware
	{
		private readonly int CLAIMS_CACHE_MINUTES = 15;

		public ClaimMappingsMiddleware(Func<IDictionary<string, object>, Task> next): base(next)
		{
		}

		private async Task<List<ClaimMapping>> LoadMappings(string urlPrefix, int companyId)
		{
			var key = $"{companyId}_{urlPrefix}_ClaimMappings";
			var mappings = Cache.GetItem<List<ClaimMapping>>(key);

			if (mappings == null)
			{
				using (var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
				{
					cnn.Open();
					mappings = (await cnn.QueryAsync<ClaimMapping>(@"exec GetClaimMappings @companyId, @urlPrefix", new { companyId, urlPrefix })).ToList();
				}

				Cache.SetItem(key, mappings, true, CLAIMS_CACHE_MINUTES);
			}

			return mappings;
		}

		public async Task Invoke(IDictionary<string, object> environment)
		{
			IOwinContext context = new OwinContext(environment);
			var host = context.Request.Headers["Host"];

			try
			{

				var urlPrefix = context.Request.Get<string>("CompanyDomain");
				var companyId = context.Request.Get<int>("CompanyID");

				await LoadMappings(urlPrefix, companyId);

			}
			catch (Exception e)
			{
				var properties = new Dictionary<string, string>
				{
					{"Middleware","ClaimMappingsMiddleware" },
					{"Host", host }
				};
				var telemetry = new Microsoft.ApplicationInsights.TelemetryClient();

				telemetry.TrackException(e, properties);
			}
			await Next(environment);
		}
	}
}
