using d360.core.entities;
using igx.UnitTests.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http.Results;
using AutoFixture;
using AutoFixture.Xunit2;
using d360.core.enums;
using d360.web.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace igx.UnitTests.V2ControllerTests
{
	[Trait("Unit tests", "Responsibilities controller")]
	public class ResponsibilitiesControllerTest : ResponsibilitiesControllerTestBase
	{
		[Fact]
		public async void GetResponsibilityTypesByAssetId()
		{
			MockAssetRepository
				.Setup(repository => repository.GetAssetTypeByUID(Guid.Parse(DataConstants.ValidGUID)))
				.Returns(new AssetType { ID = 1, Object = "object",  });

			MockCompanyContext
				.Setup(context => context.HasAssetTypePermission("object", 1, Permission.ReadAsset))
				.Returns(true);

			var result = await ResponsibilitiesController.GetResponsibilityTypesByAssetTypeAsync(Guid.Parse(DataConstants.ValidGUID));
			
			result.ShouldBeOKContent<IEnumerable<ResponsibilityTypeViewModel>>();
		}

		[Fact]
		public async void GetResponsibilityTypeAllocationsAsync()
		{
			var result = await ResponsibilitiesController.GetResponsibilityTypeAllocationsAsync(Guid.NewGuid());
			
			result.ShouldBeOKContent<IEnumerable<ResponsibilityTypeAllocationViewModel>>();
		}

		[Fact]
		public async void GetResponsibilityTypeAllocationsByAssetAsync()
		{
			var result = await ResponsibilitiesController.GetResponsibilityTypeAllocationsByAssetAsync(Guid.NewGuid());
			
			result.ShouldBeOKContent<IEnumerable<ResponsibilityTypeAllocationViewModel>>();
		}

		[Fact]
		public async void GetResponsibilityRulesForTypeAsync()
		{
			var result = await ResponsibilitiesController.GetResponsibilityRulesForTypeAsync(Guid.NewGuid());
			
			result.ShouldBeOKContent<IEnumerable<ResponsibilityTypeRuleViewModel>>();
		}

		[Fact]
		public async void GetResponsibilityRulesStats()
		{
			var result = await ResponsibilitiesController.GetResponsibilityRulesStats(Guid.NewGuid());
			
			result.ShouldBeOKContent<ResponsibilityTypeRuleStatsViewModel>();
		}

		[Fact]
		public async void GetResponsibilities()
		{
			var result = await ResponsibilitiesController.GetResponsibilities();
			var str = await result.Content.ReadAsStringAsync();

			Assert.True(result.StatusCode == HttpStatusCode.OK, XMsg.InvalidJSON);
			AssertJSON.True<AssetResponsibilitiesApiModel>(str);
		}

		#region GetResponsibilityTypeBreakdown

		public class GetResponsibilityTypeBreakdown : ResponsibilitiesControllerTestBase
		{
			[Theory, AutoData]
			public async Task Ok_Test(
				Guid? typeUid
			)
			{
				// assign
				var responsibilityType = Fixture.Create<ResponsibilityType>();
				var businessLayerResponse = Fixture.Create<ICollection<ResponsibilityBreakdownResponse>>();

				MockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid.Value)).Returns(responsibilityType);
				MockResponsibilityRepository.Setup(x => x.GetTypeBreakdownAsync(typeUid)).ReturnsAsync(businessLayerResponse);

				// act
				var actualResponse = await ResponsibilitiesController.GetResponsibilityTypeBreakdown(typeUid);

				// assert
				var okResult = actualResponse.Should().BeOfType<OkNegotiatedContentResult<ICollection<ResponsibilityBreakdownResponse>>>().Subject;
				okResult.Content.Should().Equal(businessLayerResponse);
			}

			[Theory, AutoData]
			public async Task GetResponsibilityTypeByUID_Exception_Test(
				Guid? typeUid,
				TestException testException
			)
			{
				// assign
				var businessLayerResponse = Fixture.Create<ICollection<ResponsibilityBreakdownResponse>>();

				MockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid.Value)).Throws(testException);
				MockResponsibilityRepository.Setup(x => x.GetTypeBreakdownAsync(typeUid)).ReturnsAsync(businessLayerResponse);

				// act
				try
				{
					await ResponsibilitiesController.GetResponsibilityTypeBreakdown(typeUid);
					Assert.False(true, $"Exception not thrown");
				}
				catch (Exception exception)
				{
					// assert
					exception.Should().Be(testException);
				}
			}

			[Theory, AutoData]
			public async Task GetTypeBreakdownAsync_Exception_Test(
				Guid? typeUid,
				TestException testException
			)
			{
				// assign
				var responsibilityType = Fixture.Create<ResponsibilityType>();

				MockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid.Value)).Returns(responsibilityType);
				MockResponsibilityRepository.Setup(x => x.GetTypeBreakdownAsync(typeUid)).ThrowsAsync(testException);

				// act
				try
				{
					await ResponsibilitiesController.GetResponsibilityTypeBreakdown(typeUid);
					Assert.False(true, $"Exception not thrown");
				}
				catch (Exception exception)
				{
					// assert
					exception.Should().Be(testException);
				}
			}

			[Fact]
			public async Task SkipValidationOfResponsibilityType_Test()
			{
				// assign
				Guid? typeUid = null;
				var businessLayerResponse = Fixture.Create<ICollection<ResponsibilityBreakdownResponse>>();

				MockResponsibilityRepository.Setup(x => x.GetTypeBreakdownAsync(typeUid)).ReturnsAsync(businessLayerResponse);

				await ResponsibilitiesController.GetResponsibilityTypeBreakdown(typeUid);

				MockResponsibilityRepository.Verify(x => x.GetResponsibilityTypeByUID(It.IsAny<Guid>()), Times.Never);
			}

			[Theory, AutoData]
			public async Task Argument_Test(
				Guid? typeUid
			)
			{
				// assign
				MockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid.Value)).ReturnsDefault();
				MockResponsibilityRepository.Setup(x => x.GetTypeBreakdownAsync(typeUid)).ReturnsNewValueAsync();

				// act
				var act = ResponsibilitiesController.Invoking(x => x.GetResponsibilityTypeBreakdown(typeUid));

				// assert
				await act.Should().ThrowAsync<NotFoundBusinessLayerException>();
			}
		}

		#endregion GetResponsibilityTypeBreakdown

		#region GetResponsibilityTypeBreakdownByResource

		public class GetResponsibilityTypeBreakdownByResource : ResponsibilitiesControllerTestBase
		{
			#region Happy Path

			[Theory, AutoData]
			public async Task Ok_Test(
				Guid resourceUid,
				Guid? typeUid
			)
			{
				// assign
				MockResourceRepository.Setup(x => x.GetByUidAsync(resourceUid)).ReturnsNewValueAsync();
				MockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid.Value)).ReturnsNewValue();
				var aggregate = MockResponsibilityRepository.Setup(x => x.GetTypeBreakdownByResourceAsync(resourceUid, typeUid)).ReturnsNewValueAsync();
				MockAssetService.Setup(x => x.GetAssetName(It.IsAny<AssetType>())).ReturnsNewValue();

				// act
				var actualResponse = await ResponsibilitiesController.GetResponsibilityTypeBreakdownByResource(resourceUid, typeUid);

				// assert
				var actualResult = actualResponse.ShouldBeOKContent<ICollection<ResponsibilityGetBreakdownByResourceModel>>();
				actualResult.Should().HaveCount(aggregate.Count);
			}

			#endregion Happy Path

			#region NotFound
			[Theory, AutoData]
			public async Task NotFound_Resource_Test(
				Guid resourceUid,
				Guid? typeUid
			)
			{
				// assign
				MockResourceRepository.Setup(x => x.GetByUidAsync(resourceUid)).ReturnsDefaultAsync();
				MockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid.Value)).ReturnsNewValue();
				MockResponsibilityRepository.Setup(x => x.GetTypeBreakdownByResourceAsync(resourceUid, typeUid)).ReturnsNewValueAsync();
				MockAssetService.Setup(x => x.GetAssetName(It.IsAny<AssetType>())).ReturnsNewValue();

				// act
				var act = ResponsibilitiesController.Invoking(x => x.GetResponsibilityTypeBreakdownByResource(resourceUid, typeUid));

				// assert
				await act.Should().ThrowAsync<NotFoundBusinessLayerException>();
			}

			[Theory, AutoData]
			public async Task NotFound_ResponsibilityType_Test(
				Guid resourceUid,
				Guid? typeUid
			)
			{
				// assign
				MockResourceRepository.Setup(x => x.GetByUidAsync(resourceUid)).ReturnsNewValueAsync();
				MockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid.Value)).ReturnsDefault();
				MockResponsibilityRepository.Setup(x => x.GetTypeBreakdownByResourceAsync(resourceUid, typeUid)).ReturnsNewValueAsync();
				MockAssetService.Setup(x => x.GetAssetName(It.IsAny<AssetType>())).ReturnsNewValue();

				// act
				var act = ResponsibilitiesController.Invoking(x => x.GetResponsibilityTypeBreakdownByResource(resourceUid, typeUid));

				// assert
				await act.Should().ThrowAsync<NotFoundBusinessLayerException>();
			}
			#endregion NotFound

			#region Rethrow
			[Theory, AutoData]
			public async Task RethrowException_GetResponsibilityTypeByUID_Test(
				Guid resourceUid,
				Guid? typeUid
			)
			{
				// assign
				MockResourceRepository.Setup(x => x.GetByUidAsync(resourceUid)).ReturnsNewValueAsync();
				var expectedException = MockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid.Value)).ThrowsTestException();
				MockResponsibilityRepository.Setup(x => x.GetTypeBreakdownByResourceAsync(resourceUid, typeUid)).ReturnsNewValueAsync();
				MockAssetService.Setup(x => x.GetAssetName(It.IsAny<AssetType>())).ReturnsNewValue();

				// act
				var act = ResponsibilitiesController.GetResponsibilityTypeBreakdownByResource(resourceUid, typeUid);

				// assert
				await VerifyTestExceptionAsync(act, expectedException);
			}

			[Theory, AutoData]
			public async Task RethrowException_GetByUidAsync_Test(
				Guid resourceUid,
				Guid? typeUid
			)
			{
				// assign
				var expectedException = ThrowsTestExceptionAsync(MockResourceRepository.Setup(x => x.GetByUidAsync(resourceUid)));
				MockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid.Value)).ReturnsNewValue();
				MockResponsibilityRepository.Setup(x => x.GetTypeBreakdownByResourceAsync(resourceUid, typeUid)).ReturnsNewValueAsync();
				MockAssetService.Setup(x => x.GetAssetName(It.IsAny<AssetType>())).ReturnsNewValue();

				// act
				var act = ResponsibilitiesController.GetResponsibilityTypeBreakdownByResource(resourceUid, typeUid);

				// assert
				await VerifyTestExceptionAsync(act, expectedException);
			}

			[Theory, AutoData]
			public async Task RethrowException_GetTypeBreakdownByResourceAsync_Test(
				Guid resourceUid,
				Guid? typeUid
			)
			{
				// assign
				MockResourceRepository.Setup(x => x.GetByUidAsync(resourceUid)).ReturnsNewValueAsync();
				MockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid.Value)).ReturnsNewValue();
				var expectedException = MockResponsibilityRepository.Setup(x => x.GetTypeBreakdownByResourceAsync(resourceUid, typeUid)).ThrowsTestExceptionAsync();
				MockAssetService.Setup(x => x.GetAssetName(It.IsAny<AssetType>())).ReturnsNewValue();

				// act
				var act = ResponsibilitiesController.GetResponsibilityTypeBreakdownByResource(resourceUid, typeUid);

				// assert
				await VerifyTestExceptionAsync(act, expectedException);
			}
			#endregion Rethrow
		}

		#endregion GetResponsibilityTypeBreakdownByResource

		#region DeleteResponsibilityRules

		public class DeleteResponsibilityRules : ResponsibilitiesControllerTestBase
		{
			#region Arrange "Happy Path"
			private IReadOnlyList<Guid> RulesForDeletion;
			private IReadOnlyList<ResponsibilityRuleDeleteModel> ResponsibilityRulesDeletes;
			private Guid ResponsibilityTypeUid;
			private ResponsibilityType ResponsibilityType;
			private ICollection<ResponsibilityRuleDeleteResponse> ExpectedResult;

			public DeleteResponsibilityRules()
			{
				// first of all we arrange happy path for tested method
				ResponsibilityTypeUid = Fixture.Create<Guid>();
				ResponsibilityRulesDeletes = Fixture.CreateEnumerable<ResponsibilityRuleDeleteModel>().ToArray();
				RulesForDeletion = ResponsibilityRulesDeletes.Select(x => x.Uid).ToArray();
				ResponsibilityType = MockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(ResponsibilityTypeUid)).ReturnsNewValue();
				ExpectedResult = MockResponsibilityRepository.Setup(x => x.DeleteResponsibilityRulesAsync(ResponsibilityTypeUid, RulesForDeletion)).ReturnsNewValueAsync();
				// and in each test we only slightly change behavior of used services to check if method process it properly
			}
			#endregion Arrange "Happy Path"

			#region Ok

			[Fact]
			public async Task Ok_Test()
			{
				// arrange

				// act
				var actualResponse = await ResponsibilitiesController.DeleteResponsibilityRules(ResponsibilityTypeUid, ResponsibilityRulesDeletes);

				// assert
				var content = actualResponse.ShouldBeOKContent<ICollection<ResponsibilityRuleDeleteResponse>>();
				content.Should().BeEquivalentTo(ExpectedResult);
			}

			#endregion Happy Path

			#region NotFound

			[Fact]
			public async Task NotFound_Test()
			{
				// arrange
				MockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(ResponsibilityTypeUid)).ReturnsDefault();

				// act
				var act = ResponsibilitiesController.Invoking(x => x.DeleteResponsibilityRules(ResponsibilityTypeUid, ResponsibilityRulesDeletes));

				// assert
				await act.Should().ThrowAsync<NotFoundBusinessLayerException>();
			}

			#endregion NotFound

			#region Rethrow

			[Fact]
			public async Task Rethrow_ResponsibilityRepository_GetResponsibilityTypeByUID_Test()
			{
				// arrange
				var testException = MockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(ResponsibilityTypeUid)).ThrowsTestException();

				// act
				var act = ResponsibilitiesController.DeleteResponsibilityRules(ResponsibilityTypeUid, ResponsibilityRulesDeletes);

				// assert
				await VerifyTestExceptionAsync(act, testException);
			}

			[Fact]
			public async Task Rethrow_ResponsibilityRepository_DeleteResponsibilityRulesAsync_Test()
			{
				// arrange
				var testException = MockResponsibilityRepository.Setup(x => x.DeleteResponsibilityRulesAsync(ResponsibilityTypeUid, RulesForDeletion)).ThrowsTestException();

				// act
				var act = ResponsibilitiesController.DeleteResponsibilityRules(ResponsibilityTypeUid, ResponsibilityRulesDeletes);

				// assert
				await VerifyTestExceptionAsync(act, testException);
			}

			#endregion Rethrow
		}

		#endregion DeleteResponsibilityRules

		#region GetClaimsAsync

		public class GetClaimsAsync : ResponsibilitiesControllerTestBase
		{
			#region Arrange "Happy Path"

			private ICollection<ClaimsViewModel> ExpectedResult;

			public GetClaimsAsync()
			{
				// first of all we arrange happy path for tested method
				ExpectedResult = MockResponsibilityRepository.Setup(x => x.GetClaimsAsync()).ReturnsNewValueAsync();
				// and in each test we only slightly change behavior of used services to check if method process it properly
			}
			#endregion Arrange "Happy Path"

			#region Ok

			[Fact]
			public async Task Ok_Test()
			{
				// arrange

				// act
				var actualResponse = await ResponsibilitiesController.GetClaimsAsync();

				// assert
				var content = actualResponse.ShouldBeOKContent<ICollection<ClaimsViewModel>>();
				content.Should().BeEquivalentTo(ExpectedResult);
			}

			#endregion Happy Path

			#region Exception rethrow

			[Fact]
			public async Task Rethrow_ResponsibilityRepository_GetClaimsAsync_Test()
			{
				// arrange
				var testException = MockResponsibilityRepository.Setup(x => x.GetClaimsAsync()).ThrowsTestException();

				// act
				var act = ResponsibilitiesController.GetClaimsAsync();

				// assert
				await VerifyTestExceptionAsync(act, testException);
			}

			#endregion Rethrow
		}

		#endregion GetClaimsAsync

		#region DeleteResponsibilityRules

		public class GetResponsibilityTypesAsync : ResponsibilitiesControllerTestBase
		{
			#region Arrange "Happy Path"

			private IEnumerable<ResponsibilityTypeViewModel> ExpectedResult;

			public GetResponsibilityTypesAsync()
			{
				// first of all we arrange happy path for tested method
				ExpectedResult = MockResponsibilityRepository.Setup(x => x.GetResponsibilityTypes()).ReturnsNewValueAsync();
				// and in each test we only slightly change behavior of used services to check if method process it properly
			}
			#endregion Arrange "Happy Path"

			#region Ok

			[Fact]
			public async Task Ok_Test()
			{
				// arrange

				// act
				var actualResponse = await ResponsibilitiesController.GetResponsibilityTypesAsync();

				// assert
				var content = actualResponse.ShouldBeOKContent<IEnumerable<ResponsibilityTypeViewModel>>();
				content.Should().BeEquivalentTo(ExpectedResult);
			}

			#endregion Happy Path

			#region Exception rethrow

			[Fact]
			public async Task Rethrow_ResponsibilityRepository_GetResponsibilityTypesAsync_Test()
			{
				// arrange
				var testException = MockResponsibilityRepository.Setup(x => x.GetResponsibilityTypes()).ThrowsTestException();

				// act
				var act = ResponsibilitiesController.GetResponsibilityTypesAsync();

				// assert
				await VerifyTestExceptionAsync(act, testException);
			}

			#endregion Rethrow
		}

		#endregion DeleteResponsibilityRules
	}
}
