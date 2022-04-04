using d360.core.entities.Metric;
using d360.web.Controllers.V2;
using igx.UnitTests.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Web.Http;
using Xunit;

namespace igx.UnitTests.V2ControllerTests
{
    [Trait("Unit tests", "Scoring controller")]
    public class ScoringControllerTest : BaseTest
    {
        internal ScoringController scoringController;

        public ScoringControllerTest()
        {
            this.scoringController = new ScoringController(GetCoreComponentSet(), GetScoringRepository(), GetAssetRepository(), GetMetricsRepository())
            {
                Request = new HttpRequestMessage(),
                Configuration = new HttpConfiguration()
            };
        }

        [Fact]
        public async void GetMetricStructureByAllocation()
        {
            var actionResult = scoringController.GetMetricStructureByAllocation(Guid.Parse(DataConstants.ValidGUID)).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<JArray>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
            Assert.True(Helpers.IsTypeOf(typeof(MetricAssetViewModel), data), XMsg.InvalidJSON);

        }
    }
}
