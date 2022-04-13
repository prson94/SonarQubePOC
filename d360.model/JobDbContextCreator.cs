using d360.core;
using d360.extensions;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.mail;
using d360.extensions.queue;
using d360.extensions.storage;

namespace d360.model
{
    public static class JobDbContextCreator
    {
        public static CompanyContext CreateCompanyContext(
            ISecurityContextProvider securityContextProvider,
            IMailProvider mailProvider,
            AzureQueueSource queue = null,
            string connectionString = null)
        {
            DummyCachingProvider cache = new DummyCachingProvider();

            if (queue == null)
            {
                queue = new AzureQueueSource();
            }

            CommunityContext community = InitializeCommunityContext(connectionString, securityContextProvider, cache, queue);

            return new CompanyContext(community, cache, queue, mailProvider, securityContextProvider, true);
        }

        public static CommunityContext CreateCommunityContext(ISecurityContextProvider securityContextProvider, string connectionString = null)
        {
            DummyCachingProvider cache = new DummyCachingProvider();
            AzureQueueSource queue = new AzureQueueSource();
            
            return InitializeCommunityContext(connectionString, securityContextProvider, cache, queue);
        }

        private static CommunityContext InitializeCommunityContext(string connectionString, ISecurityContextProvider sec, DummyCachingProvider cache, AzureQueueSource queue)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return new CommunityContext(cache, queue, sec);
            }

            return new CommunityContext(connectionString, cache, queue, sec);
        }
    }
}
