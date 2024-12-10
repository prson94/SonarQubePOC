using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.utils.company;
using Dapper;
using Microsoft.Extensions.Configuration;
using repositories;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace igx.jobs
{
	public class BaseWebJob
	{
		public IConfiguration Configuration { get; private set; }
		
		public ICommunity Community;

		public BaseWebJob(ICommunity community, IConfiguration configuration)
		{
			Community = community;
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
	}
}
