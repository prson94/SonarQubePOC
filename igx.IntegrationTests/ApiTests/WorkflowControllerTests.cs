using igx.IntegrationTests.Core;
using igx.IntegrationTests.TestData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Priority;

namespace igx.IntegrationTests.ApiTests
{
    [Trait("Integration tests", "Workflow get tests")]
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class WorkflowControllerTests : BaseIntegrationTestClass
    {
        [Fact, Priority(10)]
        public async void T_1_01_GetAllWorkflowTypes()
        {

            string endpointUrl = URIHelper.WorkflowTypesUri;
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            //Integration test should be run in environment with added workflows
            Assert.True(parsedData.Count > 0);
            WorkflowTestData.WorkflowTypes = parsedData;
        }

        [Fact, Priority(20)]
        public async void T_1_02_GetWorkflowTypesByChangeType()
        {
            string endpointUrl = URIHelper.WorkflowTypesUri;

            foreach (var changeType in WorkflowTestData.WorkflowTypes.GroupBy(x => x["ChangeType"].ToString()))
            {
                var response = await httpClient.GetAsync($"{endpointUrl}?ChangeType={changeType.Key}");
                var content = await response.Content.ReadAsStringAsync();
                var parsedData = JsonConvert.DeserializeObject<JArray>(content);

                Assert.True(response.IsSuccessStatusCode);
                Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
                Assert.True(parsedData.Count == changeType.Count());
            }

        }

        [Fact, Priority(30)]
        public async void T_1_03_GetWorkflowTypesByState()
        {
            string endpointUrl = URIHelper.WorkflowTypesUri;

            foreach (var stateType in WorkflowTestData.WorkflowTypes.GroupBy(x => x["State"]))
            {
                var response = await httpClient.GetAsync($"{endpointUrl}?State={stateType.Key}");
                var content = await response.Content.ReadAsStringAsync();
                var parsedData = JsonConvert.DeserializeObject<JArray>(content);


                Assert.True(response.IsSuccessStatusCode);
                Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
                Assert.True(parsedData.Count == stateType.Count());
            }

        }


        [Fact, Priority(50)]
        public async void T_1_05_GetWorkflowTypesByActionTypeUIDTest()
        {
            string endpointUrl = URIHelper.WorkflowTypesUri;

            foreach (var byUid in WorkflowTestData.WorkflowTypes.Where(x => x["ActionTypeUid"] != null).GroupBy(x => x["ActionTypeUid"].ToString()).OrderByDescending(x => x.Count()).Take(5))
            {
                var response = await httpClient.GetAsync($"{endpointUrl}?ActionTypeUid={byUid.Key}");
                var content = await response.Content.ReadAsStringAsync();
                var parsedData = JsonConvert.DeserializeObject<JArray>(content);


                Assert.True(response.IsSuccessStatusCode);
                Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
                Assert.True(parsedData.Count == byUid.Count());
            }

        }

        [Fact, Priority(60)]
        public async void T_1_06_GetWorkflowTypesByAssetTypeUIDTest()
        {
            string endpointUrl = URIHelper.WorkflowTypesUri;

            foreach (var byUid in WorkflowTestData.WorkflowTypes.Where(x => x["AssetTypeUid"] != null).GroupBy(x => x["AssetTypeUid"].ToString()).OrderByDescending(x => x.Count()).Take(5))
            {
                var response = await httpClient.GetAsync($"{endpointUrl}?AssetTypeUid={byUid.Key}");
                var content = await response.Content.ReadAsStringAsync();
                var parsedData = JsonConvert.DeserializeObject<JArray>(content);


                Assert.True(response.IsSuccessStatusCode);
                Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
                Assert.True(parsedData.Count == byUid.Count());
            }

        }

        [Fact, Priority(70)]
        public async void T_1_07_GetWorkflowTypesByRelationshipTypeUIDTest()
        {
            string endpointUrl = URIHelper.WorkflowTypesUri;

            foreach (var byUid in WorkflowTestData.WorkflowTypes.Where(x => x["RelationshipTypeUid"] != null).GroupBy(x => x["RelationshipTypeUid"].ToString()).OrderByDescending(x => x.Count()).Take(5))
            {
                var response = await httpClient.GetAsync($"{endpointUrl}?RelationshipTypeUid={byUid.Key}");
                var content = await response.Content.ReadAsStringAsync();
                var parsedData = JsonConvert.DeserializeObject<JArray>(content);


                Assert.True(response.IsSuccessStatusCode);
                Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
                Assert.True(parsedData.Count == byUid.Count());
            }

        }


        [Fact, Priority(1000)]
        public async void T_2_01_GetWorkflowVersionTest()
        {
            string endpointUrl = URIHelper.WorkflowVersionUriWithPageSize;
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            //Integration test should be run in environment with added workflows
            Assert.True(parsedData["items"].Count() > 0);
            WorkflowTestData.WorkflowVersions = parsedData;
        }

        [Fact, Priority(1010)]
        public async void T_2_02_GetWorkflowVersionByStateTest()
        {
            string endpointUrl = URIHelper.WorkflowVersionUriWithPageSize;

            foreach (var filteredBy in WorkflowTestData.WorkflowVersions["items"].GroupBy(x => x["State"].ToString()))
            {
                var response = await httpClient.GetAsync($"{endpointUrl}&State={filteredBy.Key}");
                var content = await response.Content.ReadAsStringAsync();
                var parsedData = JsonConvert.DeserializeObject<JObject>(content);

                Assert.True(response.IsSuccessStatusCode);
                Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
                Assert.True(parsedData["items"].Count() == filteredBy.Count());
            }
        }

        [Fact, Priority(1020)]
        public async void T_2_03_GetWorkflowVersionByActionType()
        {
            string endpointUrl = URIHelper.WorkflowVersionUriWithPageSize;

            foreach (var filteredBy in WorkflowTestData.WorkflowVersions["items"].Where(x => x["ActionTypeUid"] != null).GroupBy(x => x["ActionTypeUid"].ToString()))
            {
                var response = await httpClient.GetAsync($"{endpointUrl}&ActionTypeUid={filteredBy.Key}");
                var content = await response.Content.ReadAsStringAsync();
                var parsedData = JsonConvert.DeserializeObject<JObject>(content);

                Assert.True(response.IsSuccessStatusCode);
                Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
                Assert.True(parsedData["items"].Count() == filteredBy.Count());
            }
        }


        [Fact, Priority(1030)]
        public async void T_2_04_GetWorkflowVersionByAssetTypeUid()
        {
            string endpointUrl = URIHelper.WorkflowVersionUriWithPageSize;

            foreach (var filteredBy in WorkflowTestData.WorkflowVersions["items"].Where(x => x["AssetTypeUid"] != null).GroupBy(x => x["AssetTypeUid"].ToString()))
            {
                var response = await httpClient.GetAsync($"{endpointUrl}&AssetTypeUid={filteredBy.Key}");
                var content = await response.Content.ReadAsStringAsync();
                var parsedData = JsonConvert.DeserializeObject<JObject>(content);

                Assert.True(response.IsSuccessStatusCode);
                Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
                Assert.True(parsedData["items"].Count() == filteredBy.Count());
            }
        }

        [Fact, Priority(1040)]
        public async void T_2_05_GetWorkflowVersionByRelationshipTypeUid()
        {
            string endpointUrl = URIHelper.WorkflowVersionUriWithPageSize;

            foreach (var filteredBy in WorkflowTestData.WorkflowVersions["items"].Where(x => x["RelationshipTypeUid"] != null).GroupBy(x => x["RelationshipTypeUid"].ToString()))
            {
                var response = await httpClient.GetAsync($"{endpointUrl}&RelationshipTypeUid={filteredBy.Key}");
                var content = await response.Content.ReadAsStringAsync();
                var parsedData = JsonConvert.DeserializeObject<JObject>(content);

                Assert.True(response.IsSuccessStatusCode);
                Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
                Assert.True(parsedData["items"].Count() == filteredBy.Count());
            }
        }

        [Fact, Priority(1050)]
        public async void T_2_06_GetWorkflowVersionByWorkflowTypeUid()
        {
            string endpointUrl = URIHelper.WorkflowVersionUriWithPageSize;

            foreach (var filteredBy in WorkflowTestData.WorkflowVersions["items"].Where(x => x["WorkflowTypeUid"] != null).GroupBy(x => x["WorkflowTypeUid"].ToString()))
            {
                var response = await httpClient.GetAsync($"{endpointUrl}&WorkflowTypeUid={filteredBy.Key}");
                var content = await response.Content.ReadAsStringAsync();
                var parsedData = JsonConvert.DeserializeObject<JObject>(content);

                Assert.True(response.IsSuccessStatusCode);
                Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
                Assert.True(parsedData["items"].Count() == filteredBy.Count());
            }
        }

        [Theory, Priority(1060)]
        [InlineData(0, 1, "VersionNumber")]
        [InlineData(0, 15, "VersionNumber")]
        [InlineData(1, 15, "VersionNumber")]
        [InlineData(2, 15, "VersionNumber")]
        [InlineData(3, 15, "CreatedOn")]
        [InlineData(3, 15, "UpdatedOn")]

        public async void T_2_07_GetWorkflowVersionPageSizeOrdering(int pageNum, int pageSize, string orderBy)
        {
            if (pageNum <= 0) pageNum = 1;
            string endpointUrl = URIHelper.WorkflowVersionUriWithoutPageSize;

            var response = await httpClient.GetAsync($"{endpointUrl.Replace("?_pageSize=10000","")}?_pageSize={pageSize}&_pageNum={pageNum}&_order={orderBy}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(parsedData["items"].Count() == pageSize);

            var ordered = new List<JToken>();
            switch (orderBy)
            {
                case "CreatedOn":
                    ordered = WorkflowTestData.WorkflowVersions["items"].OrderBy(x => DateTime.Parse(x["CreatedOn"].ToString())).ToList();
                    break;
                case "UpdatedOn":
                    ordered = WorkflowTestData.WorkflowVersions["items"].OrderBy(x => DateTime.Parse(x["UpdatedOn"].ToString())).ToList();
                    break;
                case "VersionNumber":
                    ordered = WorkflowTestData.WorkflowVersions["items"].OrderBy(x => int.Parse(x["VersionNumber"].ToString())).ToList();
                    break;
            }

            JArray realData = new JArray();
            foreach(var item in ordered.Skip((pageNum - 1) * pageSize).Take(pageSize).ToList())
            {
                realData.Add(item);
            }

            Assert.True(SimpleJsonComparer.IsEqual(realData, parsedData["items"]));
        }

        [Fact, Priority(2000)]
        public async void T_3_01_GetWorkflowSteps()
        {
            string endpointUrl = URIHelper.WorkflowVersionUriWithoutPageSize;

            foreach (var item in WorkflowTestData.WorkflowVersions["items"].Where(x => x["State"].ToString() == "Active").Take(5))
            {

                var response = await httpClient.GetAsync($"{endpointUrl}/{item["Uid"].ToString()}/steps");
                var content = await response.Content.ReadAsStringAsync();
                var parsedData = JsonConvert.DeserializeObject<JArray>(content);

                Assert.True(response.IsSuccessStatusCode);
                Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
                Assert.True(parsedData != null);
            }


        }



    }
}
