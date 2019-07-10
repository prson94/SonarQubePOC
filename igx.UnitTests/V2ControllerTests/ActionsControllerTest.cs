using d360.web.Controllers.V2;
using igx.UnitTests.Core;
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
        public async void GetAllocationByAssetTypeAsync()
        {
            var testGuid = Guid.Parse(DataConstants.ValidGUID);
            var actionResult = await actionsController.GetAllocationByAssetTypeAsync(testGuid);
            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            Assert.True(res.Result.IsSuccessStatusCode, XMsg.BadResponseCode);

        }
    }
}
