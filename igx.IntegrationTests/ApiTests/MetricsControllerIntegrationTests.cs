using igx.IntegrationTests.Core;
using System;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using igx.IntegrationTests.TestData;
using Newtonsoft.Json.Linq;
using Xunit.Priority;
using Xunit;

namespace igx.IntegrationTests.ApiTests
{
    [Trait("Integration tests", "Metric CRUD Tests")]
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class MetricsControllerIntegrationTests : BaseIntegrationTestClass
    {
        [Fact, Priority(10)]
        public async void PrepareAssetTypeAndField()
        {

            string endpointUrl = URIHelper.AssetsUri;
            var response = await httpClient.PostAsync(endpointUrl, MetricTestsData.AssetTypeJSON.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(parsedData.GetValue("Uid") != null, XMsg.InvalidFieldValue("Uid"));
            Assert.True(parsedData.GetValue("Message") != null, XMsg.InvalidFieldValue("Message"));
            Assert.True(parsedData.GetValue("Success") != null && parsedData.GetValue("Success").ToString() == "True", XMsg.InvalidFieldValue("Success"));

            MetricTestsData.AssetTypeGuid = parsedData.GetValue("Uid").ToString();

            response = await httpClient.GetAsync($"{URIHelper.AssetFieldsUri}/{MetricTestsData.AssetTypeGuid}");
            content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            var fieldArray = JsonConvert.DeserializeObject<JArray>(content);
            foreach (var f in fieldArray)
            {
                if (f["FriendlyName"].ToString() == "Name")
                    MetricTestsData.NameFieldTypeId = f["ID"].ToString();
            }

            Assert.True(!string.IsNullOrEmpty(MetricTestsData.NameFieldTypeId), XMsg.InvalidFieldValue("FieldTypeId"));

            response = await httpClient.PostAsync($"{URIHelper.AssetsUri}/{MetricTestsData.AssetTypeGuid}", MetricTestsData.NewAssets.AsStringContent());
            content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            var assetArray = JsonConvert.DeserializeObject<JArray>(content);
            Assert.True(Guid.Parse(assetArray.First()["uid"].ToString()) != Guid.Empty, XMsg.InvalidFieldValue("uid"));
            MetricTestsData.AssetUid = assetArray.First()["uid"].ToString();


        }

        [Fact, Priority(20)]
        public async void PostMetric()
        {
            string endpointUrl = URIHelper.MetricsUri;
            var response = await httpClient.PostAsync(endpointUrl, MetricTestsData.MetricModel.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode,XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(parsedData["type"] != null && parsedData["type"].ToString() == "confirm", XMsg.InvalidFieldValue("type"));
        }

        [Fact, Priority(30)]
        public async void GetMetricDefinitionAfterPost()
        {
            string endpoint = $"{URIHelper.MetricsUri}/{MetricTestsData.AssetTypeGuid}/definition";
            var response = await httpClient.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JArray>(content);

            string name = MetricTestsData.MetricModel["Name"].ToString();
            bool isInResponse = false;
            foreach (var item in parsedData)
            {
                if (item["Name"].ToString() == name)
                {
                    isInResponse = true;
                    MetricTestsData.MetricUid = item["Uid"].ToString();
                }
            }

            Assert.True(isInResponse, XMsg.MissingAsset);
            Assert.True(!string.IsNullOrEmpty(MetricTestsData.MetricUid), XMsg.InvalidFieldValue("MetricUid"));

        }

        [Fact, Priority(40)]
        public async void UpdateMetric()
        {
            string endpointUrl = URIHelper.MetricsUri;
            MetricTestsData.MetricModel.AddNewToken("Uid", MetricTestsData.MetricUid);
            MetricTestsData.MetricModel.AppendValueOnProperty("Name", "Updated name");
            MetricTestsData.MetricModel.UpdateValueOnProperty("Weight", "0.5");


            var response = await httpClient.PostAsync(endpointUrl, MetricTestsData.MetricModel.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(parsedData["type"] != null && parsedData["type"].ToString() == "confirm", XMsg.InvalidFieldValue("type"));
        }


        [Fact, Priority(50)]
        public async void GetMetricDefinitionAfterPut()
        {
            string endpoint = $"{URIHelper.MetricsUri}/{MetricTestsData.AssetTypeGuid}/definition";
            var response = await httpClient.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            string name = MetricTestsData.MetricModel["Name"].ToString();
            bool isInResponse = false;
            foreach (var item in parsedData)
            {
                if (item["Name"].ToString() == name)
                {
                    isInResponse = true;
                    MetricTestsData.MetricUid = item["Uid"].ToString();
                }
            }

            Assert.True(isInResponse, XMsg.MissingAsset);
            Assert.True(!string.IsNullOrEmpty(MetricTestsData.MetricUid), XMsg.InvalidFieldValue("MetricUid"));

        }

        [Fact, Priority(60)]
        public async void GetMetricByUid()
        {
            string endpoint = $"{URIHelper.MetricsUri}/{MetricTestsData.MetricUid}";
            var response = await httpClient.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(JsonHelper.AreEqualOnField(parsedData, MetricTestsData.MetricModel, "Uid"), XMsg.InvalidFieldValue("Uid"));
            Assert.True(JsonHelper.AreEqualOnField(parsedData, MetricTestsData.MetricModel, "Name"), XMsg.InvalidFieldValue("Name"));
            Assert.True(JsonHelper.AreEqualOnField(parsedData, MetricTestsData.MetricModel, "Description"), XMsg.InvalidFieldValue("Description"));

            Assert.True(parsedData["State"].ToString() != "3", XMsg.InvalidFieldValue("State"));

            var definition = parsedData["Versions"].First() as JObject;
            Assert.True(JsonHelper.AreEqualOnField(definition, MetricTestsData.MetricModel, "Weight"), XMsg.InvalidFieldValue("Weight"));
            Assert.True(JsonHelper.AreEqualOnField(definition, MetricTestsData.MetricModel, "ConditionAndOr"), XMsg.InvalidFieldValue("ConditionAndOr"));

            Assert.True(!string.IsNullOrEmpty(MetricTestsData.MetricUid), XMsg.InvalidFieldValue("MetricUid"));

        }

        [Fact, Priority(70)]
        public async void PostMetricResults()
        {
            string endpoint = $"{URIHelper.MetricsUri}/results";
            var response = await httpClient.PostAsync(endpoint, MetricTestsData.MetricResultJson.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(parsedData.First["AssetUid"].ToString() == MetricTestsData.MetricResultJson.First["AssetUid"].ToString(), XMsg.InvalidFieldValue("AssetUid"));
            Assert.True(parsedData.First["MetricAssetUid"].ToString() == MetricTestsData.MetricResultJson.First["MetricAssetUid"].ToString(), XMsg.InvalidFieldValue("MetricAssetUid"));
            Assert.True(parsedData.First["Result"].ToString().ToLower() == "true", XMsg.InvalidFieldValue("Result"));
            Assert.True(parsedData.First["IsSuccess"].ToString().ToLower() == "true", XMsg.InvalidFieldValue("IsSuccess"));

        }

        [Fact, Priority(71)]
        public async void ERR_UpdateMetric_EmptyName()
        {
            string endpointUrl = URIHelper.MetricsUri;

            var temp = MetricTestsData.MetricModel.DeepClone();

            MetricTestsData.MetricModel.UpdateValueOnProperty("Name", "");

            var response = await httpClient.PostAsync(endpointUrl, MetricTestsData.MetricModel.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);
            MetricTestsData.MetricModel = temp as JObject;

            Assert.True(!response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(parsedData["type"].ToString() == "error", XMsg.InvalidFieldValue("type"));
        }

        [Fact, Priority(72)]
        public async void ERR_UpdateMetric_WeightZero()
        {
            string endpointUrl = URIHelper.MetricsUri;

            var temp = MetricTestsData.MetricModel.DeepClone();

            MetricTestsData.MetricModel.UpdateValueOnProperty("Weight", 0);

            var response = await httpClient.PostAsync(endpointUrl, MetricTestsData.MetricModel.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);
            MetricTestsData.MetricModel = temp as JObject;

            Assert.True(!response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(parsedData["type"].ToString() == "error", XMsg.InvalidFieldValue("type"));
        }

        [Fact, Priority(73)]
        public async void ERR_UpdateMetric_GroupWithConditions()
        {
            string endpointUrl = URIHelper.MetricsUri;

            var temp = MetricTestsData.MetricModel.DeepClone();

            MetricTestsData.MetricModel.UpdateValueOnProperty("IsGroup", true);

            var response = await httpClient.PostAsync(endpointUrl, MetricTestsData.MetricModel.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);
            MetricTestsData.MetricModel = temp as JObject;

            Assert.True(!response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(parsedData["type"].ToString() == "error", XMsg.InvalidFieldValue("type"));
        }

        [Fact, Priority(74)]
        public async void ERR_UpdateMetric_CounditionWithZeroFieldTypeID()
        {
            string endpointUrl = URIHelper.MetricsUri;

            var temp = MetricTestsData.MetricModel.DeepClone();

            MetricTestsData.MetricModel["Conditions"][0].UpdateValueOnProperty("FieldTypeID", 0);

            var response = await httpClient.PostAsync(endpointUrl, MetricTestsData.MetricModel.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);
            MetricTestsData.MetricModel = temp as JObject;

            Assert.True(!response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(parsedData["type"].ToString() == "error", XMsg.InvalidFieldValue("type"));
        }

        [Fact, Priority(75)]
        public async void ERR_UpdateMetric_CounditionWithInvalidFieldTypeID()
        {
            string endpointUrl = URIHelper.MetricsUri;

            var temp = MetricTestsData.MetricModel.DeepClone();

            MetricTestsData.MetricModel["Conditions"][0].UpdateValueOnProperty("FieldTypeID", 1);

            var response = await httpClient.PostAsync(endpointUrl, MetricTestsData.MetricModel.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);
            MetricTestsData.MetricModel = temp as JObject;

            Assert.True(!response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(parsedData["type"].ToString() == "error", XMsg.InvalidFieldValue("type"));
        }


        [Fact, Priority(90)]
        public async void GetMetricStructure()
        {
            string endpoint = $"{URIHelper.MetricsUri}/structure/{MetricTestsData.AssetTypeGuid}";
            var response = await httpClient.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JArray>(content).First as JObject;

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(JsonHelper.AreEqualOnField(parsedData, MetricTestsData.MetricModel, "Uid"), XMsg.InvalidFieldValue("Uid"));
            Assert.True(JsonHelper.AreEqualOnField(parsedData, MetricTestsData.MetricModel, "AssetTypeUid"), XMsg.InvalidFieldValue("AssetTypeUid"));
            Assert.True(JsonHelper.AreEqualOnField(parsedData, MetricTestsData.MetricModel, "IsGroup", true), XMsg.InvalidFieldValue("IsGroup"));
            Assert.True(JsonHelper.AreEqualOnField(parsedData, MetricTestsData.MetricModel, "Name"), XMsg.InvalidFieldValue("Name"));
            Assert.True(JsonHelper.AreEqualOnField(parsedData, MetricTestsData.MetricModel, "Description"), XMsg.InvalidFieldValue("Description"));
            Assert.True(JsonHelper.AreEqualOnField(parsedData, MetricTestsData.MetricModel, "Weight"), XMsg.InvalidFieldValue("Weight"));
            Assert.True(JsonHelper.AreEqualOnField(parsedData, MetricTestsData.MetricModel, "ConditionAndOr"), XMsg.InvalidFieldValue("ConditionAndOr"));


        }
        [Fact, Priority(100)]
        public async void GetMetricFields()
        {
            string endpoint = $"{URIHelper.MetricsUri}/fields/{MetricTestsData.AssetTypeGuid}";
            var response = await httpClient.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JArray>(content).First as JObject;

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(parsedData["ID"].ToString() == MetricTestsData.MetricModel["Conditions"][0]["FieldTypeID"].ToString(), XMsg.InvalidFieldValue("ID"));

        }


        [Fact, Priority(110)]
        public async void DeleteMetric()
        {
            var response = await httpClient.DeleteAsync($"{URIHelper.MetricsUri}/{MetricTestsData.MetricUid}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(parsedData["type"].ToString().ToLower() == "confirm", XMsg.InvalidFieldValue("type"));

        }


        [Fact, Priority(120)]
        public async void GetMetricByUidAfterDelete()
        {
            string endpoint = $"{URIHelper.MetricsUri}/{MetricTestsData.MetricUid}";
            var response = await httpClient.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            Assert.True(JsonHelper.AreEqualOnField(parsedData, MetricTestsData.MetricModel, "Uid"), XMsg.InvalidFieldValue("Uid"));
            Assert.True(JsonHelper.AreEqualOnField(parsedData, MetricTestsData.MetricModel, "Name"), XMsg.InvalidFieldValue("Name"));
            Assert.True(JsonHelper.AreEqualOnField(parsedData, MetricTestsData.MetricModel, "Description"), XMsg.InvalidFieldValue("Description"));

            Assert.True(parsedData["State"].ToString() == "3", XMsg.InvalidFieldValue("State"));

            var definition = parsedData["Versions"].First() as JObject;
            Assert.True(JsonHelper.AreEqualOnField(definition, MetricTestsData.MetricModel, "Weight"), XMsg.InvalidFieldValue("Weight"));
            Assert.True(JsonHelper.AreEqualOnField(definition, MetricTestsData.MetricModel, "ConditionAndOr"), XMsg.InvalidFieldValue("ConditionAndOr"));

            Assert.True(!string.IsNullOrEmpty(MetricTestsData.MetricUid), XMsg.InvalidFieldValue("MetricUid"));

        }
        [Fact, Priority(130)]
        public async void GetMetricsBreakdown()
        {
            string endpoint = $"{URIHelper.MetricsUri}/{MetricTestsData.AssetUid}/pointbreakdown?effectiveDate=2019-06-20T11:59:03.874Z";
            var response = await httpClient.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
        }

        [Fact, Priority(140)]
        public async void DeleteAssetType()
        {
            var endpointUrl = URIHelper.AssetsUri;
            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = AssetTestData.GetDeleteJsonForAssetTypeUid(MetricTestsData.AssetTypeGuid, true).AsStringContent(),
                Method = HttpMethod.Delete,
                RequestUri = new Uri(endpointUrl)
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);
            Assert.True(parsedData.GetValue("ExecutionID") != null, XMsg.InvalidFieldValue("ExecutionId"));
            Assert.True(parsedData.GetValue("Message") != null, XMsg.InvalidFieldValue("Message"));
            Assert.True(parsedData.GetValue("Uri") != null, XMsg.InvalidFieldValue("Uri"));
        }
    }
}
