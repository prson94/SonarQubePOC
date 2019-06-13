using System.Net.Http;

namespace igx.IntegrationTests.Core
{
    public class BaseIntegrationTestClass
    {
        protected HttpClient httpClient;
        public BaseIntegrationTestClass()
        {
            httpClient = new HttpClient();
            var authValue = Settings.ApiKey + ";" + Settings.ApiSecret;
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authValue);

        }
    }
}
