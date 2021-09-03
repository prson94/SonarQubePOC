using d360.core.entities.Workflow;
using d360.web.Controllers.V2;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Xunit;
using igx.UnitTests.Core;
using d360.core.entities;
using Newtonsoft.Json.Linq;

namespace igx.UnitTests.V2ControllerTests
{
    [Trait("Unit tests", "Tags controller")]
    public class TagsControllerTest : BaseTest
    {
        internal TagsController tagsController;

        public TagsControllerTest()
        {
            this.tagsController = new TagsController(GetCommunity(), GetCompany(), GetTagRepository(), GetAssetRepository(), GetSettingsRepository())
            {
                Request = new HttpRequestMessage(),
                Configuration = new HttpConfiguration()
            };
        }

        [Fact]
        public async void GetTags()
        {

            var actionResult = await tagsController.Get();

            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
            var str = res.Result.Content.ReadAsStringAsync().Result;

            Assert.True(res.Result.IsSuccessStatusCode, XMsg.BadResponseCode);
            AssertJSON.True<TagApiModelWrapper>(str);

        }

        [Theory]
        [InlineData(DataConstants.ValidGUID)]
        [InlineData(DataConstants.InvalidGUID)]
        public void DeleteTags(string uid)
        {

            var actionResult = tagsController.DeleteById(Guid.Parse(uid));

            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<JToken>(str);
            if (uid == DataConstants.ValidGUID)
            {
                Assert.True(res.Result.IsSuccessStatusCode, XMsg.BadResponseCode);
            }
            else
            {
                Assert.True(!res.Result.IsSuccessStatusCode, XMsg.BadResponseCode);

            }

        }

        [Fact]
        public void PostTag()
        {
            var model = new TagApiUpsertModel() { Value = DataConstants.Tags.ValidName };

            var actionResult = tagsController.PostTag(model);

            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;

            Assert.True(res.Result.IsSuccessStatusCode, XMsg.BadResponseCode);
            AssertJSON.True<TagApiModel>(str);

        }


        [Fact]
        public void PostTag_Error()
        {
            var model = new TagApiUpsertModel() { Value = "invalid_name" };

            var actionResult = tagsController.PostTag(model);

            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<JToken>(str);

            Assert.True(data != null, XMsg.InvalidJSON);
            Assert.True(data["type"] != null && data["type"].ToString() == "error", "Invalid type field");
            Assert.True(!res.Result.IsSuccessStatusCode, XMsg.BadResponseCode);

        }

        [Fact]
        public void PutTag()
        {
            var model = new TagApiUpsertModel() { Value = DataConstants.Tags.ValidName };

            var actionResult = tagsController.Put(Guid.Parse(DataConstants.ValidGUID), model);

            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;

            Assert.True(res.Result.IsSuccessStatusCode, XMsg.BadResponseCode);
            AssertJSON.True<TagApiModel>(str);

        }


        [Fact]
        public void PutTag_ErrorInvalidGuid()
        {
            var model = new TagApiUpsertModel() { Value = DataConstants.Tags.ValidName };

            var actionResult = tagsController.Put(Guid.Parse(DataConstants.InvalidGUID), model);

            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<JToken>(str);

            Assert.True(data != null, XMsg.InvalidJSON);
            Assert.True(data["type"] != null && data["type"].ToString() == "error", "Invalid type field");
            Assert.True(!res.Result.IsSuccessStatusCode, XMsg.BadResponseCode);

        }


        [Fact]
        public void PutTag_ErrorInvalidName()
        {
            var model = new TagApiUpsertModel() { Value = "invalid name" };

            var actionResult = tagsController.Put(Guid.Parse(DataConstants.InvalidGUID), model);

            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<JToken>(str);

            Assert.True(data != null, XMsg.InvalidJSON);
            Assert.True(data["type"] != null && data["type"].ToString() == "error", "Invalid type field");
            Assert.True(!res.Result.IsSuccessStatusCode, XMsg.BadResponseCode);

        }

    }
}
