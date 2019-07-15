using igx.IntegrationTests.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using igx.IntegrationTests.TestData;
using Newtonsoft.Json.Linq;
using Xunit.Priority;
using System.Threading;
using Xunit;

namespace igx.IntegrationTests.ApiTests
{
    [Trait("Integration tests", "XRef CRUD Tests")]
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class CrossReferenceIntegrationTests : BaseIntegrationTestClass
    {
        [Fact, Priority(0)]
        public async void T_1_01_PostNewXRef()
        {
            string endpointUrl = URIHelper.XRefUri;

            var response = await httpClient.PostAsync(endpointUrl, XRefTestData.XRefModel.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadResponseCode);

            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);
            Assert.True(SimpleJsonComparer.IsEqual(XRefTestData.XRefModel, parsedData), XMsg.MissingAsset);
        }


        [Fact, Priority(10)]
        public async void T_1_02_GetXrefAfterPost()
        {
            string endpointUrl = URIHelper.XRefUri;

            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedJson = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadResponseCode);
            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);
            Assert.True(parsedJson.DoesContain(x => x["uid"].ToString() == XRefTestData.XRefModel["uid"].ToString()
               && x["FieldHash"].ToString() == XRefTestData.XRefModel["FieldHash"].ToString()
               && x["DataSource"].ToString() == XRefTestData.XRefModel["DataSource"].ToString()), XMsg.MissingAsset);
        }

        [Fact, Priority(20)]
        public async void T_1_03_GetByAssetUid()
        {
            string endpointUrl = URIHelper.XRefUri;

            var response = await httpClient.GetAsync($"{endpointUrl}/{XRefTestData.XRefModel["uid"].ToString()}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedJson = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadResponseCode);
            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);
            Assert.True(parsedJson.DoesContain(x => x["uid"].ToString() == XRefTestData.XRefModel["uid"].ToString()
               && x["FieldHash"].ToString() == XRefTestData.XRefModel["FieldHash"].ToString()
               && x["DataSource"].ToString() == XRefTestData.XRefModel["DataSource"].ToString()), XMsg.MissingAsset);
        }

        [Fact, Priority(30)]
        public async void T_1_04_PutByUID()
        {
            string endpointUrl = URIHelper.XRefUri;

            XRefTestData.XRefModel.AppendValueOnProperty("FieldHash", "put_edited");

            var response = await httpClient.PutAsync($"{endpointUrl}/{XRefTestData.XRefModel["uid"].ToString()}", XRefTestData.XRefModel.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);

        }

        [Fact, Priority(40)]
        public async void T_1_05_GetByType()
        {
            string endpointUrl = URIHelper.XRefUri;

            var response = await httpClient.GetAsync($"{endpointUrl}/type/{XRefTestData.XRefModel["Type"]}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedJson = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadResponseCode);
            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);
            Assert.True(parsedJson.DoesContain(x => x["uid"].ToString() == XRefTestData.XRefModel["uid"].ToString()
               && x["FieldHash"].ToString() == XRefTestData.XRefModel["FieldHash"].ToString()
               && x["DataSource"].ToString() == XRefTestData.XRefModel["DataSource"].ToString()), XMsg.MissingAsset);
        }

        [Fact, Priority(50)]
        public async void T_1_06_PutByEveryParam()
        {
            string endpointUrl = $"{URIHelper.XRefUri}/{XRefTestData.XRefModel["uid"].ToString()}/{XRefTestData.XRefModel["DataSource"].ToString()}/{XRefTestData.XRefModel["Type"].ToString()}/{XRefTestData.XRefModel["ExternalID"].ToString()}";
            XRefTestData.XRefModel.AppendValueOnProperty("FieldHash", "put_edited");


            var response = await httpClient.PutAsync(endpointUrl, XRefTestData.XRefModel.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);

        }

        [Fact, Priority(60)]
        public async void T_1_07_GetByTypeAndExternalId()
        {
            string endpointUrl = URIHelper.XRefUri;

            var response = await httpClient.GetAsync($"{endpointUrl}/{XRefTestData.XRefModel["Type"].ToString()}/{XRefTestData.XRefModel["ExternalID"].ToString()}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedJson = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadResponseCode);
            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);
            Assert.True(parsedJson.DoesContain(x => x["uid"].ToString() == XRefTestData.XRefModel["uid"].ToString()
               && x["FieldHash"].ToString() == XRefTestData.XRefModel["FieldHash"].ToString()
               && x["DataSource"].ToString() == XRefTestData.XRefModel["DataSource"].ToString()), XMsg.MissingAsset);
        }

        [Fact, Priority(70)]
        public async void T_1_08_GetByDatasource()
        {
            string endpointUrl = URIHelper.XRefUri;

            var response = await httpClient.GetAsync($"{endpointUrl}/datasource/{XRefTestData.XRefModel["DataSource"].ToString()}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedJson = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadResponseCode);
            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);
            Assert.True(parsedJson.DoesContain(x => x["uid"].ToString() == XRefTestData.XRefModel["uid"].ToString()
               && x["FieldHash"].ToString() == XRefTestData.XRefModel["FieldHash"].ToString()
               && x["DataSource"].ToString() == XRefTestData.XRefModel["DataSource"].ToString()), XMsg.MissingAsset);
        }

        [Fact, Priority(80)]
        public async void T_1_09_DeleteByType()
        {
            string endpointUrl = URIHelper.XRefUri;

            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri($"{endpointUrl}/type/{XRefTestData.XRefModel["Type"].ToString()}")
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
        }

        [Fact, Priority(90)]
        public async void T_1_10_GetAfterDelete()
        {
            string endpointUrl = URIHelper.XRefUri;

            var response = await httpClient.GetAsync($"{endpointUrl}/datasource/{XRefTestData.XRefModel["DataSource"].ToString()}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedJson = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadResponseCode);
            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);
            Assert.False(parsedJson.DoesContain(x => x["uid"].ToString() == XRefTestData.XRefModel["uid"].ToString()
               && x["FieldHash"].ToString() == XRefTestData.XRefModel["FieldHash"].ToString()
               && x["DataSource"].ToString() == XRefTestData.XRefModel["DataSource"].ToString()), XMsg.MissingAsset);
        }

        [Fact, Priority(100)]
        public async void T_1_11_BulkPost()
        {
            string endpointUrl = URIHelper.XRefUri;

            var response = await httpClient.PostAsync($"{endpointUrl}/bulk", XRefTestData.XRefModelList.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);
            foreach (var item in parsedData)
            {
                Assert.True(parsedData.DoesContain(x => x["uid"].ToString() == item["uid"].ToString()
                      && x["FieldHash"].ToString() == item["FieldHash"].ToString()
                      && x["DataSource"].ToString() == item["DataSource"].ToString()), XMsg.MissingAsset);
            }
        }

        [Fact, Priority(110)]
        public async void T_1_12_GetAfterBulkPost()
        {
            string endpointUrl = URIHelper.XRefUri;

            var response = await httpClient.GetAsync($"{endpointUrl}/datasource/{XRefTestData.XRefModelList.First()["DataSource"].ToString()}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);
            foreach (var item in parsedData)
            {
                Assert.True(parsedData.DoesContain(x => x["uid"].ToString() == item["uid"].ToString()
                      && x["FieldHash"].ToString() == item["FieldHash"].ToString()
                      && x["DataSource"].ToString() == item["DataSource"].ToString()), XMsg.MissingAsset);
            }
        }

        [Fact, Priority(120)]
        public async void T_1_13_DeleteByDatasourceAndType()
        {
            string endpointUrl = URIHelper.XRefUri;

            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri($"{endpointUrl}/{XRefTestData.XRefModelList.First()["DataSource"].ToString()}/{XRefTestData.XRefModelList.First()["Type"].ToString()}")
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
        }

        [Fact, Priority(130)]
        public async void T_1_14_DeleteByDatasource()
        {
            string endpointUrl = URIHelper.XRefUri;

            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri($"{endpointUrl}/dataSource/{XRefTestData.XRefModelList.First()["DataSource"].ToString()}")
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);

        }

        [Fact, Priority(140)]
        public async void T_1_15_GetAfterDeletes()
        {
            string endpointUrl = URIHelper.XRefUri;

            var response = await httpClient.GetAsync($"{endpointUrl}/datasource/{XRefTestData.XRefModelList.First()["DataSource"].ToString()}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedJson = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);
            Assert.True(parsedJson.Count() == 0, XMsg.InvalidCount);
        }
    }
}
