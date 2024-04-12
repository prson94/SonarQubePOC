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

			this.tagsController = new TagsController(GetCoreComponentSet(), GetQueue(), GetTagRepository(), GetAssetRepository())
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
            AssertJSON.True<PagedApiBaseViewModel<TagApiModel>>(str);
        }

        [Fact]
        public async Task DeleteTags()
        {
            var actionResult = await tagsController.DeleteById(DataConstants.ValidGUID);
            await actionResult.ExecuteAsync(new System.Threading.CancellationToken());
        }

        [Fact]
        public async Task PostTag()
        {
            var model = new TagApiUpsertModel() { Value = DataConstants.Tags.ValidName };

            var actionResult = await tagsController.PostTag(model);
            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;

            Assert.True(res.Result.IsSuccessStatusCode, XMsg.BadResponseCode);
            AssertJSON.True<TagApiModel>(str);

        }

        [Fact]
        public async Task PutTag()
        {
            var model = new TagApiUpsertModel() { Value = DataConstants.Tags.ValidName };

            var actionResult = await tagsController.Put(DataConstants.ValidGUID, model);
            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;

            Assert.True(res.Result.IsSuccessStatusCode, XMsg.BadResponseCode);
            AssertJSON.True<TagApiModel>(str);
        }


        [Fact]
        public async Task PutTag_ErrorInvalidGuid()
        {
            var model = new TagApiUpsertModel() { Value = DataConstants.Tags.ValidName };
			var actionResult = await tagsController.Put("invalid", model);
			var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
			//var str = await res.Result.Content.ReadAsStringAsync();

			Assert.True(!res.Result.IsSuccessStatusCode, XMsg.BadResponseCode);

		}
    }
}
