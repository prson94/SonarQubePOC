using d360.core.entities;
using d360.core.enums;
using d360.utils.company;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace igx.functions
{
	public class BaseFunction
    {
        public IConfiguration Config;
        public BaseFunction(IConfiguration config)
        {
            Config = config;
        }

        public EnvironmentLevel GetEnvironmentLevelCurrentSlot()
        {

            try
            {
                var environment = Config["Environment"];
                EnvironmentLevel lvl = EnvironmentLevel.Nightly;

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
                }

                return lvl;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<CompanyWithDatabaseServerSettings> GetCompaniesByCurrentSlot()
        {
            var lvl = GetEnvironmentLevelCurrentSlot();
            return CompanyConnectionUtils.GetCompaniesWithDatabaseServerSettings(Config["CommunityContext"]).Where(i => i.EnvironmentLevel == lvl).ToList();
        }

        public List<string> GetSearchServersByCurrentSlot()
        {
            var lvl = GetEnvironmentLevelCurrentSlot();
            return CompanyConnectionUtils
                .GetCompaniesWithDatabaseServerSettings(Config["CommunityContext"])
                .Where(i => i.EnvironmentLevel == lvl)
                .Select(s => s.SearchServer)
                .Distinct()
                .ToList();
        }

        public List<CompanyWithDatabaseServerSettings> GetCompaniesBySearchServer(string searchServer)
        {
			return CompanyConnectionUtils
				.GetCompaniesWithDatabaseServerSettings(Config["CommunityContext"])
				.Where(i => i.SearchServer == searchServer)
				.ToList();
        }
    }
}