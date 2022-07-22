using d360.core.entities;
using d360.web.Controllers.V2;
using igx.UnitTests.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Web.Http;
using d360.web.Utilities;
using Moq;
using Xunit;
using System.Threading.Tasks;
using FluentAssertions;

namespace igx.UnitTests.V2ControllerTests
{
    [Trait("Unit tests", "Tags controller")]
    public class TagsControllerTest : BaseTest
    {
        internal TagsController tagsController;
        private readonly TestDependencyResolver DependencyResolver;
        private readonly Mock<IRuntimeInfo> RuntimeInfoMock;

		public TagsControllerTest()
        {
	        RuntimeInfoMock = new Mock<IRuntimeInfo>();
	        RuntimeInfoMock.Setup(x => x.IsDebuggerAttached).Returns(true);
	        RuntimeInfoMock.Setup(x => x.IsReleaseBuild).Returns(true);

	        DependencyResolver = new TestDependencyResolver();
	        DependencyResolver.AddService(RuntimeInfoMock.Object);
	        System.Web.Mvc.DependencyResolver.SetResolver(DependencyResolver);

			this.tagsController = new TagsController(GetCoreComponentSet(), GetTagRepository(), GetAssetRepository())
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

        [Fact]
        public async Task DeleteTags()
        {
            var actionResult = tagsController.DeleteById(DataConstants.ValidGUID);
            await actionResult.ExecuteAsync(new System.Threading.CancellationToken());
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

            var actionResult = tagsController.Put(DataConstants.ValidGUID, model);

            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;

            Assert.True(res.Result.IsSuccessStatusCode, XMsg.BadResponseCode);
            AssertJSON.True<TagApiModel>(str);

        }


        [Fact]
        public void PutTag_ErrorInvalidGuid()
        {
            var model = new TagApiUpsertModel() { Value = DataConstants.Tags.ValidName };

            Action act = () => tagsController.Put(DataConstants.InvalidGUID, model);

			act.Should().ThrowExactly<ArgumentException>();
        }

        [Fact]
        public void PutTag_ErrorInvalidName()
        {
            var model = new TagApiUpsertModel() { Value = "invalid name" };

            var actionResult = tagsController.Put(DataConstants.InvalidGUID, model);

            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<JToken>(str);

            Assert.True(data != null, XMsg.InvalidJSON);
            Assert.True(data["type"] != null && data["type"].ToString() == "error", "Invalid type field");
            Assert.True(!res.Result.IsSuccessStatusCode, XMsg.BadResponseCode);

        }
    }
}
