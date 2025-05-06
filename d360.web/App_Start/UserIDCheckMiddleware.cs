using d360.core.entities;
using d360.core.enums;
using d360.core.resources;
using d360.model;
using d360.utils.company;
using d360.web.Extensions;
using d360.web.Models;
using Dapper;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using repositories;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;

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

		public class UserRecacheEventArgs : EventArgs
		{
			public usercompany user { get; set; }
			public string key { get; set; }
		}

		public event EventHandler<UserRecacheEventArgs> UserRecache;

		public virtual void OnUserRecache(UserRecacheEventArgs args)
		{
			if (args.user != null)
			{
				Cache.SetItem(args.key, args.user, true, 1);
			}
		}

		public UserIDCheckMiddleware(Func<IDictionary<string, object>, Task> next) : base(next) { }

		private usercompany LoadUserFromDatabase(int companyID, string apiKey = null, string apiSecret = null, string username = null)
		{
			usercompany u = null;

			try
			{
				using (var cnn = new SqlConnection(ConfigurationManager.AppSettings[constants.Setting.ReadOnlyConnection]))
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
				Log.LogCritical(ex, $"UserIDCheckMiddleware: failed to laod users from company.");
			}

			return u;
		}

		public async Task Invoke(IDictionary<string, object> environment)
		{
			IOwinContext context = new OwinContext(environment);
			usercompany u = null;

			int companyID = context.Get<int>("CompanyID");
			string urlPrefix = context.Get<string>("CompanyDomain");
			string mappingsKey = $"{companyID}_{urlPrefix}_ClaimMappings";
			string userCacheKey = $"TenantUser_{companyID}_";
			bool isValidCacheKey = false;
			bool inCache = false;

			try
			{
				var apiCredentials = context.Request.Headers["Authorization"];
				var token = string.Empty;

				if (!string.IsNullOrEmpty(apiCredentials) && apiCredentials.ToUpper().Contains("BEARER"))
				{
					var authParts = apiCredentials.Split(' ');
					if (authParts.Length == 2)
					{
						var jwtToken = authParts[1];

						var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
						var jwtSecurityToken = handler.ReadJwtToken(jwtToken);
						var payload = jwtSecurityToken.Payload;
						var jwtClaim = await ValidateJwt(jwtToken, context);
						var allClaims = jwtSecurityToken?.Claims?.ToList() ?? new List<System.Security.Claims.Claim>();
						var claimMappings = Cache.GetItem<List<ClaimMapping>>(mappingsKey);

						var userAuth = new UserAuthentication();
						userAuth.ParseClaims(claimMappings, allClaims, payload);

						if (userAuth.Email == null && jwtClaim != null && jwtClaim.Identity != null && !string.IsNullOrEmpty(jwtClaim.Identity.Name))
						{
							userAuth.Email = jwtClaim.Identity.Name;
						}

						userCacheKey += $"E_{jwtClaim.Identity.Name.ToLower()}";
						isValidCacheKey = true;
						u = Cache.GetItem<usercompany>(userCacheKey);
						inCache = (u != null);
						if (!inCache)
						{
							u = LoadUserFromDatabase(companyID, null, null, jwtClaim.Identity.Name);
							OnUserRecache(new UserRecacheEventArgs { key = userCacheKey, user = u });
						}
						if (u != null)
						{
							await parseLoginInfoAndClaims(companyID, userAuth.FirstName, userAuth.LastName, userAuth.Email, userAuth.Groups);
						}
					}
				}
				else if (!string.IsNullOrEmpty(apiCredentials))
				{
					var authValues = apiCredentials.Split(';');

					if (authValues.Length == 2 && authValues[0].Length == 25 && authValues[1].Length == 50)
					{
						userCacheKey += $"A_{authValues[0]}";
						isValidCacheKey = true;
						u = Cache.GetItem<usercompany>(userCacheKey);
						inCache = (u != null);
						if (!inCache)
						{
							u = LoadUserFromDatabase(companyID, apiKey: authValues[0], apiSecret: authValues[1]);
							OnUserRecache(new UserRecacheEventArgs { key = userCacheKey, user = u });
						}
						else
						{
							if (u.APIPrivateKey != authValues[1])
							{
								u = null;
							}
						}
					}
				}

				if (context.Request.User.Identity.IsAuthenticated)
				{
					var username = context.Request.User.Identity.Name.ToLower();
					userCacheKey += $"E_{username}";
					isValidCacheKey = true;
					u = Cache.GetItem<usercompany>(userCacheKey);
					inCache = (u != null);
					if (!inCache && isValidCacheKey)
					{
						u = LoadUserFromDatabase(companyID, username: username);
						OnUserRecache(new UserRecacheEventArgs { key = userCacheKey, user = u });
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
						context.Response.Write($"\"{Error.EndpointNotAuthorizedHeading}\"");
						context.Response.StatusCode = 401;

						return;
					}
				}
				else
				{
					try
					{
						if (!string.IsNullOrEmpty(apiCredentials) || !string.IsNullOrEmpty(token) || context.Request.User.Identity.IsAuthenticated)
						{
							System.Web.HttpContext.Current.Response.SuppressFormsAuthenticationRedirect = true;

							context.Response.Write($"\"{Error.EndpointNotAuthorizedHeading}\"");
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
				Log.LogError(e, "Error checking user Ids in UserIDCheckMiddleware.");
			}

			await Next(environment);
		}

		private static readonly bool jwtDiscoveryValidateIssuerName = (ConfigurationManager.AppSettings["jwtDiscoveryValidateIssuerName"] ?? "").ToUpper() == "TRUE";
		private static readonly bool jwtValidateAudience = (ConfigurationManager.AppSettings["jwtValidateAudience"] ?? "").ToUpper() == "TRUE";
		private static readonly bool jwtRequireExpirationTime = (ConfigurationManager.AppSettings["jwtRequireExpirationTime"] ?? "").ToUpper() == "TRUE";
		private static readonly bool jwtValidateLifetime = (ConfigurationManager.AppSettings["jwtValidateLifetime"] ?? "").ToUpper() == "TRUE";

		private async Task<ClaimsPrincipal> ValidateJwt(string jwt, IOwinContext context)
		{
			int companyID = context.Get<int>("CompanyID");
			var urlSegment = context.Get<string>("CompanyDomain");

			string authority = await getJwtAuthority(context);

			var cacheKey = $"OidcSettings_{companyID}";
			OidcAuthenticationSettings authenticationSettings = null;
			if (Cache.ListItemExists<OidcAuthenticationSettings, int>(cacheKey, companyID))
			{
				authenticationSettings = Cache.GetItemInListByID<OidcAuthenticationSettings, int>(cacheKey, companyID);
			}
			else
			{
				var cmy = DependencyResolver.Current.GetService<ICommunity>();
				authenticationSettings = await cmy.ReadIdpOidcSettingsByTenantPrefix(urlSegment);
				Cache.SetItemInListByID(cacheKey, companyID, authenticationSettings, true, 5);
			}

			var discoveryUri = authenticationSettings.jwtAuthorityUri ?? authenticationSettings.discoveryUri ?? authority;

			Log.LogTrace($"JWT Authority : {authority}");
			Log.LogTrace($"Discovery Client Starting");

			var clientFactory = HttpClientFactory.Create(new HttpClientHandler
			{
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
			});
			var discoCache = new DiscoveryCache(discoveryUri, () => clientFactory, new DiscoveryPolicy
			{
				ValidateEndpoints = false,
				Authority = authority,
				ValidateIssuerName = jwtDiscoveryValidateIssuerName
			});
			var disco = await discoCache.GetAsync();

			if (disco == null)
			{
				Log.LogError($"Discovery response is null for Authority: {authority}, Discover Uri: {discoveryUri}.");
				return null;
			}

			if (disco.IsError)
			{
				Log.LogError(disco.Exception, $"Discovery response indicated error for Authority: {authority}, Discover Uri: {discoveryUri}. {disco.Error}");
				return null;
			}

			if (disco.KeySet == null)
			{
				Log.LogError($"Discovery response did not incldue keys for Authority: {authority}, Discover Uri: {discoveryUri}.");
				return null;
			}

			var user = jwt.ValidateJwtIdentityToken(authenticationSettings.nameClaimType,
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
				var cmy = DependencyResolver.Current.GetService<ICommunity>();

				cnName = await cmy.ReadSettingValueAsync<string>(companyId, Setting.JwtAuthority );

				if (string.IsNullOrEmpty(cnName))
				{
					cnName = Setting.JwtAuthority.AsInfoModel().DefaultValue;
				}

				// Stick in cache
				Cache.SetItem(key, cnName);
			}

			return cnName;
		}

		private async Task<Resource> parseLoginInfoAndClaims(int companyId, string firstName, string lastName, string userName, Dictionary<string, List<string>> groups)
		{
			Resource resource = null;
			CompanyResource companyResource = null;

			if (!string.IsNullOrEmpty(userName))
			{
				using (SqlConnection community = new SqlConnection(ConfigurationManager.AppSettings[constants.Setting.ReadWriteConnection]))
				{
					community.Open();
					resource = await community.QuerySingleOrDefaultAsync<Resource>("select * from Resource where username = @userName", new { userName });
					companyResource = await community.QuerySingleOrDefaultAsync<CompanyResource>("select * from CompanyResource where ResourceID = @resourceId and CompanyID = @companyId and [State] = @activeState"
						, new { resourceId = resource.ID, companyId, activeState = CompanyResourceState.Active });

					bool isValidResource = resource != null && companyResource != null && resource.ID > 0;

					if (isValidResource)
					{
						List<string> updateFields = new List<string>();
						bool updatedResource = false;

						if (!string.IsNullOrEmpty(firstName))
						{
							if (resource.FirstName != firstName)
							{
								updatedResource = true;
								updateFields.Add(" FirstName = @firstName ");
								resource.FirstName = firstName;
							}
						}

						if (!string.IsNullOrEmpty(lastName))
						{
							if (resource.LastName != lastName)
							{
								updatedResource = true;
								resource.LastName = lastName;
								updateFields.Add(" LastName = @lastName ");
							}
						}

						if (updatedResource)
						{
							await community.ExecuteAsync($"update [Resource] set {string.Join(", ", updateFields)} where ID = @resourceID", new { resourceId = resource.ID, firstName, lastName });
						}
					}
					else
					{
						return null;
					}

					using (SqlConnection company = new SqlConnection(CompanyConnectionUtils.GetCompanyConnectionString(companyId)))
					{
						company.Open();

						if (groups?.Any() == true)
						{
							var governHasGroups = (await company.QueryFirstOrDefaultAsync<Group>("select * from [Group] where IsActiveDirectoryGroup = 1")) != null;

							if (governHasGroups)
							{
								using (var trans = company.BeginTransaction())
								{
									try
									{
										SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.Default, trans);
										var dt = new DataTable();
										dt.Columns.Add("Name", typeof(string));
										dt.Columns.Add("Origin", typeof(string));

										foreach (var key in groups.Keys)
										{
											groups[key].ForEach(g =>
											{
												var row = dt.NewRow();
												row["Name"] = g.Trim();
												row["Origin"] = string.IsNullOrWhiteSpace(key) ? null : key;
												dt.Rows.Add(row);
											});
										}

										sqlBulkCopy.ColumnMappings.Add("Name", "Name");
										sqlBulkCopy.ColumnMappings.Add("Origin", "Origin");
										sqlBulkCopy.DestinationTableName = "#ADGroups";
										sqlBulkCopy.BulkCopyTimeout = 60;

										company.Execute(
											@"drop table if exists #ADGroups;
                                            create table #ADGroups ([Name] nvarchar(max), [Origin] nvarchar(10), GroupID int, HasResourceGroup bit);"
										, transaction: trans);

										sqlBulkCopy.WriteToServer(dt);

										company.Execute(
											@"update A
                                            set A.GroupID = G.ID,
                                            HasResourceGroup = case when RG.ResourceID is not null then 1 else null end
                                            from    #ADGroups A
                                                    inner join [Group] G on G.IsActiveDirectoryGroup = 1 and G.[Name] = A.[name]
                                                    left join ResourceGroup RG on RG.GroupID = G.ID and RG.ResourceID = @resourceId and (A.Origin is null or RG.Origin = A.Origin)

                                            insert into ResourceGroup (ResourceID, GroupID, Origin)
                                            select  @resourceId, GroupID, Origin
                                            from    #ADGroups 
                                            where   GroupID is not null and coalesce(HasResourceGroup, 0) = 0

                                            delete  R
                                            from    ResourceGroup R
                                                    inner join [Group] G on G.ID = R.GroupID and G.IsActiveDirectoryGroup = 1
                                            where   R.ResourceID = @resourceId and not exists (select 1 from #ADGroups where GroupID = R.GroupID and (Origin is null or Origin = R.Origin))"
										, new { resourceID = resource.ID }
										, transaction: trans);

										trans.Commit();

									}
									catch (Exception e)
									{
										try
										{
											if (trans != null)
											{
												trans.Rollback();
											}
										}
										catch
										{
											// Do nothing.
										}

										Log.LogError(e, $"Error logging user {resource.ID} in using their Jwt.");
									}
								}
							}
						}

					}
				}
			}

			return resource;
		}
	}
}