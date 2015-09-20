using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Security.Cryptography;
using System.Xml.Linq;
using System.Configuration;
using System.IO;
using d360.core;
using System.Net;

namespace d360.api.client
{
    public class Client
    {
        public Guid CompanyID { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public string RelativeUrl{ get; set; }

        string baseUrl = ConfigurationManager.AppSettings["D360BaseUrl"].ToString();

        private long GetServerEpoch()
        {
            long value = 0;
            try
            {
                HttpStatusCode code;
                string message;
                value = this.Get<long>(out code, out message, false, getFullUrl("epoch"));
            }
            catch
            {
            }
            return value;
        }

        public Client(string url, Guid companyID, string key, string secret)
        {
            RelativeUrl = url;
            ApiKey = key;
            ApiSecret = secret;
            CompanyID = companyID;
        }

        internal HttpClient CreateAnonymousClient()
        {
            HttpClient client = new HttpClient { MaxResponseContentBufferSize = 10000000, Timeout = TimeSpan.FromMinutes(10) };

            return client;
        }
        internal HttpClient CreateAuthenticatedClient()
        {
            HttpClient client = new HttpClient { MaxResponseContentBufferSize = 10000000, Timeout = TimeSpan.FromMinutes(10) };
            var hash = new SHA256Managed();

            long epochValue = GetServerEpoch();

            string correctHash = ApiSecret + epochValue.ToString();
            byte[] unhashedBytes = Encoding.ASCII.GetBytes(correctHash);
            byte[] hashedBytes = hash.ComputeHash(unhashedBytes);
            correctHash = Convert.ToBase64String(hashedBytes);
            //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(string.Empty, string.Format("{0};{1};{2}", CompanyID.ToString(), ApiKey, correctHash));
            client.DefaultRequestHeaders.AddWithoutValidation("X-Authorization", string.Format("{0};{1};{2}", CompanyID.ToString(), ApiKey, correctHash));
            return client;
        }

        #region Http Methods

        public void Delete(out HttpStatusCode status, out string description, bool requiresAuthentication = true, string url = "")
        {
            var client = (requiresAuthentication) ? CreateAuthenticatedClient() : CreateAnonymousClient();
            if (string.IsNullOrEmpty(url))
                url = getFullUrl();
            var response = client.DeleteAsync(url).Result;

            status = response.StatusCode;
            description = response.ReasonPhrase;

            response.Dispose();
            client.Dispose();
        }

        public string GetAsString(out HttpStatusCode status, out string description, bool requiresAuthentication = true, string url = "")
        {
            var client = (requiresAuthentication) ? CreateAuthenticatedClient() : CreateAnonymousClient();
            HttpResponseMessage response = null;
            string str = string.Empty;

            status = HttpStatusCode.SeeOther;
            description = string.Empty;

            try
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                response = client.GetAsync(getFullUrl()).Result;

                status = response.StatusCode;
                description = response.ReasonPhrase;

                if (response.IsSuccessStatusCode)
                {
                    str = response.Content.ReadAsStringAsync().Result;
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                response.Dispose();
                client.Dispose();
            }

            return str;
        }

        public T Get<T>(out HttpStatusCode status, out string description, bool requiresAuthentication = true, string url = "")
        {
            var client = (requiresAuthentication) ? CreateAuthenticatedClient() : CreateAnonymousClient();
            HttpResponseMessage response = null;
            T obj = default(T);

            status = HttpStatusCode.SeeOther;
            description = string.Empty;

            try
            {
                if (string.IsNullOrEmpty(url))
                    url = getFullUrl();
                response = client.GetAsync(url).Result;

                status = response.StatusCode;
                description = response.ReasonPhrase;

                if (response.IsSuccessStatusCode)
                {
                    obj = response.Content.ReadAsAsync<T>().Result;
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                response.Dispose();
                client.Dispose();
            }

            return obj;
        }

        public T Post<T>(T obj, out HttpStatusCode status, out string description, bool requiresAuthentication = true, string url = "")
        {
            var client = (requiresAuthentication) ? CreateAuthenticatedClient() : CreateAnonymousClient();

            HttpResponseMessage response = null;

            status = HttpStatusCode.SeeOther;
            description = string.Empty;

            try
            {
                if (string.IsNullOrEmpty(url))
                    url = getFullUrl();
                response = client.PostAsync(
                                      url,
                                      new ObjectContent<T>(obj, JsonMediaTypeFormatter.DefaultMediaType)
                                      ).Result;

                status = response.StatusCode;
                description = response.ReasonPhrase;

                if (status == HttpStatusCode.Created)
                {
                    obj = response.Content.ReadAsAsync<T>().Result;
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (response != null) response.Dispose();
                if (client != null) client.Dispose();
            }

            return obj;
        }

        public T Put<T>(T obj, out HttpStatusCode status, out string description, bool requiresAuthentication = true, string url = "")
        {
            var client = (requiresAuthentication) ? CreateAuthenticatedClient() : CreateAnonymousClient();
            HttpResponseMessage response = null;

            status = HttpStatusCode.SeeOther;
            description = string.Empty;

            try
            {
                if (string.IsNullOrEmpty(url))
                    url = getFullUrl();
                response = client.PutAsync(
                                     url,
                                     new ObjectContent<T>(obj, JsonMediaTypeFormatter.DefaultMediaType)
                                     ).Result;

                status = response.StatusCode;
                description = response.ReasonPhrase;

                if (status == HttpStatusCode.Accepted)
                {
                    obj = response.Content.ReadAsAsync<T>().Result;
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                response.Dispose();
                client.Dispose();
            }

            return obj;
        }

        #endregion

        private string getFullUrl(string relativeUri = "")
        {
            if (string.IsNullOrEmpty(relativeUri)) 
                relativeUri = RelativeUrl;
            string str = baseUrl + "/" + relativeUri;
            return str;
        }
    }

}
