using d360.core;
using d360.core.entities;
using d360.extensions.caching;
using Dapper;
using Microsoft.Owin;
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
            public int CompanyID { get; set; }
            public bool IsAdministrator { get; set; }
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

        async Task<List<user>> loadCache()
        {
            var key = "Users";
            var cache = new MemoryCachingProvider();// RedisCachingProvider();
            var users = cache.GetItem<List<user>>(key);

            if (users == null)
            {
                var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
                cnn.Open();
                users = (await cnn.QueryAsync<user>("select ID, lower(ltrim(rtrim(Username))) as Username, Password, APIPublicKey, APIPrivateKey, APIReadOnlyAccessToken from Resource")).ToList();
                var usercompanies = (await cnn.QueryAsync<CompanyResource>("select * from CompanyResource")).ToList();
                cnn.Close();
                cnn.Dispose();

                users.ForEach(u =>
                {
                    u.Companies.AddRange(
                        usercompanies
                        .Where(i => i.ResourceID == u.ID).Select(i => new usercompany {
                            CompanyID = i.CompanyID,
                            IsAdministrator = i.IsAdministrator
                        })
                    );
                });
                usercompanies = null;

                cache.SetItem(key, users, true, 5);
            }
            return users;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);
            
            var users = await loadCache();
            user u = null;
            usercompany uc = null;

            var companyID = context.Get<int>("CompanyID");

            var apiCredentials = context.Request.Headers["Authorization"];
            var token = string.Empty;

            if (!string.IsNullOrEmpty(apiCredentials))
            {
                var authValues = apiCredentials.Split(';');
                if (authValues.Length == 2)
                {
                    u = users.SingleOrDefault(i => i.APIPublicKey == authValues[0] && i.APIPrivateKey == authValues[1]);
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
                    u = users.SingleOrDefault(i => i.APIReadOnlyAccessToken == token);
                }
            }

            if (context.Request.User.Identity.IsAuthenticated)
            {
                u = users.SingleOrDefault(i => i.Username == context.Request.User.Identity.Name.ToLower());
            }


            if (u != null)
            {
                uc = u.Companies.SingleOrDefault(i => i.CompanyID == companyID);
                if (uc != null)
                {
                    context.Set<bool>("IsAdministrator", uc.IsAdministrator);
                    context.Set<int>("ResourceID", u.ID);
                    context.Request.User = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(u.ID.ToString(), "ID"), null);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(apiCredentials))          Trace.TraceWarning("Could not locate the user with API credentials of: {0}", apiCredentials);
                if (!string.IsNullOrEmpty(token))                   Trace.TraceWarning("Could not locate the user with API token of: {0}", token);
                if (context.Request.User.Identity.IsAuthenticated)  Trace.TraceWarning("Could not locate the user with name of: {0}", context.Request.User.Identity.Name);
                if (!string.IsNullOrEmpty(apiCredentials) || !string.IsNullOrEmpty(token) || context.Request.User.Identity.IsAuthenticated) return;
                //context.Response.Write("User Not Found For Company");
            }

            await _next.Invoke(environment);
        }
    }
}