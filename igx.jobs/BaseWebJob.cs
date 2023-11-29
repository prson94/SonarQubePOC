using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.utils.company;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace igx.jobs
{
	public class BaseWebJob
	{
		public IConfiguration Configuration { get; private set; }
		private string ConnString { get { return Configuration["CommunityContext"]; } }

		public BaseWebJob(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public EnvironmentLevel GetEnvironmentLevelCurrentSlot()
		{
			var environment = Configuration["Environment"];
			EnvironmentLevel lvl;

			switch (environment)
			{
				case "NIGHTLY":
					lvl = EnvironmentLevel.Nightly;
					break;
				case "CLIENTDEV":
					lvl = EnvironmentLevel.Development;
					break;
				case "UAT":
					lvl = EnvironmentLevel.UAT;
					break;
				case "PROD":
					lvl = EnvironmentLevel.Production;
					break;
				default:
					lvl = EnvironmentLevel.Nightly;
					break;
			}

			return lvl;
		}

		public List<CompanyWithDatabaseServerSettings> GetCompaniesByCurrentSlot()
		{
			var lvl = GetEnvironmentLevelCurrentSlot();
			return CompanyConnectionUtils.GetCompaniesWithDatabaseServerSettings(ConnString).Where(i => i.EnvironmentLevel == lvl).ToList();
		}

		public string GetCompanyConnectionString(int companyID)
		{
			string communityConnectionString = Configuration["CommunityContext"];
			string connectionString = "";

			using (var cnn = new SqlConnection(communityConnectionString))
			{
				if (cnn.State != System.Data.ConnectionState.Open)
				{
					cnn.Open();
				}

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

		public List<string> GetSearchServersByCurrentSlot()
		{
			var lvl = GetEnvironmentLevelCurrentSlot();
			return CompanyConnectionUtils
				.GetCompaniesWithDatabaseServerSettings(ConnString)
				.Where(i => i.EnvironmentLevel == lvl)
				.Select(s => s.SearchServer)
				.Distinct()
				.ToList();
		}

		public List<CompanyWithDatabaseServerSettings> GetCompaniesBySearchServer(string searchServer)
		{
			return CompanyConnectionUtils.GetCompaniesWithDatabaseServerSettings(ConnString).Where(i => i.SearchServer == searchServer).ToList();
		}
	}
}
