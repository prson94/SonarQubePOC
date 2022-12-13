using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;

using Xunit;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using d360.web.Controllers.V2;
using d360.core.entities;
using igx.UnitTests.Core;

namespace igx.UnitTests.V2ControllerTests
{
    [Trait("Unit tests", "Asset controller")]
    public class ActionsControllerTest : BaseTest
    {
        private readonly ActionsController actionsController;
        
        public ActionsControllerTest()
        {
            actionsController = new ActionsController(GetCoreComponentSet(), GetCommentRepository(), GetIssueRepository(), GetAssetRepository(), GetResponsibilityRepositoryMock().Object)
            {
                Request = new HttpRequestMessage(),
                Configuration = new HttpConfiguration()
            };
        }

        [Fact]
        public async void GetIssueTypes_MustReturnSuccessfulResult()
        {
			//Act
            var actionResult = await actionsController.GetIssueTypes();

			//Assert
			actionResult.ShouldBeOKContent<IEnumerable<IssueTypeApiModel>>();
        }

        [Fact]
        public async void GetAllocationByAssetTypeAsync_ValidAssetTypeUid_MustReturnSuccessfulResult()
        {
			//Arrange
            var testGuid = Guid.Parse(DataConstants.ValidGUID);

			//Act
            var actionResult = await actionsController.GetAllocationByAssetTypeAsync(testGuid);
            var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());

            var str = res.Result.Content.ReadAsStringAsync().Result;
            var data = JsonConvert.DeserializeObject<JArray>(str);

			//Assert
			actionResult.ShouldBeOKContent<IEnumerable<IssueTypeApiModel>>();
			data.Should().NotBeNull();
        }

        [Fact]
        public void CheckMaxPageSize()
        {
            string pageSize = "500000";
            string pageNum = "1";

            Dictionary<string, string> pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };
            string result = actionsController.isPageSizeAndNumValid(pageParams);

            Assert.Matches("Invalid pageSize value provided. Number is too large",result);

        }

        [Fact]
        public void CheckNegPageSize()
        {
            string pageSize = "-1";
            string pageNum = "1";

            Dictionary<string, string> pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };
            string result = actionsController.isPageSizeAndNumValid(pageParams);

            Assert.Matches("Invalid pageSize value provided. Value must be greater than 0",result);

        }

        [Fact]
        public void CheckNegPageNum()
        {
            string pageSize = "5";
            string pageNum = "-1";
            Dictionary<string, string> pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };

            string result = actionsController.isPageSizeAndNumValid(pageParams);

            Assert.Matches("Invalid pageNum value provided. Value must be greater than 0",result);

        }

        [Fact]
        public void CheckNonNumericValue()
        {
            string pageSize = "abcdef";
            string pageNum = "-1";
            Dictionary<string, string> pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };

            string result = actionsController.isPageSizeAndNumValid(pageParams);

            Assert.Matches("Invalid pageSize value provided. Must be a numeric value", result);

        }

        [Fact]
        public void CheckMaxLengthOfPageSize()
        {
            string pageSize = "12345678901";
            string pageNum = "1";
            Dictionary<string, string> pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };

            string result = actionsController.isPageSizeAndNumValid(pageParams);

            Assert.Matches("Invalid pageSize value provided.", result);

        }

        [Fact]
        public void CheckMaxLengthOfPageNum()
        {
            string pageNum = "12345678901";
            string pageSize = "1";
            Dictionary<string, string> pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };

            string result = actionsController.isPageSizeAndNumValid(pageParams);

            Assert.Matches("Invalid pageNum value provided.", result);

        }
    }
}
