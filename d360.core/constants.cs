namespace d360.core
{
    public static class constants
    {
        public const string AUTHORIZATION_HEADER_NAME = "Authorization";
        public const string NAMESPACE = "http://data3sixty.com/schemas";
        //public const string COMMUNITY_DATABASE_CONNECTION = @"Server=.;Database=D3S;User ID=sa;Password=6577;";
        public const string COMMUNITY_DATABASE_CONNECTION = @"Server=tcp:bzbdz2ikmp.database.windows.net;Database=D3S;User ID=d3s_user;Password=d3fGt$$@eEwq00y;Trusted_Connection=False;";
        public const string WORKFLOW_DATABASE_CONNECTION = @"Server=tcp:bzbdz2ikmp.database.windows.net;Database=Workflow;User ID=d3s_user;Password=d3fGt$$@eEwq00y;Trusted_Connection=False;Encrypt=true";
        public const string SERVICE_BUS_UI = @"Endpoint=sb://d3s-ui.servicebus.windows.net/;SharedSecretIssuer=owner;SharedSecretValue=rhrehB7tlnGohmpWROp/mD51MseScPk01vbgw6P7Lsg=";
        public const string SERVICE_BUS_ACTIONS = @"Endpoint=sb://d3s-actions.servicebus.windows.net/;SharedSecretIssuer=owner;SharedSecretValue=3954e8EnfAJYrLwGdp5R9IYCro5Y5HZv+lSRHPm/JWU=";
        public const string MEDIA_SERVICE_KEY = "iuaqQ9vyffao1Rtm1BADcSEv7qz6h1Mw9ewab+JLTgg=";
        public const string AZURE_STORAGE_BLOB_ENDPOINT = @"http://data3sixty.blob.core.windows.net/";
        public const string AZURE_STORAGE_NAME = "data3sixty";
        public const string AZURE_STORAGE_KEY = "akWskSolD1IWz+qmK2onCb10er80WsI02gNE83ufcOS1SUIMRF51p8BHPhGQ8EZTZCFkc5Pw4zIkVMnGbQnFUQ==";
        public const string WEBJOBS_STORAGE_CONNECTION = "DefaultEndpointsProtocol=https;AccountName=data3sixty;AccountKey=akWskSolD1IWz+qmK2onCb10er80WsI02gNE83ufcOS1SUIMRF51p8BHPhGQ8EZTZCFkc5Pw4zIkVMnGbQnFUQ==";

        public const string COMPANY_ICON_FOLDER = "company-icons";
        public const string COMPANY_ICON_URL = "https://data3sixty.blob.core.windows.net/company-icons/";
        public const string COMPANY_LOGO_FOLDER = "company-logos";
        public const string COMPANY_LOGO_URL = "https://data3sixty.blob.core.windows.net/company-logos/";

        public const string ARTIFACT_STATUS_DRAFT = "Draft";
        public const string ARTIFACT_STATUS_REVIEW = "Under Review";
        public const string ARTIFACT_STATUS_CERTIFIED = "Certified";
    }
}
