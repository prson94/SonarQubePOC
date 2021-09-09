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

namespace igx.UnitTests.V2ControllerTests
{
    [Trait("Unit tests", "Workflow controller")]
    public class WorkflowControllerTest : BaseTest
    {
        internal WorkflowController workflowController;

        public WorkflowControllerTest()
        {
            this.workflowController = new WorkflowController(GetCommunity(), GetCompany(), GetSettingsRepository(), GetWorkflowRepository(), GetWorkflowApiModelValidator())
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
