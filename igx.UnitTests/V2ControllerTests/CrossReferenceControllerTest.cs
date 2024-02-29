using d360.core.entities;
using d360.web.Controllers.V2;
using igx.UnitTests.Core;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Xunit;

namespace igx.UnitTests.V2ControllerTests
{
	[Trait("Unit tests", "CrossReferences controller")]
    public class CrossReferenceControllerTest : BaseTest
    {
        internal CrossReferencesController crossReferencesController;
        public CrossReferenceControllerTest()
        {
            crossReferencesController = new CrossReferencesController(GetCoreComponentSet(), GetQueue(), GetStorage())
            {
                Request = new HttpRequestMessage(),
                Configuration = new HttpConfiguration()
            };
        }

        [Fact]
        public async void Get()
        {
            var res = await (await crossReferencesController.Get()).ExecuteAsync(new System.Threading.CancellationToken());
			var parsedData = await res.Content.ReadAsAsync<List<AssetCrossReference>>();
			Assert.True(parsedData != null, XMsg.InvalidJSON);
        }

        [Fact]
        public async void GetByAssetUid()
        {
            var res = await (await crossReferencesController.GetByUid("")).ExecuteAsync(new System.Threading.CancellationToken());
			var data = res.Content.ReadAsStringAsync().Result;

            Assert.True(res.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(data != null, XMsg.InvalidJSON);
            AssertJSON.True<List<AssetCrossReference>>(data);
        }

        [Fact]
        public async void GetByTypeID()
        {
            var res = await (await crossReferencesController.GetByTypeID("", "")).ExecuteAsync(new System.Threading.CancellationToken());
			var data = res.Content.ReadAsStringAsync().Result;

            Assert.True(res.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(data != null, XMsg.InvalidJSON);
            AssertJSON.True<List<AssetCrossReference>>(data);
        }

        [Fact]
        public async void GetByType()
        {
            var res = await (await crossReferencesController.GetByType("")).ExecuteAsync(new System.Threading.CancellationToken());
			var data = res.Content.ReadAsStringAsync().Result;

            Assert.True(res.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(data != null, XMsg.InvalidJSON);
            AssertJSON.True<List<AssetCrossReference>>(data);
        }

        [Fact]
        public async void GetByDataSource()
        {
            var res = await (await crossReferencesController.GetByDataSource("")).ExecuteAsync(new System.Threading.CancellationToken());
			var data = res.Content.ReadAsStringAsync().Result;

            Assert.True(res.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(data != null, XMsg.InvalidJSON);
            AssertJSON.True<List<AssetCrossReference>>(data);
        }

        [Fact]
        public async void Post()
        {
            var myAssetCrossReference = new AssetCrossReference();

            var res = await (await crossReferencesController.Post(myAssetCrossReference)).ExecuteAsync(new System.Threading.CancellationToken()); 
            Assert.True(res.StatusCode == System.Net.HttpStatusCode.Created);
        }

        [Fact]
        public async void PostBulk()
        {
            var xRef = new AssetCrossReference();
            var xRefList = new List<AssetCrossReference>
			{
				new AssetCrossReference() { uid = Guid.Parse(DataConstants.ValidGUID) },
				xRef
			};

            IHttpActionResult actionResult;
            Task<HttpResponseMessage> responseMessageResult;

            try
            {
                actionResult = await crossReferencesController.PostBulk(xRefList);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                var str = responseMessageResult.Result.Content.ReadAsStringAsync().Result;

                Assert.True(responseMessageResult.Result.IsSuccessStatusCode);
                AssertJSON.True<List<AssetCrossReferenceResult>>(str);

            }
            catch (HttpResponseException ex)
            {
                Assert.True(ex.Response.StatusCode == System.Net.HttpStatusCode.Conflict, XMsg.BadResponseCode);
            }

        }
        
		[Fact]
        public async void PutByParams()
        {
            Guid uid = Guid.Parse(DataConstants.InvalidGUID);
            string dataSource = string.Empty;
            string type = string.Empty;
            string externalId = string.Empty;
            var xRef = new AssetCrossReference();

            var res = await (await crossReferencesController.PutByXrefUid(uid, dataSource, type, externalId, xRef)).ExecuteAsync(new System.Threading.CancellationToken());
            Assert.True(res.StatusCode == System.Net.HttpStatusCode.OK);
        }

        [Fact]
        public async void PutByModel()
        {
            Guid uid = Guid.Parse(DataConstants.InvalidGUID);
            var xRef = new AssetCrossReference();
            xRef.DataSource = xRef.Type = xRef.ExternalID = string.Empty;
            HttpResponseMessage res;

            res = await (await crossReferencesController.PutByUid(uid, xRef)).ExecuteAsync(new System.Threading.CancellationToken());
            Assert.True(res.StatusCode == System.Net.HttpStatusCode.OK);
        }

		[Fact]
        public async void DeleteByUid()
        {
            var res = await (await crossReferencesController.DeleteByUid(Guid.Parse(DataConstants.ValidGUID))).ExecuteAsync(new System.Threading.CancellationToken());
			Assert.True(res.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
        }

        [Fact]
        public async void DeleteByDataSource()
        {
			var res = await (await crossReferencesController.DeleteByDataSource(string.Empty)).ExecuteAsync(new System.Threading.CancellationToken());
			Assert.True(res.StatusCode == System.Net.HttpStatusCode.NotAcceptable, XMsg.BadResponseCode);

            res = await (await crossReferencesController.DeleteByDataSource(DataConstants.ValidDataSource)).ExecuteAsync(new System.Threading.CancellationToken());
			Assert.True(res.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
        }

        [Fact]
        public async void DeleteByDataSourceAndType()
        {
			var res = await (await crossReferencesController.DeleteByDataSourceAndType(string.Empty, string.Empty)).ExecuteAsync(new System.Threading.CancellationToken());
			Assert.True(res.StatusCode == System.Net.HttpStatusCode.NotAcceptable, XMsg.BadResponseCode);

            res = await (await crossReferencesController.DeleteByDataSourceAndType(DataConstants.ValidDataSource, "type")).ExecuteAsync(new System.Threading.CancellationToken());
            Assert.True(res.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
        }

        [Fact]
        public async void DeleteByType()
        {
			var res = await (await crossReferencesController.DeleteByType(string.Empty)).ExecuteAsync(new System.Threading.CancellationToken());
			Assert.True(res.StatusCode == System.Net.HttpStatusCode.NotAcceptable, XMsg.BadResponseCode);

            res = await (await crossReferencesController.DeleteByType(DataConstants.ValidType)).ExecuteAsync(new System.Threading.CancellationToken());
            Assert.True(res.IsSuccessStatusCode);
            Assert.True(res.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
        }

    }
}
