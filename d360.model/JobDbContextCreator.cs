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
            AzureStorageProvider storage = null)
        {
            var sec = new UriSecurityContextProvider()
            {
                CompanyID = companyId,
                ResourceID = resourceId,
                CompanyPrefix = urlPrefix,
                IsAdministrator = isAdmin
            };
            var cache = new DummyCachingProvider();
            var mail = new MandrillMailProvider
            {
                ApiKey = Config.GetValue<string>(constants.MAIL_API_KEY)
            };
            if (queue == null) 
            {
                queue = new AzureQueueSource();
            }
            var community = new CommunityContext(cache, queue, sec);
            if (storage == null)
            {
                storage = new AzureStorageProvider();
            }

            return new CompanyContext(community, cache, queue, mail, sec, storage, true);
        }

        public static CommunityContext CreateCommunityContext(int companyId, int resourceId, string urlPrefix, bool isAdmin)
        {
            var sec = new UriSecurityContextProvider()
            {
                CompanyID = companyId,
                ResourceID = resourceId,
                CompanyPrefix = urlPrefix,
                IsAdministrator = isAdmin
            };
            var cache = new DummyCachingProvider();
            var queue = new AzureQueueSource();

            return new CommunityContext(cache, queue, sec);
        }
    }
}
