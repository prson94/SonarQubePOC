using d360.core.entities;
using d360.core.enums;
using d360.extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using d360.core;

namespace d360.web.Models
{
	public class OidcDiscoveryCache
	{
		ICachingProvider Cache;
		public OidcDiscoveryCache(ICachingProvider cache)
		{
			Cache = cache;
		}

		public async Task<OidcDiscoveryDocument> GetDiscoverDocument(HttpClient client, string uri)
		{
			if (!uri.Contains(".well-known/openid-configuration"))
			{
				uri += "/.well-known/openid-configuration";
			}

			var cacheKey = $"Discovery_{uri.GetD3sHashString()}";
			var doc = Cache.GetItem<OidcDiscoveryDocument>(cacheKey);
			if (doc == null)
			{
				var result = await client.GetAsync(uri);
				doc = await result.Content.ReadAsAsync<OidcDiscoveryDocument>();
				Cache.SetItem(cacheKey, doc);
			}
			return doc;
		}
	}


	public class OidcDiscoveryDocument
	{
		public string issuer { get; set; }
		public string authorization_endpoint { get; set; }
		public string token_endpoint { get; set; }
		public string end_session_endpoint { get; set; }
		public string jwks_uri { get; set; }
		public List<string> response_modes_supported { get; set; }
		public List<string> response_types_supported { get; set; }
		public List<string> scopes_supported { get; set; }
		public List<string> subject_types_supported { get; set; }
		public List<string> id_token_signing_alg_values_supported { get; set; }
		public List<string> token_endpoint_auth_methods_supported { get; set; }
		public List<string> claims_supported { get; set; }
	}

	public class UserAuthentication
	{
		public Dictionary<string, string> CustomClaims { get; set; } = new Dictionary<string, string>();
		public string Email { get; set; }
		public string FirstName { get; set; }
		public Dictionary<string, List<string>> Groups { get; set; } = new Dictionary<string, List<string>>();
		public string LastName { get; set; }


		public void ParseClaims(
			List<ClaimMapping> claimMappings,
			List<System.Security.Claims.Claim> combinedClaims,
			JwtPayload payload)
		{
			var excludedProps = new List<string> { "amr", "aud", "at_hash", "auth_time", "cid", "exp", "iat", "idp", "iss", "jti", "name", "nonce", "preferred_username", "scp", "ver", "uid" };

			foreach (var claim in claimMappings)
			{
				var type = claim.Path.Replace("$.", "").Split('.')[0];

				var props = combinedClaims.Where(p => p.Type.ToLower() == type.ToLower());

				foreach (var prop in props)
				{
					string val = null;
					List<string> vals = new List<string>();
					JToken propToken = null;

					var childPath = "$." + claim.Path.Replace("$.", "").Replace(type, "");
					var isRootProperty = "$." == childPath;

					if (isRootProperty)
					{
						if (claim.IsArray)
						{
							if (payload.ContainsKey(type))
							{
								var propJsonString = JsonConvert.SerializeObject(payload[type]);

								propToken = JToken.Parse(propJsonString);
								vals = propToken?.Select(s => (string)s)?.ToList();
							}
						}
						else
						{
							val = prop.Value.ToString();
						}

					}
					else
					{
						if (payload.ContainsKey(type))
						{
							var propJsonString = JsonConvert.SerializeObject(payload[type]);

							propToken = JToken.Parse(propJsonString);

							if (claim.IsArray)
							{
								propToken = JToken.Parse(propJsonString);
								vals = propToken?.SelectToken(childPath, false)?.Select(s => (string)s)?.ToList();
							}
							else
							{
								val = propToken?.SelectToken(childPath, false)?.ToString() ?? "";
							}
						}
					}

					switch (claim.ClaimType)
					{
						case ClaimType.Email:
							if (!string.IsNullOrWhiteSpace(val))
							{
								Email = val;
							}
							break;
						case ClaimType.FirstName:
							if (!string.IsNullOrWhiteSpace(val))
							{
								FirstName = val;
							}
							break;
						case ClaimType.LastName:
							if (!string.IsNullOrWhiteSpace(val))
							{
								LastName = val;
							}
							break;
						case ClaimType.Groups:
							if (claim.IsArray)
							{
								if (vals?.Any() ?? false)
								{
									if (!Groups.ContainsKey(claim.PathHash))
									{
										Groups.Add(claim.PathHash, new List<string>());
									}
									Groups[claim.PathHash].AddRange(vals);
								}
							}
							else
							{
								if (!string.IsNullOrWhiteSpace(val))
								{
									if (!Groups.ContainsKey(claim.PathHash))
									{
										Groups.Add(claim.PathHash, new List<string>());
									}
									Groups[claim.PathHash].Add(val);
								}
							}

							break;
					}

					excludedProps.Add(prop.Type);
				}
			}

			foreach (var prop in combinedClaims)
			{
				if (!excludedProps.Contains(prop.Type) && !CustomClaims.ContainsKey(prop.Type))
				{
					CustomClaims.Add(prop.Type, prop.Value.ToString());
				}
			}
		}
	}
}