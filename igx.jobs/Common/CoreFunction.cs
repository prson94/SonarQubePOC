using d360.core.entities;
using d360.utils.company;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;


namespace igx.jobs
{
	public static class CoreFunction
    {
        #region AppInsights

        static string _AppInsightsInstrumentationKey = null;
        public static string AppInsightsInstrumentationKey(string key)
        {
            if (string.IsNullOrEmpty(_AppInsightsInstrumentationKey))
            {
                _AppInsightsInstrumentationKey = key;
            }
            return _AppInsightsInstrumentationKey;
        }


        static TelemetryClient _AITelemetryClient;
        public static TelemetryClient AITelemetryClient
        {
            get
            {
                if (_AITelemetryClient == null)
                {
                    var key = CloudConfigurationManager.GetSetting("APPINSIGHTS_INSTRUMENTATIONKEY");

                    if (string.IsNullOrEmpty(key)) key = "2dd165d7-28b2-4258-8b55-32d9c83a3f43";

                    TelemetryConfiguration.Active.InstrumentationKey = AppInsightsInstrumentationKey(key);

                    _AITelemetryClient = new TelemetryClient();
                }
                return _AITelemetryClient;
            }
        }

        public static void AIFlush()
        {
            AITelemetryClient.Flush();
        }

        #endregion

        public static string GetConfigValueByKey(string name)
        {
            return CloudConfigurationManager.GetSetting(name);
        }

        public static d360.core.enums.EnvironmentLevel GetEnvironmentLevelCurrentSlot()
        {

            try
            {
                var environment = GetConfigValueByKey("Environment");
                d360.core.enums.EnvironmentLevel lvl = d360.core.enums.EnvironmentLevel.Nightly;

                switch (environment)
                {
                    case "NIGHTLY":
                        lvl = d360.core.enums.EnvironmentLevel.Nightly;
                        break;
                    case "CLIENTDEV":
                        lvl = d360.core.enums.EnvironmentLevel.Development;
                        break;
                    case "UAT":
                        lvl = d360.core.enums.EnvironmentLevel.UAT;
                        break;
                    case "PROD":
                        lvl = d360.core.enums.EnvironmentLevel.Production;
                        break;                    
                }

                return lvl;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static List<CompanyWithDatabaseServerSettings> GetCompaniesByCurrentSlot()
        {

            try
            {
                var lvl = GetEnvironmentLevelCurrentSlot();
                return CompanyConnectionUtils.GetCompaniesWithDatabaseServerSettings().Where(i => i.EnvironmentLevel == lvl).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static IHostBuilder JobHostConfigBuilder()
        {
            var builder = new HostBuilder();
            var env = GetConfigValueByKey("Environment");
			var logLevel = LogLevel.Warning;

			var configLogLevel = GetConfigValueByKey("LogLevel");
			if (!string.IsNullOrEmpty(configLogLevel))
			{ 
				Enum.TryParse<LogLevel>(configLogLevel, out logLevel);
			}

            builder
            .UseEnvironment(env)
            .ConfigureWebJobs(c =>
            {
                c.AddAzureStorageCoreServices()
                .AddAzureStorage();
            })
            .ConfigureAppConfiguration((context, b) =>
            {
                b.AddConfiguration(context.Configuration)
                .AddEnvironmentVariables()
                .Build();
            })
            .ConfigureLogging((context, b) =>
            {
				b.SetMinimumLevel(logLevel);
#if DEBUG
				b.AddConsole();
#endif
				string appInsightsKey = context.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
                if (!string.IsNullOrEmpty(appInsightsKey))
                {
                    b.AddApplicationInsights(appInsightsKey);
                }
            })
            .UseConsoleLifetime();

            return builder;
        }
    }
}
