using d360.core.entities;
using d360.utils.company;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;


namespace igx.function
{
    public static class CoreFunction
    {
        #region AppInsights

        static TelemetryClient _AITelemetryClient;
        public static TelemetryClient AITelemetryClient
        {
            get
            {
                if (_AITelemetryClient == null)
                {
                    TelemetryConfiguration.Active.InstrumentationKey = "2dd165d7-28b2-4258-8b55-32d9c83a3f43";

                    _AITelemetryClient = new TelemetryClient();
                }
                return _AITelemetryClient;
            }
        }

        public static void AITrackTrace(string jobName, string message, IDictionary<string, string> properties = null, int? companyId = null)
        {
            if (properties == null)
                properties = new Dictionary<string, string>();

            properties["Function"] = jobName;
            if (companyId.HasValue) properties["CompanyId"] = companyId.Value.ToString();
            properties["Environment"] = ConfigurationManager.AppSettings["Environment"];

            AITelemetryClient.TrackTrace(message, properties);
        }

        public static void AITrackEvent(string jobName, string eventName, IDictionary<string, string> properties = null, int? companyId = null, IDictionary<string, double> metrics = null)
        {
            if (properties == null)
                properties = new Dictionary<string, string>();

            properties["Function"] = jobName;
            if (companyId.HasValue) properties["CompanyId"] = companyId.Value.ToString();
            properties["Environment"] = ConfigurationManager.AppSettings["Environment"];

            if (metrics != null)
            {
                foreach (var k in metrics)
                {
                    properties.Add(k.Key, k.Value.ToString());
                }
            }

            AITelemetryClient.TrackEvent(eventName, properties);
        }

        public static void AITrackException(string jobName, Exception e, int? companyId = null)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties["Function"] = jobName;
            properties["Environment"] = ConfigurationManager.AppSettings["Environment"];

            if (companyId.HasValue) properties["CompanyId"] = companyId.Value.ToString();
            AITelemetryClient.TrackException(e, properties);

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
            return ConfigurationManager.AppSettings[name].ToString();
        }

        public static List<CompanyWithDatabaseServerSettings> GetCompaniesByCurrentSlot()
        {

            try
            {
                var companies = CompanyConnectionUtils.GetCompaniesWithDatabaseServerSettings();
                var environment = ConfigurationManager.AppSettings["Environment"];

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
                }

                return companies;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
