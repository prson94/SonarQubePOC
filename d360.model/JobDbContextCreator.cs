using d360.core;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.mail;
using d360.extensions.queue;
using d360.extensions.storage;

namespace d360.model
{
    public static class JobDbContextCreator
    {
        public static CompanyContext CreateCompanyContext(int companyId, int resourceId, string urlPrefix, bool isAdmin,
            AzureQueueSource queue = null,
            AzureStorageProvider storage = null,
            string connectionString = null,
            string mandrillApiKey = null,
            string mandrillSubAccount = null)
        {
            UriSecurityContextProvider sec = new UriSecurityContextProvider
            {
                CompanyID = companyId,
                ResourceID = resourceId,
                CompanyPrefix = urlPrefix,
                IsAdministrator = isAdmin
            };
            DummyCachingProvider cache = new DummyCachingProvider();
            MandrillMailProvider mail = new MandrillMailProvider
            {
                ApiKey = mandrillApiKey,
                SubAccount = mandrillSubAccount,
            };

            if (queue == null)
            {
                queue = new AzureQueueSource();
            }

            CommunityContext community = InitializeCommunityContext(connectionString, sec, cache, queue);

            if (storage == null)
            {
                storage = new AzureStorageProvider();
            }

            return new CompanyContext(community, cache, queue, mail, sec, storage, true);
        }

        public static CommunityContext CreateCommunityContext(int companyId, int resourceId, string urlPrefix, bool isAdmin, string connectionString = null)
        {
            UriSecurityContextProvider sec = new UriSecurityContextProvider
            {
                CompanyID = companyId,
                ResourceID = resourceId,
                CompanyPrefix = urlPrefix,
                IsAdministrator = isAdmin
            };
            DummyCachingProvider cache = new DummyCachingProvider();
            AzureQueueSource queue = new AzureQueueSource();
            
            return InitializeCommunityContext(connectionString, sec, cache, queue);
        }

        private static CommunityContext InitializeCommunityContext(string connectionString, UriSecurityContextProvider sec, DummyCachingProvider cache, AzureQueueSource queue)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return new CommunityContext(cache, queue, sec);
            }

            return new CommunityContext(connectionString, cache, queue, sec);
        }
    }
}
