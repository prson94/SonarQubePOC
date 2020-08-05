using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.queue;
using d360.extensions.storage;

namespace d360.model
{
    public static class JobDbContextCreator
    {
        public static CompanyContext CreateWebjobCompanyContext(int companyId, int resourceId, string urlPrefix, bool isAdmin)
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
            var community = new CommunityContext(cache, queue, sec);
            var storage = new AzureStorageProvider();

            return new CompanyContext(community, cache, queue, sec, storage, true);
        }
    }
}
