using igx.IntegrationTests.Core;
using System;
using System.Linq;
using Newtonsoft.Json;
using igx.IntegrationTests.TestData;
using Newtonsoft.Json.Linq;
using Xunit.Priority;
using Xunit;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using SpreadsheetLight;
using System.IO;

namespace igx.IntegrationTests.ApiTests
{
    [Trait("Integration tests", "Tag CRUD Tests")]
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class TagControllerIntegrationTests : BaseIntegrationTestClass
    {
        [Fact, Priority(0)]
        public async void PostNewTag()
        {
            string endpointUrl = URIHelper.TagUri;

            var response = await httpClient.PostAsync(endpointUrl, TagTestData.TagJSON.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);
            Assert.True(Guid.Parse(parsedData["uid"].ToString()) != Guid.Empty, XMsg.InvalidFieldValue("uid"));
            Assert.True(Guid.Parse(parsedData["CreatedByUid"].ToString()) != Guid.Empty, XMsg.InvalidFieldValue("CreatedByUid"));

            Assert.True(TagTestData.TagJSON.HasSameFieldValue(parsedData, "Value"), XMsg.InvalidFieldValue("Value"));

            TagTestData.TagJSON = content.AsJobject();

            List<string> mustHaveFields = new List<string>() { "uid", "Value", "UseCount", "CreatedByUid", "CreatedOn", "UpdatedByUid", "UpdatedOn" };

            mustHaveFields.ForEach(f =>
            {
                Assert.True(TagTestData.TagJSON.GetValue(f) != null, XMsg.MissingField(f));
            });

        }

        [Fact, Priority(10)]
        public async void GetAfterPost()
        {
            string endpointUrl = URIHelper.TagUri;
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);

            Assert.True(parsedData["items"].Count() > 0, XMsg.InvalidCount);
            Assert.True(parsedData["items"].DoesContainToken(TagTestData.TagJSON), XMsg.MissingAsset);

            var item = parsedData["items"].First as JObject;

            List<string> mustHaveFields = new List<string>() { "uid", "Value", "UseCount", "CreatedByUid", "CreatedOn", "UpdatedByUid", "UpdatedOn" };

            mustHaveFields.ForEach(f =>
            {
                Assert.True(item.GetValue(f) != null, XMsg.MissingField(f));
            });

        }

        [Fact, Priority(20)]
        public async void Validation_PostSameTagName()
        {
            string endpointUrl = URIHelper.TagUri;

            var data = TagTestData.TagJSON.DeepClone() as JObject;
            var response = await httpClient.PostAsync(endpointUrl, data.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
        }

        [Fact, Priority(30)]
        public async void Validation_PostEmptyValue()
        {
            string endpointUrl = URIHelper.TagUri;

            var data = TagTestData.TagJSON.DeepClone() as JObject;
            data.UpdateValueOnProperty("Value", "");
            var response = await httpClient.PostAsync(endpointUrl, data.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
        }

        [Fact, Priority(40)]
        public async void Validation_PostNameTooLong()
        {
            string endpointUrl = URIHelper.TagUri;
            var newName = string.Join("", Enumerable.Repeat(0, 251).Select(n => (char)new Random().Next(127)));
            var data = TagTestData.TagJSON.DeepClone() as JObject;
            data.UpdateValueOnProperty("Value", newName);

            var response = await httpClient.PostAsync(endpointUrl, data.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
        }

        [Fact, Priority(50)]
        public async void GetAfterPostVerify()
        {
            string endpointUrl = URIHelper.TagUri;
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);

            Assert.True(parsedData["items"].Count() > 0, XMsg.InvalidCount);
            Assert.True(parsedData["items"].DoesContainToken(TagTestData.TagJSON), XMsg.MissingAsset);

        }

        [Fact, Priority(60)]
        public async void PutTag()
        {
            string endpointUrl = URIHelper.TagUri;

            TagTestData.TagJSON.AppendValueOnProperty("Value", "Put_Edit");

            var response = await httpClient.PutAsync($"{endpointUrl}/{TagTestData.TagJSON["uid"]}", TagTestData.TagJSON.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);

            List<string> mustHaveFields = new List<string>() { "uid", "Value", "UseCount", "CreatedByUid", "CreatedOn", "UpdatedByUid", "UpdatedOn" };

            mustHaveFields.ForEach(f =>
            {
                Assert.True(parsedData.GetValue(f) != null, XMsg.MissingField(f));
            });

            Assert.True(TagTestData.TagJSON["uid"].ToString() == parsedData["uid"].ToString(), XMsg.InvalidFieldValue("uid"));
            Assert.True(TagTestData.TagJSON["Value"].ToString() == parsedData["Value"].ToString(), XMsg.InvalidFieldValue("Value"));
            Assert.True(TagTestData.TagJSON["CreatedByUid"].ToString() == parsedData["CreatedByUid"].ToString(), XMsg.InvalidFieldValue("CreatedByUid"));
            Assert.True(TagTestData.TagJSON["CreatedOn"].ToString() == parsedData["CreatedOn"].ToString(), XMsg.InvalidFieldValue("CreatedOn"));
            Assert.True(TagTestData.TagJSON["UpdatedOn"].ToString() != parsedData["UpdatedOn"].ToString(), XMsg.InvalidFieldValue("UpdatedOn"));


            TagTestData.TagJSON = content.AsJobject();
        }

        [Fact, Priority(70)]
        public async void GetAfterPut()
        {
            string endpointUrl = URIHelper.TagUri;
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);

            Assert.True(parsedData["items"].Count() > 0, XMsg.InvalidCount);
            Assert.True(parsedData["items"].DoesContainToken(TagTestData.TagJSON), XMsg.MissingAsset);

        }

        [Fact, Priority(80)]
        public async void Validation_PutEmptyValue()
        {
            string endpointUrl = URIHelper.TagUri;
            var data = TagTestData.TagJSON.DeepClone() as JObject;
            data.UpdateValueOnProperty("Value", "");
            var response = await httpClient.PutAsync($"{endpointUrl}/{TagTestData.TagJSON["uid"]}", data.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
        }

        [Fact, Priority(90)]
        public async void Validation_PutNameTooLong()
        {
            string endpointUrl = URIHelper.TagUri;
            var newName = string.Join("", Enumerable.Repeat(0, 251).Select(n => (char)new Random().Next(127)));
            var data = TagTestData.TagJSON.DeepClone() as JObject;
            data.UpdateValueOnProperty("Value", newName);
            var response = await httpClient.PutAsync($"{endpointUrl}/{data["uid"]}", data.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
        }

        [Fact, Priority(100)]
        public async void Validation_PutUidNotMatching()
        {
            string endpointUrl = URIHelper.TagUri;
            var newName = string.Join("", Enumerable.Repeat(0, 251).Select(n => (char)new Random().Next(127)));

            var data = TagTestData.TagJSON.DeepClone() as JObject;
            data.UpdateValueOnProperty("Value", newName);
            var response = await httpClient.PutAsync($"{endpointUrl}/{Guid.NewGuid()}", data.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
        }

        [Fact, Priority(110)]
        public async void GetAfterPutVerify()
        {
            string endpointUrl = URIHelper.TagUri;
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);

            Assert.True(parsedData["items"].Count() > 0, XMsg.InvalidCount);
            Assert.True(parsedData["items"].DoesContainToken(TagTestData.TagJSON), XMsg.MissingAsset);

        }

        [Fact, Priority(120)]
        public async void PostAdditionalTAGS()
        {
            string endpointUrl = URIHelper.TagUri;

            var response = await httpClient.PostAsync(endpointUrl, TagTestData.TagJSON2.AsStringContent());
            var content = await response.Content.ReadAsStringAsync();

            TagTestData.TagJSON2 = content.AsJobject();

            response = await httpClient.PostAsync(endpointUrl, TagTestData.TagJSON3.AsStringContent());
            content = await response.Content.ReadAsStringAsync();

            TagTestData.TagJSON3 = content.AsJobject();
        }

        [Fact, Priority(130)]
        public async void GetAfterAdditionalPosts()
        {
            string endpointUrl = URIHelper.TagUri;
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);

            Assert.True(parsedData["items"].Count() > 0, XMsg.InvalidCount);
            Assert.True(parsedData["items"].DoesContainToken(TagTestData.TagJSON), XMsg.MissingAsset);
            Assert.True(parsedData["items"].DoesContainToken(TagTestData.TagJSON2), XMsg.MissingAsset);
            Assert.True(parsedData["items"].DoesContainToken(TagTestData.TagJSON3), XMsg.MissingAsset);
            TagTestData.TagsCount = parsedData["items"].Count();
        }

        [Fact, Priority(130)]
        public async void SearchTagsWithoutParam()
        {
            string endpointUrl = $"{URIHelper.TagUri}/search";
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JArray>(content);


            Assert.True(parsedData.Count == TagTestData.TagsCount, XMsg.InvalidCount);

            var item = parsedData.First as JObject;
            List<string> mustHaveFields = new List<string>() { "name", "code", "count" };
            mustHaveFields.ForEach(f =>
            {
                Assert.True(item.GetValue(f) != null, XMsg.MissingField(f));
            });

            var uidsFound = parsedData.Select(x => x["code"].ToString()).ToList();

            Assert.True(uidsFound.Contains(TagTestData.TagJSON["uid"].ToString()), XMsg.MissingAsset);
            Assert.True(uidsFound.Contains(TagTestData.TagJSON2["uid"].ToString()), XMsg.MissingAsset);
            Assert.True(uidsFound.Contains(TagTestData.TagJSON3["uid"].ToString()), XMsg.MissingAsset);

        }

        [Fact, Priority(130)]
        public async void SearchTagsWithParamValue()
        {
            string endpointUrl = $"{URIHelper.TagUri}/search?value=int_test_tag";
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JArray>(content);


            var item = parsedData.First as JObject;
            List<string> mustHaveFields = new List<string>() { "name", "code", "count" };
            mustHaveFields.ForEach(f =>
            {
                Assert.True(item.GetValue(f) != null, XMsg.MissingField(f));
            });

            var uidsFound = parsedData.Select(x => x["code"].ToString()).ToList();
            Assert.True(uidsFound.Contains(TagTestData.TagJSON["uid"].ToString()), XMsg.MissingAsset);
            Assert.True(uidsFound.Contains(TagTestData.TagJSON2["uid"].ToString()), XMsg.MissingAsset);
            Assert.True(uidsFound.Contains(TagTestData.TagJSON3["uid"].ToString()), XMsg.MissingAsset);

        }

        [Fact, Priority(140)]
        public async void SearchTagsWithParamValueAndExcept()
        {
            string endpointUrl = $"{URIHelper.TagUri}/search?value=int_test_tag&exceptuid={TagTestData.TagJSON["uid"].ToString()}";
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JArray>(content);


            var item = parsedData.First as JObject;
            List<string> mustHaveFields = new List<string>() { "name", "code", "count" };
            mustHaveFields.ForEach(f =>
            {
                Assert.True(item.GetValue(f) != null, XMsg.MissingField(f));
            });

            var uidsFound = parsedData.Select(x => x["code"].ToString()).ToList();
            Assert.False(uidsFound.Contains(TagTestData.TagJSON["uid"].ToString()), XMsg.MissingAsset);
            Assert.True(uidsFound.Contains(TagTestData.TagJSON2["uid"].ToString()), XMsg.MissingAsset);
            Assert.True(uidsFound.Contains(TagTestData.TagJSON3["uid"].ToString()), XMsg.MissingAsset);

        }

        [Fact, Priority(150)]
        public async void ConsolidateInvalidNoBody()
        {
            string endpointUrl = $"{URIHelper.TagUri}/consolidate/{TagTestData.TagJSON["uid"].ToString()}";
            var response = await httpClient.PostAsync(endpointUrl, null);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(!response.IsSuccessStatusCode, XMsg.BadResponseCode);
            List<string> mustHaveFields = new List<string>() { "type", "title", "message" };

            mustHaveFields.ForEach(f =>
            {
                Assert.True(parsedData.GetValue(f) != null, XMsg.MissingField(f));
            });

        }

        [Fact, Priority(160)]
        public async void ConsolidateInvalidRepeatedItem()
        {
            string endpointUrl = $"{URIHelper.TagUri}/consolidate/{TagTestData.TagJSON["uid"].ToString()}";

            List<string> children = new List<string>() { TagTestData.TagJSON["uid"].ToString(), TagTestData.TagJSON2["uid"].ToString() };
            var httpContent = new StringContent(JsonConvert.SerializeObject(children));
            var response = await httpClient.PostAsync(endpointUrl, httpContent);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JObject>(content);

            Assert.True(!response.IsSuccessStatusCode, XMsg.BadResponseCode);
            List<string> mustHaveFields = new List<string>() { "type", "title", "message" };

            mustHaveFields.ForEach(f =>
            {
                Assert.True(parsedData.GetValue(f) != null, XMsg.MissingField(f));
            });

        }

        [Fact, Priority(170)]
        public async void Consolidate()
        {
            string endpointUrl = $"{URIHelper.TagUri}/consolidate/{TagTestData.TagJSON["uid"].ToString()}";

            List<string> children = new List<string>() { TagTestData.TagJSON3["uid"].ToString(), TagTestData.TagJSON2["uid"].ToString() };
            var httpContent = new StringContent(JsonConvert.SerializeObject(children), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(endpointUrl, httpContent);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JArray>(content).First as JObject;

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            List<string> mustHaveFields = new List<string>() { "uid", "UseCount" };

            mustHaveFields.ForEach(f =>
            {
                Assert.True(parsedData.GetValue(f) != null, XMsg.MissingField(f));
            });


            Assert.True(parsedData["uid"].ToString() == TagTestData.TagJSON["uid"].ToString(), XMsg.InvalidFieldValue("uid"));

        }

        [Fact, Priority(180)]
        public async void GetAfterConsolidate()
        {
            string endpointUrl = URIHelper.TagUri;
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);

            Assert.True(parsedData["items"].Count() > 0, XMsg.InvalidCount);
            Assert.True(parsedData["items"].DoesContainToken(TagTestData.TagJSON), XMsg.MissingAsset);
            Assert.True(!parsedData["items"].DoesContainToken(TagTestData.TagJSON2), XMsg.MissingAsset);
            Assert.True(!parsedData["items"].DoesContainToken(TagTestData.TagJSON3), XMsg.MissingAsset);
        }

        [Fact, Priority(190)]
        public async void SearchTagsAfterConsolidate()
        {
            string endpointUrl = $"{URIHelper.TagUri}/search";
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JArray>(content);


            Assert.True(parsedData.Count == TagTestData.TagsCount - 2, XMsg.InvalidCount);

            var item = parsedData.First as JObject;
            List<string> mustHaveFields = new List<string>() { "name", "code", "count" };
            mustHaveFields.ForEach(f =>
            {
                Assert.True(item.GetValue(f) != null, XMsg.MissingField(f));
            });

            var uidsFound = parsedData.Select(x => x["code"].ToString()).ToList();

            Assert.True(uidsFound.Contains(TagTestData.TagJSON["uid"].ToString()), XMsg.MissingAsset);
            Assert.True(!uidsFound.Contains(TagTestData.TagJSON2["uid"].ToString()), XMsg.MissingAsset);
            Assert.True(!uidsFound.Contains(TagTestData.TagJSON3["uid"].ToString()), XMsg.MissingAsset);

        }

        [Fact, Priority(200)]
        public async void DeleteTag()
        {
            string endpointUrl = URIHelper.TagUri;
            var response = await httpClient.DeleteAsync($"{endpointUrl}/{TagTestData.TagJSON["uid"]}");
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);

        }

        [Fact, Priority(210)]
        public async void GetAfterDelete()
        {
            string endpointUrl = URIHelper.TagUri;
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStringAsync();
            var parsedData = JsonConvert.DeserializeObject<JToken>(content);

            Assert.True(response.IsSuccessStatusCode, XMsg.BadResponseCode);
            Assert.True(response.Content.Headers.ContentType.MediaType == "application/json", XMsg.BadContentType);
            Assert.True(!string.IsNullOrEmpty(content), XMsg.NoContent);

            Assert.True(!parsedData["items"].DoesContainToken(TagTestData.TagJSON), XMsg.MissingAsset);
            TagTestData.AllItems = parsedData["items"] as JArray;
        }

        [InlineData("ab","","","Value","1")]
        [InlineData("ab","","","Value","0")]
        [InlineData("ab","","","UseCount","1")]
        [InlineData("ab","","", "UseCount", "0")]
        [Theory, Priority(220)]
        public async void ExcelExportTest(string globalSearch, string value, string useCount, string sortBy, string sortOrder)
        {
            string endpointUrl = $"{URIHelper.TagUri}/export?globalSearch={globalSearch}&value={value}&useCount={useCount}&sortBy={sortBy}&sortOrder={sortOrder}";
            var response = await httpClient.GetAsync(endpointUrl);
            var content = await response.Content.ReadAsStreamAsync();
            List<JToken> filtered = TagTestData.AllItems.ToList();

            if (!string.IsNullOrEmpty(globalSearch))
            {
                filtered = filtered.Where(x => x["Value"].ToString().ToLower().Contains(globalSearch.ToLower()) || x["UseCount"].ToString().ToLower().Contains(globalSearch.ToLower())).ToList();
            }

            if (sortOrder == "1") {
                filtered = filtered.OrderBy(x => x[sortBy]).ToList();
            }
            else
            {
                filtered = filtered.OrderByDescending(x => x[sortBy]).ToList();
            }



            using (SLDocument doc = new SLDocument(content))
            {
                doc.SelectWorksheet("Items");
                var cells = doc.GetCells();

                Assert.True(cells.Count == filtered.Count + 1, XMsg.InvalidCount);

                int row = 1;
                int cell = 1;
                int uid_cell = 1;
                List<string> existingHeaders = new List<string>() { "Uid", "Name", "Use Count", "Created On", "Created By", "Updated On", "Updated By" };
                List<string> parsedHeaders = new List<string>();

                List<string> existingUids = new List<string>();
                List<string> excelUids = new List<string>();
                existingUids.AddRange(filtered.Select(x => x["uid"].ToString()));

                foreach (var item in cells)
                {
                    cell = 1;
                    var rowData = item.Value.Values;

                    //check header
                    if(row == 1)
                    {
                        foreach(SLCell c in rowData)
                        {
                            var cell_value = doc.GetCellValueAsString(row, cell);
                            parsedHeaders.Add(cell_value);
                            cell++;
                        }
                    }
                    else
                    {
                        var cell_value = doc.GetCellValueAsString(row, uid_cell);
                        excelUids.Add(cell_value);
                    }
                    row++;
                }

                for(int i = 0; i< existingHeaders.Count; i++)
                {
                    Assert.True(existingHeaders[i] == parsedHeaders[i], "Invalid header order!");
                }

                using (StreamWriter file =  new StreamWriter(@"test.txt"))
                {

                    for(int i = 0; i< excelUids.Count; i++)
                    {
                        file.WriteLine(existingUids[i] + "-" + excelUids[i]);
                    }

                }


            }


        }



    }
}
