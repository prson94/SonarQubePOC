using d360.core;
using d360.extensions;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.mail;
using d360.extensions.queue;
using d360.model;
using d360.web.caching;

namespace d360.web
{
    public class BaseMiddleware
    {
        internal ICachingProvider Cache;

        public BaseMiddleware()
        {
            if (Config.GetValue<bool>("RedisEnabled"))
            {
                Cache = new RedisCachingProvider();
            }
            else
            {
                Cache = new MemoryCachingProvider();
            }
        }

        public CompanyContext CreateOwinCompanyContext(int companyId)
        {
            var sec = new UriSecurityContextProvider
            {
                CompanyID = companyId,
                ResourceID = 0,
                CompanyPrefix = "",
                IsAdministrator = false
            };
            var community = new CommunityContext(Cache, null, sec);
            var mail = new MandrillMailProvider
            {
                ApiKey = Config.GetValue<string>(constants.MAIL_API_KEY)
            };
            var queue = new AzureQueueSource();
            return new CompanyContext(community, Cache, queue, mail, sec, null, false);
        }
    }
}