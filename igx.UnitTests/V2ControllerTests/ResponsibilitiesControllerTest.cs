using d360.core.entities;
using d360.web.Controllers.V2;
using igx.UnitTests.Core;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using AutoFixture;
using AutoFixture.Xunit2;
using d360.model.DataAccessLayer;
using d360.web.Services;
using FluentAssertions;
using igx.UnitTests.ServicesTests;
using Moq;
using Xunit;

namespace igx.UnitTests.V2ControllerTests
{
	public abstract class ResponsibilitiesControllerTestBase : BaseTest
	{
		protected ResponsibilitiesController responsibilitiesController;

		protected readonly Mock<IResponsibilityRepository> mockResponsibilityRepository;
		protected readonly Mock<IResourceRepository> mockResourceRepository;
		protected readonly Mock<IAssetService> mockAssetService;

		protected ResponsibilitiesControllerTestBase()
		{
			mockResponsibilityRepository = GetResponsibilityRepositoryMock();
			mockResourceRepository = new Mock<IResourceRepository>();
			mockAssetService = new Mock<IAssetService>();

			responsibilitiesController = new ResponsibilitiesController(GetCoreComponentSet(), GetAssetRepository(), GetMediator(), mockResponsibilityRepository.Object, mockResourceRepository.Object, mockAssetService.Object)
			{
				Request = new HttpRequestMessage(),
				Configuration = new HttpConfiguration()
			};
		}
	}

	[Trait("Unit tests", "Responsibilities controller")]
	public class ResponsibilitiesControllerTest : ResponsibilitiesControllerTestBase
	{
		[Fact]
		public async void GetResponsibilityTypes()
		{
			var result = await responsibilitiesController.GetResponsibilityTypesAsync();
			var str = await result.Content.ReadAsStringAsync();

			Assert.True(result.StatusCode == HttpStatusCode.OK, XMsg.InvalidJSON);
			AssertJSON.True<IEnumerable<ResponsibilityTypeViewModel>>(str);
		}

		[Fact]
		public async void GetResponsibilityTypesByAssetId()
		{
			var result = await responsibilitiesController.GetResponsibilityTypesByAssetTypeAsync(Guid.Parse(DataConstants.ValidGUID));
			var str = await result.Content.ReadAsStringAsync();

			Assert.True(result.StatusCode == HttpStatusCode.OK, XMsg.InvalidJSON);
			AssertJSON.True<IEnumerable<ResponsibilityTypeViewModel>>(str);
		}

		[Fact]
		public async void GetResponsibilityTypeAllocationsAsync()
		{
			var result = await responsibilitiesController.GetResponsibilityTypeAllocationsAsync(Guid.NewGuid());
			var str = await result.Content.ReadAsStringAsync();

			Assert.True(result.StatusCode == HttpStatusCode.OK, XMsg.InvalidJSON);
			AssertJSON.True<IEnumerable<ResponsibilityTypeAllocationViewModel>>(str);
		}

		[Fact]
		public async void GetResponsibilityTypeAllocationsByAssetAsync()
		{
			var result = await responsibilitiesController.GetResponsibilityTypeAllocationsByAssetAsync(Guid.NewGuid());
			var str = await result.Content.ReadAsStringAsync();

			Assert.True(result.StatusCode == HttpStatusCode.OK, XMsg.InvalidJSON);
			AssertJSON.True<IEnumerable<ResponsibilityTypeAllocationViewModel>>(str);
		}

		[Fact]
		public async void GetResponsibilityRulesForTypeAsync()
		{
			var result = await responsibilitiesController.GetResponsibilityRulesForTypeAsync(Guid.NewGuid());
			var str = await result.Content.ReadAsStringAsync();

			Assert.True(result.StatusCode == HttpStatusCode.OK, XMsg.InvalidJSON);
			AssertJSON.True<IEnumerable<ResponsibilityTypeRuleViewModel>>(str);
		}

		[Fact]
		public async void GetResponsibilityRulesStats()
		{
			var result = await responsibilitiesController.GetResponsibilityRulesStats(Guid.NewGuid());
			var str = await result.Content.ReadAsStringAsync();

			Assert.True(result.StatusCode == HttpStatusCode.OK, XMsg.InvalidJSON);
			AssertJSON.True<ResponsibilityTypeRuleStatsViewModel>(str);
		}

		[Fact]
		public async void GetResponsibilities()
		{
			var result = await responsibilitiesController.GetResponsibilities();
			var str = await result.Content.ReadAsStringAsync();

			Assert.True(result.StatusCode == HttpStatusCode.OK, XMsg.InvalidJSON);
			AssertJSON.True<AssetResponsibilitiesApiModel>(str);
		}

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

				mockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid.Value)).Returns(responsibilityType);
				mockResponsibilityRepository.Setup(x => x.GetTypeBreakdownAsync(typeUid)).ReturnsAsync(businessLayerResponse);

				// act
				var actualResponse = await responsibilitiesController.GetResponsibilityTypeBreakdown(typeUid);

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

				mockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid.Value)).Throws(testException);
				mockResponsibilityRepository.Setup(x => x.GetTypeBreakdownAsync(typeUid)).ReturnsAsync(businessLayerResponse);

				// act
				try
				{
					await responsibilitiesController.GetResponsibilityTypeBreakdown(typeUid);
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

				mockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid.Value)).Returns(responsibilityType);
				mockResponsibilityRepository.Setup(x => x.GetTypeBreakdownAsync(typeUid)).ThrowsAsync(testException);

				// act
				try
				{
					await responsibilitiesController.GetResponsibilityTypeBreakdown(typeUid);
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
				var businessLayerResponse = AutoFixtureHelpers.CreateClassWithRecursiveData<ICollection<ResponsibilityBreakdownResponse>>();

				mockResponsibilityRepository.Setup(x => x.GetTypeBreakdownAsync(typeUid)).ReturnsAsync(businessLayerResponse);

				await responsibilitiesController.GetResponsibilityTypeBreakdown(typeUid);

				mockResponsibilityRepository.Verify(x => x.GetResponsibilityTypeByUID(It.IsAny<Guid>()), Times.Never);
			}

			[Theory, AutoData]
			public async Task Argument_Test(
				Guid? typeUid
			)
			{
				// assign
				mockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid.Value)).ReturnsDefault();
				mockResponsibilityRepository.Setup(x => x.GetTypeBreakdownAsync(typeUid)).ReturnsNewValueAsync();

				// act
				var act = responsibilitiesController.Invoking(x => x.GetResponsibilityTypeBreakdown(typeUid));

				// assert
				await act.Should().ThrowAsync<NotFoundBusinessLayerException>();
			}
		}

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
				mockResourceRepository.Setup(x => x.GetByUidAsync(resourceUid)).ReturnsNewValueAsync();
				mockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid.Value)).ReturnsNewValue();
				var aggregate = mockResponsibilityRepository.Setup(x => x.GetTypeBreakdownByResourceAsync(resourceUid, typeUid)).ReturnsNewValueAsync();
				mockAssetService.Setup(x => x.GetAssetName(It.IsAny<AssetType>())).ReturnsNewValue();

				// act
				var actualResponse = await responsibilitiesController.GetResponsibilityTypeBreakdownByResource(resourceUid, typeUid);

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
				mockResourceRepository.Setup(x => x.GetByUidAsync(resourceUid)).ReturnsDefaultAsync();
				mockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid.Value)).ReturnsNewValue();
				mockResponsibilityRepository.Setup(x => x.GetTypeBreakdownByResourceAsync(resourceUid, typeUid)).ReturnsNewValueAsync();
				mockAssetService.Setup(x => x.GetAssetName(It.IsAny<AssetType>())).ReturnsNewValue();

				// act
				var act = responsibilitiesController.Invoking(x => x.GetResponsibilityTypeBreakdownByResource(resourceUid, typeUid));

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
				mockResourceRepository.Setup(x => x.GetByUidAsync(resourceUid)).ReturnsNewValueAsync();
				mockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid.Value)).ReturnsDefault();
				mockResponsibilityRepository.Setup(x => x.GetTypeBreakdownByResourceAsync(resourceUid, typeUid)).ReturnsNewValueAsync();
				mockAssetService.Setup(x => x.GetAssetName(It.IsAny<AssetType>())).ReturnsNewValue();

				// act
				var act = responsibilitiesController.Invoking(x => x.GetResponsibilityTypeBreakdownByResource(resourceUid, typeUid));

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
				mockResourceRepository.Setup(x => x.GetByUidAsync(resourceUid)).ReturnsNewValueAsync();
				var expectedException = mockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid.Value)).ThrowsTestException();
				mockResponsibilityRepository.Setup(x => x.GetTypeBreakdownByResourceAsync(resourceUid, typeUid)).ReturnsNewValueAsync();
				mockAssetService.Setup(x => x.GetAssetName(It.IsAny<AssetType>())).ReturnsNewValue();

				// act
				var act = responsibilitiesController.GetResponsibilityTypeBreakdownByResource(resourceUid, typeUid);

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
				var expectedException = ThrowsTestExceptionAsync(mockResourceRepository.Setup(x => x.GetByUidAsync(resourceUid)));
				mockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid.Value)).ReturnsNewValue();
				mockResponsibilityRepository.Setup(x => x.GetTypeBreakdownByResourceAsync(resourceUid, typeUid)).ReturnsNewValueAsync();
				mockAssetService.Setup(x => x.GetAssetName(It.IsAny<AssetType>())).ReturnsNewValue();

				// act
				var act = responsibilitiesController.GetResponsibilityTypeBreakdownByResource(resourceUid, typeUid);

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
				mockResourceRepository.Setup(x => x.GetByUidAsync(resourceUid)).ReturnsNewValueAsync();
				mockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid.Value)).ReturnsNewValue();
				var expectedException = mockResponsibilityRepository.Setup(x => x.GetTypeBreakdownByResourceAsync(resourceUid, typeUid)).ThrowsTestExceptionAsync();
				mockAssetService.Setup(x => x.GetAssetName(It.IsAny<AssetType>())).ReturnsNewValue();

				// act
				var act = responsibilitiesController.GetResponsibilityTypeBreakdownByResource(resourceUid, typeUid);

				// assert
				await VerifyTestExceptionAsync(act, expectedException);
			}
			#endregion Rethrow
		}

		#endregion GetResponsibilityTypeBreakdownByResource
	}

}
