using d360.core.entities;
using d360.web.Models;
using igx.IntegrationTests.Core;
using igx.IntegrationTests.TestData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Priority;

namespace igx.IntegrationTests.ApiTests
{
    [Trait("Integration tests", "Fields CRUD tests")]
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class FieldsControllerIntegrationTests : BaseIntegrationTestClass
    {
        [Fact, Priority(10)]
        public async void T_1_01_AssetTypePost()
        {

            string endpointUrl = URIHelper.AssetsUri;
            var response = await httpClient.PostAsJsonAsync(endpointUrl, FieldsTestData.AssetTypeInsert);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(parsedData.GetValue("Uid") != null);
            Assert.True(parsedData.GetValue("Message") != null);
            Assert.True(parsedData.GetValue("Success") != null && parsedData.GetValue("Success").ToString() == "True");

            FieldsTestData.AssetTypeInsert.Uid = Guid.Parse(parsedData.GetValue("Uid").ToString());
        }

        [Fact, Priority(20)]
        public async void T_1_02_AssetTypeGetAfterPost()
        {
            string endPointUrl = URIHelper.AssetTypesUri + "?Class=" + FieldsTestData.AssetTypeInsert.Class.ToString();
            var response = await httpClient.GetAsync(endPointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var assetTypeApiViewModels = JsonConvert.DeserializeObject<List<AssetTypeApiViewModel>>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");

            Assert.True(assetTypeApiViewModels.Count != 0);
            Assert.Contains(assetTypeApiViewModels,
                x => x.uid == FieldsTestData.AssetTypeInsert.Uid
                    && x.Name == FieldsTestData.AssetTypeInsert.Name
                    && x.Description == FieldsTestData.AssetTypeInsert.Description);
        }

        [Fact, Priority(30)]
        public async void T_1_03_PutFields()
        {
            FieldsTestData.FieldsModel.AssetTypeUid = FieldsTestData.AssetTypeInsert.Uid;
            string endPointUri = URIHelper.FieldsUri;
            var response = await httpClient.PutAsJsonAsync(endPointUri, FieldsTestData.FieldsModel);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(parsedData.GetValue("Uid") != null);
            Assert.True(parsedData.GetValue("Message") != null);
            Assert.True(parsedData.GetValue("Success") != null && bool.Parse(parsedData.GetValue("Success").ToString()) == true);
            Assert.True(Guid.Parse(parsedData.GetValue("Uid").ToString()) == FieldsTestData.FieldsModel.AssetTypeUid);
        }

        [Fact, Priority(40)]
        public async void T_1_04_GetAfterPutFields()
        {
            string endPointUri = URIHelper.FieldsUri;
            var response = await httpClient.GetAsync($"{endPointUri}?AssetTypeUid={FieldsTestData.AssetTypeInsert.Uid}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<FieldTypesApiViewModel>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(parsedData.items.Count == FieldsTestData.FieldsModel.Fields.Count + 1);
            foreach (var field in FieldsTestData.FieldsModel.Fields)
            {
                Assert.Contains(parsedData.items, x => x.FriendlyName == field.FriendlyName && x.Name == field.Name);
            }
        }

        [Fact, Priority(50)]
        public async void T_1_05_DeleteFields()
        {
            string endpointUri = URIHelper.FieldsUri;
            var fieldsToDelete = FieldsTestData.FieldsModel.Fields.Select(f => new FieldTypeApiDeleteModel()
            {
                Name = f.Name
            }).ToList();
            var deleteModel = new FieldTypesApiDeleteModel()
            {
                AssetTypeUid = FieldsTestData.AssetTypeInsert.Uid,
                Fields = new List<FieldTypeApiDeleteModel>()
            };
            deleteModel.Fields.AddRange(fieldsToDelete);

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(deleteModel), Encoding.UTF8, "application/json"),
                Method = HttpMethod.Delete,
                RequestUri = new Uri(endpointUri)
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(parsedData.GetValue("Uid") != null);
            Assert.True(parsedData.GetValue("Success") != null);
            Assert.True(parsedData.GetValue("Message") != null);
            Assert.True(bool.Parse(parsedData.GetValue("Success").ToString()) == true);

        }

        [Fact, Priority(60)]
        public async void T_1_05_GetAfterDeleteFields()
        {
            string endPointUri = URIHelper.FieldsUri;
            var response = await httpClient.GetAsync($"{endPointUri}?AssetTypeUid={FieldsTestData.AssetTypeInsert.Uid}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<FieldTypesApiViewModel>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(parsedData.items.Count == 1);
            foreach (var field in FieldsTestData.FieldsModel.Fields)
            {
                Assert.DoesNotContain(parsedData.items, x => x.FriendlyName == field.FriendlyName && x.Name == field.Name);
            }
        }

        [Fact, Priority(70)]
        public async void T_2_05_AssetTypeDelete()
        {
            AssetTypeDeletes forDelete = new AssetTypeDeletes();
            forDelete.Add(new AssetTypeDelete() { Uid = FieldsTestData.AssetTypeInsert.Uid });
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

            FieldsTestData.ExecutionUrl = parsedData.GetValue("Uri").ToString();
        }

        [Fact, Priority(80)]
        private async Task<bool> T_2_06_ExecutionStatusCheck()
        {

            int retryCount = 1;
            int retryMax = 10;
            bool doRetry = true;
            bool isSuccess = false;

            while (doRetry)
            {
                var response = await httpClient.GetAsync(FieldsTestData.ExecutionUrl);
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
}
