using igx.IntegrationTests.Core;
using System;
using Newtonsoft.Json;
using igx.IntegrationTests.TestData;
using Newtonsoft.Json.Linq;
using Xunit.Priority;
using Xunit;
using System.Linq;

namespace igx.IntegrationTests.ApiTests
{
    [Trait("Integration tests", "Relationship CRUD Tests")]
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class RelationshipControllerTests : BaseIntegrationTestClass
    {
        [Fact, Priority(0)]
        public async void GetPredicateTypes()
        {
            string endpointUrl = $"{URIHelper.RelationshipsUri}/predicates/types";

            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            var parsedData = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);

            Assert.True(parsedData.Count > 0, "No data returned, testing environment must have predefined relationships!");

            Assert.True(JsonHelper.DoesContainFields(parsedData[0], "Type", "Name", "Description"),"Property missing in response!");

            RelationshipTestData.PredicateTypes = parsedData;
        }

        [Fact, Priority(10)]
        public async void GetRelationshipsTypes()
        {
            string endpointUrl = $"{URIHelper.RelationshipsUri}/types";

            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            var parsedData = JsonConvert.DeserializeObject<JArray>(content);


            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);

            Assert.True(parsedData.Count > 0, "No data returned, testing environment must have predefined relationships!");

            Assert.True(JsonHelper.DoesContainFields(parsedData[0], "Id", "Uid", "State", "IsSystem", "Predicate", "Subject", "Object"), "Property missing in response!");

            RelationshipTestData.RelationshipTypes = parsedData;
        }

        [Fact, Priority(20)]
        public async void GetRelationshipsByPredicateUid()
        {
            var groups = RelationshipTestData.RelationshipTypes.Select(x => x["Predicate"]).Where(x => x["Uid"] != null).GroupBy(x => x["Uid"].ToString());

            Assert.True(groups.Count() > 0, "Testing environment must have relationships with predicates");

            foreach(var data in groups)
            {
                string endpointUrl = $"{URIHelper.RelationshipsUri}/types?PredicateUid={data.Key}";

                var response = await httpClient.GetAsync(endpointUrl);
                var content = await response.Content.ReadAsStringAsync();

                Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
                Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

                var parsedData = JsonConvert.DeserializeObject<JArray>(content);

                Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);
                Assert.True(parsedData.Count == data.Count(), "Invalid count returned!");


            }
        }

        [Fact, Priority(30)]
        public async void GetRelationshipsByState()
        {
            var groups = RelationshipTestData.RelationshipTypes.Where(x => x["State"] != null).GroupBy(x => x["State"].ToString());

            Assert.True(groups.Count() > 0, "Testing environment must have relationships with states");

            foreach (var data in groups)
            {
                string endpointUrl = $"{URIHelper.RelationshipsUri}/types?State={data.Key}";

                var response = await httpClient.GetAsync(endpointUrl);
                var content = await response.Content.ReadAsStringAsync();

                Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
                Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

                var parsedData = JsonConvert.DeserializeObject<JArray>(content);

                Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);
                Assert.True(parsedData.Count == data.Count(), "Invalid count returned!");


            }
        }

        [Fact, Priority(40)]
        public async void GetRelationshipsExport()
        {
            var uidsToExport = RelationshipTestData.RelationshipTypes.Where(x => x["Uid"] != null).Select(x=> x["Uid"].ToString()).ToList();

            Assert.True(groups.Count() > 0, "Testing environment must have relationships with states");

            foreach (var data in groups)
            {
                string endpointUrl = $"{URIHelper.RelationshipsUri}/types?State={data.Key}";

                var response = await httpClient.GetAsync(endpointUrl);
                var content = await response.Content.ReadAsStringAsync();

                Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
                Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

                var parsedData = JsonConvert.DeserializeObject<JArray>(content);

                Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);
                Assert.True(parsedData.Count == data.Count(), "Invalid count returned!");


            }
        }



    }
}
