using d360.web.Controllers.V2;
using igx.UnitTests.Core;
using System;
using System.Net.Http;
using System.Threading;
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
            this.environmentController = new EnvironmentController(GetCoreComponentSet(), GetThemeRepository(), GetDashboardRepository(), GetStorage(), GetResourceSettingRepository())
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

		[Fact]
		public async void GetUserSettingsTest()
		{
			var actionResult = await environmentController.GetUserSettings(CancellationToken.None);
			var res = actionResult.ExecuteAsync(new CancellationToken());

			Assert.True(res.Result.IsSuccessStatusCode, XMsg.BadResponseCode);
		}
	}
}
