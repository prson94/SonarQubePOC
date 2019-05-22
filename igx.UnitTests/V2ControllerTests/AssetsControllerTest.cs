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
using System.Net;

namespace igx.UnitTests
{
    [Trait("Unit tests", "Asset controller")]
    public class AssetControllerTest : BaseTest
    {
        internal AssetsController assetsController;
        public AssetControllerTest()
        {
            this.assetsController = new AssetsController(GetCommunity(), GetCompany(), GetStorage(), GetQueue(), GetAssetRepository())
            {
                Request = new HttpRequestMessage(),
                Configuration = new HttpConfiguration()
            };
        }
        [Fact]
        public async void GetAssetTypesAsync()
        {
            var results = await assetsController.GetAssetTypesAsync();
            var list = new List<AssetTypeApiViewModel>();
            var res = results.TryGetContentValue(out list);

            Assert.True(results.IsSuccessStatusCode);
            Assert.True(list != null);

        }

        [Fact]
        public void GetAssetTypeClasses()
        {
            var result = assetsController.GetAssetTypeClassesAsync();
            var list = new List<AssetTypeClassInfo>();
            var listOfAssets = result.TryGetContentValue(out list);
            Assert.True(list.Count > 0);
            Assert.True(result.IsSuccessStatusCode);
        }

        [Fact]
        public async void GetAssetTypeByGUID()
        {

            var testGuid = Guid.NewGuid();

            var actionResult = await assetsController.GetAssetsAsync(testGuid);
            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<AssetsApiViewModel>(str);

            Assert.True(res.Result.IsSuccessStatusCode);
            Assert.IsType<AssetsApiViewModel>(data);
            Assert.True(data != null);

        }

        [Fact]
        public async void GetAssetsTypeFieldsAsync()
        {
            List<string> fieldContract = new List<string>() { "FriendlyName", "ID", "IsListable", "IsRequired", "ColumnOrder", "SortOrder", "ObjectType", "ObjectID", "Type" };

            var testGuid = Guid.NewGuid();
            var actionResult = await assetsController.GetAssetsTypeFieldsAsync(testGuid);
            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<JArray>(str);

            foreach (JObject @object in data)
            {
                foreach (var field in fieldContract)
                {
                    Assert.True(@object.GetValue(field) != null);
                }
            }

            Assert.True(res.Result.IsSuccessStatusCode);
            Assert.True(data != null);

        }

        [Theory]
        [InlineData(DataConstants.InvalidGUID, 0)]
        [InlineData(DataConstants.ValidGUID, 0)]
        [InlineData(DataConstants.ValidGUID, 2)]
        [InlineData(DataConstants.ValidGUID, 2, true)]
        public async void PostAssets(string uid, int numberOfassetInsertList, bool checkAsSendByJSONContent = false)
        {
            var assetUID = Guid.Parse(uid);
            bool isGoodUID = uid == DataConstants.ValidGUID;

            var assetInsertList = new List<AssetInsert>();
            for (int i = 0; i < numberOfassetInsertList; i++)
            {
                assetInsertList.Add(new AssetInsert() { });
            }
            IHttpActionResult actionResult;
            Task<HttpResponseMessage> responseMessageResult;

            if (!isGoodUID)
            {
                actionResult = await assetsController.PostAssetsAsync(assetUID, assetInsertList);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                Assert.True(!responseMessageResult.Result.IsSuccessStatusCode);
            }

            if (isGoodUID && numberOfassetInsertList == 0)
            {
                actionResult = await assetsController.PostAssetsAsync(assetUID, assetInsertList);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                var str = responseMessageResult.Result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<List<DatabaseBulkAssetResult>>(str);

                Assert.True(responseMessageResult.Result.IsSuccessStatusCode);
                Assert.True(data == null);
            }

            if (isGoodUID && numberOfassetInsertList > 0)
            {
                actionResult = await assetsController.PostAssetsAsync(assetUID, assetInsertList);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                var str = responseMessageResult.Result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<List<DatabaseBulkAssetResult>>(str);

                Assert.True(responseMessageResult.Result.IsSuccessStatusCode);
                Assert.True(data != null);
            }

            if (checkAsSendByJSONContent)
            {

                var asJson = JsonConvert.SerializeObject(assetInsertList);
                assetInsertList = null;

                var content = new StringContent(asJson, Encoding.UTF8, "application/json");

                assetsController.Request = new HttpRequestMessage()
                {
                    Content = content
                };

                actionResult = await assetsController.PostAssetsAsync(assetUID, assetInsertList);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                var str = responseMessageResult.Result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<List<DatabaseBulkAssetResult>>(str);

                Assert.True(responseMessageResult.Result.IsSuccessStatusCode);
                Assert.True(data != null);
            }


        }


        [Theory]
        [InlineData(DataConstants.InvalidGUID, 0)]
        [InlineData(DataConstants.ValidGUID, 0)]
        [InlineData(DataConstants.ValidGUID, 2)]
        [InlineData(DataConstants.ValidGUID, 2, true)]
        public async void PutAssetsAsync(string uid, int numberOfassetInsertList, bool checkAsSendByJSONContent = false)
        {
            var assetUID = Guid.Parse(uid);
            bool isGoodUID = !uid.StartsWith("000");

            var assetUpdateList = new List<AssetUpdate>();
            for (int i = 0; i < numberOfassetInsertList; i++)
            {
                assetUpdateList.Add(new AssetUpdate() { });
            }
            IHttpActionResult actionResult;
            Task<HttpResponseMessage> responseMessageResult;

            if (!isGoodUID)
            {
                actionResult = await assetsController.PutAssetsAsync(assetUID, assetUpdateList);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                Assert.True(!responseMessageResult.Result.IsSuccessStatusCode);
            }

            if (isGoodUID && numberOfassetInsertList == 0)
            {
                actionResult = await assetsController.PutAssetsAsync(assetUID, assetUpdateList);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                var str = responseMessageResult.Result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<List<DatabaseBulkAssetResult>>(str);

                Assert.True(responseMessageResult.Result.IsSuccessStatusCode);
                Assert.True(data == null);
            }

            if (isGoodUID && numberOfassetInsertList > 0)
            {
                actionResult = await assetsController.PutAssetsAsync(assetUID, assetUpdateList);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                var str = responseMessageResult.Result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<List<DatabaseBulkAssetResult>>(str);

                Assert.True(responseMessageResult.Result.IsSuccessStatusCode);
                Assert.True(data != null);
            }

            if (checkAsSendByJSONContent)
            {

                var asJson = JsonConvert.SerializeObject(assetUpdateList);
                assetUpdateList = null;

                var content = new StringContent(asJson, Encoding.UTF8, "application/json");

                assetsController.Request = new HttpRequestMessage()
                {
                    Content = content
                };

                actionResult = await assetsController.PutAssetsAsync(assetUID, assetUpdateList);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                var str = responseMessageResult.Result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<List<DatabaseBulkAssetResult>>(str);

                Assert.True(responseMessageResult.Result.IsSuccessStatusCode);
                Assert.True(data != null);
            }


        }


        [Theory]
        [InlineData(DataConstants.InvalidGUID, false)]
        [InlineData(DataConstants.ValidGUID, false)]
        [InlineData(DataConstants.ValidGUID, true)]
        [InlineData(DataConstants.ValidGUID, true, true)]
        public async void DeleteAssetsAsync(string uid, bool hasDeleteAsset, bool checkAsSendByJSONContent = false)
        {
            var assetUID = Guid.Parse(uid);
            bool isGoodUID = uid == DataConstants.ValidGUID;

            var assetDeletes = new AssetDeletes();
            if (!hasDeleteAsset)
                assetDeletes = null;

            IHttpActionResult actionResult;
            Task<HttpResponseMessage> responseMessageResult;

            if (!isGoodUID)
            {
                actionResult = await assetsController.DeleteAssetsAsync(assetUID, assetDeletes);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                Assert.True(!responseMessageResult.Result.IsSuccessStatusCode);
            }

            if (isGoodUID && !hasDeleteAsset)
            {
                actionResult = await assetsController.DeleteAssetsAsync(assetUID, assetDeletes);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

                Assert.True(responseMessageResult.Result.StatusCode == System.Net.HttpStatusCode.InternalServerError);
            }

            if (isGoodUID && hasDeleteAsset)
            {
                actionResult = await assetsController.DeleteAssetsAsync(assetUID, assetDeletes);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                var str = responseMessageResult.Result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<List<DatabaseBulkAssetResult>>(str);

                Assert.True(responseMessageResult.Result.IsSuccessStatusCode);
                Assert.True(data != null);
            }

            if (checkAsSendByJSONContent)
            {

                var asJson = JsonConvert.SerializeObject(assetDeletes);
                assetDeletes = null;

                var content = new StringContent(asJson, Encoding.UTF8, "application/json");

                assetsController.Request = new HttpRequestMessage()
                {
                    Content = content
                };

                actionResult = await assetsController.DeleteAssetsAsync(assetUID, assetDeletes);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                var str = responseMessageResult.Result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<List<DatabaseBulkAssetResult>>(str);

                Assert.True(responseMessageResult.Result.IsSuccessStatusCode);
                Assert.True(data != null);
            }
        }

        [Theory]
        [InlineData(DataConstants.InvalidGUID, 0)]
        [InlineData(DataConstants.ValidGUID, 0)]
        [InlineData(DataConstants.ValidGUID, 2)]
        [InlineData(DataConstants.ValidGUID, 2, true)]
        public async void PostBulkAssetsAsync(string uid, int numberOfassetInsertList, bool checkAsSendByJSONContent = false)
        {
            var testURI = new Uri("http://www.testapi-gov.com/");
            var assetUID = Guid.Parse(uid);
            bool isGoodUID = uid == DataConstants.ValidGUID;
            assetsController.Request = new HttpRequestMessage()
            {
                RequestUri = testURI
            };

            var assetInsertList = new List<AssetInsert>();
            for (int i = 0; i < numberOfassetInsertList; i++)
            {
                assetInsertList.Add(new AssetInsert() { });
            }

            IHttpActionResult actionResult;
            Task<HttpResponseMessage> responseMessageResult;

            if (!isGoodUID)
            {
                actionResult = await assetsController.PostBulkAssetsAsync(assetUID, assetInsertList);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                Assert.True(!responseMessageResult.Result.IsSuccessStatusCode);
            }

            if (isGoodUID && numberOfassetInsertList == 0)
            {
                actionResult = await assetsController.PostBulkAssetsAsync(assetUID, null);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

                Assert.True(responseMessageResult.Result.StatusCode == System.Net.HttpStatusCode.InternalServerError);
            }

            if (isGoodUID && numberOfassetInsertList > 0)
            {
                actionResult = await assetsController.PostBulkAssetsAsync(assetUID, assetInsertList);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                var str = responseMessageResult.Result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<JObject>(str);

                Assert.True(responseMessageResult.Result.IsSuccessStatusCode);
                Assert.True(data != null);
                Assert.True(data.GetValue("ExecutionID") != null);
                Assert.True(data.GetValue("Message") != null);
                Assert.True(data.GetValue("Uri") != null);
            }

            if (checkAsSendByJSONContent)
            {

                var asJson = JsonConvert.SerializeObject(assetInsertList);
                assetInsertList = null;

                var content = new StringContent(asJson, Encoding.UTF8, "application/json");

                assetsController.Request = new HttpRequestMessage()
                {
                    Content = content,
                    RequestUri = testURI
                };

                actionResult = await assetsController.PostBulkAssetsAsync(assetUID, assetInsertList);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                var str = responseMessageResult.Result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<JObject>(str);

                Assert.True(responseMessageResult.Result.IsSuccessStatusCode);
                Assert.True(data != null);
                Assert.True(data.GetValue("ExecutionID") != null);
                Assert.True(data.GetValue("Message") != null);
                Assert.True(data.GetValue("Uri") != null);
            }
        }

        [Theory]
        [InlineData(DataConstants.InvalidGUID, 0)]
        [InlineData(DataConstants.ValidGUID, 0)]
        [InlineData(DataConstants.ValidGUID, 2)]
        [InlineData(DataConstants.ValidGUID, 2, true)]
        public async void PutBulkAssetsAsync(string uid, int numberOfassetInsertList, bool checkAsSendByJSONContent = false)
        {
            var assetUID = Guid.Parse(uid);
            bool isGoodUID = uid == DataConstants.ValidGUID;

            var testURI = new Uri("http://www.testapi-gov.com/");
            assetsController.Request = new HttpRequestMessage()
            {
                RequestUri = testURI
            };

            var assetUpdateList = new List<AssetUpdate>();
            for (int i = 0; i < numberOfassetInsertList; i++)
            {
                assetUpdateList.Add(new AssetUpdate() { });
            }

            IHttpActionResult actionResult;
            Task<HttpResponseMessage> responseMessageResult;

            if (!isGoodUID)
            {
                actionResult = await assetsController.PutBulkAssetsAsync(assetUID, assetUpdateList);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                Assert.True(!responseMessageResult.Result.IsSuccessStatusCode);
            }

            if (isGoodUID && numberOfassetInsertList == 0)
            {
                actionResult = await assetsController.PutBulkAssetsAsync(assetUID, null);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

                Assert.True(responseMessageResult.Result.StatusCode == System.Net.HttpStatusCode.InternalServerError);
            }

            if (isGoodUID && numberOfassetInsertList > 0)
            {
                actionResult = await assetsController.PutBulkAssetsAsync(assetUID, assetUpdateList);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                var str = responseMessageResult.Result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<JObject>(str);

                Assert.True(responseMessageResult.Result.IsSuccessStatusCode);
                Assert.True(data != null);
                Assert.True(data.GetValue("ExecutionID") != null);
                Assert.True(data.GetValue("Message") != null);
                Assert.True(data.GetValue("Uri") != null);
            }

            if (checkAsSendByJSONContent)
            {

                var asJson = JsonConvert.SerializeObject(assetUpdateList);
                assetUpdateList = null;

                var content = new StringContent(asJson, Encoding.UTF8, "application/json");

                assetsController.Request = new HttpRequestMessage()
                {
                    Content = content,
                    RequestUri = testURI
                };

                actionResult = await assetsController.PutBulkAssetsAsync(assetUID, assetUpdateList);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                var str = responseMessageResult.Result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<JObject>(str);

                Assert.True(responseMessageResult.Result.IsSuccessStatusCode);
                Assert.True(data != null);
                Assert.True(data.GetValue("ExecutionID") != null);
                Assert.True(data.GetValue("Message") != null);
                Assert.True(data.GetValue("Uri") != null);
            }
        }

        [Theory]
        [InlineData(DataConstants.InvalidGUID, false)]
        [InlineData(DataConstants.ValidGUID, false)]
        [InlineData(DataConstants.ValidGUID, true)]
        [InlineData(DataConstants.ValidGUID, true, true)]
        public async void DeleteBulkAssetsAsync(string uid, bool hasDeleteAsset, bool checkAsSendByJSONContent = false)
        {
            var assetUID = Guid.Parse(uid);
            bool isGoodUID = uid == DataConstants.ValidGUID;

            var testURI = new Uri("http://www.testapi-gov.com/");
            assetsController.Request = new HttpRequestMessage()
            {
                RequestUri = testURI
            };

            var assetDeletes = new AssetDeletes();
            if (!hasDeleteAsset)
                assetDeletes = null;

            IHttpActionResult actionResult;
            Task<HttpResponseMessage> responseMessageResult;

            if (!isGoodUID)
            {
                actionResult = await assetsController.DeleteBulkAssetsAsync(assetUID, assetDeletes);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                Assert.True(!responseMessageResult.Result.IsSuccessStatusCode);
            }

            if (isGoodUID && !hasDeleteAsset)
            {
                actionResult = await assetsController.DeleteBulkAssetsAsync(assetUID, assetDeletes);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

                Assert.True(responseMessageResult.Result.StatusCode == System.Net.HttpStatusCode.InternalServerError);
            }

            if (isGoodUID && hasDeleteAsset)
            {
                actionResult = await assetsController.DeleteBulkAssetsAsync(assetUID, assetDeletes);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                var str = responseMessageResult.Result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<JObject>(str);

                Assert.True(responseMessageResult.Result.IsSuccessStatusCode);
                Assert.True(data != null);
                Assert.True(data.GetValue("ExecutionID") != null);
                Assert.True(data.GetValue("Message") != null);
                Assert.True(data.GetValue("Uri") != null);
            }

            if (checkAsSendByJSONContent)
            {

                var asJson = JsonConvert.SerializeObject(assetDeletes);
                assetDeletes = null;

                var content = new StringContent(asJson, Encoding.UTF8, "application/json");

                assetsController.Request = new HttpRequestMessage()
                {
                    Content = content,
                    RequestUri = testURI

                };

                actionResult = await assetsController.DeleteBulkAssetsAsync(assetUID, assetDeletes);
                responseMessageResult = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
                var str = responseMessageResult.Result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<JObject>(str);

                Assert.True(responseMessageResult.Result.IsSuccessStatusCode);
                Assert.True(data != null);
                Assert.True(data.GetValue("ExecutionID") != null);
                Assert.True(data.GetValue("Message") != null);
                Assert.True(data.GetValue("Uri") != null);
            }
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
                actionResult = assetsController.GetExecutionStatus(executionUid);
                responseMessageResult = actionResult.Result.ExecuteAsync(new System.Threading.CancellationToken());

                Assert.True(responseMessageResult.Result.StatusCode == System.Net.HttpStatusCode.NotFound);
            }
            if (isGoodUID)
            {
                actionResult = assetsController.GetExecutionStatus(executionUid);
                responseMessageResult = actionResult.Result.ExecuteAsync(new System.Threading.CancellationToken());
                var str = responseMessageResult.Result.Content.ReadAsStringAsync().Result;
                var data = JsonConvert.DeserializeObject<JObject>(str);

                Assert.True(responseMessageResult.Result.IsSuccessStatusCode);
                Assert.True(data != null);

                var importantFields = new List<string>() { "CompletedOn", "Error", "Fields", "Processed", "StartedOn", "Total", "Results" };
                foreach(var field in importantFields)
                {
                    Assert.True(data.GetValue(field) != null);
                }

            }

        }

        [Fact]
        public async void PostAssetTypeAsync()
        {


            var insertItem = new AssetTypeInsert() {
                IconStyle = new IconStyleInsert()
            };

            HttpResponseMessage responseMessageResult;

            responseMessageResult = await GetResponseForPostAsset(insertItem);
            Assert.True(responseMessageResult.StatusCode == HttpStatusCode.BadRequest);
            

            insertItem.Class = AssetTypeClass.Glossary;
            responseMessageResult = await GetResponseForPostAsset(insertItem);
            Assert.True(responseMessageResult.StatusCode == HttpStatusCode.InternalServerError);

            insertItem.Name = "testName";
            responseMessageResult = await GetResponseForPostAsset(insertItem);
            Assert.True(responseMessageResult.StatusCode == HttpStatusCode.InternalServerError);

            insertItem.DisplayFormat = "{name}";
            responseMessageResult = await GetResponseForPostAsset(insertItem);
            Assert.True(responseMessageResult.StatusCode == HttpStatusCode.InternalServerError);

            insertItem.IconStyle.BackColor = "#000000";
            insertItem.IconStyle.ForeColor = "#000000";
            responseMessageResult = await GetResponseForPostAsset(insertItem);
            Assert.True(responseMessageResult.StatusCode == HttpStatusCode.OK);

            var str = responseMessageResult.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<JObject>(str);
            Assert.True(data != null);
            Assert.True(data.GetValue("Uid") != null);
            Assert.True(data.GetValue("Message") != null);
            Assert.True(data.GetValue("Success") != null);



        }

        [Fact]
        public async void PutAssetTypeAsync()
        {


            var insertItem = new AssetTypeInsert()
            {
                IconStyle = new IconStyleInsert()
            };

            HttpResponseMessage responseMessageResult;

            responseMessageResult = await GetResponseForPutAsset(insertItem);
            Assert.True(responseMessageResult.StatusCode == HttpStatusCode.BadRequest);


            insertItem.Class = AssetTypeClass.Glossary;
            responseMessageResult = await GetResponseForPutAsset(insertItem);
            Assert.True(responseMessageResult.StatusCode == HttpStatusCode.InternalServerError);

            insertItem.Name = "testName";
            insertItem.Uid = Guid.Parse(DataConstants.InvalidGUID);
            responseMessageResult = await GetResponseForPutAsset(insertItem);
            Assert.True(responseMessageResult.StatusCode == HttpStatusCode.NotFound);

            insertItem.Uid = Guid.Parse(DataConstants.ValidGUID);
            responseMessageResult = await GetResponseForPutAsset(insertItem);
            Assert.True(responseMessageResult.StatusCode == HttpStatusCode.InternalServerError);
           
            insertItem.DisplayFormat = "{name}";
            responseMessageResult = await GetResponseForPutAsset(insertItem);
            Assert.True(responseMessageResult.StatusCode == HttpStatusCode.InternalServerError);

            insertItem.IconStyle.BackColor = "#000000";
            insertItem.IconStyle.ForeColor = "#000000";
            responseMessageResult = await GetResponseForPutAsset(insertItem);
            Assert.True(responseMessageResult.StatusCode == HttpStatusCode.OK);

            var str = responseMessageResult.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<JObject>(str);
            Assert.True(data != null);
            Assert.True(data.GetValue("Uid") != null);
            Assert.True(data.GetValue("Message") != null);
            Assert.True(data.GetValue("Success") != null);



        }

        private async Task<HttpResponseMessage> GetResponseForPostAsset(AssetTypeInsert insertItem)
        {
            IHttpActionResult actionResult;

            actionResult = await assetsController.PostAssetTypeAsync(insertItem);
            return await actionResult.ExecuteAsync(new System.Threading.CancellationToken());
            
        }

        private async Task<HttpResponseMessage> GetResponseForPutAsset(AssetTypeInsert insertItem)
        {
            IHttpActionResult actionResult;

            actionResult = await assetsController.PutAssetTypeAsync(insertItem);
            return await actionResult.ExecuteAsync(new System.Threading.CancellationToken());

        }

    }
}
