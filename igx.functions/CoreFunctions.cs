using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.utils.company;
using Dapper;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace igx.functions
{

    public class CoreFunction
    {
        private readonly IConfiguration Configuration;
        public CoreFunction(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        #region AppInsights

        string _AppInsightsInstrumentationKey;
        public string AppInsightsInstrumentationKey(string key)
        {
            if (string.IsNullOrEmpty(_AppInsightsInstrumentationKey))
            {
                _AppInsightsInstrumentationKey = key;
            }
            return _AppInsightsInstrumentationKey;
        }


        TelemetryClient _AITelemetryClient;
        public TelemetryClient AITelemetryClient
        {
            get
            {
                if (_AITelemetryClient == null)
                {
                    var key = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");

                    if (string.IsNullOrEmpty(key)) key = "2dd165d7-28b2-4258-8b55-32d9c83a3f43";

                    TelemetryConfiguration.Active.InstrumentationKey = AppInsightsInstrumentationKey(key);

                    _AITelemetryClient = new TelemetryClient();
                }
                return _AITelemetryClient;
            }
        }

        private Dictionary<string, string> buildPropertiesToLog(string jobName, IDictionary<string, string> properties = null, int? companyId = null)
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

        public void AITrackTrace(string jobName, string message, IDictionary<string, string> properties = null, int? companyId = null)
        {
            var propsToSend = buildPropertiesToLog(jobName, properties, companyId);
            AITelemetryClient.TrackTrace(message, propsToSend);
        }

        public void AITrackEvent(string jobName, string eventName, IDictionary<string, string> properties = null, int? companyId = null, IDictionary<string, double> metrics = null)
        {
            var propsToSend = buildPropertiesToLog(jobName, properties, companyId);
            AITelemetryClient.TrackEvent(eventName, propsToSend, metrics);
        }

        public void AITrackException(string jobName, Exception e, int? companyId = null, IDictionary<string, string> properties = null)
        {
            var propsToSend = buildPropertiesToLog(jobName, properties, companyId);
            AITelemetryClient.TrackException(e, propsToSend);
            AIFlush();
        }

        public void AITrackJobCompletedNoErrors(string jobName)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties["Function"] = jobName;
            properties["Environment"] = ConfigurationManager.AppSettings["Environment"];

            AITelemetryClient.TrackEvent("Function Completed Successfully", properties);

            AIFlush();
        }

        public void AITrackJobStart(string name)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties["Function"] = name;
            properties["Environment"] = ConfigurationManager.AppSettings["Environment"];

            AITelemetryClient.TrackEvent("Function Started", properties);
        }

        public void AITrackRequest(string name, TimeSpan elapsedTime)
        {
            AITelemetryClient.TrackRequest(name, DateTime.Now, elapsedTime, "", true);
        }

        public void AIFlush()
        {
            AITelemetryClient.Flush();
        }

        #endregion

        public T GetConfigValueByKey<T>(string name)
        {
            return Configuration.GetValue<T>(name);
        }

        public string GetConnectionString(string name)
        {
            return Configuration.GetConnectionString(name);
        }

        public d360.core.enums.EnvironmentLevel GetEnvironmentLevelCurrentSlot()
        {

            try
            {
                var environment = GetConfigValueByKey<string>("Environment");
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

        public List<CompanyWithDatabaseServerSettings> GetCompaniesByCurrentSlot()
        {

            try
            {
                var lvl = GetEnvironmentLevelCurrentSlot();
                return CompanyConnectionUtils.GetCompaniesWithDatabaseServerSettings(GetConnectionString("CommunityContext")).Where(i => i.EnvironmentLevel == lvl).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}