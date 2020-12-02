using System.Configuration;

namespace d360.core
{
    public static class constants
    {
        public const string NAMESPACE = "http://data3sixty.com/schemas";

        public static string COMMUNITY_DATABASE_CONNECTION = ConfigurationManager.AppSettings["CommunityContext"];

        public static string V2_ENVIRONMENT_JOB_REBUILD_TIMEOUT_IN_HOURS = ConfigurationManager.AppSettings["V2EnvironmentJobRebuildTimeoutInHours"];

        private static string AZURE_STORAGE_NAME = ConfigurationManager.AppSettings["AzureStorageName"];

        public static string COMPANY_ICON_FOLDER = "company-icons";
        public static string COMPANY_ICON_URL = $"https://{AZURE_STORAGE_NAME}.blob.core.windows.net/{COMPANY_ICON_FOLDER}/";

        public static string COMPANY_LOGO_FOLDER = "company-logos";
        public static string COMPANY_LOGO_URL = $"https://{AZURE_STORAGE_NAME}.blob.core.windows.net/{COMPANY_LOGO_FOLDER}/";

        public static string COMPANY_RESOURCES_FOLDER = "company-resources";
        public static string COMPANY_RESOURCES_URL = $"https://{AZURE_STORAGE_NAME}.blob.core.windows.net/{COMPANY_RESOURCES_FOLDER}/";

        public static string COMPANY_STYLES_FOLDER = "company-styles";
        public static string COMPANY_BULK_LOAD_FOLDER = "bulk-loads";
        public static string COMPANY_STYLES_URL = $"https://{AZURE_STORAGE_NAME}.blob.core.windows.net/{COMPANY_STYLES_FOLDER}/";

        //azure container names
        public static string AZURE_CLOUD_FUSION_CONTAINER = "cloud-fusion-data";

        public const string TITLE_PREFIX = "Data360";
        public const string COMPANY = "Infogix, Inc.";
        public const string PRODUCT = "Data360 Govern";
        public const string COPYRIGHT = "Copyright © Infogix, Inc. 2020";
        public const string PRODUCT_VERSION = "2021.01.22.*";
        public const string PRODUCT_VERSION_NOREVISION = "2021.01.22";

    }
}
