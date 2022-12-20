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
using System.Linq;

namespace igx.UnitTests.V2ControllerTests
{
	[Trait("Unit tests", "Asset controller")]
	public class ActionsControllerTest : BaseTest
	{
		private readonly ActionsController actionsController;

		private static readonly string[] GetIssue_UIDFilters = new string[] {
			"_actionTypeUid",
			"_resourceUid",
			"_assetTypeUid",
			"_assetUid",
		};

		private static readonly string[] InvalidUIDs = new string[] {
			DataConstants.InvalidGUID,
			"adfadfaadf-asdfasdf-asdfadfa",
			"ab129a23-91b3-468d-b318-4ea0d5c5641k",
		};

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

		public static IEnumerable<object[]> GetInvalidUIDFilterData()
		{
			foreach (var p in GetIssue_UIDFilters)
			{
				foreach (var uid in InvalidUIDs)
				{
					yield return new object[] { p, uid };
				}
			}
		}

		private string GetUriWithQueryString(Dictionary<string, string> parameters)
		{
			return "http://unit-tests.eng.data3sixty.local/api/v2/actions?" +
				string.Join("&",
					parameters.Select(kvp =>
						string.Format("{0}={1}", kvp.Key, kvp.Value)
					)
				);
		}

		[Theory]
		[MemberData("GetInvalidUIDFilterData")]
		public async void GetIssueTypes_TestInvalidUIDParameters(string filterParam, string guid)
		{
			var qs = new Dictionary<string, string>();
			qs.Add(filterParam, guid);
			actionsController.Request = new HttpRequestMessage(HttpMethod.Get, GetUriWithQueryString(qs));

			await Assert.ThrowsAsync<ArgumentException>(async () => {
				var actionResult = await actionsController.GetIssueTypes();
				var res = actionResult.ExecuteAsync(new System.Threading.CancellationToken());
			});
		}
	}
}
