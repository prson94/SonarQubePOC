using d360.core.entities;
using d360.web.Controllers.V2;
using igx.UnitTests.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Xunit;

namespace igx.UnitTests.V2ControllerTests
{

    [Trait("Unit tests", "Executions controller")]
    public class ExecutionsControllerTest : BaseTest
    {

        internal ExecutionsController executionsController;
        public ExecutionsControllerTest()
        {
            this.executionsController = new ExecutionsController(GetCommunity(), GetCompany(), GetAssetRepository(), GetSettingsRepository(), GetStorage())
            {
                Request = new HttpRequestMessage(),
                Configuration = new HttpConfiguration()
            };
        }

        [Fact]
        public void GetExecutions()
        {
            var result = executionsController.GetExecutions();
            var responseMessage = result.Result.ExecuteAsync(new System.Threading.CancellationToken());
            var jsonstring = responseMessage.Result.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<APIExecutionAPIModelResult>(jsonstring);

            Assert.True(data != null, XMsg.NoContent);
            Assert.True(responseMessage.Result.IsSuccessStatusCode, XMsg.BadResponseCode);
        }

    }
}
