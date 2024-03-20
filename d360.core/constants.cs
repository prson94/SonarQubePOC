using System.Configuration;

namespace d360.core
{
    public static class constants
    {
        public const string NAMESPACE = "http://data3sixty.com/schemas";
        public static readonly string COMMUNITYDB_APPSETTING = "CommunityContext";
        public static readonly int V2_ENVIRONMENT_JOB_REBUILD_TIMEOUT_IN_HOURS = int.Parse(ConfigurationManager.AppSettings["V2EnvironmentJobRebuildTimeoutInHours"]);
        private static readonly string AZURE_STORAGE_NAME = ConfigurationManager.AppSettings["AzureStorageName"];
        public static readonly string COMPANY_ICON_FOLDER = "company-icons";
        public static readonly string COMPANY_ICON_URL = $"https://{AZURE_STORAGE_NAME}.blob.core.windows.net/{COMPANY_ICON_FOLDER}/";
        public static readonly string COMPANY_LOGO_FOLDER = "company-logos";
        public static readonly string COMPANY_LOGO_URL = $"https://{AZURE_STORAGE_NAME}.blob.core.windows.net/{COMPANY_LOGO_FOLDER}/";
        public static readonly string COMPANY_RESOURCES_FOLDER = "company-resources";
        public static readonly string COMPANY_RESOURCES_URL = $"https://{AZURE_STORAGE_NAME}.blob.core.windows.net/{COMPANY_RESOURCES_FOLDER}/";
        public static readonly string COMPANY_STYLES_FOLDER = "company-styles";
        public static readonly string COMPANY_BULK_LOAD_FOLDER = "bulk-loads";
        public static readonly int COMPANY_BULK_LOAD_MAX_ROWS = int.Parse(ConfigurationManager.AppSettings["BulkLoadMaxRows"]);
        public static readonly string COMPANY_STYLES_URL = $"https://{AZURE_STORAGE_NAME}.blob.core.windows.net/{COMPANY_STYLES_FOLDER}/";
        public static readonly int ERROR_MESSAGE_CHARACTER_LIMIT = 2000;
        public const string TITLE_PREFIX = "Data360";
        public const string COMPANY = "Precisely.";
        public const string PRODUCT = "Data360 Govern";

		public const string DYNAMIC_FIELD = "fields";
		public const string DYNAMIC_FIELD_PREFIX = DYNAMIC_FIELD + ".";

		public const string D3S_FIELD = "d3s";
		public const string D3S_FIELD_PREFIX = D3S_FIELD + ".";
	}
}
