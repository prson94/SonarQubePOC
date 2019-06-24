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

namespace igx.IntegrationTests.ApiTests
{
    [Trait("Integration tests", "Metric CRUD Tests")]
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class MetricsControllerIntegrationTests : BaseIntegrationTestClass
    {
        [Fact, Priority(10)]
        public async void T_1_01_PrepareAssetTypeAndField()
        {

            string endpointUrl = URIHelper.AssetsUri;
            var response = await httpClient.PostAsync(endpointUrl, MetricTestsData.AssetTypeJSON.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(parsedData.GetValue("Uid") != null);
            Assert.True(parsedData.GetValue("Message") != null);
            Assert.True(parsedData.GetValue("Success") != null && parsedData.GetValue("Success").ToString() == "True");

            MetricTestsData.AssetTypeGuid = parsedData.GetValue("Uid").ToString();

            response = await httpClient.GetAsync($"{URIHelper.AssetFieldsUri}/{MetricTestsData.AssetTypeGuid}");
            content = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            var fieldArray = JsonConvert.DeserializeObject<JArray>(content);
            foreach (var f in fieldArray)
            {
                if (f["FriendlyName"].ToString() == "Name")
                    MetricTestsData.NameFieldTypeId = f["ID"].ToString();
            }

            Assert.True(!string.IsNullOrEmpty(MetricTestsData.NameFieldTypeId));

            response = await httpClient.PostAsync($"{URIHelper.AssetsUri}/{MetricTestsData.AssetTypeGuid}", MetricTestsData.NewAsset.AsStringContent());
            content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");

            var assetArray = JsonConvert.DeserializeObject<JArray>(content);
            Assert.True(Guid.Parse(assetArray.First()["uid"].ToString()) != Guid.Empty);
            MetricTestsData.AssetUid = assetArray.First()["uid"].ToString();


        }

        [Fact, Priority(20)]
        public async void T_1_02_PostMetric()
        {
            string endpointUrl = URIHelper.MetricsUri;
            var response = await httpClient.PostAsync(endpointUrl, MetricTestsData.MetricModel.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(parsedData["type"] != null && parsedData["type"].ToString() == "confirm");
        }

        [Fact, Priority(30)]
        public async void T_1_03_GetMetricDefinition()
        {
            string endpoint = $"{URIHelper.MetricsUri}/{MetricTestsData.AssetTypeGuid}/definition";
            var response = await httpClient.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JArray>(content);

            string name = MetricTestsData.MetricModel.GetJTokenValue("Name");
            bool isInResponse = false;
            foreach (var item in parsedData)
            {
                if (item["Name"].ToString() == name)
                {
                    isInResponse = true;
                    MetricTestsData.MetricUid = item["Uid"].ToString();
                }
            }

            Assert.True(isInResponse);
            Assert.True(!string.IsNullOrEmpty(MetricTestsData.MetricUid));

        }

        [Fact, Priority(40)]
        public async void T_1_04_UpdateMetric()
        {
            string endpointUrl = URIHelper.MetricsUri;
            MetricTestsData.MetricModel = JsonHelper.AddNewToken(MetricTestsData.MetricModel, "Uid", MetricTestsData.MetricUid);
            MetricTestsData.MetricModel = JsonHelper.AppendJsonOnField(MetricTestsData.MetricModel, "Name", "Updated name");
            MetricTestsData.MetricModel = JsonHelper.UpdateJsonOnField(MetricTestsData.MetricModel, "Weight", "0.5");


            var response = await httpClient.PostAsync(endpointUrl, MetricTestsData.MetricModel.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(parsedData["type"] != null && parsedData["type"].ToString() == "confirm");
        }


        [Fact, Priority(50)]
        public async void T_1_05_GetMetricDefinition()
        {
            string endpoint = $"{URIHelper.MetricsUri}/{MetricTestsData.AssetTypeGuid}/definition";
            var response = await httpClient.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");

            string name = MetricTestsData.MetricModel.GetJTokenValue("Name");
            bool isInResponse = false;
            foreach (var item in parsedData)
            {
                if (item["Name"].ToString() == name)
                {
                    isInResponse = true;
                    MetricTestsData.MetricUid = item["Uid"].ToString();
                }
            }

            Assert.True(isInResponse);
            Assert.True(!string.IsNullOrEmpty(MetricTestsData.MetricUid));

        }

        [Fact, Priority(60)]
        public async void T_1_05_GetMetricByUid()
        {
            string endpoint = $"{URIHelper.MetricsUri}/{MetricTestsData.MetricUid}";
            var response = await httpClient.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");

            Assert.True(parsedData["Uid"].ToString() == MetricTestsData.MetricModel.GetJTokenValue("Uid"));
            Assert.True(parsedData["Name"].ToString() == MetricTestsData.MetricModel.GetJTokenValue("Name"));
            Assert.True(parsedData["Description"].ToString() == MetricTestsData.MetricModel.GetJTokenValue("Description"));

            var definition = parsedData["Versions"].First();
            Assert.True(definition["Weight"].ToString() == MetricTestsData.MetricModel.GetJTokenValue("Weight"));
            Assert.True(definition["ConditionAndOr"].ToString() == MetricTestsData.MetricModel.GetJTokenValue("ConditionAndOr"));

            Assert.True(!string.IsNullOrEmpty(MetricTestsData.MetricUid));

        }

        [Fact, Priority(70)]
        public async void T_1_06_GetMetricsBreakdown()
        {
            string endpoint = $"{URIHelper.MetricsUri}/{MetricTestsData.AssetUid}/pointbreakdown?effectiveDate=2019-06-20T11:59:03.874Z";
            var response = await httpClient.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
        }
    }
}
