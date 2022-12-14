using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;

using Moq;
using Xunit;
using Newtonsoft.Json;

using d360.core.entities.Workflow;
using d360.web.Controllers.V2;
using d360.web.Utilities;
using igx.UnitTests.Core;
using FluentAssertions;

namespace igx.UnitTests.V2ControllerTests
{
    [Trait("Unit tests", "Workflow controller")]
    public class WorkflowControllerTest : BaseTest
    {
		private readonly WorkflowController WorkflowController;
        private readonly TestDependencyResolver DependencyResolver;
        private readonly Mock<IRuntimeInfo> RuntimeInfoMock;

		public WorkflowControllerTest()
        {
	        RuntimeInfoMock = new Mock<IRuntimeInfo>();
	        RuntimeInfoMock.Setup(x => x.IsDebuggerAttached).Returns(true);
	        RuntimeInfoMock.Setup(x => x.IsReleaseBuild).Returns(true);

	        DependencyResolver = new TestDependencyResolver();
	        DependencyResolver.AddService(RuntimeInfoMock.Object);
	        System.Web.Mvc.DependencyResolver.SetResolver(DependencyResolver);

	        WorkflowController = new WorkflowController(GetCoreComponentSet(), GetWorkflowRepository(), GetWorkflowApiModelValidator())
            {
                Request = new HttpRequestMessage(),
                Configuration = new HttpConfiguration()
            };
        }

        [Fact]
        public async void GetWorkflowTypesAsync_MustReturnsSuccessfullResult()
        {
			//Act
            var actionResult = await WorkflowController.GetWorkflowTypeAsync();
            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<List<WorkflowTypeApiViewModel>>(str);

			//Assert
			actionResult.ShouldBeOKContent<IEnumerable<WorkflowTypeApiViewModel>>();
			data.Should().NotBeNull();
        }

        [Fact]
        public async void GetWorkflowVersionSteps()
        {

            var actionResult = await WorkflowController.GetWorkflowVersionStepsAsync(Guid.Parse(DataConstants.ValidGUID));
            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<List<WorkflowVersionsApiViewModel>>(str);

            Assert.True(res.Result.IsSuccessStatusCode);
            Assert.True(data != null);

            actionResult = await WorkflowController.GetWorkflowVersionStepsAsync(Guid.Parse(DataConstants.InvalidGUID));
            res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            str = res.Result.Content.ReadAsStringAsync().Result;
            Assert.Equal(System.Net.HttpStatusCode.NotFound, res.Result.StatusCode);


        }

        [Fact]
        public async void GetWorkflowVersions()
        {
            var actionResult = await WorkflowController.GetWorkflowVersionAsync();
            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<WorkflowVersionsApiViewModel>(str);

            Assert.True(res.Result.IsSuccessStatusCode);
            Assert.True(data != null);
        }

        [Fact]
        public async void GetWorkflows()
        {
            var actionResult = await WorkflowController.GetWorkflowsAsync();
            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;

            var data = JsonConvert.DeserializeObject<WorkflowsApiViewModel>(str);

            Assert.True(res.Result.IsSuccessStatusCode);
            Assert.True(data != null);
        }

    }
}
