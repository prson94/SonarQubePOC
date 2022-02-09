
using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.utils.company;
using Dapper;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;


namespace igx.jobs
{
    public static class ConnectionExtensions
    {
        public static void ProcessTask(this SqlConnection company, TextWriter log, string functionName, int companyID, string sql, int timeout = 1400)
        {
            bool processStatus = false;
            var processTask = company.ExecuteAsync(sql, commandTimeout: 1400);
            processTask.ContinueWith(t =>
            {
                string exceptionData = "";
                if (t.Exception != null)
                {
                    exceptionData = t.Exception.GetFullExceptionData();
                    if (t.Exception.InnerExceptions != null)
                    {
                        foreach (var ex in t.Exception.InnerExceptions)
                        {
                            exceptionData += ex.GetFullExceptionData();
                        }
                    }
                    CoreFunction.AITrackException(functionName, t.Exception, companyID);
                }

                if (t.IsCompleted)
                {
                    if (t.IsFaulted)
                    {
                        CoreFunction.AITrackException(functionName, t.Exception, companyID);
                    }
                }

                processStatus = false;
            });

            while (processStatus && (processTask.Exception == null))
            {
                log.WriteLine("Processing scores for company {0}...", companyID);
                System.Threading.Thread.Sleep(30000);
            }
        }
    }

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

        private static Dictionary<string, string> buildPropertiesToLog(string jobName, IDictionary<string, string> properties = null, int? companyId = null)
        {
            var propsToSend = new Dictionary<string, string> {
                { "Environment", ConfigurationManager.AppSettings["Environment"] },
                { "Function", jobName }
            };
            if (companyId.HasValue) propsToSend["CompanyID"] = companyId.Value.ToString();

            if (properties != null)
            {
                foreach (var key in properties.Keys)
                {
                    if (!propsToSend.ContainsKey(key) && !string.IsNullOrEmpty(properties[key]))
                    {
                        propsToSend.Add(key, properties[key]);
                    }
                }
            }

            return propsToSend;
        }

        public static void AITrackTrace(string jobName, string message, IDictionary<string, string> properties = null, int? companyId = null)
        {
            var propsToSend = buildPropertiesToLog(jobName, properties, companyId);
            AITelemetryClient.TrackTrace(message, propsToSend);
        }

        public static void AITrackEvent(string jobName, string eventName, IDictionary<string, string> properties = null, int? companyId = null, IDictionary<string, double> metrics = null)
        {
            var propsToSend = buildPropertiesToLog(jobName, properties, companyId);
            AITelemetryClient.TrackEvent(eventName, propsToSend, metrics);
        }

        public static void AITrackException(string jobName, Exception e, int? companyId = null, IDictionary<string, string> properties = null)
        {
            var propsToSend = buildPropertiesToLog(jobName, properties, companyId);
            AITelemetryClient.TrackException(e, propsToSend);
            AIFlush();
        }

        public static void AITrackJobCompletedNoErrors(string jobName)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties["Function"] = jobName;
            properties["Environment"] = ConfigurationManager.AppSettings["Environment"];

            AITelemetryClient.TrackEvent("Function Completed Successfully", properties);

            AIFlush();
        }

        public static void AITrackJobStart(string name)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties["Function"] = name;
            properties["Environment"] = ConfigurationManager.AppSettings["Environment"];

            AITelemetryClient.TrackEvent("Function Started", properties);
        }

        public static void AITrackRequest(string name, TimeSpan elapsedTime)
        {
            AITelemetryClient.TrackRequest(name, DateTime.Now, elapsedTime, "", true);
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
#if !DEBUG
               b.SetMinimumLevel(LogLevel.Warning); // turn off trace messages
#endif
                b.AddConsole();                

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
