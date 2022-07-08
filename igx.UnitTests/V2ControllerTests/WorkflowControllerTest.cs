using d360.core.entities.Workflow;
using d360.web.Controllers.V2;
using igx.UnitTests.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using d360.web.Utilities;
using Moq;
using Xunit;

namespace igx.UnitTests.V2ControllerTests
{
    [Trait("Unit tests", "Workflow controller")]
    public class WorkflowControllerTest : BaseTest
    {
        internal WorkflowController workflowController;
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

	        this.workflowController = new WorkflowController(GetCoreComponentSet(), GetWorkflowRepository(), GetWorkflowApiModelValidator())
            {
                Request = new HttpRequestMessage(),
                Configuration = new HttpConfiguration()
            };
        }

        [Fact]
        public async void GetWorkflowTypesAsync()
        {

            var actionResult = await workflowController.GetWorkflowTypeAsync();
            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<List<WorkflowTypeApiViewModel>>(str);

            Assert.True(res.Result.IsSuccessStatusCode);
            Assert.True(data != null);

        }

        [Fact]
        public async void GetWorkflowVersionSteps()
        {

            var actionResult = await workflowController.GetWorkflowVersionStepsAsync(Guid.Parse(DataConstants.ValidGUID));
            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<List<WorkflowVersionsApiViewModel>>(str);

            Assert.True(res.Result.IsSuccessStatusCode);
            Assert.True(data != null);

            actionResult = await workflowController.GetWorkflowVersionStepsAsync(Guid.Parse(DataConstants.InvalidGUID));
            res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            str = res.Result.Content.ReadAsStringAsync().Result;
            Assert.Equal(System.Net.HttpStatusCode.NotFound, res.Result.StatusCode);


        }

        [Fact]
        public async void GetWorkflowVersions()
        {
            var actionResult = await workflowController.GetWorkflowVersionAsync();
            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<WorkflowVersionsApiViewModel>(str);

            Assert.True(res.Result.IsSuccessStatusCode);
            Assert.True(data != null);
        }

        [Fact]
        public async void GetWorkflows()
        {
            var actionResult = await workflowController.GetWorkflowsAsync();
            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;

            var data = JsonConvert.DeserializeObject<WorkflowsApiViewModel>(str);

            Assert.True(res.Result.IsSuccessStatusCode);
            Assert.True(data != null);
        }

    }
}
