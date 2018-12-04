using System.Configuration;

namespace d360.core
{
    public static class constants
    {
        public const string NAMESPACE = "http://data3sixty.com/schemas";

        public static string COMMUNITY_DATABASE_CONNECTION = ConfigurationManager.AppSettings["CommunityContext"];

        public static string AZURE_STORAGE_NAME = ConfigurationManager.AppSettings["AzureStorageName"];
        public static string AZURE_STORAGE_KEY = ConfigurationManager.AppSettings["AzureStorageKey"];

        public static string MANDRILL_API_KEY = ConfigurationManager.AppSettings["MandrillApiKey"];

        public static string COMPANY_ICON_FOLDER = "company-icons";
        public static string COMPANY_ICON_URL = $"https://{AZURE_STORAGE_NAME}.blob.core.windows.net/{COMPANY_ICON_FOLDER}/";

        public static string COMPANY_LOGO_FOLDER = "company-logos";
        public static string COMPANY_LOGO_URL = $"https://{AZURE_STORAGE_NAME}.blob.core.windows.net/{COMPANY_LOGO_FOLDER}/";

        public static string COMPANY_RESOURCES_FOLDER = "company-resources";
        public static string COMPANY_RESOURCES_URL = $"https://{AZURE_STORAGE_NAME}.blob.core.windows.net/{COMPANY_RESOURCES_FOLDER}/";

        public static string COMPANY_STYLES_FOLDER = "company-styles";
        public static string COMPANY_STYLES_URL = $"https://{AZURE_STORAGE_NAME}.blob.core.windows.net/{COMPANY_STYLES_FOLDER}/";

        public static string EVENTS_SERVICE_BUS = ConfigurationManager.AppSettings["EventServiceBus"];

        //azure container names
        public static string AZURE_CLOUD_FUSION_CONTAINER = "cloud-fusion-data";
    }
}
