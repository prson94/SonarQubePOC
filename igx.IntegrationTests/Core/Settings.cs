using System;
using System.Configuration;

namespace igx.IntegrationTests.Core
{
    public class Settings
    {
        public static string Host
        {
            get
            {
                var hostURI = ConfigurationManager.AppSettings["HostURI"];

                if (string.IsNullOrEmpty(hostURI))
                    throw new Exception("Host URL must be set in appSettings section in app.config");

                if (!IsValidURI(hostURI))
                    throw new Exception("Invalid URI format!");

                return hostURI;
            }
        }

        public static string ApiKey
        {
            get
            {
                var apiKey = ConfigurationManager.AppSettings["ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                    throw new Exception("ApiKey must be set in appSettings section in app.config");
                return apiKey;
            }
        }

        public static string ApiSecret
        {
            get
            {
                var apiSecret = ConfigurationManager.AppSettings["ApiSecret"];
                if (string.IsNullOrEmpty(apiSecret))
                    throw new Exception("ApiKey must be set in appSettings section in app.config");
                return apiSecret;
            }
        }
        private static bool IsValidURI(string uri)
        {
            if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                return false;
            Uri tmp;
            if (!Uri.TryCreate(uri, UriKind.Absolute, out tmp))
                return false;
            return tmp.Scheme == Uri.UriSchemeHttp || tmp.Scheme == Uri.UriSchemeHttps;
        }

    }
}
