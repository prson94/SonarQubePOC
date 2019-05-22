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
using igx.IntegrationTests.Core;

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

            var response = await httpClient.PostAsJsonAsync(endpointUrl, XRefTestData.XRefModel);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<AssetCrossReference>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(!string.IsNullOrEmpty(content));
            Assert.True(Helpers.PublicInstancePropertiesEqual(XRefTestData.XRefModel, parsedData));
        }


        [Fact, Priority(10)]
        public async void T_1_02_GetXrefAfterPost()
        {
            string endpointUrl = URIHelper.XRefUri;

            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedJson = JsonConvert.DeserializeObject<List<AssetCrossReference>>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(!string.IsNullOrEmpty(content));
            Assert.Contains(parsedJson, 
                x => x.uid == XRefTestData.XRefModel.uid 
                && x.FieldHash == XRefTestData.XRefModel.FieldHash
                && x.DataSource == XRefTestData.XRefModel.DataSource);
        }

        [Fact, Priority(20)]
        public async void T_1_03_GetByAssetUid()
        {
            string endpointUrl = URIHelper.XRefUri;

            var response = await httpClient.GetAsync($"{endpointUrl}/{XRefTestData.XRefModel.uid}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedJson = JsonConvert.DeserializeObject<List<AssetCrossReference>>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(!string.IsNullOrEmpty(content));
            Assert.Contains(parsedJson,
                x => x.uid == XRefTestData.XRefModel.uid
                && x.FieldHash == XRefTestData.XRefModel.FieldHash
                && x.DataSource == XRefTestData.XRefModel.DataSource);
        }

        [Fact, Priority(30)]
        public async void T_1_04_PutByUID()
        {
            string endpointUrl = URIHelper.XRefUri;
            XRefTestData.XRefModel.FieldHash += "put_edited";

            var response = await httpClient.PutAsJsonAsync($"{endpointUrl}/{XRefTestData.XRefModel.uid}", XRefTestData.XRefModel);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<AssetCrossReference>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK);

        }

        [Fact, Priority(40)]
        public async void T_1_05_GetByType()
        {
            string endpointUrl = URIHelper.XRefUri;

            var response = await httpClient.GetAsync($"{endpointUrl}/type/{XRefTestData.XRefModel.Type}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedJson = JsonConvert.DeserializeObject<List<AssetCrossReference>>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(!string.IsNullOrEmpty(content));
            Assert.Contains(parsedJson,
                x => x.uid == XRefTestData.XRefModel.uid
                && x.FieldHash == XRefTestData.XRefModel.FieldHash
                && x.DataSource == XRefTestData.XRefModel.DataSource);
        }

        [Fact, Priority(50)]
        public async void T_1_06_PutByEveryParam()
        {
            string endpointUrl = $"{URIHelper.XRefUri}/{XRefTestData.XRefModel.uid}/{XRefTestData.XRefModel.DataSource}/{XRefTestData.XRefModel.Type}/{XRefTestData.XRefModel.ExternalID}";
            XRefTestData.XRefModel.FieldHash += "put_edited";


            var response = await httpClient.PutAsJsonAsync(endpointUrl, XRefTestData.XRefModel);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<AssetCrossReference>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK);

        }

        [Fact, Priority(60)]
        public async void T_1_07_GetByTypeAndExternalId()
        {
            string endpointUrl = URIHelper.XRefUri;

            var response = await httpClient.GetAsync($"{endpointUrl}/{XRefTestData.XRefModel.Type}/{XRefTestData.XRefModel.ExternalID}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedJson = JsonConvert.DeserializeObject<List<AssetCrossReference>>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(!string.IsNullOrEmpty(content));
            Assert.Contains(parsedJson,
                x => x.uid == XRefTestData.XRefModel.uid
                && x.FieldHash == XRefTestData.XRefModel.FieldHash
                && x.DataSource == XRefTestData.XRefModel.DataSource);
        }

        [Fact, Priority(70)]
        public async void T_1_08_GetByDatasource()
        {
            string endpointUrl = URIHelper.XRefUri;

            var response = await httpClient.GetAsync($"{endpointUrl}/datasource/{XRefTestData.XRefModel.DataSource}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedJson = JsonConvert.DeserializeObject<List<AssetCrossReference>>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(!string.IsNullOrEmpty(content));
            Assert.Contains(parsedJson,
                x => x.uid == XRefTestData.XRefModel.uid
                && x.FieldHash == XRefTestData.XRefModel.FieldHash
                && x.DataSource == XRefTestData.XRefModel.DataSource);
        }

        [Fact, Priority(80)]
        public async void T_1_09_DeleteByType()
        {
            string endpointUrl = URIHelper.XRefUri;

            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri($"{endpointUrl}/type/{XRefTestData.XRefModel.Type}")
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK);
        }

        [Fact, Priority(90)]
        public async void T_1_10_GetAfterDelete()
        {
            string endpointUrl = URIHelper.XRefUri;

            var response = await httpClient.GetAsync($"{endpointUrl}/datasource/{XRefTestData.XRefModel.DataSource}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedJson = JsonConvert.DeserializeObject<List<AssetCrossReference>>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(!string.IsNullOrEmpty(content));
            Assert.DoesNotContain(parsedJson,
                x => x.uid == XRefTestData.XRefModel.uid
                && x.FieldHash == XRefTestData.XRefModel.FieldHash
                && x.DataSource == XRefTestData.XRefModel.DataSource);
        }

        [Fact, Priority(100)]
        public async void T_1_11_BulkPost()
        {
            string endpointUrl = URIHelper.XRefUri;

            var response = await httpClient.PostAsJsonAsync($"{endpointUrl}/bulk", XRefTestData.XRefModelList);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<List<AssetCrossReference>>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(!string.IsNullOrEmpty(content));
            foreach (var item in parsedData)
            {
                Assert.Contains(parsedData,
                    x => x.uid == item.uid
                    && x.FieldHash == item.FieldHash
                    && x.DataSource == item.DataSource);
            }
        }

        [Fact, Priority(110)]
        public async void T_1_12_GetAfterBulkPost()
        {
            string endpointUrl = URIHelper.XRefUri;

            var response = await httpClient.GetAsync($"{endpointUrl}/datasource/{XRefTestData.XRefModelList.First().DataSource}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedJson = JsonConvert.DeserializeObject<List<AssetCrossReference>>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(!string.IsNullOrEmpty(content));
            foreach (var item in parsedJson)
            {
                Assert.Contains(parsedJson,
                    x => x.uid == item.uid
                    && x.FieldHash == item.FieldHash
                    && x.DataSource == item.DataSource);
            }
        }

        [Fact, Priority(120)]
        public async void T_1_13_DeleteByDatasourceAndType()
        {
            string endpointUrl = URIHelper.XRefUri;

            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri($"{endpointUrl}/{XRefTestData.XRefModelList.First().DataSource}/{XRefTestData.XRefModelList.First().Type}")
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK);
        }

        [Fact, Priority(130)]
        public async void T_1_14_DeleteByDatasource()
        {
            string endpointUrl = URIHelper.XRefUri;

            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri($"{endpointUrl}/dataSource/{XRefTestData.XRefModelList.First().DataSource}")
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK);
        }

        [Fact, Priority(140)]
        public async void T_1_15_GetAfterDeletes()
        {
            string endpointUrl = URIHelper.XRefUri;

            var response = await httpClient.GetAsync($"{endpointUrl}/datasource/{XRefTestData.XRefModelList.First().DataSource}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedJson = JsonConvert.DeserializeObject<List<AssetCrossReference>>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(!string.IsNullOrEmpty(content));
            Assert.True(parsedJson.Count() == 0);
        }
    }
}
