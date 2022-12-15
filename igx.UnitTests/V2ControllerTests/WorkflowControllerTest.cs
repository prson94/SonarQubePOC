using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web.Http;

using Moq;
using Xunit;
using FluentAssertions;
using Newtonsoft.Json;

using d360.core.entities.Workflow;
using d360.web.Controllers.V2;
using d360.web.Utilities;
using d360.web.Services;
using igx.UnitTests.Core;

using Resources;

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
        public async void GetWorkflowVersionStepsAsync_ValidWorkflowVersionUid_MustReturnsSuccessfullResult()
        {
			//Act
            var actionResult = await WorkflowController.GetWorkflowVersionStepsAsync(Guid.Parse(DataConstants.ValidGUID));
            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<List<WorkflowVersionsApiViewModel>>(str);

			//Assert
			actionResult.ShouldBeOKContent<IEnumerable<WorkflowVersionStepsApiViewModel>>();
			data.Should().NotBeNull();
        }

		[Fact]
		public void GetWorkflowVersionStepsAsync_InvalidWorkflowVersionUid_MustThrowsNotFoundException()
		{
			//Act
			var workflowVersionUid = Guid.Parse(DataConstants.InvalidGUID);
			Func<Task> actionResult = async () => { await WorkflowController.GetWorkflowVersionStepsAsync(workflowVersionUid); };

			//Assert
			actionResult.Invoking(x => x.Should()
										.ThrowAsync<NotFoundBusinessLayerException>()
										.WithMessage(string.Format(WorkflowApiMessages.WorkflowVersionUIDNotFound, workflowVersionUid.ToString())));
		}

		[Fact]
        public async void GetWorkflowVersions_MustReturnsSuccessfullResult()
        {
			//Act
            var actionResult = await WorkflowController.GetWorkflowVersionAsync();
            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<WorkflowVersionsApiViewModel>(str);

			//Assert
			actionResult.ShouldBeOKContent<WorkflowVersionsApiViewModel>();
			data.Should().NotBeNull();
		}

        [Fact]
        public async void GetWorkflowsAsync_MustReturnsSuccessfullResult()
        {
			//Act
            var actionResult = await WorkflowController.GetWorkflowsAsync();
            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<WorkflowsApiViewModel>(str);

			//Assert
			actionResult.ShouldBeOKContent<WorkflowsApiViewModel>();
			data.Should().NotBeNull();
		}
    }
}
