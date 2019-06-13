using igx.IntegrationTests.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using d360.core.enums;
using d360.model;
using d360.core.entities;
using igx.IntegrationTests.TestData;
using Newtonsoft.Json.Linq;
using Xunit.Priority;
using d360.web.Models;
using System.Threading;
using Xunit;
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace igx.IntegrationTests.ApiTests
{
    [Trait("Integration tests", "General tests")]
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
            var parsedJson = JsonConvert.DeserializeObject<List<AssetTypeClassInfo>>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(!string.IsNullOrEmpty(content));
            Assert.True(parsedJson.Count == AssetTypeClass.Glossary.GetAsList().Count);

            Assert.True(1 == 1);
        }
        [Fact, Priority(10)]
        public async void T_1_02_GetAssetTypesAsync()
        {
            string endpointUrl = URIHelper.AssetTypesUri;

            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<List<AssetTypeApiViewModel>>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(!string.IsNullOrEmpty(content));

            if (parsedData.Count == 0)
                throw new Exception("Testing environment should have asset types!");

            foreach (var assetClass in parsedData.Select(x => x.Class).GroupBy(x => x.Name))
            {
                response = await httpClient.GetAsync(endpointUrl + "?Class=" + assetClass.Key.Replace(" ", ""));
                content = await response.Content.ReadAsStringAsync();
                parsedData = JsonConvert.DeserializeObject<List<AssetTypeApiViewModel>>(content);

                Assert.True(response.IsSuccessStatusCode);
                Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
                Assert.True(!string.IsNullOrEmpty(content));
                Assert.True(parsedData.Count == assetClass.Count());
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
            var response = await httpClient.PostAsJsonAsync(endpointUrl, AssetTypeTestData.assetTypeInsert);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(parsedData.GetValue("Uid") != null);
            Assert.True(parsedData.GetValue("Message") != null);
            Assert.True(parsedData.GetValue("Success") != null && parsedData.GetValue("Success").ToString() == "True");

            AssetTypeTestData.assetTypeInsert.Uid = Guid.Parse(parsedData.GetValue("Uid").ToString());
        }

        [Fact, Priority(40)]
        public async void T_2_02_AssetTypeGetAfterPost()
        {
            string endPointUrl = URIHelper.AssetTypesUri + "?Class=" + AssetTypeTestData.assetTypeInsert.Class.ToString();
            var response = await httpClient.GetAsync(endPointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var assetTypeApiViewModels = JsonConvert.DeserializeObject<List<AssetTypeApiViewModel>>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");

            Assert.True(assetTypeApiViewModels.Count != 0);
            Assert.Contains(assetTypeApiViewModels, x => x.uid == AssetTypeTestData.assetTypeInsert.Uid && x.Name == AssetTypeTestData.assetTypeInsert.Name && x.Description == AssetTypeTestData.assetTypeInsert.Description);
        }

        [Fact, Priority(50)]
        public async void T_2_03_AssetTypePut()
        {
            AssetTypeTestData.assetTypeInsert.Name += " edited on put";
            AssetTypeTestData.assetTypeInsert.Description += " edited on put";
            var endpointUrl = URIHelper.AssetsUri;
            var response = await httpClient.PutAsJsonAsync(endpointUrl, AssetTypeTestData.assetTypeInsert);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(parsedData.GetValue("Uid") != null);
            Assert.True(parsedData.GetValue("Message") != null);
            Assert.True(parsedData.GetValue("Success") != null && parsedData.GetValue("Success").ToString() == "True");
        }

        [Fact, Priority(60)]
        public async void T_2_04_AssetTypeGetAfterPut()
        {
            string endPointUrl = URIHelper.AssetTypesUri + "?Class=" + AssetTypeTestData.assetTypeInsert.Class.ToString();
            var response = await httpClient.GetAsync(endPointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var assetTypeApiViewModels = JsonConvert.DeserializeObject<List<AssetTypeApiViewModel>>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");

            Assert.True(assetTypeApiViewModels.Count != 0);
            Assert.Contains(assetTypeApiViewModels, x => x.uid == AssetTypeTestData.assetTypeInsert.Uid && x.Name == AssetTypeTestData.assetTypeInsert.Name && x.Description == AssetTypeTestData.assetTypeInsert.Description);
        }

        [Fact, Priority(70)]
        public async void T_2_05_AssetTypeDelete()
        {
            AssetTypeDeletes forDelete = new AssetTypeDeletes();
            forDelete.Add(new AssetTypeDelete() { Uid = AssetTypeTestData.assetTypeInsert.Uid });
            var endpointUrl = URIHelper.AssetsUri;
            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(forDelete), Encoding.UTF8, "application/json"),
                Method = HttpMethod.Delete,
                RequestUri = new Uri(endpointUrl)
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);
            Assert.True(parsedData.GetValue("ExecutionID") != null);
            Assert.True(parsedData.GetValue("Message") != null);
            Assert.True(parsedData.GetValue("Uri") != null);

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
                var parsedData = JsonConvert.DeserializeObject<ApiExecutionStatusModel>(content);


                if (parsedData.Results != null && parsedData.Results.Count > 0)
                {
                    doRetry = false;
                    isSuccess = parsedData.Results.All(x => x.Success == true);
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
            var response = await httpClient.PostAsJsonAsync(endpointUrl, AssetTestData.assetTypeInsert);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(parsedData.GetValue("Uid") != null);
            Assert.True(parsedData.GetValue("Message") != null);
            Assert.True(parsedData.GetValue("Success") != null && parsedData.GetValue("Success").ToString() == "True");

            AssetTestData.assetTypeInsert.Uid = Guid.Parse(parsedData.GetValue("Uid").ToString());
        }

        [Fact, Priority(110)]
        public async void T_3_02_AssetTypeGetAfterPost()
        {
            string endPointUrl = URIHelper.AssetsUri + "/types?Class=" + AssetTestData.assetTypeInsert.Class.ToString();
            var response = await httpClient.GetAsync(endPointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var assetTypeApiViewModels = JsonConvert.DeserializeObject<List<AssetTypeApiViewModel>>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");

            Assert.True(assetTypeApiViewModels.Count != 0);
            Assert.Contains(assetTypeApiViewModels, x => x.uid == AssetTestData.assetTypeInsert.Uid && x.Name == AssetTestData.assetTypeInsert.Name && x.Description == AssetTestData.assetTypeInsert.Description);
        }

        [Fact, Priority(120)]
        public async void T_3_03_AssetsPost()
        {
            string endPointUrl = URIHelper.AssetsUri + AssetTestData.assetTypeInsert.Uid.ToString();
            var response = await httpClient.PostAsJsonAsync(endPointUrl, AssetTestData.assetInserts);
            var content = await response.Content.ReadAsStringAsync();
            var databaseBulkAssetResults = JsonConvert.DeserializeObject<List<DatabaseBulkAssetResult>>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");

            Assert.True(databaseBulkAssetResults.Count == AssetTestData.assetInserts.Count);
            Assert.True(databaseBulkAssetResults.All(x => x.Success == true));

            foreach (var item in databaseBulkAssetResults.Select((value, index) => new { index, value }))
            {
                Assert.True(item.value.uid != Guid.Empty);
                AssetTestData.assetInserts[item.index].Uid = item.value.uid;
            }

        }

        [Fact, Priority(130)]
        public async void T_3_04_GetAssetsAfterPost()
        {
            string endPointUrl = URIHelper.AssetsUri + AssetTestData.assetTypeInsert.Uid.ToString();
            var response = await httpClient.GetAsync(endPointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var assetsApiViewModel = JsonConvert.DeserializeObject<AssetsApiViewModel>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");

            Assert.True(assetsApiViewModel.total == AssetTestData.assetInserts.Count);

            foreach (var item in assetsApiViewModel.items)
            {
                var compareItem = AssetTestData.assetInserts.Where(x => x.Uid == Guid.Parse(Convert.ToString(item.AssetUid))).FirstOrDefault();
                Assert.True(compareItem != null);
                Assert.True(compareItem.Fields["Name"] == Convert.ToString(item.Name));

            }

        }

        [Fact, Priority(140)]
        public async void T_3_05_PutAssets()
        {
            foreach (var inserted in AssetTestData.assetInserts.Select((value, index) => new { index, value }))
            {
                AssetTestData.assetUpdates[inserted.index].Uid = inserted.value.Uid;
            }

            string endPointUrl = URIHelper.AssetsUri + AssetTestData.assetTypeInsert.Uid.ToString();
            var response = await httpClient.PutAsJsonAsync(endPointUrl, AssetTestData.assetUpdates);
            var content = await response.Content.ReadAsStringAsync();
            var databaseBulkAssetResults = JsonConvert.DeserializeObject<List<DatabaseBulkAssetResult>>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");

            Assert.True(databaseBulkAssetResults.Count == AssetTestData.assetUpdates.Count);
            Assert.True(databaseBulkAssetResults.All(x => x.Success == true));

            foreach (var item in databaseBulkAssetResults.Select((value, index) => new { index, value }))
            {
                Assert.True(item.value.uid != Guid.Empty);
                AssetTestData.assetUpdates[item.index].Uid = item.value.uid;
            }
        }

        [Fact, Priority(150)]
        public async void T_3_06_GetAssetsAfterPost()
        {
            string endPointUrl = URIHelper.AssetsUri + AssetTestData.assetTypeInsert.Uid.ToString();
            var response = await httpClient.GetAsync(endPointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var assetsApiViewModel = JsonConvert.DeserializeObject<AssetsApiViewModel>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");

            Assert.True(assetsApiViewModel.total == AssetTestData.assetUpdates.Count);

            foreach (var item in assetsApiViewModel.items)
            {
                var compareItem = AssetTestData.assetUpdates.Where(x => x.Uid == Guid.Parse(Convert.ToString(item.AssetUid))).FirstOrDefault();
                Assert.True(compareItem != null);
                Assert.True(compareItem.Fields["Name"] == Convert.ToString(item.Name));

            }

        }

        [Fact, Priority(160)]
        public async void T_3_07_AssetDelete()
        {
            AssetDeletes forDelete = new AssetDeletes();

            foreach (var item in AssetTestData.assetUpdates)
            {
                forDelete.Add(new AssetDelete() { Uid = item.Uid });
            }
            string endPointUrl = URIHelper.AssetsUri + AssetTestData.assetTypeInsert.Uid.ToString();
            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(forDelete), Encoding.UTF8, "application/json"),
                Method = HttpMethod.Delete,
                RequestUri = new Uri(endPointUrl)
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            var parsedData = JsonConvert.DeserializeObject<List<DatabaseBulkAssetResult>>(content);

            Assert.True(parsedData.All(x => x.Success == true));
            Assert.True(parsedData.Count == forDelete.Count);

        }

        [Fact, Priority(170)]
        public async void T_3_08_AssetTypeDelete()
        {
            AssetTypeDeletes forDelete = new AssetTypeDeletes();
            forDelete.Add(new AssetTypeDelete() { Uid = AssetTestData.assetTypeInsert.Uid });
            var endpointUrl = URIHelper.AssetsUri;
            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(forDelete), Encoding.UTF8, "application/json"),
                Method = HttpMethod.Delete,
                RequestUri = new Uri(endpointUrl)
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);
            Assert.True(parsedData.GetValue("ExecutionID") != null);
            Assert.True(parsedData.GetValue("Message") != null);
            Assert.True(parsedData.GetValue("Uri") != null);

            AssetTestData.ExecutionUrl = parsedData.GetValue("Uri").ToString();
        }

        [Fact, Priority(180)]
        private async Task<bool> T_3_08_ExecutionStatusCheck()
        {

            int retryCount = 1;
            int retryMax = 10;
            bool doRetry = true;
            bool isSuccess = false;

            while (doRetry)
            {
                var response = await httpClient.GetAsync(AssetTestData.ExecutionUrl);
                var content = await response.Content.ReadAsStringAsync();
                var parsedData = JsonConvert.DeserializeObject<ApiExecutionStatusModel>(content);


                if (parsedData.Results != null && parsedData.Results.Count > 0)
                {
                    doRetry = false;
                    isSuccess = parsedData.Results.All(x => x.Success == true);
                }
                retryCount++;
                if (retryCount == retryMax) doRetry = false;

                Thread.Sleep(10000);
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
            var response = await httpClient.PostAsJsonAsync(endpointUrl, BulkTestData.assetTypeInsert);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(parsedData.GetValue("Uid") != null);
            Assert.True(parsedData.GetValue("Message") != null);
            Assert.True(parsedData.GetValue("Success") != null && parsedData.GetValue("Success").ToString() == "True");

            BulkTestData.assetTypeInsert.Uid = Guid.Parse(parsedData.GetValue("Uid").ToString());
        }

        [Fact, Priority(210)]
        public async void T_4_02_AssetTypeGetAfterPost()
        {
            string endPointUrl = URIHelper.AssetsUri + "/types?Class=" + BulkTestData.assetTypeInsert.Class.ToString();
            var response = await httpClient.GetAsync(endPointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var assetTypeApiViewModels = JsonConvert.DeserializeObject<List<AssetTypeApiViewModel>>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");

            Assert.True(assetTypeApiViewModels.Count != 0);
            Assert.Contains(assetTypeApiViewModels, x => x.uid == BulkTestData.assetTypeInsert.Uid && x.Name == BulkTestData.assetTypeInsert.Name && x.Description == BulkTestData.assetTypeInsert.Description);
        }

        [Fact, Priority(220)]
        public async void T_4_03_BatchAssetPost()
        {
            string endpoint = URIHelper.AssetsBatchUri + BulkTestData.assetTypeInsert.Uid;
            var response = await httpClient.PostAsJsonAsync(endpoint, BulkTestData.assetInserts);
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);
            Assert.True(parsedData.GetValue("ExecutionID") != null);
            Assert.True(parsedData.GetValue("Message") != null);
            Assert.True(parsedData.GetValue("Uri") != null);

            BulkTestData.ExecutionUrl = parsedData.GetValue("Uri").ToString();
            Assert.True(await T_4_09_ExecutionStatusCheck() == true);

        }

        [Fact, Priority(230)]
        public async void T_4_04_BatchGetAssetsAfterPost()
        {
            string endPointUrl = URIHelper.AssetsUri + BulkTestData.assetTypeInsert.Uid.ToString();
            var response = await httpClient.GetAsync(endPointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var assetsApiViewModel = JsonConvert.DeserializeObject<AssetsApiViewModel>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");

            Assert.True(assetsApiViewModel.total == BulkTestData.assetInserts.Count);

            foreach (var item in assetsApiViewModel.items)
            {
                var compareItem = BulkTestData.assetInserts.Where(x => x.Fields["Name"] == Convert.ToString(item.Name)).FirstOrDefault();
                Assert.True(compareItem != null);
                compareItem.Uid = item["AssetUid"];
            }

        }

        [Fact, Priority(240)]
        public async void T_4_05_BatchPutAssets()
        {
            foreach (var inserted in BulkTestData.assetInserts.Select((value, index) => new { index, value }))
            {
                BulkTestData.assetUpdates[inserted.index].Uid = inserted.value.Uid;
            }

            string endPointUrl = URIHelper.AssetsBatchUri + BulkTestData.assetTypeInsert.Uid.ToString();
            var response = await httpClient.PutAsJsonAsync(endPointUrl, BulkTestData.assetUpdates);
            var content = await response.Content.ReadAsStringAsync();
            var databaseBulkAssetResults = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);
            Assert.True(parsedData.GetValue("ExecutionID") != null);
            Assert.True(parsedData.GetValue("Message") != null);
            Assert.True(parsedData.GetValue("Uri") != null);

            BulkTestData.ExecutionUrl = parsedData.GetValue("Uri").ToString();
            Assert.True(await T_4_09_ExecutionStatusCheck() == true);
        }

        [Fact, Priority(250)]
        public async void T_4_06_GetAssetsAfterPut()
        {
            string endPointUrl = URIHelper.AssetsUri + BulkTestData.assetTypeInsert.Uid.ToString();
            var response = await httpClient.GetAsync(endPointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var assetsApiViewModel = JsonConvert.DeserializeObject<AssetsApiViewModel>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");

            Assert.True(assetsApiViewModel.total == BulkTestData.assetUpdates.Count);

            foreach (var item in assetsApiViewModel.items)
            {
                var compareItem = BulkTestData.assetUpdates.Where(x => x.Uid == Guid.Parse(Convert.ToString(item.AssetUid))).FirstOrDefault();
                Assert.True(compareItem != null);
                Assert.True(compareItem.Fields["Name"] == Convert.ToString(item.Name));

            }

        }

        [Fact, Priority(260)]
        public async void T_4_07_BulkDeleteAsset()
        {
            AssetDeletes forDelete = new AssetDeletes();

            foreach (var item in BulkTestData.assetUpdates)
            {
                forDelete.Add(new AssetDelete() { Uid = item.Uid });
            }
            string endPointUrl = URIHelper.AssetsBatchUri + BulkTestData.assetTypeInsert.Uid.ToString();
            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(forDelete), Encoding.UTF8, "application/json"),
                Method = HttpMethod.Delete,
                RequestUri = new Uri(endPointUrl)
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);
            Assert.True(parsedData.GetValue("ExecutionID") != null);
            Assert.True(parsedData.GetValue("Message") != null);
            Assert.True(parsedData.GetValue("Uri") != null);

            BulkTestData.ExecutionUrl = parsedData.GetValue("Uri").ToString();
            Assert.True(await T_4_09_ExecutionStatusCheck() == true);
        }

        [Fact, Priority(280)]
        public async void T_4_08_AssetTypeDelete()
        {
            AssetTypeDeletes forDelete = new AssetTypeDeletes();
            forDelete.Add(new AssetTypeDelete() { Uid = BulkTestData.assetTypeInsert.Uid });
            var endpointUrl = URIHelper.AssetsUri;
            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(forDelete), Encoding.UTF8, "application/json"),
                Method = HttpMethod.Delete,
                RequestUri = new Uri(endpointUrl)
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);
            Assert.True(parsedData.GetValue("ExecutionID") != null);
            Assert.True(parsedData.GetValue("Message") != null);
            Assert.True(parsedData.GetValue("Uri") != null);

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
                var parsedData = JsonConvert.DeserializeObject<ApiExecutionStatusModel>(content);


                if (parsedData.Results != null && parsedData.Results.Count > 0)
                {
                    doRetry = false;
                    isSuccess = parsedData.Results.All(x => x.Success == true);
                }
                retryCount++;
                if (retryCount == retryMax) doRetry = false;

                Thread.Sleep(2000);
            }

            return isSuccess;
        }

    }

}
