using d360.web.Controllers.V2;
using System.Net.Http;
using System.Web.Http;
using Xunit;

namespace igx.UnitTests.V2ControllerTests
{
    [Trait("Unit tests", "Environment controller")]
    public class EnvironmentControllerTest : BaseTest
    {

        internal EnvironmentController environmentController;
        
        public EnvironmentControllerTest()
        {
            this.environmentController = new EnvironmentController(GetCommunity(), GetCompany(), GetStorage(), GetAssetRepository(), GetSettingsRepository())
            {
                Request = new HttpRequestMessage(),
                Configuration = new HttpConfiguration()
            };
        }

        [Fact]
        public void GetSettingsTest()
        {
            var actionResult = environmentController.Settings();
            Assert.True(actionResult.IsSuccessStatusCode, XMsg.BadResponseCode);
        }
    }
}
