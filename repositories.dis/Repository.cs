using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;

namespace repositories.dis
{
	public abstract class Repository
	{
		public Platform Platform { get { return Platform.Dis; } }

		internal class TokenResponse
		{
			public string access_token { get; set; }
		}

		internal async Task<string> GetServiceJwt()
		{
			var url = "https://auth-dev.cloud.precisely.services/auth/realms/Precisely/protocol/openid-connect/token";
			var client = new HttpClient();
			var content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("client_id", "OIDC-DIS-GOVERN"),
				new KeyValuePair<string, string>("grant_type", "password"),
				new KeyValuePair<string, string>("scope", "openid"),
				new KeyValuePair<string, string>("username", "dsn1_apiuiauto_tnt204@mailinator.com"),
				new KeyValuePair<string, string>("password", "Password@1234")
			});
			var response = await client.PostAsync(url, content);

			var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
			return token.access_token;
		}

		internal async Task<T> Get_PayloadFromService<T>(string url)
		{
			var client = new HttpClient();
			var jwt = await GetServiceJwt();
			//client.DefaultRequestHeaders.Add("X-WorkspaceId", "abcxyz");
			//client.DefaultRequestHeaders.Add("X-UserId", "joe@seph.com");
			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

			var response = await client.GetFromJsonAsync<T>(url);
			return response;
		}
	}
}
