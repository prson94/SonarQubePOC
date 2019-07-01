using igx.IntegrationTests.Core;
using System;
using System.Linq;
using Newtonsoft.Json;
using igx.IntegrationTests.TestData;
using Newtonsoft.Json.Linq;
using Xunit.Priority;
using Xunit;

namespace igx.IntegrationTests.ApiTests
{
    [Trait("Integration tests", "Tag CRUD Tests")]
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class TagControllerIntegrationTests : BaseIntegrationTestClass
    {
        [Fact, Priority(0)]
        public async void T_1_01_PostNewTag()
        {
            string endpointUrl = URIHelper.TagUri;

            var response = await httpClient.PostAsync(endpointUrl, TagTestData.TagJSON.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(!string.IsNullOrEmpty(content));
            Assert.True(Guid.Parse(parsedData["uid"].ToString()) != Guid.Empty);
            Assert.True(Guid.Parse(parsedData["CreatedByUid"].ToString()) != Guid.Empty);

            Assert.True(TagTestData.TagJSON.HasSameFieldValue(parsedData, "Value"));

            TagTestData.TagJSON = content.AsJobject();
        }

        [Fact, Priority(10)]
        public async void T_1_02_GetAfterPost()
        {
            string endpointUrl = URIHelper.TagUri;
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(!string.IsNullOrEmpty(content));

            Assert.True(parsedData["items"].Count() > 0);
            Assert.True(parsedData["items"].DoesContainToken(TagTestData.TagJSON));

        }

        [Fact, Priority(20)]
        public async void T_1_03_PutTag()
        {
            string endpointUrl = URIHelper.TagUri;

            TagTestData.TagJSON.AppendValueOnProperty("Value", "Put_Edit");

            var response = await httpClient.PutAsync($"{endpointUrl}/{TagTestData.TagJSON["uid"]}", TagTestData.TagJSON.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(!string.IsNullOrEmpty(content));
            Assert.True(TagTestData.TagJSON["uid"].ToString() == parsedData["uid"].ToString());
            Assert.True(TagTestData.TagJSON["Value"].ToString() == parsedData["Value"].ToString());
            Assert.True(TagTestData.TagJSON["CreatedByUid"].ToString() == parsedData["CreatedByUid"].ToString());
            Assert.True(TagTestData.TagJSON["CreatedOn"].ToString() == parsedData["CreatedOn"].ToString());
            Assert.True(TagTestData.TagJSON["UpdatedOn"].ToString() != parsedData["UpdatedOn"].ToString());


            TagTestData.TagJSON = content.AsJobject();
        }

        [Fact, Priority(30)]
        public async void T_1_04_GetAfterPut()
        {
            string endpointUrl = URIHelper.TagUri;
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(!string.IsNullOrEmpty(content));

            Assert.True(parsedData["items"].Count() > 0);
            Assert.True(parsedData["items"].DoesContainToken(TagTestData.TagJSON));

        }

        [Fact, Priority(40)]
        public async void T_1_05_DeleteTag()
        {
            string endpointUrl = URIHelper.TagUri;
            var response = await httpClient.DeleteAsync($"{endpointUrl}/{TagTestData.TagJSON["uid"]}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(!string.IsNullOrEmpty(content));

        }

        [Fact, Priority(50)]
        public async void T_1_06_GetAfterDelete()
        {
            string endpointUrl = URIHelper.TagUri;
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(!string.IsNullOrEmpty(content));

            Assert.True(!parsedData["items"].DoesContainToken(TagTestData.TagJSON));

        }

        [Fact, Priority(60)]
        public async void T_2_01_Validation_PostSameTagName()
        {
            string endpointUrl = URIHelper.TagUri;

            var response = await httpClient.PostAsync(endpointUrl, TagTestData.TagJSON.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(!response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest);
        }

        [Fact, Priority(70)]
        public async void T_2_02_Validation_PostEmptyValue()
        {
            string endpointUrl = URIHelper.TagUri;
            TagTestData.TagJSON.UpdateValueOnProperty("Value", "");
            var response = await httpClient.PostAsync(endpointUrl, TagTestData.TagJSON.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(!response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest);
        }

        [Fact, Priority(80)]
        public async void T_2_03_Validation_PostNameTooLong()
        {
            string endpointUrl = URIHelper.TagUri;
            var newName = string.Join("", Enumerable.Repeat(0, 251).Select(n => (char)new Random().Next(127)));
            TagTestData.TagJSON.UpdateValueOnProperty("Value", newName);

            var response = await httpClient.PostAsync(endpointUrl, TagTestData.TagJSON.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(!response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest);
        }

        [Fact, Priority(90)]
        public async void T_2_04_Validation_PutEmptyValue()
        {
            string endpointUrl = URIHelper.TagUri;
            TagTestData.TagJSON.UpdateValueOnProperty("Value", "");
            var response = await httpClient.PutAsync($"{endpointUrl}/{TagTestData.TagJSON["uid"]}", TagTestData.TagJSON.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(!response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest);
        }

        [Fact, Priority(100)]
        public async void T_2_05_Validation_PutNameTooLong()
        {
            string endpointUrl = URIHelper.TagUri;
            var newName = string.Join("", Enumerable.Repeat(0, 251).Select(n => (char)new Random().Next(127)));
            TagTestData.TagJSON.UpdateValueOnProperty("Value", newName);
            var response = await httpClient.PutAsync($"{endpointUrl}/{TagTestData.TagJSON["uid"]}", TagTestData.TagJSON.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(!response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest);
        }

        [Fact, Priority(110)]
        public async void T_2_06_Validation_PutUidNotMatching()
        {
            string endpointUrl = URIHelper.TagUri;
            var newName = string.Join("", Enumerable.Repeat(0, 251).Select(n => (char)new Random().Next(127)));
            TagTestData.TagJSON.UpdateValueOnProperty("Value", newName);
            var response = await httpClient.PutAsync($"{endpointUrl}/{Guid.NewGuid()}", TagTestData.TagJSON.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(!response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json");
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest);
        }

    }
}
