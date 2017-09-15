using d360.core.entities;
using d360.utils.company;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;


namespace igx.functions.Core
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

        public static void AITrackEvent(string eventName, IDictionary<string, string> properties = null, int? companyId = null)
        {
            if (properties == null)
                properties = new Dictionary<string, string>();

            if (companyId.HasValue) properties["CompanyId"] = companyId.Value.ToString();
            properties["Environment"] = ConfigurationManager.AppSettings["Environment"];

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
                        companies = companies.Where(i => i.CompanyID == 4).ToList();
                        break;
                    case "CLIENTDEV":
                        companies = companies.Where(i => i.IsDevelopment && i.CompanyID != 4).ToList();
                        break;
                    case "PROD":
                        companies = companies.Where(i => !i.IsDevelopment).ToList();
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
