using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using d360.web.Controllers.V2;
using System.Web.Http;
using System.Net.Http;
using d360.core.enums;
using d360.core.entities;

namespace igx.UnitTests
{
	public class AssetControllerTest : BaseTest
	{
		[Fact]
		public void GetAssetTypeClassesAsync_PassingNoParameters_ReturnsListOfAssetTypeInfoObjects()
		{
			var testClass = new AssetsController(GetCommunity(), GetCompany(), GetStorage(), GetQueue())
            {
                Request = new System.Net.Http.HttpRequestMessage(),
                Configuration = new HttpConfiguration()
            };

            var result = testClass.GetAssetTypeClassesAsync();
            var list = new List<AssetTypeClassInfo>();
            var listOfAssets = result.TryGetContentValue(out list);
            Assert.True(list.Count > 0);
            Assert.True(result.IsSuccessStatusCode);
        }

		[Fact]
		public async void GetAssetTypesAsync_PassingNoParameters_ReturnsListOfAssetTypeApiViewModel()
        {
            var testClass = new AssetsController(GetCommunity(), GetCompany(), GetStorage(), GetQueue())
            {
                Request = new System.Net.Http.HttpRequestMessage(),
                Configuration = new HttpConfiguration()
            };

            var results = await testClass.GetAssetTypesAsync();
            var list = new List<AssetTypeApiViewModel>();
            var res = results.TryGetContentValue(out list);

            Assert.True(results.IsSuccessStatusCode);
            Assert.True(list.Count > 0);
            Assert.True(list.First().Name == "unit test mock name");

        }



    }
}
