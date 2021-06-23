using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using d360.core;
using d360.core.entities;
using d360.core.enums;

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

        public static List<CompanyWithDatabaseServerSettings> GetCompaniesWithDatabaseServerSettings()
        {
            List<CompanyWithDatabaseServerSettings> companies = null;
            using (var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
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

        public static List<CompanySetting> GetCompanySettings(int companyID)
        {
            using (var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
            {
                cnn.Open();

                return cnn.Query<CompanySetting>(@"
                    select 
                        @companyID as CompanyID, 
                        S.ID as SettingID, 
                        coalesce(CS.Value, S.DefaultValue) as Value
                    from Setting S 
                    left join CompanySetting CS on CS.CompanyID = @companyID and CS.SettingID = S.ID", new { companyID }).ToList();
            }
        }


        public static List<int> UpdateRebuildRequestForEnvironmentLevel(EnvironmentLevel level, CompanyRebuildJobToken jobToken)
        {
            List<int> companies = null;
            int timeoutInHours = 18;
            if (int.TryParse(constants.V2_ENVIRONMENT_JOB_REBUILD_TIMEOUT_IN_HOURS, out int timeout))
            {
                timeoutInHours = timeout;
            }

            using (var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
            {
                cnn.Open();
                
                companies = cnn.Query<int>(@"
declare @ids table (CompanyID int)

merge	CompanyRebuildJobStatus as T
using	(
		select  C.ID as CompanyID
		from    Company C
		where   C.EnvironmentLevel = @level
				and C.[Status] = 'Active'
		) as S
on		(T.CompanyID = S.CompanyID and T.JobToken = @jobToken) 
when	matched and (T.[State] = 2 OR T.LastStartedOn <= @timeoutOn) then
update	set 
		T.[State] = 1,
		T.LastStartedOn = getutcdate(),
		T.LastStartedBy = 0
when	not matched by target then
insert	(CompanyID, JobToken, LastStartedOn, LastStartedBy, [State])
values	(S.CompanyID, @jobToken, getutcdate(), 0, 1)
output inserted.CompanyID into @ids;

select CompanyID from @ids", new { level = (int)level, jobToken = (int)jobToken, timeoutOn = DateTime.UtcNow.AddHours(-1*timeoutInHours) }).ToList();
            }

            return companies;
        }
    }
}
