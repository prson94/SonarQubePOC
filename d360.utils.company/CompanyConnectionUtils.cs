using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using d360.core;
using System.Diagnostics;
using d360.core.entities;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System.Configuration;

namespace d360.utils.company
{
    public static class CompanyConnectionUtils
    {
        public static string GetConnectionString(int id, string server, string username, string password)
        {
            return string.Format("server={0};Database=D3S_{1};User ID={2};Password={3};MultipleActiveResultSets=True;", server, id, username, password);
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

        public static string GetEventTopicName(int companyID)
        {
            return ConfigurationManager.AppSettings["EventBusTopicName"].ToString();
        }

        public static List<CompanyWithDatabaseServerSettings> GetCompaniesWithDatabaseServerSettings()
        {
            var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);
            var companies = cnn.Query<CompanyWithDatabaseServerSettings>(@"
select  c.ID as CompanyID, 
        c.ClientID,
        c.Status, 
        ds.Server, 
        ds.Username, 
        ds.Password, 
        ds.FusionQueue, 
        ds.SearchServer, 
        ds.EventTopic, 
        ds.IsDevelopment,
        c.EnvironmentLevel,
        CDS.UrlPrefix,
        c.Priority
from    company c 
        inner join databaseserver ds on c.databaseserverid = ds.id and c.Status = 'Active' 
        inner join CompanyDomainSetting CDS on CDS.CompanyID = c.ID and CDS.IsPrimary = 1").ToList();
            cnn.Close();
            cnn.Dispose();

            return companies;
        }

        public static List<CompanySetting> GetCompanySettings(int companyID)
        {
            var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

            var settings = cnn.Query<CompanySetting>(@"
            select 
                @companyID as CompanyID, 
                S.ID as SettingID, 
                coalesce(CS.Value, S.DefaultValue) as Value
            from Setting S 
            left join CompanySetting CS on CS.CompanyID = @companyID and CS.SettingID = S.ID", new { companyID }).ToList();
            return settings;
        }
    }
}
