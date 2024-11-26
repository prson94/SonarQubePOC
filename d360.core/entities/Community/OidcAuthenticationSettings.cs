using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace d360.core.entities
{
	public class OidcAuthenticationSettings
	{
		public string type { get; set; }

		public string baseUri { get; set; }

		public string discoveryUri { get; set; }

		public string jwtAuthorityUri { get; set; }

		public string clientId { get; set; }

		public string clientSecret { get; set; }

		public string audience { get; set; }

		public string nameClaimType { get; set; }


		public string scopesJson { get; set; }
		public List<string> scopes { get { return JsonConvert.DeserializeObject<List<string>>(scopesJson ?? "[]"); } }

		public string extraParametersJson { get; set; }
		public JObject extraParameters { get { return JObject.Parse(extraParametersJson ?? "{}"); } }
	}
}
