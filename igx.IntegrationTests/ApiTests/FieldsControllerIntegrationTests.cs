using igx.IntegrationTests.Core;
using igx.IntegrationTests.TestData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
            var response = await httpClient.PostAsync(endpointUrl, FieldsTestData.AssetTypeInsert.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(parsedData.GetValue("Uid") != null, XMsg.MissingField("Uid"));
            Assert.True(parsedData.GetValue("Message") != null, XMsg.MissingField("Message"));
            Assert.True(parsedData.GetValue("Success") != null && parsedData.GetValue("Success").ToString() == "True", XMsg.MissingField("Success"));

            FieldsTestData.AssetTypeInsert.UpdateValueOnProperty("Uid", parsedData.GetValue("Uid"));
        }

        [Fact, Priority(20)]
        public async void T_1_02_AssetTypeGetAfterPost()
        {
            string endPointUrl = URIHelper.AssetTypesUri + "?Class=" + FieldsTestData.AssetTypeInsert["Class"].ToString();
            var response = await httpClient.GetAsync(endPointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var assetTypeApiViewModels = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(assetTypeApiViewModels.Count != 0, XMsg.InvalidCount);
            Assert.True(assetTypeApiViewModels.DoesContain(x => x["uid"].ToString() == FieldsTestData.AssetTypeInsert["Uid"].ToString()
                    && x["Name"].ToString() == FieldsTestData.AssetTypeInsert["Name"].ToString()
                    && x["Description"].ToString() == FieldsTestData.AssetTypeInsert["Description"].ToString()), XMsg.MissingAsset);
        }

        [Fact, Priority(30)]
        public async void T_1_03_PutFields()
        {
            FieldsTestData.FieldsModel.UpdateValueOnProperty("AssetTypeUid", FieldsTestData.AssetTypeInsert["Uid"]);

            string endPointUri = URIHelper.FieldsUri;
            var response = await httpClient.PutAsync(endPointUri, FieldsTestData.FieldsModel.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(parsedData.GetValue("Uid") != null, XMsg.MissingField("Uid"));
            Assert.True(parsedData.GetValue("Message") != null, XMsg.MissingField("Message"));
            Assert.True(parsedData.GetValue("Success") != null && parsedData.GetValue("Success").ToString() == "True", XMsg.MissingField("Success"));

            Assert.True(parsedData.GetValue("Uid").ToString() == FieldsTestData.FieldsModel["AssetTypeUid"].ToString(), XMsg.InvalidFieldValue("Uid"));
        }

        [Fact, Priority(40)]
        public async void T_1_04_GetAfterPutFields()
        {
            string endPointUri = URIHelper.FieldsUri;
            var response = await httpClient.GetAsync($"{endPointUri}?AssetTypeUid={FieldsTestData.AssetTypeInsert["Uid"].ToString()}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(parsedData["items"].Count() == FieldsTestData.FieldsModel["Fields"].Count() + 1, XMsg.InvalidCount);
            foreach (var field in FieldsTestData.FieldsModel["Fields"])
            {
                Assert.True((parsedData["items"] as JArray).DoesContain(x => x["FriendlyName"].ToString() == field["FriendlyName"].ToString()
                    && x["Name"].ToString() == field["Name"].ToString()), XMsg.MissingAsset);
            }
        }

        [Fact, Priority(50)]
        public async void T_1_05_DeleteFields()
        {
            string endpointUri = URIHelper.FieldsUri;
            List<string> fieldsToDelete = new List<string>();
            FieldsTestData.FieldsModel["Fields"].ToList().ForEach(x => fieldsToDelete.Add(x["Name"].ToString()));

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = FieldsTestData.GetJsonForDelete(fieldsToDelete, FieldsTestData.AssetTypeInsert["Uid"].ToString()).AsStringContent(),
                Method = HttpMethod.Delete,
                RequestUri = new Uri(endpointUri)
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(parsedData.GetValue("Uid") != null, XMsg.InvalidFieldValue("Uid"));
            Assert.True(parsedData.GetValue("Success") != null, XMsg.InvalidFieldValue("Success"));
            Assert.True(parsedData.GetValue("Message") != null, XMsg.InvalidFieldValue("Message"));
            Assert.True(bool.Parse(parsedData.GetValue("Success").ToString()) == true, XMsg.InvalidFieldValue("Success"));

        }

        [Fact, Priority(60)]
        public async void T_1_05_GetAfterDeleteFields()
        {
            string endPointUri = URIHelper.FieldsUri;
            var response = await httpClient.GetAsync($"{endPointUri}?AssetTypeUid={FieldsTestData.AssetTypeInsert["Uid"]}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(parsedData["items"].Count() == 1, XMsg.InvalidCount);
            foreach (var field in FieldsTestData.FieldsModel["Fields"])
            {
                Assert.False((parsedData["items"] as JArray).DoesContain(x => x["FriendlyName"].ToString() == field["FriendlyName"].ToString()
                    && x["Name"].ToString() == field["Name"].ToString()), XMsg.InvalidFieldValue("items"));
            }
        }

        [Fact, Priority(70)]
        public async void T_2_05_AssetTypeDelete()
        {
            var endpointUrl = URIHelper.AssetsUri;
            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = AssetTypeTestData.GetDeleteAssetTypeJSON(Guid.Parse(FieldsTestData.AssetTypeInsert["Uid"].ToString())).AsStringContent(),
                Method = HttpMethod.Delete,
                RequestUri = new Uri(endpointUrl)
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);
            Assert.True(parsedData.GetValue("ExecutionID") != null, XMsg.MissingField("ExecutionID"));
            Assert.True(parsedData.GetValue("Message") != null, XMsg.MissingField("Message"));
            Assert.True(parsedData.GetValue("Uri") != null, XMsg.MissingField("Uri"));

            FieldsTestData.ExecutionUrl = parsedData.GetValue("Uri").ToString();
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
                var response = await httpClient.GetAsync(FieldsTestData.ExecutionUrl);
                var content = await response.Content.ReadAsStringAsync();
                var parsedData = JsonConvert.DeserializeObject<JObject>(content);


                if (parsedData["Results"] != null && parsedData["Results"].Count() > 0)
                {
                    doRetry = false;
                    isSuccess = parsedData["Results"].All(x => bool.Parse(x["Success"].ToString()) == true);
                }
                retryCount++;
                if (retryCount == retryMax) doRetry = false;

                Thread.Sleep(2000);
            }
            Assert.True(isSuccess, XMsg.ExecutionStatusErr);
            return isSuccess;
        }
    }
}
