using d360.core;
using d360.core.entities;
using d360.extensions.caching;
using Dapper;
using Microsoft.Owin;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
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

            if (!string.IsNullOrEmpty(apiCredentials))
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
    }
}