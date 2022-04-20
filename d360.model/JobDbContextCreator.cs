using d360.extensions;

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
