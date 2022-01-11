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
using igx.UnitTests.Core;
using System.Web.Http.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace igx.UnitTests.V2ControllerTests
{
    [Trait("Unit tests", "CrossReferences controller")]
    public class CrossReferenceControllerTest : BaseTest
    {
        internal CrossReferencesController crossReferencesController;
        public CrossReferenceControllerTest()
        {
            this.crossReferencesController = new CrossReferencesController(GetCoreComponentSet(), GetCrossReferencesRepository(), GetAssetRepository())
            {
                Request = new HttpRequestMessage(),
                Configuration = new HttpConfiguration()
            };
        }

        [Fact]
        public async void Get()
        {
            var res = await crossReferencesController.Get();
            var data = res.Content.ReadAsStringAsync().Result;

            Assert.True(res.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(data != null, XMsg.InvalidJSON);

            var parsedData = JsonConvert.DeserializeObject<List<AssetCrossReference>>(data);

            Assert.True(parsedData != null && parsedData.Count > 0, XMsg.InvalidJSON);
        }

        [Fact]
        public async void GetByAssetUid()
        {
            var res = await crossReferencesController.GetByUid("");
            var data = res.Content.ReadAsStringAsync().Result;

            Assert.True(res.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(data != null, XMsg.InvalidJSON);

            AssertJSON.True<List<AssetCrossReference>>(data);
        }

        [Fact]
        public async void GetByTypeID()
        {
            var res = await crossReferencesController.GetByTypeID("", "");
            var data = res.Content.ReadAsStringAsync().Result;

            Assert.True(res.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(data != null, XMsg.InvalidJSON);

            AssertJSON.True<List<AssetCrossReference>>(data);
        }

        [Fact]
        public async void GetByType()
        {
            var res = await crossReferencesController.GetByType("");
            var data = res.Content.ReadAsStringAsync().Result;

            Assert.True(res.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(data != null, XMsg.InvalidJSON);

            AssertJSON.True<List<AssetCrossReference>>(data);
        }

        [Fact]
        public async void GetByDataSource()
        {
            var res = await crossReferencesController.GetByDataSource("");
            var data = res.Content.ReadAsStringAsync().Result;

            Assert.True(res.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(data != null, XMsg.InvalidJSON);

            AssertJSON.True<List<AssetCrossReference>>(data);
        }

        [Fact]
        public async void Post()
        {
            var myAssetCrossReference = new AssetCrossReference();
            var res = new AssetCrossReference();

            try
            {
                res = await crossReferencesController.Post(myAssetCrossReference);
                Assert.True(false, XMsg.ExceptionExpected);
            }
            catch (HttpResponseException ex)
            {
                Assert.True(ex.Response.StatusCode == System.Net.HttpStatusCode.NotAcceptable, XMsg.BadResponseCode);
            }
            myAssetCrossReference.DataSource = "non-empty";
            myAssetCrossReference.ExternalID = "non-empty";
            myAssetCrossReference.Type = "non-empty";

            try
            {
                myAssetCrossReference.uid = Guid.Parse(DataConstants.InvalidGUID);
                res = await crossReferencesController.Post(myAssetCrossReference);
                Assert.True(false, XMsg.ExceptionExpected);
            }
            catch (HttpResponseException ex)
            {
                Assert.True(ex.Response.StatusCode == System.Net.HttpStatusCode.Conflict, XMsg.BadResponseCode);
            }

            myAssetCrossReference.uid = Guid.Parse(DataConstants.ValidGUID);
            res = await crossReferencesController.Post(myAssetCrossReference);

            Assert.True(res != null, XMsg.NoContent);

        }

        [Fact]
        public async void PostBulk()
        {
            var xRef = new AssetCrossReference();
            var xRefList = new List<AssetCrossReference>() { new AssetCrossReference() { uid = Guid.Parse(DataConstants.ValidGUID) } };
            xRefList.Add(xRef);

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

            HttpResponseMessage res;

            try
            {
                res = await crossReferencesController.Put(uid, dataSource, type, externalId, xRef);
                Assert.True(false, XMsg.ExceptionExpected);
            }
            catch (HttpResponseException ex)
            {
                Assert.True(ex.Response.StatusCode == System.Net.HttpStatusCode.NotAcceptable, XMsg.BadResponseCode);
            }


            dataSource = type = externalId = "not-empty";
            xRef.uid = Guid.Parse(DataConstants.InvalidGUID);
            res = await crossReferencesController.Put(uid, dataSource, type, externalId, xRef);
            Assert.True(res.StatusCode == System.Net.HttpStatusCode.NotFound, XMsg.BadResponseCode);

            xRef.uid = Guid.Parse(DataConstants.ValidGUID);
            res = await crossReferencesController.Put(uid, dataSource, type, externalId, xRef);
            Assert.True(res.IsSuccessStatusCode, XMsg.BadResponseCode);
        }

        [Fact]
        public async void PutByModel()
        {
            Guid uid = Guid.Parse(DataConstants.InvalidGUID);
            var xRef = new AssetCrossReference();
            xRef.DataSource = xRef.Type = xRef.ExternalID = String.Empty;
            HttpResponseMessage res;

            try
            {
                res = await crossReferencesController.Put(uid, xRef);
                Assert.True(false, XMsg.ExceptionExpected);
            }
            catch (HttpResponseException ex)
            {
                Assert.True(ex.Response.StatusCode == System.Net.HttpStatusCode.NotAcceptable, XMsg.BadResponseCode);
            }
            xRef.DataSource = xRef.Type = xRef.ExternalID = "not-empty";

            xRef.uid = Guid.Parse(DataConstants.InvalidGUID);
            res = await crossReferencesController.Put(uid, xRef);

            Assert.True(res.StatusCode == System.Net.HttpStatusCode.NotFound, XMsg.BadResponseCode);

            xRef.uid = Guid.Parse(DataConstants.ValidGUID);
            res = await crossReferencesController.Put(uid, xRef);
            Assert.True(res.IsSuccessStatusCode, XMsg.BadResponseCode);
        }
        [Fact]
        public async void DeleteByUid()
        {
            HttpResponseMessage res;

            res = await crossReferencesController.DeleteByUid(Guid.Parse(DataConstants.InvalidGUID));

            Assert.True(res.StatusCode == System.Net.HttpStatusCode.NotFound, XMsg.BadResponseCode);

            res = await crossReferencesController.DeleteByUid(Guid.Parse(DataConstants.ValidGUID));

            Assert.True(res.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
        }

        [Fact]
        public async void DeleteByDataSource()
        {
            HttpResponseMessage res;

            try
            {
                res = await crossReferencesController.DeleteByDataSource(string.Empty);
                Assert.True(false, XMsg.ExceptionExpected);
            }
            catch (HttpResponseException ex)
            {
                Assert.True(ex.Response.StatusCode == System.Net.HttpStatusCode.NotAcceptable, XMsg.BadResponseCode);
            }

            res = await crossReferencesController.DeleteByDataSource("random invalid string");

            Assert.True(res.StatusCode == System.Net.HttpStatusCode.NotFound, XMsg.BadResponseCode);

            res = await crossReferencesController.DeleteByDataSource(DataConstants.ValidDataSource);

            Assert.True(res.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
        }

        [Fact]
        public async void DeleteByDataSourceAndType()
        {
            HttpResponseMessage res;
            string type = string.Empty;

            try
            {
                res = await crossReferencesController.DeleteByDataSource(string.Empty, type);
                Assert.True(false, XMsg.ExceptionExpected);
            }
            catch (HttpResponseException ex)
            {
                Assert.True(ex.Response.StatusCode == System.Net.HttpStatusCode.NotAcceptable, XMsg.BadResponseCode);
            }

            res = await crossReferencesController.DeleteByDataSource("random invalid string", "type");

            Assert.True(res.StatusCode == System.Net.HttpStatusCode.NotFound, XMsg.BadResponseCode);

            res = await crossReferencesController.DeleteByDataSource(DataConstants.ValidDataSource, "type");

            Assert.True(res.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
        }

        [Fact]
        public async void DeleteByType()
        {
            HttpResponseMessage res;

            try
            {
                res = await crossReferencesController.DeleteByType(string.Empty);
                Assert.True(false, XMsg.ExceptionExpected);
            }
            catch (HttpResponseException ex)
            {
                Assert.True(ex.Response.StatusCode == System.Net.HttpStatusCode.NotAcceptable, XMsg.BadResponseCode);
            }

            res = await crossReferencesController.DeleteByType("random invalid string");
            Assert.True(!res.IsSuccessStatusCode);
            Assert.True(res.StatusCode == System.Net.HttpStatusCode.NotFound, XMsg.BadResponseCode);

            res = await crossReferencesController.DeleteByType(DataConstants.ValidType);
            Assert.True(res.IsSuccessStatusCode);
            Assert.True(res.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
        }

    }
}
