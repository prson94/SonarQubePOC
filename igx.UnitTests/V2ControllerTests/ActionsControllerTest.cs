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
        public void isPageSizeAndNumValid_TooLargePageSize_MustReturnCorrespondStringError()
        {
			//Arrange
			var pageSize = "500000";
			var pageNum = "1";
			var expectedErrorMessage = "Invalid pageSize value provided. Number is too large";
			var pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };

			//Act
			var actionResult = actionsController.isPageSizeAndNumValid(pageParams);

			//Assert
			actionResult.Should().BeEquivalentTo(expectedErrorMessage);
        }

        [Fact]
        public void isPageSizeAndNumValid_LessThanZeroPageSize_MustReturnCorrespondStringError()
        {
			//Arrange
			var pageSize = "-1";
			var pageNum = "1";
			var expectedErrorMessage = "Invalid pageSize value provided. Value must be greater than 0";
			var pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };

			//Act
			var actionResult = actionsController.isPageSizeAndNumValid(pageParams);

			//Assert
			actionResult.Should().BeEquivalentTo(expectedErrorMessage);
		}

		[Fact]
        public void isPageSizeAndNumValid_LessThanZeroPageNumber_MustReturnCorrespondStringError()
        {
			//Arrange
			var pageSize = "5";
			var pageNum = "-1";
			var expectedErrorMessage = "Invalid pageNum value provided. Value must be greater than 0";
			var pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };

			//Act
			var actionResult = actionsController.isPageSizeAndNumValid(pageParams);

			//Assert
			actionResult.Should().BeEquivalentTo(expectedErrorMessage);
		}

		[Fact]
        public void isPageSizeAndNumValid_NonNumericPageSize_MustReturnCorrespondStringError()
        {
			//Arrange
			var pageSize = "abcdef";
			var pageNum = "-1";
			var expectedErrorMessage = "Invalid pageSize value provided. Must be a numeric value";
			var pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };

			//Act
			var actionResult = actionsController.isPageSizeAndNumValid(pageParams);

			//Assert
			actionResult.Should().BeEquivalentTo(expectedErrorMessage);
        }

        [Fact]
        public void isPageSizeAndNumValid_InvalidPageSize_MustReturnCorrespondStringError()
        {
			//Arrange
            var pageSize = "12345678901";
            var pageNum = "1";
			var expectedErrorMessage = "Invalid pageSize value provided.";
			var pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };

			//Act
            var actionResult = actionsController.isPageSizeAndNumValid(pageParams);

			//Assert
            actionResult.Should().BeEquivalentTo(expectedErrorMessage);
        }

        [Fact]
        public void isPageSizeAndNumValid_InvalidPageNumber_MustReturnCorrespondStringError()
		{
			//Arrange
			var pageNum = "12345678901";
			var pageSize = "1";
			var expectedErrorMessage = "Invalid pageNum value provided.";
			var pageParams = new Dictionary<string, string> { { "_pageSize", pageSize }, { "_pageNum", pageNum } };

			//Act
			var actionResult = actionsController.isPageSizeAndNumValid(pageParams);

			//Assert
			actionResult.Should().BeEquivalentTo(expectedErrorMessage);
		}
    }
}
