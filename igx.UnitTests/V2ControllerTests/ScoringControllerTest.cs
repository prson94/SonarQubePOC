using d360.core.entities.Workflow;
using d360.web.Controllers.V2;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Xunit;
using igx.UnitTests.Core;
using d360.core.entities;
using Newtonsoft.Json.Linq;
using d360.core.entities.Metric;
using d360.web.Models;

namespace igx.UnitTests.V2ControllerTests
{
    [Trait("Unit tests", "Scoring controller")]
    public class ScoringControllerTest : BaseTest
    {
        internal ScoringController scoringController;

        public ScoringControllerTest()
        {
            this.scoringController = new ScoringController(GetCommunity(), GetCompany(), GetQueue(), GetScoringRepository(), GetAssetRepository(), GetMetricsRepository(), GetSettingsRepository())
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
