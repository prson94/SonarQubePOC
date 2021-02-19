using ApplicationInsights.Helpers.WebJobs;
using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.utils.company;
using Dapper;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Storage;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.Azure.WebJobs.Host;

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

        public static List<CompanyWithDatabaseServerSettings> GetCompaniesByCurrentSlot(bool FusionEnabledOnly = false)
        {

            try
            {
                var lvl = GetEnvironmentLevelCurrentSlot();
                if(FusionEnabledOnly)
                    return CompanyConnectionUtils.GetCompaniesWithDatabaseServerSettings().Where(i => i.EnvironmentLevel == lvl && i.IsFusionEnabled).ToList();
                else
                    return CompanyConnectionUtils.GetCompaniesWithDatabaseServerSettings().Where(i => i.EnvironmentLevel == lvl).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static List<int> UpdateRebuildRequestByCurrentSlot(CompanyRebuildJobToken jobToken)
        {
            var lvl = GetEnvironmentLevelCurrentSlot();
            try
            {
                return CompanyConnectionUtils.UpdateRebuildRequestForEnvironmentLevel(lvl, jobToken);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public class ConfigNameResolver : INameResolver
        {
            public string Resolve(string name)
            {
                return ConfigurationManager.AppSettings[name];
            }
        }

        public class ConnectionNameResolver : INameResolver
        {
            public string Resolve(string name)
            {
                return ConfigurationManager.ConnectionStrings[name].ConnectionString;
            }
        }

        public static IHost JobHostConfig(int? queueBatchSize = null, TimeSpan? queueVisibilityTimeout = null)
        {
            var builder = new HostBuilder();
            var env = GetConfigValueByKey("Environment");
            var configResolver = new ConfigNameResolver();


            builder
            .UseEnvironment(env)
            .ConfigureServices(s =>
            {
            })
            .ConfigureAppConfiguration((context, b) =>
            {
                b.AddConfiguration(context.Configuration)
                .AddJsonFile("commonAppSettings.json")
                .AddJsonFile("commonConnectionSettings.json")
                .AddEnvironmentVariables()
                .Build();

            })
            .ConfigureWebJobs(c =>
            {
                c.AddAzureStorageCoreServices();
                c.AddAzureStorage(a =>
                {
                    if (queueBatchSize.HasValue)
                    {
                        a.BatchSize = (int)queueBatchSize;
                    }

                    if (queueVisibilityTimeout.HasValue)
                    {
                        a.VisibilityTimeout = (TimeSpan)queueVisibilityTimeout;
                    }
                });
                c.AddServiceBus();
                c.AddTimers();
            })
            .ConfigureLogging((context, b) =>
            {
                b.AddConsole();

                string appInsightsKey = context.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
                if (!string.IsNullOrEmpty(appInsightsKey))
                {
                    b.AddApplicationInsights(appInsightsKey);
                }
            }).UseConsoleLifetime();

            return builder.Build();
        }
    }
}
