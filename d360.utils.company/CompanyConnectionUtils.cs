using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using d360.core;
using d360.core.entities;
using d360.core.enums;
using System.Configuration;

namespace d360.utils.company
{
    public static class CompanyConnectionUtils
    {
        public static string GetConnectionString(int id, string server, string username, string password)
        {
            return CompanyConnectionStringHelper.ConnectionString(id, server, username, password);
        }

        public static string GetCompanyConnectionString(int companyID)
        {
            string connectionString = "";
            using (var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
            {
                if (cnn.State != System.Data.ConnectionState.Open)
                    cnn.Open();

                var company = cnn.Query<dynamic>(
                    @"select  ds.Server, ds.Username, ds.Password from company c inner join databaseserver ds on c.databaseserverid = ds.id and c.Id = @companyID",
                    new { companyID }
                ).FirstOrDefault();

                if (company != null)
                {
                    connectionString = CompanyConnectionStringHelper.ConnectionString(companyID, company.Server, company.Username, company.Password);
                }
            }

            return connectionString;
        }

        public static SqlConnection GetCompanyConnection(int id, string server, string username, string password)
        {
            return new SqlConnection(GetConnectionString(id, server, username, password));
        }

        public static SqlConnection GetCompanyConnection(int companyID)
        {
            using (var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
            {
                cnn.Open();
                var db = cnn.Query<DatabaseServer>(
                    @"select D.* from Company C inner join DatabaseServer D on D.ID = C.DatabaseServerID where C.ID = @id",
                    new { id = companyID }
                ).SingleOrDefault();

                if (db == null) throw new Exception("Invalid company id or database server id.  Cannot load server information.");

                return new SqlConnection(GetConnectionString(companyID, db.Server, db.Username, db.Password));
            }
        }

        public static SqlConnection GetCompanyConnection(int companyID, string connectionString)
        {
            using (var cnn = new SqlConnection(connectionString))
            {
                cnn.Open();
                var db = cnn.Query<DatabaseServer>(
                    @"select D.* from Company C inner join DatabaseServer D on D.ID = C.DatabaseServerID where C.ID = @id",
                    new { id = companyID }
                ).SingleOrDefault();

                if (db == null) throw new Exception("Invalid company id or database server id.  Cannot load server information.");

                return new SqlConnection(GetConnectionString(companyID, db.Server, db.Username, db.Password));
            }
        }
        public static string GetEventTopicName(int companyID)
        {
            return ConfigurationManager.AppSettings["EventBusTopicName"].ToString();
        }

        public static List<CompanyWithDatabaseServerSettings> GetCompaniesWithDatabaseServerSettings(string connectionString)
        {
            List<CompanyWithDatabaseServerSettings> companies = null;
            using (var cnn = new SqlConnection(connectionString))
            {
                cnn.Open();
                companies = cnn.Query<CompanyWithDatabaseServerSettings>(@"
                    select  c.ID as CompanyID, 
                            c.ClientID,
                            ds.Server, 
                            ds.Username, 
                            ds.Password,                             
                            ds.SearchServer, 
                            ds.EventTopic, 
                            c.EnvironmentLevel,
                            CDS.UrlPrefix,
                            c.Priority		
                    from    company c 
                            inner join databaseserver ds on c.databaseserverid = ds.id and c.Status = 'Active' 
                            inner join CompanyDomainSetting CDS on CDS.CompanyID = c.ID and CDS.IsPrimary = 1").ToList();
            }

            return companies;
        }

        public static List<CompanyWithDatabaseServerSettings> GetCompaniesWithDatabaseServerSettings()
        {
            return GetCompaniesWithDatabaseServerSettings(constants.COMMUNITY_DATABASE_CONNECTION);
        }
    }
}
