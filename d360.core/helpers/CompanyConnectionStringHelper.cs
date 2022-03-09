namespace d360.core
{
    public static class CompanyConnectionStringHelper
    {
        private const string CONNECTION_ENABLE_MARS = "True";

        // retry logic documentation https://docs.microsoft.com/en-us/azure/azure-sql/database/troubleshoot-common-connectivity-issues
        private const int CONNECTION_RETRY_COUNT = 10;   // Default is 1. Range is 0 through 255.
        private const int CONNECTION_RETRY_INTERVAL = 10; //Default is 10 seconds. Range is 1 through 60.  https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlconnectionstringbuilder.connectretryinterval?view=dotnet-plat-ext-3.1
        private const int CONNECTION_TIMEOUT = 100;  // Default is 15 seconds. Range is 0 through 2147483647, NEEDS TO BE AT LEAST  Connection Timeout = ConnectRetryCount * ConnectionRetryInterval
        private const string GOVERN_ENVIRONMENT_DATABASE_NAME_PREFIX = "D3S";

        /// <summary>
        /// Returns the database connection string for the provided govern environment id, server, username and password.
        /// </summary>        
        public static string ConnectionString(int id, string server, string username, string password)
        {
            if (string.IsNullOrEmpty(server))
            {
                throw new System.Exception("Please specify a valid server to generate a Govern connection string for.");
            }

            if (string.IsNullOrEmpty(username))
            {
                throw new System.Exception("Please specify a valid database username to generate a Govern connection string for.");
            }

            return $"server={server};Database={GOVERN_ENVIRONMENT_DATABASE_NAME_PREFIX}_{id};User ID={username};Password={password};MultipleActiveResultSets={CONNECTION_ENABLE_MARS};ConnectRetryCount={CONNECTION_RETRY_COUNT};ConnectRetryInterval={CONNECTION_RETRY_INTERVAL};Connection Timeout={CONNECTION_TIMEOUT};";
        }
    }
}
