using igx.IntegrationTests.Core;
using igx.IntegrationTests.TestData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Xunit;
using Xunit.Priority;

namespace igx.IntegrationTests.ApiTests
{
    [Trait("Integration tests", "Responsibilities get tests")]
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class ResponsibilitesControllerTests : BaseIntegrationTestClass
    {
        [Fact, Priority(10)]
        public async void GetResponsibilityTypes()
        {

            string endpointUrl = URIHelper.ResponsibilitiesUri + "/types";
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.StatusCode == HttpStatusCode.OK, XMsg.BadResponseCode);
            Assert.True(parsedData.Count > 0, XMsg.InvalidCount);

            foreach(var item in parsedData)
            {
                Assert.True(item["uid"] != null, XMsg.InvalidFieldValue("uid"));
                Assert.True(item["Name"] != null, XMsg.InvalidFieldValue("Name"));
            }
            ResponsibilitesTestData.ResponsibilityTypes = parsedData;
            
        }

        [Fact, Priority(20)]
        public async void GetResposibilitiesAssignments()
        {
            string endpointUrl = URIHelper.ResponsibilitiesUri + "/assignments";
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.StatusCode == HttpStatusCode.OK, XMsg.BadResponseCode);
            Assert.True(parsedData.Count > 0, XMsg.InvalidCount);

            Assert.True(parsedData["pageSize"] != null, XMsg.InvalidFieldValue("pageSize"));
            Assert.True(parsedData["pageNum"] != null, XMsg.InvalidFieldValue("pageNum"));
            Assert.True(parsedData["total"] != null, XMsg.InvalidFieldValue("total"));

            Assert.True(parsedData["items"].Count() > 0, XMsg.InvalidCount);

            foreach(var item in parsedData["items"])
            {
                Assert.True(item["AssetUid"] != null, XMsg.InvalidFieldValue("AssetUid"));
                Assert.True(item["AssetTypeUid"] != null, XMsg.InvalidFieldValue("AssetTypeUid"));
                Assert.True(item["AssetTypeName"] != null, XMsg.InvalidFieldValue("AssetTypeName"));


                Assert.True(item["Responsibilities"] != null, XMsg.InvalidFieldValue("Responsibilities"));

                foreach(var resp in item["Responsibilities"])
                {
                    Assert.True(resp["AssignedToType"] != null, XMsg.InvalidFieldValue("AssignedToType"));
                    Assert.True(resp["ResponsibilityTypeUid"] != null, XMsg.InvalidFieldValue("ResponsibilityTypeUid"));
                    Assert.True(resp["AssigneeMethod"] != null, XMsg.InvalidFieldValue("AssigneeMethod"));
                    Assert.True(resp["AssigneeUid"] != null, XMsg.InvalidFieldValue("AssigneeUid"));
                    Assert.True(resp["AssigneeName"] != null, XMsg.InvalidFieldValue("AssigneeName"));
                    Assert.True(resp["AssigneeType"] != null, XMsg.InvalidFieldValue("AssigneeType"));

                }

            }
            ResponsibilitesTestData.ResponsibilityAssignments = parsedData;

        }

        [Fact, Priority(30)]
        public async void GetResponsibilityAssignmentsByAssetUid()
        {
            var assetUid = ResponsibilitesTestData.ResponsibilityAssignments["items"][0]["AssetUid"];

            string endpointUrl = $"{URIHelper.ResponsibilitiesUri}/assignments?_assetUid={assetUid}";
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.StatusCode == HttpStatusCode.OK, XMsg.BadResponseCode);
            Assert.True(parsedData.Count > 0, XMsg.InvalidCount);

            Assert.True(parsedData["items"].DoesContainToken(ResponsibilitesTestData.ResponsibilityAssignments["items"][0] as JObject), XMsg.MissingAsset);

        }

        [Fact, Priority(40)]
        public async void GetResponsibilityAssignmentsByAssetTypeUid()
        {
            var assetTypeUid = ResponsibilitesTestData.ResponsibilityAssignments["items"][0]["AssetTypeUid"];

            string endpointUrl = $"{URIHelper.ResponsibilitiesUri}/assignments?_assetTypeUid{assetTypeUid}";
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.StatusCode == HttpStatusCode.OK, XMsg.BadResponseCode);
            Assert.True(parsedData.Count > 0, XMsg.InvalidCount);

            Assert.True(parsedData["items"].DoesContainToken(ResponsibilitesTestData.ResponsibilityAssignments["items"][0] as JObject), XMsg.MissingAsset);

        }

        [Fact, Priority(50)]
        public async void GetResponsibilityAssignmentsByResponsibilityTypeUid()
        {
            var responsibilityUid = ResponsibilitesTestData.ResponsibilityAssignments["items"][0]["Responsibilities"][0]["ResponsibilityTypeUid"];

            string endpointUrl = $"{URIHelper.ResponsibilitiesUri}/assignments?_responsibilityTypeUid={responsibilityUid}";
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.StatusCode == HttpStatusCode.OK, XMsg.BadResponseCode);
            Assert.True(parsedData.Count > 0, XMsg.InvalidCount);

            Assert.True(parsedData["items"].DoesContainToken(ResponsibilitesTestData.ResponsibilityAssignments["items"][0] as JObject), XMsg.MissingAsset);

        }

        [Fact, Priority(60)]
        public async void GetResponsibilityTypesByAssetTypeUid()
        {
            var assetTypeUid = ResponsibilitesTestData.ResponsibilityAssignments["items"][0]["AssetTypeUid"];

            string endpointUrl = $"{URIHelper.ResponsibilitiesUri}/types/{assetTypeUid}";
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.StatusCode == HttpStatusCode.OK, XMsg.BadResponseCode);
            Assert.True(parsedData.Count > 0, XMsg.InvalidCount);

        }


        [Fact, Priority(70)]
        public async void GetStatsByResponsibilityType()
        {
            var responsibilityUid = ResponsibilitesTestData.ResponsibilityAssignments["items"][0]["Responsibilities"][0]["ResponsibilityTypeUid"];

            string endpointUrl = $"{URIHelper.ResponsibilitiesUri}/rules/{responsibilityUid}/stats";
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.StatusCode == HttpStatusCode.OK, XMsg.BadResponseCode);
            Assert.True(parsedData.Count > 0, XMsg.InvalidCount);
            Assert.True(parsedData["AssignedUsers"] != null, XMsg.InvalidFieldValue("AssignedUsers"));
            Assert.True(parsedData["AssignedAssets"] != null, XMsg.InvalidFieldValue("AssignedAssets"));
        }

        [Fact, Priority(80)]
        public async void GetAllocations()
        {
            var responsibilityUid = ResponsibilitesTestData.ResponsibilityAssignments["items"][0]["Responsibilities"][0]["ResponsibilityTypeUid"];

            string endpointUrl = $"{URIHelper.ResponsibilitiesUri}/types/{responsibilityUid}/allocations";
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.StatusCode == HttpStatusCode.OK, XMsg.BadResponseCode);
            Assert.True(parsedData.Count > 0, XMsg.InvalidCount);

            foreach(var item in parsedData)
            {
                Assert.True(item["ResponsibilityTypeUid"] != null, XMsg.InvalidFieldValue("ResponsibilityTypeUid"));
                Assert.True(item["ResponsibilityTypeName"] != null, XMsg.InvalidFieldValue("ResponsibilityTypeName"));
                Assert.True(item["AssetTypeUid"] != null, XMsg.InvalidFieldValue("AssetTypeUid"));
                Assert.True(item["AssetTypeName"] != null, XMsg.InvalidFieldValue("AssetTypeName"));
                Assert.True(item["AssetTypeClass"] != null, XMsg.InvalidFieldValue("AssetTypeClass"));
                Assert.True(item["PermissionsMask"] != null, XMsg.InvalidFieldValue("PermissionsMask"));
                Assert.True(item["Permissions"] != null, XMsg.InvalidFieldValue("Permissions"));
                Assert.True(item["AssetTypeName"] != null, XMsg.InvalidFieldValue("AssetTypeName"));

                Assert.True(item["Permissions"].Count() > 0, XMsg.MissingAsset);

                foreach(var perm in item["Permissions"])
                {
                    Assert.True(perm["Value"] != null, XMsg.InvalidFieldValue("Value"));
                    Assert.True(perm["ID"] != null, XMsg.InvalidFieldValue("ID"));
                    Assert.True(perm["Name"] != null, XMsg.InvalidFieldValue("Name"));
                    Assert.True(perm["Category"] != null, XMsg.InvalidFieldValue("Category"));
                    Assert.True(perm["Description"] != null, XMsg.InvalidFieldValue("Description"));
                    Assert.True(perm["Selected"] != null, XMsg.InvalidFieldValue("Selected"));
                }

            }


        }

        [Fact, Priority(90)]
        public async void GetOwnershipRules()
        {
            var responsibilityUid = ResponsibilitesTestData.ResponsibilityAssignments["items"][0]["Responsibilities"][0]["ResponsibilityTypeUid"];

            string endpointUrl = $"{URIHelper.ResponsibilitiesUri}/types/{responsibilityUid}/ownershiprules";
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(response.StatusCode == HttpStatusCode.OK, XMsg.BadResponseCode);
            Assert.True(parsedData.Count > 0, XMsg.InvalidCount);

            foreach (var item in parsedData)
            {
                Assert.True(item["uid"] != null, XMsg.InvalidFieldValue("uid"));
                Assert.True(item["Name"] != null, XMsg.InvalidFieldValue("Name"));
                Assert.True(item["Definition"] != null, XMsg.InvalidFieldValue("Definition"));
                Assert.True(item["IsVisible"] != null, XMsg.InvalidFieldValue("IsVisible"));
                Assert.True(item["ApplyToType"] != null, XMsg.InvalidFieldValue("ApplyToType"));
                Assert.True(item["LastRunOn"] != null, XMsg.InvalidFieldValue("LastRunOn"));
                Assert.True(item["AssetTypeUid"] != null, XMsg.InvalidFieldValue("AssetTypeUid"));
                Assert.True(item["AssetTypeName"] != null, XMsg.InvalidFieldValue("AssetTypeName"));
            }

        }


    }
}
