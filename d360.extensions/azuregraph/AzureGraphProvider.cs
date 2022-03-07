using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace d360.extensions.azuregraph
{
    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> PatchAsync(this HttpClient client, Uri requestUri, HttpContent iContent)
        {
            var method = new HttpMethod("PATCH");
            var request = new HttpRequestMessage(method, requestUri)
            {
                Content = iContent
            };

            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                response = await client.SendAsync(request);
            }
            catch (TaskCanceledException e)
            {
                Debug.WriteLine("ERROR: " + e.ToString());
            }

            return response;
        }
    }

    public class MicrosoftGraphAccessToken
    {
        public string token_type { get; set; }

        public string expires_in { get; set; }

        public string scope { get; set; }

        public string expires_on { get; set; }

        public string not_before { get; set; }

        public string resource { get; set; }

        public string access_token { get; set; }
    }

    public class InvitedUser
    {
        public string givenName { get; set; }

        public string surname { get; set; }

        public string jobTitle { get; set; }
    }

    public class InvitedUserResultId
    {
        public string id { get; set; }
    }

    public class InvitedUserResult
    {
        public InvitedUserResultId invitedUser { get; set; }

        public string invitedUserDisplayName { get; set; }

        public string inviteRedeemUrl { get; set; }
    }

    public class UserInvitation
    {
        public string invitedUserEmailAddress { get; set; }

        public string inviteRedirectUrl { get; set; }

        public bool sendInvitationMessage { get; set; }

        public InvitedUser invitedUser { get; set; }
    }

    public class AzureGraphProvider
    {
        // Get an authenticated Microsoft Graph Service client.
        public static async Task<InvitedUserResult> CreateGuestAccount(string email, string firstName, string lastName, string title, string url, string tenantId, string clientId, string clientSecret)
        {
            string token = await GetAuthCode(tenantId, clientId, clientSecret);

            var inviteResponse = await InviteUser(token, email, url, false);

            if (inviteResponse != null && inviteResponse.invitedUser != null)
            {
                await UpdateUser(token, inviteResponse.invitedUser.id, firstName, lastName, title);
            }

            return inviteResponse;
        }

        private static async Task<string> GetAuthCode(string tenantId, string clientId, string clientSecret)
        {
            HttpClient client = new HttpClient();
            StringContent queryString = new StringContent($"grant_type=client_credentials&client_id={clientId}&client_secret={WebUtility.UrlEncode(clientSecret)}&resource=https://graph.microsoft.com", Encoding.UTF8, "application/x-www-form-urlencoded");

            HttpResponseMessage response = await client.PostAsync(new Uri($"https://login.microsoftonline.com/{tenantId}/oauth2/token"), queryString);

            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            var res = JsonConvert.DeserializeObject<MicrosoftGraphAccessToken>(responseBody);

            return res.access_token;
        }

        private static async Task UpdateUser(string token, string email, string firstName, string lastName, string title)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var invite = new InvitedUser
                {
                    givenName = firstName,
                    surname = lastName,
                    jobTitle = title
                };

                StringContent queryString = new StringContent(JsonConvert.SerializeObject(invite), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PatchAsync(new Uri($"https://graph.microsoft.com/v1.0/users/{email}"), queryString);

                response.EnsureSuccessStatusCode();
                _ = await response.Content.ReadAsStringAsync();
            }
        }

        private static async Task<InvitedUserResult> InviteUser(string token, string email, string url, bool sendInviteEmail = true)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var invite = new UserInvitation
                {
                    invitedUserEmailAddress = email,
                    inviteRedirectUrl = url,
                    sendInvitationMessage = sendInviteEmail
                };

                StringContent queryString = new StringContent(JsonConvert.SerializeObject(invite), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(new Uri($"https://graph.microsoft.com/beta/invitations"), queryString);

                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<InvitedUserResult>(responseBody);
            }
        }
    }
}
