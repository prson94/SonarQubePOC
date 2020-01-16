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
using System.Threading;
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
            this.executionsController = new ExecutionsController(GetCommunity(), GetCompany(), GetAssetRepository(), GetStorage(), GetRelationshipRepository())
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

        [Theory]
        [InlineData(DataConstants.InvalidGUID)]
        [InlineData(DataConstants.ValidGUID)]
        public void GetExecutionStatus(string uid)
        {
            var executionUid = Guid.Parse(uid);
            bool isGoodUID = uid == DataConstants.ValidGUID;
            Task<IHttpActionResult> actionResult;
            Task<HttpResponseMessage> responseMessageResult;

            if (!isGoodUID)
            {
                actionResult = executionsController.GetExecutionStatus(executionUid);
                responseMessageResult = actionResult.Result.ExecuteAsync(new CancellationToken());

                Assert.True(responseMessageResult.Result.StatusCode == HttpStatusCode.NotFound, XMsg.BadResponseCode);
            }
            if (isGoodUID)
            {
                actionResult = executionsController.GetExecutionStatus(executionUid);
                responseMessageResult = actionResult.Result.ExecuteAsync(new CancellationToken());
                var str = responseMessageResult.Result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<JObject>(str);

                Assert.True(responseMessageResult.Result.IsSuccessStatusCode, XMsg.BadResponseCode);
                Assert.True(data != null, XMsg.InvalidJSON);

                var importantFields = new List<string>() { "CompletedOn", "Error", "Fields", "Processed", "StartedOn", "Total", "Results" };
                foreach (var field in importantFields)
                {
                    Assert.True(data.GetValue(field) != null, $"{field} missing from response!");
                }

            }
        }

        [Fact]
        public async void GetRelationshipsExecutionStatus()
        {

            var actionResult = executionsController.GetRelationshipsExecutionStatus(Guid.Parse(DataConstants.ValidGUID));

            var result = await actionResult.Result.ExecuteAsync(new CancellationToken());
            var str = await result.Content.ReadAsStringAsync();

            Assert.True(result.IsSuccessStatusCode);
            Assert.True(result.StatusCode == HttpStatusCode.OK);
            Assert.True(!string.IsNullOrEmpty(str));
            var data = JsonConvert.DeserializeObject<JObject>(str);
            Assert.True(data.GetValue("CompletedOn").ToString() != null);
            Assert.True(data.GetValue("Error").ToString() != null);
            Assert.True(data.GetValue("Fields").ToString() != null);
            Assert.True(data.GetValue("Processed").ToString() != null);
            Assert.True(data.GetValue("StartedOn").ToString() != null);
            Assert.True(data.GetValue("Total").ToString() != null);
            Assert.True(data.GetValue("Results").ToString() != null);

        }

    }
}
