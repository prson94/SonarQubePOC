using igx.IntegrationTests.Core;
using System;
using Newtonsoft.Json;
using igx.IntegrationTests.TestData;
using Newtonsoft.Json.Linq;
using Xunit.Priority;
using Xunit;
using System.Linq;
using System.Xml;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

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

            Assert.True(JsonHelper.DoesContainFields(parsedData[0], "Type", "Name", "Description"), "Property missing in response!");

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

            foreach (var data in groups)
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
            var uidsToExport = RelationshipTestData.RelationshipTypes.Where(x => x["Uid"] != null).Select(x => x["Uid"].ToString()).Take(3).ToList();

            foreach (var uid in uidsToExport)
            {
                string endpointUrl = $"{URIHelper.RelationshipsUri}/export/{uid}";

                var response = await httpClient.GetAsync(endpointUrl);
                var content = await response.Content.ReadAsStringAsync();

                Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
                Assert.True(response.Content.Headers.ContentType.MediaType == "application/vnd.ms-excel", XMsg.BadContentType);
            }
        }


        [Fact, Priority(50)]
        public async void GetRelationships()
        {
            string endpointUrl = $"{URIHelper.RelationshipsUri}";

            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);


            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);

            Assert.True(parsedData["items"].Count() > 0, "No data returned, testing environment must have predefined relationships!");

            Assert.True(JsonHelper.DoesContainFields(parsedData, "pageSize", "pageNum", "total", "items"), "Property missing in response!");
            Assert.True(JsonHelper.DoesContainFields(parsedData["items"][0], "Uid", "RelationshipTypeUid", "State", "Predicate", "Subject", "Object"), "Property missing in response!");

            RelationshipTestData.Relationships = parsedData;
        }

        [Fact, Priority(60)]
        public async void DeleteAssetsRelationship()
        {
            RelationshipTestData.RelationshipItem = RelationshipTestData.Relationships["items"][0] as JObject;

            string endPointUrl = $"{URIHelper.RelationshipsUri}/types/{RelationshipTestData.RelationshipItem["RelationshipTypeUid"].ToString()}";

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = RelationshipTestData.GetRelationshipForDelete(new List<string>() { RelationshipTestData.RelationshipItem["Uid"].ToString() }).AsStringContent(),
                Method = HttpMethod.Delete,
                RequestUri = new Uri(endPointUrl)
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            var parsedData = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(parsedData.All(x => x["Success"].ToString().ToLower() == "true"));


        }

        [Fact, Priority(70)]
        public async void GetRelationshipsAfterDelete()
        {
            string endpointUrl = $"{URIHelper.RelationshipsUri}";

            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);


            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);

            bool isElementFound = false;

            foreach (var item in parsedData["items"])
            {
                if (item["Uid"].ToString() == RelationshipTestData.RelationshipItem["Uid"].ToString())
                    isElementFound = true;
            }

            Assert.False(isElementFound);
        }

        [Fact, Priority(80)]
        public async void PostNewRelationship()
        {
            string endpointUrl = $"{URIHelper.RelationshipsUri}/{RelationshipTestData.RelationshipItem["RelationshipTypeUid"]}";


            var forInsert = RelationshipTestData.GetRelationshipsForInsert(
                new List<string>() { RelationshipTestData.RelationshipItem["Subject"]["Uid"].ToString() },
                new List<string>() { RelationshipTestData.RelationshipItem["Object"]["Uid"].ToString() }
                );

            var response = await httpClient.PostAsync(endpointUrl, forInsert.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            var parsedData = JsonConvert.DeserializeObject<JArray>(content);


            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);
            Assert.True(parsedData.All(x => x["Success"].ToString().ToLower() == "true"));

        }

        [Fact, Priority(90)]
        public async void GetRelationshipsAfterPost()
        {
            string endpointUrl = $"{URIHelper.RelationshipsUri}";

            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);


            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);

            bool isElementFound = false;

            foreach (var item in parsedData["items"])
            {
                if (item["Subject"]["Uid"].ToString() == RelationshipTestData.RelationshipItem["Subject"]["Uid"].ToString()
                    && item["Object"]["Uid"].ToString() == RelationshipTestData.RelationshipItem["Object"]["Uid"].ToString())
                    isElementFound = true;
            }

            Assert.True(isElementFound);
            RelationshipTestData.Relationships = parsedData;

        }

        [Fact, Priority(100)]
        public async void DeleteAssetsRelationshipBeforeBatch()
        {
            RelationshipTestData.RelationshipItem = RelationshipTestData.Relationships["items"][0] as JObject;

            string endPointUrl = $"{URIHelper.RelationshipsUri}/{RelationshipTestData.RelationshipItem["RelationshipTypeUid"].ToString()}";

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = RelationshipTestData.GetRelationshipForDelete(new List<string>() { RelationshipTestData.RelationshipItem["Uid"].ToString() }).AsStringContent(),
                Method = HttpMethod.Delete,
                RequestUri = new Uri(endPointUrl)
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            var parsedData = JsonConvert.DeserializeObject<JArray>(content);

            Assert.True(parsedData.All(x => x["Success"].ToString().ToLower() == "true"));


        }


        [Fact, Priority(110)]
        public async void BatchPostNewRelationship()
        {
            string endpointUrl = $"{URIHelper.RelationshipsUri}/batch/{RelationshipTestData.RelationshipItem["RelationshipTypeUid"]}";


            var forInsert = RelationshipTestData.GetRelationshipsForInsert(
                new List<string>() { RelationshipTestData.RelationshipItem["Subject"]["Uid"].ToString() },
                new List<string>() { RelationshipTestData.RelationshipItem["Object"]["Uid"].ToString() }
                );

            var response = await httpClient.PostAsync(endpointUrl, forInsert.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);


            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);
            Assert.True(JsonHelper.DoesContainFields(parsedData, "ExecutionID", "Message", "Uri"));
            Assert.True(Guid.Parse(parsedData["ExecutionID"].ToString()) != Guid.Empty);

            RelationshipTestData.ExecutionUri = parsedData["Uri"].ToString();

        }

        [Fact, Priority(120)]
        private async Task<bool> ExecutionStatusCheck()
        {

            int retryCount = 1;
            int retryMax = 50;
            bool doRetry = true;
            bool isSuccess = false;

            while (doRetry)
            {
                var response = await httpClient.GetAsync(RelationshipTestData.ExecutionUri);
                var content = await response.Content.ReadAsStringAsync();
                var parsedData = JsonConvert.DeserializeObject<JObject>(content);


                if (parsedData["Results"] != null && parsedData["Results"].Count() > 0)
                {
                    doRetry = false;
                    isSuccess = parsedData["Results"].All(x => x["Success"].ToString().ToLower() == "true");
                }
                retryCount++;
                if (retryCount == retryMax) doRetry = false;

                Thread.Sleep(2000);
            }

            return isSuccess;
        }

        [Fact, Priority(130)]
        private async void ERR_ExecutionStatusCheck_InvalidUID()
        {

            var response = await httpClient.GetAsync(RelationshipTestData.ExecutionUri);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);


            if (parsedData["Results"] != null && parsedData["Results"].Count() > 0)
            {
                Assert.True(parsedData["Results"].All(x => x["Success"].ToString().ToLower() == "true"), "All statuses should be true");
                Assert.True(parsedData["Results"].All(x => Guid.Parse(x["uid"].ToString()) != Guid.Empty), "Invalid uid returned");
            }

        }

        public async void GetRelationshipsAfterBatchPost()
        {
            string endpointUrl = $"{URIHelper.RelationshipsUri}";

            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);

            var parsedData = JsonConvert.DeserializeObject<JObject>(content);


            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);

            bool isElementFound = false;

            foreach (var item in parsedData["items"])
            {
                if (item["Subject"]["Uid"].ToString() == RelationshipTestData.RelationshipItem["Subject"]["Uid"].ToString()
                    && item["Object"]["Uid"].ToString() == RelationshipTestData.RelationshipItem["Object"]["Uid"].ToString())
                    isElementFound = true;
            }

            Assert.True(isElementFound);
        }
    }

    [Trait("Integration tests", "Relationship UI Tests")]
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class RelationshipControllerUITests : BaseIntegrationTestClass
    {


        [Fact, Priority(10)]
        public async void GetRelationshipsCounts()
        {
            //Input data with different intersects (object/subject)
            var data = JsonConvert.DeserializeObject<JArray>(RelationshipUITestData.JsonInputMedium);
            foreach (var item in data)
            {
                var IntersectTypeID = item["IntersectTypeID"];
                var Subject = item["Subject"].ToString();
                var SubjectID = item["SubjectID"].ToString();
                var Object = item["Object"].ToString();
                var Objectid = item["Objectid"].ToString();

                //Check does subject has all fields
                string subjectCountUrl = $"{Settings.Host}/api/{Subject}/{SubjectID}/relationships/counts";
                var response = await httpClient.GetAsync(subjectCountUrl);
                var content = await response.Content.ReadAsStringAsync();
                var parsedData = JsonConvert.DeserializeObject<JArray>(content);
                JToken subjectItem = null;


                bool checkITId = false;

                List<string> requiredFields = new List<string> { "uid", "IntersectTypeID", "ObjectUid", "Object", "ObjectID", "Count", "Name", "Cardinality", "IsSubject", "AllowEditFromRelationshipEditor" };
                foreach (var cntItem in parsedData)
                {
                    foreach (var field in requiredFields)
                    {
                        Assert.True(cntItem[field] != null, IntersectTypeID.ToString() + "::" + XMsg.MissingField(field) + " on relationship " + Subject + "--" + Object);
                    }
                    if (cntItem["IntersectTypeID"].ToString() == IntersectTypeID.ToString())
                    {
                        subjectItem = cntItem;
                        Assert.True(cntItem["IsSubject"].ToString() == "1", XMsg.InvalidFieldValue("IsSubject") + " on subject" + subjectCountUrl);
                        checkITId = true;
                    }
                }

                Assert.True(checkITId, "Missing IntersectTypeID in response!");

                //Check does object has all fields
                string objectCountUrl = $"{Settings.Host}/api/{Object}/{Objectid}/relationships/counts";
                response = await httpClient.GetAsync(objectCountUrl);
                content = await response.Content.ReadAsStringAsync();
                parsedData = JsonConvert.DeserializeObject<JArray>(content);
                checkITId = false;
                foreach (var cntItem in parsedData)
                {
                    foreach (var field in requiredFields)
                    {
                        Assert.True(cntItem[field] != null, XMsg.MissingField(field) + " on relationship " + Subject + "--" + Object);
                    }

                    if (cntItem["IntersectTypeID"].ToString() == IntersectTypeID.ToString())
                    {
                        bool areSameType = subjectItem["Object"].ToString() == cntItem["Object"].ToString() && subjectItem["ObjectID"].ToString() == cntItem["ObjectID"].ToString();
                        if (!areSameType)
                            Assert.True(cntItem["IsSubject"].ToString() == "0", IntersectTypeID.ToString() + "::" + XMsg.InvalidFieldValue("IsSubject") + " on object " + objectCountUrl);

                        checkITId = true;
                    }
                }
                Assert.True(checkITId, "Missing IntersectTypeID in response!");

            }
        }

        [Fact, Priority(20)]
        public async void GetRelationshipsDataTables()
        {            
            //Input data with different intersects (object/subject)
            var data = JsonConvert.DeserializeObject<JArray>(RelationshipUITestData.JsonInputMedium);
            List<string> requiredFields = new List<string> { "Text", "Value", "ObjectType" };
            List<string> failedRequests = new List<string>();
            foreach (var item in data)
            {
                var IntersectTypeID = item["IntersectTypeID"].ToString();
                var Subject = item["Subject"].ToString();
                var SubjectID = item["SubjectID"].ToString();
                var Object = item["Object"].ToString();
                var Objectid = item["Objectid"].ToString();

                //Check does subject has all fields
                string subjectCountUrl = $"{Settings.Host}/form/Relationship_DataTable?intersectTypeId={IntersectTypeID}&type={Subject}&objectId={SubjectID}";
                var response = await httpClient.GetAsync(subjectCountUrl);
                var content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var parsedData = JsonConvert.DeserializeObject<JArray>(content);
                    foreach (var cntItem in parsedData)
                    {
                        foreach (var field in requiredFields)
                        {
                            Assert.True(cntItem[field] != null, IntersectTypeID.ToString() + "::" + XMsg.MissingField(field) + " on relationship " + Subject + "--" + Object);
                        }
                    }
                }

                //Check does object has all fields
                string objectCountUrl = $"{Settings.Host}/form/Relationship_DataTable?intersectTypeId={IntersectTypeID}&type={Object}&objectId={Objectid}";
                response = await httpClient.GetAsync(objectCountUrl);
                content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var parsedData = JsonConvert.DeserializeObject<JArray>(content);
                    foreach (var cntItem in parsedData)
                    {
                        foreach (var field in requiredFields)
                        {
                            Assert.True(cntItem[field] != null, IntersectTypeID.ToString() + "::" + XMsg.MissingField(field) + " on relationship " + Subject + "--" + Object);
                        }

                    }
                }
            }
        }

        [Fact, Priority(20)]
        public async void GetRelationshipsIncludeReverse()
        {
            //Input data with different intersects (object/subject)
            var data = JsonConvert.DeserializeObject<JArray>(RelationshipUITestData.JsonInputMedium);
            List<string> requiredFields = new List<string> { "Uid", "ID", "IntersectTypeID", "Object", "ObjectID", "ObjectUid", "Name", "Type", "TypeID", "TypeName", "HasTechnicalRelationships" };
            List<string> failedRequests = new List<string>();
            foreach (var item in data)
            {
                var IntersectTypeUid = item["IntersectTypeUid"].ToString();
                var IntersectTypeID = item["IntersectTypeID"].ToString();
                var Subject = item["Subject"].ToString();
                var SubjectID = item["SubjectID"].ToString();
                var Object = item["Object"].ToString();
                var Objectid = item["Objectid"].ToString();
                var ObjectType = item["ObjectType"].ToString();
                var ObjectTypeId = item["ObjectTypeId"].ToString();
                var SubjectType = item["SubjectType"].ToString();
                var SubjectTypeId = item["SubjectTypeId"].ToString();

                //Check does subject has all fields
                string subjectCountUrl = $"{Settings.Host}/api/{Subject}/{SubjectID}/relationships/{ObjectType}/{ObjectTypeId}/{IntersectTypeUid}?includeInverse=true";
                var response = await httpClient.GetAsync(subjectCountUrl);
                var content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var parsedData = JsonConvert.DeserializeObject<JArray>(content);
                    foreach (var cntItem in parsedData)
                    {
                        foreach (var field in requiredFields)
                        {
                            Assert.True(cntItem[field] != null, IntersectTypeID.ToString() + "::" + XMsg.MissingField(field) + " on relationship " + Subject + "--" + Object);
                        }
                    }
                }

                //Check does object has all fields
                string objectUrl = $"{Settings.Host}/api/{Object}/{Objectid}/relationships/{SubjectType}/{SubjectTypeId}/{IntersectTypeUid}?includeInverse=true";
                response = await httpClient.GetAsync(objectUrl);
                content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var parsedData = JsonConvert.DeserializeObject<JArray>(content);
                    foreach (var cntItem in parsedData)
                    {
                        foreach (var field in requiredFields)
                        {
                            Assert.True(cntItem[field] != null, IntersectTypeID.ToString() + "::" + XMsg.MissingField(field) + " on relationship " + Subject + "--" + Object);
                        }
                    }
                }

            }
        }
    }
}
