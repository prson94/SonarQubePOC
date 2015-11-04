using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using d360.core;
using Dapper;
using d360.core.entities;
using System.Data.SqlClient;
using d360.extensions.caching;

namespace d360.jobs.queue.ProcessRecache
{
    public class cd
    {
        public int CompanyID { get; set; }
        public string UrlPrefix { get; set; }
    }

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

    public class CacheStatus
    {
        public string Name { get; set; }
        public bool ShouldReCache { get; set; }
    }
    class Program: FunctionsBase
    {
        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(constants.WEBJOBS_STORAGE_CONNECTION));

            var mex = new List<Exception>();

            try
            {
                var ctx = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
                ctx.Open();

                var caches = ctx.Query<CacheStatus>("select * from CacheStatus where ShouldReCache = 1").ToList();
                var cache = new RedisCachingProvider();
                caches.ForEach(c =>
                {
                    switch (c.Name)
                    {
                        case "Users":
                            var users = ctx.Query<user>("select ID, Username, Password, APIPublicKey, APIPrivateKey, APIReadOnlyAccessToken from Resource").ToList();
                            var usercompanies = ctx.Query<CompanyResource>("select * from CompanyResource").ToList();

                            users.ForEach(u =>
                            {
                                u.Companies.AddRange(
                                    usercompanies
                                    .Where(i => i.ResourceID == u.ID).Select(i => new usercompany
                                    {
                                        CompanyID = i.CompanyID,
                                        IsAdministrator = i.IsAdministrator
                                    })
                                );
                            });
                            usercompanies = null;

                            cache.SetItem(c.Name, users, true, 15);
                            break;
                        case "CompanyPrefixes":
                            var dict = ctx.Query<cd>("select CompanyID, UrlPrefix from CompanyDomainSetting").ToDictionary(k => k.UrlPrefix, v => v.CompanyID);
                            cache.SetItem(c.Name, dict, true, 15);
                            break;
                    }

                    ctx.Execute("update CacheStatus set ShouldReCache = 0 where Name = @n", new { n = c.Name });
                });

                ctx.Close();
                ctx.Dispose();
            }
            catch (Exception ex)
            {
                var msg = ex.Message + ((ex.InnerException != null) ? "  " + ex.InnerException.Message : "");
                Console.WriteLine(msg);
            }

            if (mex.Count > 0) throw new AggregateException("One or more exceptions occurred", mex);
        }
    }
}
