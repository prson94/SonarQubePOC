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
            IQueueSource queueSource,
            string connectionString = null)
        {
            DummyCachingProvider cache = new DummyCachingProvider();
            CommunityContext community = InitializeCommunityContext(connectionString, securityContextProvider, cache, queueSource);

            return new CompanyContext(community, cache, queueSource, mailProvider, securityContextProvider, true);
        }

        public static CommunityContext CreateCommunityContext(ISecurityContextProvider securityContextProvider, IQueueSource queueSource, string connectionString = null)
        {
            DummyCachingProvider cache = new DummyCachingProvider();
            
            return InitializeCommunityContext(connectionString, securityContextProvider, cache, queueSource);
        }

        private static CommunityContext InitializeCommunityContext(string connectionString, ISecurityContextProvider sec, DummyCachingProvider cache, IQueueSource queueSource)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return new CommunityContext(cache, queueSource, sec);
            }

            return new CommunityContext(connectionString, cache, queueSource, sec);
        }
    }
}
