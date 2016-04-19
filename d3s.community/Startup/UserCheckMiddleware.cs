using d3s.community.core;
using Dapper;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace d3s.community.startup
{
    public class UserCheckMiddleware
    {
        private class UserCompany
        {
            public int CompanyID { get; set; }
            public bool IsAdministrator { get; set; }
        }
        private class User
        {
            public int ID { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string APIPublicKey { get; set; }
            public string APIPrivateKey { get; set; }
            public string APIReadOnlyAccessToken { get; set; }
            public List<UserCompany> Companies { get; set; } = new List<UserCompany>();
        }

        public readonly RequestDelegate next;
        public readonly IHostingEnvironment env;

        public UserCheckMiddleware(RequestDelegate next, IHostingEnvironment env)
        {
            this.next = next;
            this.env = env;
        }

        public async Task Invoke(HttpContext context)
        {
            string companyID = context.Request.Headers["CompanyID"];
            string apiCredentials = context.Request.Headers["Authorization"];
            var token = string.Empty;


            User user = null;
            UserCompany userCompany = null;


            var cnn = new SqlConnection(Constants.COMMUNITY_DATABASE_CONNECTION);
            var users = cnn.Query<User>(@"select 
                                            ID, 
                                            lower(ltrim(rtrim(Username))) as Username,
                                            Password,
                                            APIPublicKey,
                                            APIPrivateKey,
                                            APIReadOnlyAccessToken
                                         from Resource").ToList();
            var userCompanies = cnn.Query<dynamic>("select * from CompanyResource").ToList();
            cnn.Close();
            cnn.Dispose();

            users.ForEach(u =>
            {
                u.Companies.AddRange(
                    userCompanies
                    .Where(i => i.ResourceID == u.ID).Select(i => new UserCompany
                    {
                        CompanyID = i.CompanyID,
                        IsAdministrator = i.IsAdministrator
                    })
                );
            });

            if (!string.IsNullOrEmpty(apiCredentials))
            {
                var authValues = apiCredentials.Split(';');
                if (authValues.Length == 2)
                {
                    user = users.SingleOrDefault(i => i.APIPublicKey == authValues[0] && i.APIPrivateKey == authValues[1]);
                }
            }
            else
            {
                var keyPair = context.Request.Query.FirstOrDefault(i => i.Key == "oauth2_access_token");
                if (!string.IsNullOrEmpty(keyPair.Value))
                    token = keyPair.Value.First();
                else
                {
                    keyPair = context.Request.Query.FirstOrDefault(i => i.Key == "key");
                    if (!string.IsNullOrEmpty(keyPair.Value))
                        token = keyPair.Value.First();
                }

                if (!string.IsNullOrEmpty(token))
                    user = users.SingleOrDefault(i => i.APIReadOnlyAccessToken == token);
            }

            if (context.User.Identity.IsAuthenticated)
            {
                user = users.SingleOrDefault(i => i.Username == context.User.Identity.Name.ToLower());
            }


            if (user != null)
            {
                userCompany = user.Companies.SingleOrDefault(i => i.CompanyID.ToString() == companyID);
                if (userCompany != null)
                {
                    if (!context.Request.Headers.ContainsKey("IsAdministrator"))
                        context.Request.Headers.Add("IsAdministrator", userCompany.IsAdministrator.ToString());
                    else
                        context.Request.Headers["IsAdministrator"] = userCompany.IsAdministrator.ToString();

                    if (!context.Request.Headers.ContainsKey("ResourceID"))
                        context.Request.Headers.Add("ResourceID", user.ID.ToString());
                    else
                        context.Request.Headers["ResourceID"] = user.ID.ToString();

                    context.User = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(user.ID.ToString(), "ID"), null);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(apiCredentials)) Trace.TraceWarning($"Could not locate the user with API credentials of: {apiCredentials}");
                if (!string.IsNullOrEmpty(token)) Trace.TraceWarning($"Could not locate the user with API token of: {token}");
                if (context.User.Identity.IsAuthenticated) Trace.TraceWarning($"Could not locate the user with name of: {context.User.Identity.Name}");
                if (!string.IsNullOrEmpty(apiCredentials) || !string.IsNullOrEmpty(token) || context.User.Identity.IsAuthenticated) return;
            }

            await next.Invoke(context);
        }
    }
}
