using igx.IntegrationTests.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using igx.IntegrationTests.TestData;
using Newtonsoft.Json.Linq;
using Xunit.Priority;
using System.Threading;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace igx.IntegrationTests.ApiTests
{

    [Trait("Integration tests", "Assets GET tests")]
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class AssetControllerGeneralTests : BaseIntegrationTestClass
    {
        [Fact, Priority(0)]
        public async void T_1_01_GetAssetClasses()
        {
            Console.WriteLine("Starting test");
            string endpointUrl = URIHelper.AssetClassesUri;

            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedJson = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);
            Assert.True(parsedJson.Count > 0, XMsg.InvalidCount);
        }
        [Fact, Priority(10)]
        public async void T_1_02_GetAssetTypesAsync()
        {
            string endpointUrl = URIHelper.AssetTypesUri;

            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(!string.IsNullOrEmpty(content));

            if (parsedData.Count == 0)
                throw new Exception("Testing environment should have asset types!");

            foreach (var assetClass in parsedData.Select(x => x["Class"]).GroupBy(x => x["Name"]))
            {
                response = await httpClient.GetAsync(endpointUrl + "?Class=" + assetClass.Key.ToString().Replace(" ", ""));
                content = await response.Content.ReadAsStringAsync();
                parsedData = JsonConvert.DeserializeObject<JArray>(content);


                Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
                Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

                Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);
                Assert.True(parsedData.Count > 0, XMsg.InvalidCount);
            }


        }
    }

    [Trait("Integration tests", "Asset type CRUD tests")]
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class AssetControllerAssetTypeCRUDTests : BaseIntegrationTestClass
    {
        [Fact, Priority(30)]
        public async void T_2_01_AssetTypePost()
        {

            string endpointUrl = URIHelper.AssetsUri;
            var response = await httpClient.PostAsync(endpointUrl, AssetTypeTestData.AssetTypeInsert.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(parsedData.GetValue("Uid") != null, XMsg.MissingField("Uid"));
            Assert.True(parsedData.GetValue("Message") != null, XMsg.MissingField("Message"));
            Assert.True(parsedData.GetValue("Success") != null && parsedData.GetValue("Success").ToString() == "True", XMsg.MissingField("Success"));

            AssetTypeTestData.AssetTypeInsert.UpdateValueOnProperty("Uid", parsedData.GetValue("Uid"));
        }

        [Fact, Priority(40)]
        public async void T_2_02_AssetTypeGetAfterPost()
        {
            string endPointUrl = URIHelper.AssetTypesUri + "?Class=" + AssetTypeTestData.AssetTypeInsert["Class"].ToString();
            var response = await httpClient.GetAsync(endPointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var assetTypeApiViewModels = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(assetTypeApiViewModels.Count != 0, XMsg.NoContent);

            Assert.True(assetTypeApiViewModels.DoesContain(x => x["uid"].ToString() == AssetTypeTestData.AssetTypeInsert["Uid"].ToString()
                    && x["Name"].ToString() == AssetTypeTestData.AssetTypeInsert["Name"].ToString()
                    && x["Description"].ToString() == AssetTypeTestData.AssetTypeInsert["Description"].ToString()), XMsg.MissingAsset);
        }

        [Fact, Priority(50)]
        public async void T_2_03_AssetTypePut()
        {

            AssetTypeTestData.AssetTypeInsert.AppendValueOnProperty("Name", " edited on put");
            AssetTypeTestData.AssetTypeInsert.AppendValueOnProperty("Description", " edited on put");
            var endpointUrl = URIHelper.AssetsUri;
            var response = await httpClient.PutAsync(endpointUrl, AssetTypeTestData.AssetTypeInsert.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);


            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(parsedData.GetValue("Uid") != null, XMsg.MissingField("Uid"));
            Assert.True(parsedData.GetValue("Message") != null, XMsg.MissingField("Message"));
            Assert.True(parsedData.GetValue("Success") != null && parsedData.GetValue("Success").ToString() == "True", XMsg.MissingField("Success"));

        }

        [Fact, Priority(60)]
        public async void T_2_04_AssetTypeGetAfterPut()
        {
            string endPointUrl = URIHelper.AssetTypesUri + "?Class=" + AssetTypeTestData.AssetTypeInsert["Class"].ToString();
            var response = await httpClient.GetAsync(endPointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var assetTypeApiViewModels = JsonConvert.DeserializeObject<JArray>(content);


            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(assetTypeApiViewModels.Count != 0);
            Assert.True(assetTypeApiViewModels.DoesContain(x => x["uid"].ToString() == AssetTypeTestData.AssetTypeInsert["Uid"].ToString()
                               && x["Name"].ToString() == AssetTypeTestData.AssetTypeInsert["Name"].ToString()
                               && x["Description"].ToString() == AssetTypeTestData.AssetTypeInsert["Description"].ToString()), XMsg.MissingAsset);
        }

        [Fact, Priority(70)]
        public async void T_2_05_AssetTypeDelete()
        {
            var endpointUrl = URIHelper.AssetsUri;
            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = AssetTypeTestData.GetDeleteAssetTypeJSON(Guid.Parse(AssetTypeTestData.AssetTypeInsert["Uid"].ToString())).AsStringContent(),
                Method = HttpMethod.Delete,
                RequestUri = new Uri(endpointUrl)
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();


            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);
            Assert.True(parsedData.GetValue("ExecutionID") != null, XMsg.MissingField("ExecutionID"));
            Assert.True(parsedData.GetValue("Message") != null, XMsg.MissingField("Message"));
            Assert.True(parsedData.GetValue("Uri") != null, XMsg.MissingField("Uri"));

            AssetTypeTestData.ExecutionUrl = parsedData.GetValue("Uri").ToString();
        }

        [Fact, Priority(80)]
        private async Task<bool> T_2_06_ExecutionStatusCheck()
        {

            int retryCount = 1;
            int retryMax = 50;
            bool doRetry = true;
            bool isSuccess = false;

            while (doRetry)
            {
                var response = await httpClient.GetAsync(AssetTypeTestData.ExecutionUrl);
                var content = await response.Content.ReadAsStringAsync();
                var parsedData = JsonConvert.DeserializeObject<JObject>(content);

                Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
                Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

                if (parsedData["Results"] != null && parsedData["Results"].Count() > 0)
                {
                    doRetry = false;
                    isSuccess = parsedData["Results"].All(x => x["Success"].ToString().ToLower() == "true");
                }
                retryCount++;
                if (retryCount == retryMax) doRetry = false;

                Thread.Sleep(2000);
            }

            return isSuccess;
        }
    }

    [Trait("Integration tests", "Assets CRUD tests")]
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class AssetControllerAssetsCRUDTest : BaseIntegrationTestClass
    {
        [Fact, Priority(100)]
        public async void T_3_01_AssetTypePost()
        {

            string endpointUrl = URIHelper.AssetsUri;
            var response = await httpClient.PostAsync(endpointUrl, AssetTestData.AssetTypeInsert.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);


            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(parsedData.GetValue("Uid") != null, XMsg.MissingField("Uid"));
            Assert.True(parsedData.GetValue("Message") != null, XMsg.MissingField("Message"));
            Assert.True(parsedData.GetValue("Success") != null && parsedData.GetValue("Success").ToString() == "True", XMsg.MissingField("Success"));


            AssetTestData.AssetTypeInsert.UpdateValueOnProperty("Uid", parsedData.GetValue("Uid").ToString());
        }

        [Fact, Priority(110)]
        public async void T_3_02_AssetTypeGetAfterPost()
        {
            string endPointUrl = URIHelper.AssetsUri + "/types?Class=" + AssetTestData.AssetTypeInsert["Class"].ToString();
            var response = await httpClient.GetAsync(endPointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var assetTypeApiViewModels = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(assetTypeApiViewModels.Count != 0, XMsg.MissingAsset);
            Assert.True(assetTypeApiViewModels.DoesContain(x => x["uid"].ToString() == AssetTestData.AssetTypeInsert["Uid"].ToString() && x["Name"].ToString() == AssetTestData.AssetTypeInsert["Name"].ToString() && x["Description"].ToString() == AssetTestData.AssetTypeInsert["Description"].ToString()), XMsg.MissingAsset);
        }

        [Fact, Priority(120)]
        public async void T_3_03_AssetsPost()
        {
            string endPointUrl = URIHelper.AssetsUri + AssetTestData.AssetTypeInsert["Uid"].ToString();
            var response = await httpClient.PostAsync(endPointUrl, AssetTestData.AssetInserts.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var databaseBulkAssetResults = JsonConvert.DeserializeObject<JArray>(content);


            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(databaseBulkAssetResults.Count == AssetTestData.AssetInserts.Count, XMsg.MissingAsset);
            Assert.True(databaseBulkAssetResults.All(x => x["Success"].ToString().ToLower() == "true"), "Success should be set to true");

            foreach (var item in databaseBulkAssetResults.Select((value, index) => new { index, value }))
            {
                Assert.True(item.value["uid"].ToString() != Guid.Empty.ToString(), "Guid not valid");
                AssetTestData.AssetInserts[item.index].UpdateValueOnProperty("Uid", item.value["uid"].ToString());
            }

        }

        [Fact, Priority(130)]
        public async void T_3_04_GetAssetsAfterPost()
        {
            string endPointUrl = URIHelper.AssetsUri + AssetTestData.AssetTypeInsert["Uid"].ToString();
            var response = await httpClient.GetAsync(endPointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var assetsApiViewModel = JsonConvert.DeserializeObject<JObject>(content);


            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(int.Parse(assetsApiViewModel["total"].ToString()) == AssetTestData.AssetInserts.Count);

            foreach (var item in assetsApiViewModel["items"])
            {
                var compareItem = AssetTestData.AssetInserts.Where(x => Guid.Parse(x["Uid"].ToString()) == Guid.Parse(Convert.ToString(item["AssetUid"]))).FirstOrDefault();
                Assert.True(compareItem != null, XMsg.MissingAsset);
                Assert.True(compareItem["Fields"]["Name"].ToString() == Convert.ToString(item["Name"]), XMsg.InvalidFieldValue("Name"));

            }

        }

        [Fact, Priority(140)]
        public async void T_3_05_PutAssets()
        {
            foreach (var inserted in AssetTestData.AssetInserts.Select((value, index) => new { index, value }))
            {
                AssetTestData.AssetUpdates[inserted.index].UpdateValueOnProperty("Uid", inserted.value["Uid"].ToString());
            }

            string endPointUrl = URIHelper.AssetsUri + AssetTestData.AssetTypeInsert["Uid"].ToString();
            var response = await httpClient.PutAsync(endPointUrl, AssetTestData.AssetUpdates.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var databaseBulkAssetResults = JsonConvert.DeserializeObject<JArray>(content);


            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(databaseBulkAssetResults.Count == AssetTestData.AssetInserts.Count, XMsg.MissingAsset);
            Assert.True(databaseBulkAssetResults.All(x => x["Success"].ToString().ToLower() == "true"), XMsg.InvalidFieldValue("Success"));

            foreach (var item in databaseBulkAssetResults.Select((value, index) => new { index, value }))
            {
                Assert.True(item.value["uid"].ToString() != Guid.Empty.ToString(), XMsg.InvalidFieldValue("uid"));
                AssetTestData.AssetInserts[item.index].UpdateValueOnProperty("Uid", item.value["uid"].ToString());
                AssetTestData.AssetInserts[item.index]["Fields"].UpdateValueOnProperty("Name", AssetTestData.AssetUpdates[item.index]["Fields"]["Name"]);
            }
        }

        [Fact, Priority(150)]
        public async void T_3_06_GetAssetsAfterPut()
        {
            string endPointUrl = URIHelper.AssetsUri + AssetTestData.AssetTypeInsert["Uid"].ToString();
            var response = await httpClient.GetAsync(endPointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var assetsApiViewModel = JsonConvert.DeserializeObject<JObject>(content);


            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(int.Parse(assetsApiViewModel["total"].ToString()) == AssetTestData.AssetInserts.Count, XMsg.MissingAsset);

            foreach (var item in assetsApiViewModel["items"])
            {
                var compareItem = AssetTestData.AssetInserts.Where(x => Guid.Parse(x["Uid"].ToString()) == Guid.Parse(Convert.ToString(item["AssetUid"]))).FirstOrDefault();
                Assert.True(compareItem != null, XMsg.MissingAsset);
                Assert.True(compareItem["Fields"]["Name"].ToString() == Convert.ToString(item["Name"]), XMsg.InvalidFieldValue("Name"));

            }

        }

        [Fact, Priority(160)]
        public async void T_3_07_AssetDelete()
        {
            List<Guid> forDelete = new List<Guid>();

            foreach (var item in AssetTestData.AssetUpdates)
            {
                forDelete.Add(Guid.Parse(item["Uid"].ToString()));
            }
            string endPointUrl = URIHelper.AssetsUri + AssetTestData.AssetTypeInsert["Uid"].ToString();
            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = AssetTestData.GetDeleteAssetJSON(forDelete).AsStringContent(),
                Method = HttpMethod.Delete,
                RequestUri = new Uri(endPointUrl)
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            var parsedData = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(parsedData.All(x => x["Success"].ToString().ToLower() == "true"), XMsg.InvalidFieldValue("Success"));
            Assert.True(parsedData.Count == forDelete.Count, XMsg.InvalidCount);

        }

        [Fact, Priority(170)]
        public async void T_3_08_AssetTypeDelete()
        {
            var endpointUrl = URIHelper.AssetsUri;
            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = AssetTypeTestData.GetDeleteAssetTypeJSON(Guid.Parse(AssetTestData.AssetTypeInsert["Uid"].ToString())).AsStringContent(),
                Method = HttpMethod.Delete,
                RequestUri = new Uri(endpointUrl)
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);
            Assert.True(parsedData.GetValue("ExecutionID") != null, XMsg.InvalidFieldValue("ExecutionID"));
            Assert.True(parsedData.GetValue("Message") != null, XMsg.InvalidFieldValue("Message"));
            Assert.True(parsedData.GetValue("Uri") != null, XMsg.InvalidFieldValue("Uri"));

            AssetTestData.ExecutionUrl = parsedData.GetValue("Uri").ToString();
        }

        [Fact, Priority(180)]
        private async Task<bool> T_3_08_ExecutionStatusCheck()
        {

            int retryCount = 1;
            int retryMax = 50;
            bool doRetry = true;
            bool isSuccess = false;

            while (doRetry)
            {
                var response = await httpClient.GetAsync(AssetTestData.ExecutionUrl);
                var content = await response.Content.ReadAsStringAsync();
                var parsedData = JsonConvert.DeserializeObject<JObject>(content);

                Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
                Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

                if (parsedData["Results"] != null && parsedData["Results"].Count() > 0)
                {
                    doRetry = false;
                    isSuccess = parsedData["Results"].All(x => x["Success"].ToString().ToLower() == "true");
                }
                retryCount++;
                if (retryCount == retryMax) doRetry = false;

                Thread.Sleep(2000);
            }

            return isSuccess;
        }

    }

    [Trait("Integration tests", "Asset BULK tests")]
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class AssetControllerBulkTests : BaseIntegrationTestClass
    {
        [Fact, Priority(200)]
        public async void T_4_01_AssetTypePost()
        {

            string endpointUrl = URIHelper.AssetsUri;
            var response = await httpClient.PostAsync(endpointUrl, BulkTestData.assetTypeInsert.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);


            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(parsedData.GetValue("Uid") != null, XMsg.InvalidFieldValue("Uid"));
            Assert.True(parsedData.GetValue("Message") != null, XMsg.InvalidFieldValue("Message"));
            Assert.True(parsedData.GetValue("Success") != null && parsedData.GetValue("Success").ToString() == "True", XMsg.InvalidFieldValue("Success"));

            BulkTestData.assetTypeInsert.UpdateValueOnProperty("Uid", parsedData.GetValue("Uid"));
        }

        [Fact, Priority(210)]
        public async void T_4_02_AssetTypeGetAfterPost()
        {
            string endPointUrl = URIHelper.AssetsUri + "/types?Class=" + BulkTestData.assetTypeInsert["Class"].ToString();
            var response = await httpClient.GetAsync(endPointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var assetTypeApiViewModels = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(assetTypeApiViewModels.Count != 0);
            Assert.True(assetTypeApiViewModels.DoesContain(x => x["uid"].ToString() == BulkTestData.assetTypeInsert["Uid"].ToString()
                && x["Name"].ToString() == BulkTestData.assetTypeInsert["Name"].ToString()
                && x["Description"].ToString() == BulkTestData.assetTypeInsert["Description"].ToString()), XMsg.MissingAsset);
        }

        [Fact, Priority(220)]
        public async void T_4_03_BatchAssetPost()
        {
            string endpoint = URIHelper.AssetsBatchUri + BulkTestData.assetTypeInsert["Uid"].ToString();
            var response = await httpClient.PostAsync(endpoint, BulkTestData.assetInserts.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);
            Assert.True(parsedData.GetValue("ExecutionID") != null, XMsg.InvalidFieldValue("ExecutionID"));
            Assert.True(parsedData.GetValue("Message") != null, XMsg.InvalidFieldValue("Message"));
            Assert.True(parsedData.GetValue("Uri") != null, XMsg.InvalidFieldValue("Uri"));

            BulkTestData.ExecutionUrl = parsedData.GetValue("Uri").ToString();
            Assert.True(await T_4_09_ExecutionStatusCheck() == true, XMsg.ExecutionStatusErr);

        }

        [Fact, Priority(230)]
        public async void T_4_04_BatchGetAssetsAfterPost()
        {
            string endPointUrl = URIHelper.AssetsUri + BulkTestData.assetTypeInsert["Uid"].ToString();
            var response = await httpClient.GetAsync(endPointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var assetsApiViewModel = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(int.Parse(assetsApiViewModel["total"].ToString()) == BulkTestData.assetInserts.Count, XMsg.InvalidCount);

            foreach (var item in assetsApiViewModel["items"])
            {
                var compareItem = BulkTestData.assetInserts.Where(x => x["Fields"]["Name"].ToString() == Convert.ToString(item["Name"])).FirstOrDefault();
                Assert.True(compareItem != null, XMsg.MissingAsset);
                Assert.True(compareItem["Fields"]["Name"].ToString() == item["Name"].ToString(), XMsg.InvalidFieldValue("Name"));
                (BulkTestData.assetInserts.FirstOrDefault(x => x["Fields"]["Name"].ToString() == item["Name"].ToString()) as JObject).AddNewToken("Uid", item["AssetUid"].ToString());

                Assert.True(BulkTestData.assetTypeInsert["Uid"].ToString() == item["AssetTypeUid"].ToString(), XMsg.InvalidFieldValue("AssetTypeUid"));
            }

        }

        [Fact, Priority(240)]
        public async void T_4_05_BatchPutAssets()
        {
            foreach (var inserted in BulkTestData.assetInserts.Select((value, index) => new { index, value }))
            {
                BulkTestData.assetUpdates[inserted.index].UpdateValueOnProperty("Uid", inserted.value["Uid"].ToString());
            }

            string endPointUrl = URIHelper.AssetsBatchUri + BulkTestData.assetTypeInsert["Uid"].ToString();
            var response = await httpClient.PutAsync(endPointUrl, BulkTestData.assetUpdates.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var databaseBulkAssetResults = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);
            Assert.True(parsedData.GetValue("ExecutionID") != null, XMsg.InvalidFieldValue("ExecutionID"));
            Assert.True(parsedData.GetValue("Message") != null, XMsg.InvalidFieldValue("Message"));
            Assert.True(parsedData.GetValue("Uri") != null, XMsg.InvalidFieldValue("Uri"));

            BulkTestData.ExecutionUrl = parsedData.GetValue("Uri").ToString();
            Assert.True(await T_4_09_ExecutionStatusCheck() == true,XMsg.ExecutionStatusErr);
        }

        [Fact, Priority(250)]
        public async void T_4_06_GetAssetsAfterPut()
        {
            string endPointUrl = URIHelper.AssetsUri + BulkTestData.assetTypeInsert["Uid"].ToString();
            var response = await httpClient.GetAsync(endPointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var assetsApiViewModel = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(int.Parse(assetsApiViewModel["total"].ToString()) == BulkTestData.assetUpdates.Count, XMsg.InvalidCount);

            foreach (var item in assetsApiViewModel["items"])
            {
                var compareItem = BulkTestData.assetUpdates.Where(x => x["Fields"]["Name"].ToString() == Convert.ToString(item["Name"])).FirstOrDefault();
                Assert.True(compareItem != null, XMsg.MissingAsset);
                Assert.True(compareItem["Fields"]["Name"].ToString() == item["Name"].ToString(), XMsg.InvalidFieldValue("Name"));
                Assert.True(BulkTestData.assetTypeInsert["Uid"].ToString() == item["AssetTypeUid"].ToString(), XMsg.InvalidFieldValue("AssetTypeUid"));
            }

        }

        [Fact, Priority(260)]
        public async void T_4_07_BulkDeleteAsset()
        {

            List<Guid> forDelete = new List<Guid>();

            foreach (var item in BulkTestData.assetUpdates)
            {
                forDelete.Add(Guid.Parse(item["Uid"].ToString()));
            }
            string endPointUrl = URIHelper.AssetsBatchUri + BulkTestData.assetTypeInsert["Uid"].ToString();
            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = AssetTestData.GetDeleteAssetJSON(forDelete).AsStringContent(),
                Method = HttpMethod.Delete,
                RequestUri = new Uri(endPointUrl)
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);
            Assert.True(parsedData.GetValue("ExecutionID") != null, XMsg.InvalidFieldValue("ExecutionID"));
            Assert.True(parsedData.GetValue("Message") != null, XMsg.InvalidFieldValue("Message"));
            Assert.True(parsedData.GetValue("Uri") != null, XMsg.InvalidFieldValue("Uri"));

            BulkTestData.ExecutionUrl = parsedData.GetValue("Uri").ToString();
            Assert.True(await T_4_09_ExecutionStatusCheck() == true, XMsg.ExecutionStatusErr);
        }

        [Fact, Priority(280)]
        public async void T_4_08_AssetTypeDelete()
        {
            var endpointUrl = URIHelper.AssetsUri;
            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = AssetTypeTestData.GetDeleteAssetTypeJSON(Guid.Parse(BulkTestData.assetTypeInsert["Uid"].ToString())).AsStringContent(),
                Method = HttpMethod.Delete,
                RequestUri = new Uri(endpointUrl)
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);
            Assert.True(parsedData.GetValue("ExecutionID") != null, XMsg.InvalidFieldValue("ExecutionID"));
            Assert.True(parsedData.GetValue("Message") != null, XMsg.InvalidFieldValue("Message"));
            Assert.True(parsedData.GetValue("Uri") != null, XMsg.InvalidFieldValue("Uri"));

            BulkTestData.ExecutionUrl = parsedData.GetValue("Uri").ToString();
        }

        [Fact, Priority(290)]
        private async Task<bool> T_4_09_ExecutionStatusCheck()
        {
            int retryCount = 1;
            int retryMax = 50;
            bool doRetry = true;
            bool isSuccess = false;

            while (doRetry)
            {
                var response = await httpClient.GetAsync(BulkTestData.ExecutionUrl);
                var content = await response.Content.ReadAsStringAsync();
                var parsedData = JsonConvert.DeserializeObject<JObject>(content);

                Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
                Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

                if (parsedData["Results"] != null && parsedData["Results"].Count() > 0)
                {
                    doRetry = false;
                    isSuccess = parsedData["Results"].All(x => x["Success"].ToString().ToLower() == "true");
                }
                retryCount++;
                if (retryCount == retryMax) doRetry = false;

                Thread.Sleep(2000);
            }

            return isSuccess;
        }

    }

}
