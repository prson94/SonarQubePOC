using System.Configuration;

namespace d360.core
{
    public static class constants
    {
        public const string NAMESPACE = "http://data3sixty.com/schemas";

        public static readonly string COMMUNITY_DATABASE_CONNECTION = ConfigurationManager.AppSettings["CommunityContext"];

        public static readonly string V2_ENVIRONMENT_JOB_REBUILD_TIMEOUT_IN_HOURS = ConfigurationManager.AppSettings["V2EnvironmentJobRebuildTimeoutInHours"];

        private static readonly string AZURE_STORAGE_NAME = ConfigurationManager.AppSettings["AzureStorageName"];

        public static readonly string COMPANY_ICON_FOLDER = "company-icons";
        public static readonly string COMPANY_ICON_URL = $"https://{AZURE_STORAGE_NAME}.blob.core.windows.net/{COMPANY_ICON_FOLDER}/";

        public static readonly string COMPANY_LOGO_FOLDER = "company-logos";
        public static readonly string COMPANY_LOGO_URL = $"https://{AZURE_STORAGE_NAME}.blob.core.windows.net/{COMPANY_LOGO_FOLDER}/";

        public static readonly string COMPANY_RESOURCES_FOLDER = "company-resources";
        public static readonly string COMPANY_RESOURCES_URL = $"https://{AZURE_STORAGE_NAME}.blob.core.windows.net/{COMPANY_RESOURCES_FOLDER}/";

        public static readonly string COMPANY_STYLES_FOLDER = "company-styles";
        public static readonly string COMPANY_BULK_LOAD_FOLDER = "bulk-loads";
        public static readonly string COMPANY_STYLES_URL = $"https://{AZURE_STORAGE_NAME}.blob.core.windows.net/{COMPANY_STYLES_FOLDER}/";

        public static readonly int ERROR_MESSAGE_CHARACTER_LIMIT = 2000;

        public static readonly string MAIL_API_KEY = "MandrillApiKey";
        public static readonly string MAIL_SUB_ACCOUNT = "MandrillSubAccount";
        public static readonly string BUS_TOPIC_NAME = "EventBusTopicName";
        public static readonly string BUS_CONNECTION = "EventServiceBus";
        public static readonly string QUEUE_CONNECTION = "AzureWebJobsQueueStorageAccount";
        public static readonly string STORAGE_CONNECTION = "AzureStorageConnectionString";
        public static readonly string REDIS_CONNECTION = "RedisCacheConnectionString";

        public const string TITLE_PREFIX = "Data360";
        public const string COMPANY = "Precisely.";
        public const string PRODUCT = "Data360 Govern";
        public const string COPYRIGHT = "Copyright © Precisely. 2021";
        public const string PRODUCT_VERSION = "2021.11.05.*";
        public const string PRODUCT_VERSION_NOREVISION = "2021.11.05";

    }
}
