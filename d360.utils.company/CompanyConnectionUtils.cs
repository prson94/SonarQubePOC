using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using d360.core;
using System.Diagnostics;
using d360.core.entities;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace d360.utils.company
{
    public static class CompanyConnectionUtils
    {
        internal class CompanyDomainPrefixModel
        {
            public int ID { get; set; }
            public string UrlPrefix { get; set; }
        }
        public static Dictionary<int, string> GetCompanyDomainPrefixes()
        {
            var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);
            var companies = cnn.Query<CompanyDomainPrefixModel>("select C.ID, D.UrlPrefix from Company C inner join CompanyDomainSetting D on D.CompanyID = C.ID and C.Status = 'Active' and D.IsPrimary = 1").ToDictionary(k => k.ID, v => v.UrlPrefix);
            cnn.Close();
            cnn.Dispose();

            return companies;
        }

        public static string GetConnectionString(int id, string server, string username, string password)
        {
            return string.Format("server={0};Database=D3S_{1};User ID={2};Password={3};MultipleActiveResultSets=True;", server, id, username, password);
        }

        public static void ExecuteActionOnAllCompanies(string actionName, string sql, int timeout)
        {
            var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);
            var companies = cnn.Query<dynamic>("select C.ID, D.Server, D.Username, D.Password from Company C inner join DatabaseServer D on C.DatabaseServerID = D.ID and C.Status = 'Active'").ToList();
            cnn.Close();
            cnn.Dispose();

            companies.AsParallel().WithDegreeOfParallelism(1).ForAll(c =>
            {
                int companyID = c.ID;

                var connectionString = GetConnectionString(companyID, c.Server, c.Username, c.Password);
                var company = new SqlConnection(connectionString);

                try
                {
                    company.OpenWithRetry(RetryPolicy.DefaultProgressive);
                    company.Execute(sql, null, null, timeout);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("{0} : Exception occurred when on company {1}. Error is {2}", actionName, companyID, ex.Message + ((ex.InnerException != null) ? ex.InnerException.Message : ""));
                }
                finally
                {
                    if (company.State != System.Data.ConnectionState.Closed)
                        company.Close();

                    company.Dispose();
                }
            });
        }

        public static string GetCompanyConnectionString(int companyID)
        {
            var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);
            var db = cnn.Query<DatabaseServer>(
                @"select D.* from Company C inner join DatabaseServer D on D.ID = C.DatabaseServerID where C.ID = @id",
                new { id = companyID }
            ).SingleOrDefault();
            cnn.Close();
            cnn.Dispose();

            if (db != null)
            {
                return GetConnectionString(companyID, db.Server, db.Username, db.Password);
            }
            else
            {
                return null;
            }
        }

        public static SqlConnection GetCompanyConnection(int id, string server, string username, string password)
        {
            return new SqlConnection(GetConnectionString(id, server, username, password));
        }

        public static SqlConnection GetCompanyConnection(int companyID)
        {
            var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);
            var db = cnn.Query<DatabaseServer>(
                @"select D.* from Company C inner join DatabaseServer D on D.ID = C.DatabaseServerID where C.ID = @id",
                new { id = companyID }
            ).SingleOrDefault();
            cnn.Close();
            cnn.Dispose();

            if (db != null)
            {
                cnn = new SqlConnection(GetConnectionString(companyID, db.Server, db.Username, db.Password));
                db = null;
            }
            return cnn;
        }

        public static List<int> GetActiveCompanyIDs()
        {
            var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);
            var companies = cnn.Query<int>(@"select ID from Company where Status = 'Active'").ToList();
            cnn.Close();
            cnn.Dispose();

            return companies;
        }

        public static string GetEventTopicName(int companyID)
        {
            var topic = "";
            using(var community = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
            {
                community.OpenWithRetry(RetryPolicy.DefaultProgressive);
                var db = community.Query<DatabaseServer>(@"select D.* from Company C inner join DatabaseServer D on D.ID = C.DatabaseServerID where C.ID = @id", new { id = companyID }).SingleOrDefault();

                topic = db.EventTopic;
            }
            
            return topic;
        }

        public static bool IsCompanyDevelopmentEnvironment(int companyID)
        {
            var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);
            var IsDevelopment = cnn.Query<bool>(
                @"select DS.IsDevelopment from Company C inner join DatabaseServer DS on DS.ID = C.DatabaseServerID and C.ID = @id",
                new { id = companyID }
            ).Single();
            cnn.Close();
            cnn.Dispose();

            return IsDevelopment;
        }

        public static List<int> GetActiveDevelopmentCompanyIDs()
        {
            var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);
            var companies = cnn.Query<int>(@"select c.id from company c inner join databaseserver ds on (c.databaseserverid = ds.id and ds.IsDevelopment = 1 and c.[status] ='Active')").ToList();
            cnn.Close();
            cnn.Dispose();

            return companies;
        }

        public static List<CompanyWithDatabaseServerSettings> GetCompaniesWithDatabaseServerSettings()
        {
            var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);
            var companies = cnn.Query<CompanyWithDatabaseServerSettings>(@"
select  c.ID as CompanyID, 
        c.Status, 
        ds.Server, 
        ds.Username, 
        ds.Password, 
        ds.FusionQueue, 
        ds.SearchServer, 
        ds.EventTopic, 
        ds.IsDevelopment,
        c.EnvironmentLevel,
        CDS.UrlPrefix
from    company c 
        inner join databaseserver ds on c.databaseserverid = ds.id and c.Status = 'Active' 
        inner join CompanyDomainSetting CDS on CDS.CompanyID = c.ID and CDS.IsPrimary = 1").ToList();
            cnn.Close();
            cnn.Dispose();

            return companies;
        }
    }
}
