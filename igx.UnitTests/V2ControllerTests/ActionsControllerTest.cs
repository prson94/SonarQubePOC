using d360.web.Controllers.V2;
using igx.UnitTests.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Xunit;

namespace igx.UnitTests.V2ControllerTests
{
    [Trait("Unit tests", "Asset controller")]
    public class ActionsControllerTest : BaseTest
    {

        internal ActionsController actionsController;
        public ActionsControllerTest()
        {
            this.actionsController = new ActionsController(GetCommunity(), GetCompany(), GetIssueRepository(), GetAssetRepository())
            {
                Request = new HttpRequestMessage(),
                Configuration = new HttpConfiguration()
            };
        }
        [Fact]
        public async void GetIssueTypesTest()
        {
            var actionResult = await actionsController.GetIssueTypes();
            Assert.True(actionResult.IsSuccessStatusCode, XMsg.BadResponseCode);

        }
        [Fact]
        public async void GetAllocationByAssetTypeAsyncTest()
        {
            var testGuid = Guid.Parse(DataConstants.ValidGUID);
            var actionResult = await actionsController.GetAllocationByAssetTypeAsync(testGuid);
            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<JArray>(str);

            Assert.True(res.Result.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(data != null, XMsg.InvalidJSON);

        }

        [Fact]
        public void CheckNegativePageSize()
        {
            int pageSize = -10000;
            int pageNum = 1;
            actionsController.isPageSizeAndNumValid(ref pageSize, ref pageNum);

            Assert.InRange(pageSize, 1, 200000);

        }

        [Fact]
        public void CheckMaxPageSize()
        {
            int pageSize = 500000;
            int pageNum = 1;
            actionsController.isPageSizeAndNumValid(ref pageSize, ref pageNum);

            Assert.InRange(pageSize, 1, 200000);

        }
        [Fact]
        public void CheckNegativePageNum()
        {
            int pageSize = 5;
            int pageNum = -5000;
            actionsController.isPageSizeAndNumValid(ref pageSize, ref pageNum);

            Assert.InRange(pageSize, 1, 10000);

        }

        [Fact]
        public void CheckMaxPageNum()
        {
            int pageSize = 5;
            int pageNum = 50000;
            actionsController.isPageSizeAndNumValid(ref pageSize, ref pageNum);

            Assert.InRange(pageSize, 1, 10000);

        }
    }
}
