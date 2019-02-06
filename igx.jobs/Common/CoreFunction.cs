using ApplicationInsights.Helpers.WebJobs;
using d360.core;
using d360.core.entities;
using d360.utils.company;
using Dapper;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace igx.jobs
{
    public class WebJob
    {
        public string Environment { get; set; }
        public string Name { get; set; }
        public DateTime? LockedOn { get; set; }
        public string LockingServer { get; set; }
    }

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

        public static List<CompanyWithDatabaseServerSettings> GetCompaniesByCurrentSlot()
        {

            try
            {
                var companies = CompanyConnectionUtils.GetCompaniesWithDatabaseServerSettings();
                var environment = GetConfigValueByKey("Environment");

                switch (environment)
                {
                    case "NIGHTLY":
                        companies = companies.Where(i => i.EnvironmentLevel == d360.core.enums.EnvironmentLevel.Nightly).ToList();
                        break;
                    case "CLIENTDEV":
                        companies = companies.Where(i => i.EnvironmentLevel == d360.core.enums.EnvironmentLevel.Development).ToList();
                        break;
                    case "UAT":
                        companies = companies.Where(i => i.EnvironmentLevel == d360.core.enums.EnvironmentLevel.UAT).ToList();
                        break;
                    case "PROD":
                        companies = companies.Where(i => i.EnvironmentLevel == d360.core.enums.EnvironmentLevel.Production).ToList();
                        break;
                    case "ALTERNATE":
                        companies = companies.Where(i => i.EnvironmentLevel == d360.core.enums.EnvironmentLevel.Alternate).ToList();
                        break;
                }

                return companies;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static JobHostConfiguration GetJobHostConfiguration()
        {
            var config = new JobHostConfiguration
            {
                DashboardConnectionString = CoreFunction.GetConfigValueByKey("WebJobsAccount"),
                StorageConnectionString = CoreFunction.GetConfigValueByKey("WebJobsAccount"),
                NameResolver = new QueueNameResolver()
            };

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }

            config.UseApplicationInsights();
            config.UseCore();

            return config;
        }

        public static bool LockWebJobIfAvailable(string name)
        {
            var available = false;

            SqlConnection cnn = null;

            try
            {
                var environment = ConfigurationManager.AppSettings["Environment"];
                var machine = System.Environment.MachineName;

                cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
                cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

                var job = cnn.Query<WebJob>("select * from WebJob where Environment = @e and Name = @n", new { e = environment, n = name }).SingleOrDefault();
                if (job == null)
                {
                    job = new WebJob { Environment = environment, Name = name, LockedOn = DateTime.UtcNow, LockingServer = machine };
                    var recordCount = cnn.Execute("insert into WebJob (Environment, Name, LockedOn, LockingServer) values (@Environment, @Name, @LockedOn, @LockingServer)", job);
                    if (recordCount > 0)
                    {
                        available = true;
                    }
                }
                else
                {
                    bool executeUpdate = true;
                    if (job.LockedOn.HasValue)
                    {
                        if (job.LockedOn.Value > DateTime.UtcNow.AddHours(-20)) //Last webjob execution is less than 20 hours old, let's give that bad boy some time to do its thing!
                        {
                            executeUpdate = false;
                        }
                    }

                    if (executeUpdate)
                    {
                        job.LockedOn = DateTime.UtcNow;
                        job.LockingServer = machine;
                        var recordCount = cnn.Execute("update WebJob set LockedOn = @LockedOn, LockingServer = @LockingServer where Environment = @Environment and Name = @Name", job);
                        if (recordCount > 0)
                        {
                            available = true;
                        }
                    }
                }
            }
            catch
            {
            }
            finally
            {
                if (cnn != null)
                {
                    cnn.Dispose();
                    cnn = null;
                }
            }

            return available;
        }

        public static bool UnlockWebJob(string name)
        {
            var unlocked = false;

            SqlConnection cnn = null;

            try
            {
                var environment = ConfigurationManager.AppSettings["Environment"];
                var machine = System.Environment.MachineName;

                cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
                cnn.OpenWithRetry(RetryPolicy.DefaultProgressive);

                var job = cnn.Query<WebJob>("select * from WebJob where Environment = @e and Name = @n and LockingServer = @s", new { e = environment, n = name, s = machine }).SingleOrDefault();
                if (job != null)
                {
                    var recordCount = cnn.Execute("update WebJob set LockedOn = null, LockingServer = null where Environment = @Environment and Name = @Name and LockingServer = @LockingServer", job);
                    if (recordCount > 0)
                    {
                        unlocked = true;
                    }
                }
            }
            catch
            {
            }
            finally
            {
                if (cnn != null)
                {
                    cnn.Dispose();
                    cnn = null;
                }
            }

            return unlocked;
        }
    }
}
