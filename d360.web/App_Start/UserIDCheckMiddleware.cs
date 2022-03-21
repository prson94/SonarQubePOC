using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

using d360.core;
using d360.core.enums;
using d360.utils.company;
using d360.web.Extensions;

using Dapper;

using IdentityModel.Client;

using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Owin;

using Newtonsoft.Json;

namespace d360.web
{
	public class UserIDCheckMiddleware : BaseMiddleware
	{
		public class usercompany
		{
			public int ResourceID { get; set; }
			
			public int CompanyID { get; set; }
			
			public bool IsAdministrator { get; set; }
						
			public CompanyResourceState State { get; set; }

			public string Username { get; set; }
						
			public string APIPublicKey { get; set; }
			
			public string APIPrivateKey { get; set; }

		}

		private readonly Func<IDictionary<string, object>, Task> _next;

		public UserIDCheckMiddleware(Func<IDictionary<string, object>, Task> next)
		{
			_next = next;
		}

		public ConcurrentBag<usercompany> Users
		{
			get
			{
				var users = Cache.GetItem<ConcurrentBag<usercompany>>("Users");
				
				if (users == null)
				{
					users = new ConcurrentBag<usercompany>();
				}

				return users;
			}
			set => Cache.SetItem("Users", value, true, 10);
		}

		private usercompany LoadUserFromDatabase(int companyID, string apiKey = null, string apiSecret = null, string apiReadOnlyKey = null, string username = null)
		{
			usercompany u = null;

			try
			{
				using (var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
				{
					cnn.Open();
					var baseSql = @"
									select	C.*,
											R.APIPrivateKey,
											R.APIPublicKey,
											R.Username,
											R.Password
									from	Resource R
											inner join CompanyResource C on C.ResourceID = R.ID and C.CompanyID = @com";

					if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
					{
						u = cnn.Query<usercompany>(baseSql + @" and R.APIPublicKey = @pub and R.APIPrivateKey = @pri", new { com = companyID, pri = new Dapper.DbString { IsAnsi = true, IsFixedLength = true, Length = 50, Value = apiSecret }, pub = new Dapper.DbString { IsAnsi = true, IsFixedLength = true, Length = 25, Value = apiKey } }).FirstOrDefault();
					}
					else if (!string.IsNullOrEmpty(username))
					{
						u = cnn.Query<usercompany>(baseSql + @" and ltrim(rtrim(R.Username)) = @username", new { com = companyID, username = new DbString { IsAnsi = false, Length = 250, Value = username } }).FirstOrDefault();
					}

				}
			}
			catch (Exception ex)
			{
				Trace.TraceError($"UserIDCheckMiddleware: {ex.GetFullExceptionData()}");
			}

			return u;
		}

		public async Task Invoke(IDictionary<string, object> environment)
		{
			IOwinContext context = new OwinContext(environment);

			usercompany u = null;

			var companyID = context.Get<int>("CompanyID");

			try
			{

				var apiCredentials = context.Request.Headers["Authorization"];
				var token = string.Empty;

				var cachedUsers = Users;

				// llyods custom auth depends on ceriticate and JWT token
				if (!string.IsNullOrEmpty(apiCredentials) && apiCredentials.ToUpper().Contains("BEARER"))
				{
					var jwtTelemetry = new Microsoft.ApplicationInsights.TelemetryClient();

					jwtTelemetry.TrackTrace(new TraceTelemetry { Message = $"Creds : {apiCredentials}", SeverityLevel = SeverityLevel.Verbose });

					var authParts = apiCredentials.Split(' ');

					if (authParts.Length == 2)
					{
						var jwtToken = authParts[1];

						var claim = await ValidateJwt(jwtToken, context, jwtTelemetry);

						if (claim != null && claim.Identity != null && !string.IsNullOrEmpty(claim.Identity.Name))
						{
							jwtTelemetry.TrackTrace(new TraceTelemetry { Message = $"JWT Username {claim.Identity.Name}", SeverityLevel = SeverityLevel.Verbose });
							u = LoadUserFromDatabase(companyID, null, null, null, claim.Identity.Name);
							
							if (u != null)
							{
								if (!cachedUsers.Any(i => i.Username == u.Username && i.CompanyID == u.CompanyID))
								{
									cachedUsers.Add(u);
								}

								Users = cachedUsers;
							}
						}
					}
				}
				else if (!string.IsNullOrEmpty(apiCredentials))
				{
					var authValues = apiCredentials.Split(';');

					if (authValues.Length == 2 && authValues[0].Length == 25 && authValues[1].Length == 50)
					{
						u = cachedUsers.FirstOrDefault(i => i.CompanyID == companyID && i.APIPrivateKey == authValues[1] && i.APIPublicKey == authValues[0]);
						
						if (u == null)
						{
							u = LoadUserFromDatabase(companyID, apiKey: authValues[0], apiSecret: authValues[1]);
							
							if (u != null)
							{
								if (!cachedUsers.Any(i => i.Username == u.Username && i.CompanyID == u.CompanyID))
								{
									cachedUsers.Add(u);
								}

								Users = cachedUsers;
							}
						}
					}
				}

				if (context.Request.User.Identity.IsAuthenticated)
				{
					u = cachedUsers.FirstOrDefault(i => i.CompanyID == companyID && i.Username == context.Request.User.Identity.Name.ToLower());
					
					if (u == null)
					{
						u = LoadUserFromDatabase(companyID, username: context.Request.User.Identity.Name.ToLower());
						
						if (u != null)
						{
							if (!cachedUsers.Any(i => i.Username == u.Username && i.CompanyID == u.CompanyID))
							{
								cachedUsers.Add(u);
							}

							Users = cachedUsers;
						}
					}
				}

				if (u != null)
				{
					if (u.State == CompanyResourceState.Active)
					{
						context.Set("IsAdministrator", u.IsAdministrator);
						context.Set("ResourceID", u.ResourceID);
						context.Request.User = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(u.ResourceID.ToString(), "ID"), null);
					}
					else
					{
						u = null;
						System.Web.HttpContext.Current.Response.SuppressFormsAuthenticationRedirect = true;
						context.Response.Write("\"Not authorized\"");
						context.Response.StatusCode = 401;

						return;
					}
				}
				else
				{
					try
					{
						if (!string.IsNullOrEmpty(apiCredentials))
						{
							Trace.TraceWarning("Could not locate the user with API credentials of: {0}", apiCredentials);
						}

						if (!string.IsNullOrEmpty(token))
						{
							Trace.TraceWarning("Could not locate the user with API token of: {0}", token);
						}

						if (context.Request.User.Identity.IsAuthenticated)
						{
							Trace.TraceWarning("Could not locate the user with name of: {0}", context.Request.User.Identity.Name);
						}

						if (!string.IsNullOrEmpty(apiCredentials) || !string.IsNullOrEmpty(token) || context.Request.User.Identity.IsAuthenticated)
						{
							System.Web.HttpContext.Current.Response.SuppressFormsAuthenticationRedirect = true;

							context.Response.Write("\"Not authorized\"");
							context.Response.StatusCode = 401;

							return;
						}
					}
					catch (Exception ex)
					{
						Trace.TraceError($"UserIDCheckMiddleware - {ex.GetFullExceptionData()}");
					}
				}
			}
			catch (Exception e)
			{
				//log error
				var properties = new Dictionary<string, string>
				{
					{"Middleware","UserIDCheckMiddleware" },
					{"companyID", companyID.ToString() }
				};
				var telemetry = new Microsoft.ApplicationInsights.TelemetryClient();

				telemetry.TrackException(e, properties);
			}

			await _next.Invoke(environment);
		}

		public const string Authority = "http://localhost:5000";
		private static readonly bool jwtDiscoveryValidateIssuerName = (ConfigurationManager.AppSettings["jwtDiscoveryValidateIssuerName"] ?? "").ToUpper() == "TRUE";
		private static readonly bool jwtValidateAudience = (ConfigurationManager.AppSettings["jwtValidateAudience"] ?? "").ToUpper() == "TRUE";
		private static readonly bool jwtRequireExpirationTime = (ConfigurationManager.AppSettings["jwtRequireExpirationTime"] ?? "").ToUpper() == "TRUE";
		private static readonly bool jwtValidateLifetime = (ConfigurationManager.AppSettings["jwtValidateLifetime"] ?? "").ToUpper() == "TRUE";

		private async Task<ClaimsPrincipal> ValidateJwt(string jwt, IOwinContext context, Microsoft.ApplicationInsights.TelemetryClient telemetry)
		{
			string authority = await getJwtAuthority(context);

			telemetry.TrackTrace(new TraceTelemetry { Message = $"JWT Authority : {authority}", SeverityLevel = SeverityLevel.Verbose });

			telemetry.TrackTrace(new TraceTelemetry { Message = $"Discovery Client Starting", SeverityLevel = SeverityLevel.Verbose });

			if (string.IsNullOrEmpty(authority))
			{
				telemetry.TrackTrace(new TraceTelemetry { Message = $"Jwt Authority Uri is not set cannot continue", SeverityLevel = SeverityLevel.Verbose });

				return null;
			}

			var clientFactory = HttpClientFactory.Create(new HttpClientHandler
			{
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
			});
			var discoCache = new DiscoveryCache(authority, () => clientFactory, new DiscoveryPolicy { ValidateIssuerName = jwtDiscoveryValidateIssuerName });
			var disco = await discoCache.GetAsync();

			if (disco == null)
			{
				telemetry.TrackTrace(new TraceTelemetry { Message = $"Discovery response is null.", SeverityLevel = SeverityLevel.Verbose });
				
				return null;
			}

			if (disco.IsError)
			{
				telemetry.TrackTrace(new TraceTelemetry { Message = $"Discovery response indicated error(s). {disco.Error}", SeverityLevel = SeverityLevel.Verbose });
				
				return null;
			}

			if (disco.KeySet == null)
			{
				telemetry.TrackTrace(new TraceTelemetry { Message = $"Discovery response included no keys.", SeverityLevel = SeverityLevel.Verbose });
				
				return null;
			}

			var user = jwt.ValidateJwtIdentityToken("upn",
				null, jwtValidateAudience,
				disco.Issuer, true,
				disco.KeySet.Keys, true, true,
				jwtRequireExpirationTime, jwtValidateLifetime);

			return user;
		}

		private async Task<string> getJwtAuthority(IOwinContext context)
		{
			var companyId = context.Get<int>("CompanyID");
			string cnName;
			var key = $"JWTAuthority{companyId}";

			// try the cache
			cnName = Cache.GetItem<string>(key);

			// not in cache query community
			if (string.IsNullOrEmpty(cnName))
			{
				var connectionString = CompanyConnectionUtils.GetCompanyConnectionString(companyId);
				using (var cnn = new SqlConnection(connectionString))
				{
					cnn.Open();
					cnName = (await cnn.QueryAsync<string>(@"select Value from Setting where ID = @s", new { @s = (int)Setting.JwtAuthority })).FirstOrDefault();
					
					if (string.IsNullOrEmpty(cnName))
					{
						cnName = Setting.JwtAuthority.AsInfoModel().DefaultValue;
					}
				}

				// Stick in cache
				Cache.SetItem(key, cnName);
			}

			return cnName;
		}
	}
}
