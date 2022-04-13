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
            ICachingProvider cachingProvider,
            string connectionString)
        {
            CommunityContext community = new CommunityContext(connectionString, cachingProvider, queueSource, securityContextProvider);

            return new CompanyContext(community, cachingProvider, queueSource, mailProvider, securityContextProvider, true);
        }
    }
}
