using Microsoft.VisualStudio.TestTools.UnitTesting;
using d360.web.Controllers;
using d360.model;
using d360.extensions;
using d360.extensions.caching;
using d360.extensions.queue;

namespace d360.test.web
{
    [TestClass]
    public class ArtifactTests
    {

        [TestMethod, Description("")]
        public void GetArtifact()
        {
            
            ICachingProvider cache = new DummyCachingProvider();
            IQueueSource queue = new DummyQueueSource();
            ISecurityContextProvider context = new DummySecurityContextProvider();
            context.CompanyID = 4;
            context.CompanyPrefix = "demo.dev";
            context.IsAdministrator = true;
            context.ResourceID = 3243;
            var community = new CommunityContext(cache, queue, context);
            var company = new CompanyContext(community, cache, queue, context, true);

            var api = new D3SApiController(community, company, context);

            var testId = 4651;
            var model = api.GetArtifact(testId);

            Assert.IsNotNull(model);
            Assert.AreEqual(model["ID"].ToString(), testId.ToString());
        }
    }
}
