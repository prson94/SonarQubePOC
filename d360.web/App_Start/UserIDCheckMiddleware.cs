using d360.core;
using d360.core.entities;
using d360.extensions.caching;
using Dapper;
using Microsoft.Owin;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace d360.web
{
    public class UserIDCheckMiddleware
    {
        public class usercompany
        {
            public int ResourceID { get; set; }
            public int CompanyID { get; set; }
            public bool IsAdministrator { get; set; }


            public string Username { get; set; }
            public string Password { get; set; }
            public string APIPublicKey { get; set; }
            public string APIPrivateKey { get; set; }
            public string APIReadOnlyAccessToken { get; set; }
        }
        public class user
        {
            public user()
            {
                Companies = new List<usercompany>();
            }

            public int ID { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string APIPublicKey { get; set; }
            public string APIPrivateKey { get; set; }
            public string APIReadOnlyAccessToken { get; set; }
            public List<usercompany> Companies { get; set; }
        }


        public class JwtToken
        {
            [JsonProperty(PropertyName = "appid", NullValueHandling = NullValueHandling.Ignore, Order = 1)]
            public string Appid { get; set; }

            [JsonProperty(PropertyName = "aud", NullValueHandling = NullValueHandling.Ignore, Order = 2)]
            public string Aud { get; set; }

            [JsonProperty(PropertyName = "amr", NullValueHandling = NullValueHandling.Ignore, Order = 3)]
            public string[] Amr { get; set; }

            [JsonProperty(PropertyName = "given_name", NullValueHandling = NullValueHandling.Ignore, Order = 4)]
            public string Given_name { get; set; }

            [JsonProperty(PropertyName = "idp", NullValueHandling = NullValueHandling.Ignore, Order = 5)]
            public string Idp { get; set; }

            [JsonProperty(PropertyName = "iat", NullValueHandling = NullValueHandling.Ignore, Order = 6)]
            public long Iat { get; set; }

            [JsonProperty(PropertyName = "family_name", NullValueHandling = NullValueHandling.Ignore, Order = 7)]
            public string Family_name { get; set; }

            [JsonProperty(PropertyName = "unique_name", NullValueHandling = NullValueHandling.Ignore, Order = 8)]
            public string Unique_name { get; set; }

            [JsonProperty(PropertyName = "oid", NullValueHandling = NullValueHandling.Ignore, Order = 9)]
            public string Oid { get; set; }

            [JsonProperty(PropertyName = "sub", NullValueHandling = NullValueHandling.Ignore, Order = 10)]
            public string Sub { get; set; }

            [JsonProperty(PropertyName = "scp", NullValueHandling = NullValueHandling.Ignore, Order = 11)]
            public string Scp { get; set; }

            [JsonProperty(PropertyName = "nbf", NullValueHandling = NullValueHandling.Ignore, Order = 12)]
            public long Nbf { get; set; }

            [JsonProperty(PropertyName = "exp", NullValueHandling = NullValueHandling.Ignore, Order = 13)]
            public long Exp { get; set; }

            [JsonProperty(PropertyName = "upn", NullValueHandling = NullValueHandling.Ignore, Order = 14)]
            public string Upn { get; set; }

          //  [JsonProperty(PropertyName = "lmg_mpo", NullValueHandling = NullValueHandling.Ignore, Order = 15)]
        //    public LmgImo Lmg_mpo { get; set; }

            [JsonProperty(PropertyName = "lmg_cert_dn", NullValueHandling = NullValueHandling.Ignore, Order = 16)]
            public string Lmg_cert_dn { get; set; }

            [JsonProperty(PropertyName = "Iss", NullValueHandling = NullValueHandling.Ignore, Order = 17)]
            public string Iss { get; set; }
        }

        Func<IDictionary<string, object>, Task> _next;
        public UserIDCheckMiddleware(Func<IDictionary<string, object>, Task> next)
        {
            _next = next;
        }

        public List<usercompany> Users
        {
            get {
                var cache = new MemoryCachingProvider();// RedisCachingProvider();
                var users = cache.GetItem<List<usercompany>>("Users");
                if (users == null)
                {
                    users = new List<usercompany>();
                }
                return users;
            }
            set {
                var cache = new MemoryCachingProvider();// RedisCachingProvider();
                cache.SetItem("Users", value, true, 10);
            }
        }

        usercompany loadUserFromDatabase(int companyID, string apiKey = null, string apiSecret = null, string apiReadOnlyKey = null, string username = null)
        {
            usercompany u = null;

            using (var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
            {
                cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);
                var baseSql = @"
select	C.*,
        R.APIPrivateKey,
        R.APIPublicKey,
        R.APIReadOnlyAccessToken,
        R.Username,
        R.Password
from	Resource R
		inner join CompanyResource C on C.ResourceID = R.ID and C.CompanyID = @com";

                if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
                {
                    u = cnn.Query<usercompany>(baseSql + @" and R.APIPublicKey = @pub and R.APIPrivateKey = @pri", new { com = companyID, pri = apiSecret, pub = apiKey }).FirstOrDefault();
                }
                else if (!string.IsNullOrEmpty(apiReadOnlyKey))
                {
                    u = cnn.Query<usercompany>(baseSql + @" and R.APIReadOnlyAccessToken = @token", new { com = companyID, token = apiReadOnlyKey }).FirstOrDefault();
                }
                else if (!string.IsNullOrEmpty(username))
                {
                    u = cnn.Query<usercompany>(baseSql + @" and lower(ltrim(rtrim(R.Username))) = @username", new { com = companyID, username }).FirstOrDefault();
                }

                cnn.Close();
            }

            return u;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);

            usercompany u = null;

            var companyID = context.Get<int>("CompanyID");

            var apiCredentials = context.Request.Headers["Authorization"];            
            var token = string.Empty;

            var cachedUsers = Users;
            
            // llyods custom auth depends on ceriticate and JWT token
            if (!string.IsNullOrEmpty(apiCredentials) && apiCredentials.ToUpper().Contains("BEARER"))
            {
                // load the certificate info                
                var certificateCommonName = await getCertificateCommonName(context);
                
                //validate the certificate first
                if (!ValidateX509IfRequired(context, certificateCommonName)) return;  // end pipeline if no valid cert with error code set within

                // certificate is valid at this point we just get the user from the jwt token
                var authParts = apiCredentials.Split(' ');

                if (authParts.Length == 2)
                {
                    var jwtToken = authParts[1];
                    
                    var tokenParts = jwtToken.Split('.');

                    if (tokenParts.Length != 2 || tokenParts.Length != 3) // element 0 is head and 1 is payload
                    {

                        var jwtPayloadString = tokenParts[1];

                        if (!string.IsNullOrEmpty(jwtPayloadString))
                        {
                            
                            var decodeJwtPayload = DecodeToken(jwtPayloadString);
                            
                            var claimToken = Newtonsoft.Json.JsonConvert.DeserializeObject<JwtToken>(decodeJwtPayload);

                            // if the token in expired please leave
                            if (!ValidateTokenLifeTime(context, claimToken)) return;

                            if (claimToken != null && !string.IsNullOrEmpty(claimToken.Upn))
                            {
                                u = loadUserFromDatabase(companyID, null, null, null, claimToken.Upn);
                                if (u != null)
                                {
                                    cachedUsers.Add(u);
                                    Users = cachedUsers;
                                }
                            }
                        }
                    }
                }
            }
            else if (!string.IsNullOrEmpty(apiCredentials))
            {
                var authValues = apiCredentials.Split(';');
                if (authValues.Length == 2)
                {
                    u = cachedUsers.FirstOrDefault(i => i.CompanyID == companyID && i.APIPrivateKey == authValues[1] && i.APIPublicKey == authValues[0]);
                    if (u == null)
                    {
                        u = loadUserFromDatabase(companyID, apiKey: authValues[0], apiSecret: authValues[1]);
                        if (u != null)
                        {
                            cachedUsers.Add(u);
                            Users = cachedUsers;
                        }
                    }
                }
            }
            else
            {
                var keyPair = context.Request.Query.FirstOrDefault(i => i.Key == "oauth2_access_token");
                if (keyPair.Value != null)
                {
                    token = keyPair.Value.First();
                }
                else
                {
                    keyPair = context.Request.Query.FirstOrDefault(i => i.Key == "key");
                    if (keyPair.Value != null)
                    {
                        token = keyPair.Value.First();
                    }
                }

                if (!string.IsNullOrEmpty(token))
                {
                    u = cachedUsers.FirstOrDefault(i => i.CompanyID == companyID && i.APIReadOnlyAccessToken == token);
                    if (u == null)
                    {
                        u = loadUserFromDatabase(companyID, apiReadOnlyKey: token);
                        if (u != null)
                        {
                            cachedUsers.Add(u);
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
                    u = loadUserFromDatabase(companyID, username: context.Request.User.Identity.Name.ToLower());
                    if (u != null)
                    {
                        cachedUsers.Add(u);
                        Users = cachedUsers;
                    }
                }
            }

            if (u != null)
            {
                context.Set("IsAdministrator", u.IsAdministrator);
                context.Set("ResourceID", u.ResourceID);
                context.Request.User = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(u.ResourceID.ToString(), "ID"), null);
            }
            else
            {
                if (!string.IsNullOrEmpty(apiCredentials))          Trace.TraceWarning("Could not locate the user with API credentials of: {0}", apiCredentials);
                if (!string.IsNullOrEmpty(token))                   Trace.TraceWarning("Could not locate the user with API token of: {0}", token);
                if (context.Request.User.Identity.IsAuthenticated)  Trace.TraceWarning("Could not locate the user with name of: {0}", context.Request.User.Identity.Name);
                if (!string.IsNullOrEmpty(apiCredentials) || !string.IsNullOrEmpty(token) || context.Request.User.Identity.IsAuthenticated) return;
            }

            await _next.Invoke(environment);
        }

        private bool ValidateTokenLifeTime(IOwinContext context, JwtToken claimToken)
        {
            //if(LifetimeValidator(token.Nbf, token.Exp))
            //Add code to validate the lifetime of the token.
            
            if(claimToken.Exp < DateTimeOffset.Now.ToUnixTimeSeconds())
            {
                context.Response.StatusCode = 403; // token expired
                return false;
            }

            return true;
        }

        private async Task<string> getCertificateCommonName(IOwinContext context)
        {
            var sCn = "CustomApiClientCertificateCommonName";
            var companyId = context.Get<int>("CompanyID");
            var cnName = "";
            var key = $"CustomApiClientCertificateCommonName{companyId}";

            // try the cache
            var cache = new MemoryCachingProvider();            
            if (cache != null)
            {
                cnName = cache.GetItem<string>(key);
            }

            // not in cache query community
            if (string.IsNullOrEmpty(cnName))
            {
                using (var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
                {
                    cnName = (await cnn.QueryAsync<string>(@"select coalesce(C.Value, S.DefaultValue) as Value
from Setting S left join CompanySetting C on C.SettingID = S.ID and C.CompanyID = 4
where S.ID = 54")).FirstOrDefault();
                }

                //stick in cache
                if (cache != null)
                {
                    cache.SetItem<string>(key, cnName);
                }
            }


            return cnName;
        }

        public static string DecodeToken(string encodedToken)
        {
            byte[] data = Convert.FromBase64String(encodedToken);
            string decodedString = Encoding.UTF8.GetString(data);
            return decodedString;
        }

        #region X509 Check

        public bool ValidateX509IfRequired(IOwinContext context, string certificateCommonName)
        {
            // Do something with context near the beginning of request processing.
            X509Certificate2 clientCertificate = null;
            try
            {
                //***** 1. Get a Client Certificate from the Http context or http header ****/
                //   clientCertificate = context.Request.c.ClientCertificate;
                
                if (System.Web.HttpContext.Current != null && System.Web.HttpContext.Current.Request != null && System.Web.HttpContext.Current.Request.ClientCertificate != null && System.Web.HttpContext.Current.Request.ClientCertificate.Count > 0) {
                    clientCertificate = new X509Certificate2(System.Web.HttpContext.Current.Request.ClientCertificate.Certificate);
                }
                
                if (clientCertificate == null)
                {
                    var clientCertificateInHeader = context.Request.Headers["X-ARR-ClientCert"];
                    if (!string.IsNullOrEmpty(clientCertificateInHeader))
                    {
                        var clientCertBytes = Convert.FromBase64String(clientCertificateInHeader);
                        clientCertificate = new X509Certificate2(clientCertBytes);
                    }
                }

                if (clientCertificate != null) //***** 2. Perform clientCertificate null validation check ****/
                {
                    try
                    {
                        //***** 3. If the clientCertificate is not null, then Validate the incoming certificate CN with the allowed CN ****/
                        var isValidCert = IsValidClientCertificate(clientCertificate, certificateCommonName);


                        if (!isValidCert)  //***** 4. If it is a valid Certificate, then invoke next middleware ****/
                        //***** 5. Return 403 if it is not a valid Certificate ****/
                        {
                            //Do your logging if required.

                            //Stop the pipeline here.
                            context.Response.StatusCode = 403;
                            return false;
                        }
                    }
                    catch (Exception ex) //***** 6. Any exception throw 403??? Not sure, this is valid case ****/
                    {
                        //Do your logging here.

                        //What to do with exceptions in middleware?
                        context.Response.WriteAsync(ex.Message);
                        context.Response.StatusCode = 403;
                        return false;
                    }
                }
                else //***** 7. If the clientCertificate is null, then return 403 status code ****
                {
                    //Do your logging here.

                    context.Response.StatusCode = 403;
                    return false;
                }

            }
            finally
            {
                clientCertificate?.Dispose();
            }

            return true;

        }

        private bool IsValidClientCertificate(X509Certificate2 certificate, string certificateCommonName)
        {
            var commonName = certificate.GetNameInfo(X509NameType.SimpleName, false);

            if (string.IsNullOrEmpty(commonName)) return false;

            //Compare the incoming Certificate CN with the allowed Certificate CN.
            return commonName.Equals(certificateCommonName);
        }

        #endregion

    }
}