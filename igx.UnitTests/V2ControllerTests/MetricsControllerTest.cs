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

namespace igx.UnitTests.V2ControllerTests
{
    [Trait("Unit tests", "Metrics controller")]
    public class MetricsControllerTest : BaseTest
    {
        internal MetricsController metricsController;

        public MetricsControllerTest()
        {
            this.metricsController = new MetricsController(GetCommunity(), GetCompany(), GetQueue(), GetMetricsRepository(), GetAssetRepository())
            {
                Request = new HttpRequestMessage(),
                Configuration = new HttpConfiguration()
            };
        }

        [Fact]
        public async void GetAssetByUid()
        {

            var actionResult = metricsController.GetAssetById(Guid.Parse(DataConstants.ValidGUID)).ExecuteAsync(new System.Threading.CancellationToken()).Result;

            var str = await actionResult.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<MetricAsset>(str);

            Assert.True(actionResult.StatusCode == System.Net.HttpStatusCode.OK);
            Assert.True(data != null);

        }

       

    }
}
