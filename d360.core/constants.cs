using System.Configuration;

namespace d360.core
{
    public static class constants
    {
        public const string NAMESPACE = "http://data3sixty.com/schemas";

        public static string COMMUNITY_DATABASE_CONNECTION = ConfigurationManager.AppSettings["CommunityContext"]; //@"Server=tcp:bzbdz2ikmp.database.windows.net;Database=D3S;User ID=d3s_user;Password=d3fGt$$@eEwq00y;Trusted_Connection=False;";

        public const string AZURE_STORAGE_NAME = "data3sixty";
        public const string AZURE_STORAGE_KEY = "akWskSolD1IWz+qmK2onCb10er80WsI02gNE83ufcOS1SUIMRF51p8BHPhGQ8EZTZCFkc5Pw4zIkVMnGbQnFUQ==";

        public const string WEBJOBS_STORAGE_CONNECTION = "DefaultEndpointsProtocol=https;AccountName=data3sixty;AccountKey=akWskSolD1IWz+qmK2onCb10er80WsI02gNE83ufcOS1SUIMRF51p8BHPhGQ8EZTZCFkc5Pw4zIkVMnGbQnFUQ==";

        public const string MANDRILL_API_KEY = "XBspYSVRlKva-pXOlDYWEg";

        public const string COMPANY_ICON_FOLDER = "company-icons";
        public const string COMPANY_ICON_URL = "https://data3sixty.blob.core.windows.net/company-icons/";
        public const string COMPANY_LOGO_FOLDER = "company-logos";
        public const string COMPANY_LOGO_URL = "https://data3sixty.blob.core.windows.net/company-logos/";
        public const string COMPANY_RESOURCES_FOLDER = "company-resources";
        public const string COMPANY_RESOURCES_URL = "https://data3sixty.blob.core.windows.net/company-resources/";
        public const string COMPANY_STYLES_FOLDER = "company-styles";

        public const string EVENTS_SERVICE_BUS = "Endpoint=sb://d3sevent.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=0AvdgR0EA0djFqV+EqSewmChgWHdqOPGZPUK+KJ8LZQ=";

        //azure container names
        public const string AZURE_CLOUD_FUSION_CONTAINER = "cloud-fusion-data";

        //azure media service constants
        public const string AZURE_MEDIA_SERVICES_ACCOUNT_NAME = "d3s";
        public const string AZURE_MEDIA_SERVICES_KEY = "MGSgtrabw6+HiphKwKgQp+gNvfrX0DJ1ecY2Ql60bOk=";

        //cosmos db info
        public const string COSMOS_AUTH_KEY = "mYhaK0v43dhEhPod02Jpz8qVRAXW04DNcyXDvp1GysEfPzgfir7dfkUMxN6CzvrJvqr05XZ4maMeldfxZtnVnA==";
        public const string COSMOS_ENDPOINT = "https://d3s-graph.documents.azure.com:443/";
    }
}
