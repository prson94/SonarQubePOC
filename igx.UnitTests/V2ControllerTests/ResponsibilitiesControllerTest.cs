using d360.core.entities;
using igx.UnitTests.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http.Results;
using AutoFixture;
using AutoFixture.Xunit2;
using d360.core.enums;
using d360.web.Services;
using FluentAssertions;
using Moq;
using Xunit;
using d360.web.Models;
using System.Linq.Expressions;

namespace igx.UnitTests.V2ControllerTests
{
	[Trait("Unit tests", "Responsibilities controller")]
	public class ResponsibilitiesControllerTest : ResponsibilitiesControllerTestBase
	{
		public ResponsibilitiesControllerTest()
		{
			#region MockResponsibilityRepository initialization

			MockResponsibilityRepository
				.Setup(repository => repository.DeleteAllocation(It.IsAny<ResponsibilityType>(), It.IsAny<AssetType>(), It.IsAny<bool>()))
				.ReturnsAsync((ResponsibilityType responsibilityType, AssetType assetType, bool cascade) =>
					new ResponsibilityTypeAllocationResponseModel() { AssetTypeUid = assetType.uid });

			MockResponsibilityRepository
				.Setup(repository => repository.GetResponsibilityTypeUsedInOwnershipLookupMessage(It.IsAny<ResponsibilityType>(), It.IsAny<AssetType>()))
				.Returns(string.Empty);

			MockResponsibilityRepository
				.Setup(repository => repository.AddAllocation(It.IsAny<ResponsibilityType>(), It.IsAny<AssetType>(), It.IsAny<List<int>>()))
				.Returns((ResponsibilityType responsibilityType, AssetType assetType, List<int> permissionaBitMask) => new ResponsibilityTypeAllocationResponseModel() { AssetTypeUid = assetType.uid });

			MockResponsibilityRepository
				.Setup(repository => repository.IsValidResponsibilityForAsset(It.IsAny<Guid>(), It.IsAny<Guid>()))
				.Returns(true);

			#endregion

			#region MockAssetRepository initialization

			MockAssetRepository
				.Setup(repository => repository.GetAssetByUID(It.IsAny<Guid>()))
				.Returns((Guid assetUid) => new Asset() { uid = assetUid });

			#endregion

			#region MockCompanyContext initialization

			MockCompanyContext
				.Setup(context => context.Filter(It.IsAny<Expression<Func<AssetType, bool>>>()))
				.Returns(new List<AssetType> { new AssetType { ID = 1, Class = AssetTypeClass.BusinessAsset } }.AsQueryable());

			MockCompanyContext
				.Setup(context => context.Filter(It.IsAny<Expression<Func<ResponsibilityType, bool>>>()))
				.Returns(new List<ResponsibilityType> { new ResponsibilityType { UID = Guid.Parse(DataConstants.ValidGUID) } }.AsQueryable());

			List<ResponsibilityTypeRelation> responsibilityTypeRelations = new List<ResponsibilityTypeRelation>();
			responsibilityTypeRelations.Add(new ResponsibilityTypeRelation() { ObjectType = "Object" });

			var dbSetResponsibilityTypeRelationMock = CreateDbSetMock(responsibilityTypeRelations);

			MockCompanyContext
				.Setup(context => context.ResponsibilityTypeRelations)
				.Returns(dbSetResponsibilityTypeRelationMock.Object);

			List<Asset> assets = new List<Asset>();
			assets.Add(new Asset() { uid = Guid.Parse(DataConstants.ValidGUID) });

			var dbSetAssetMock = CreateDbSetMock(assets);

			MockCompanyContext
				.Setup(context => context.Assets)
				.Returns(dbSetAssetMock.Object);

			#endregion
		}

		[Fact]
		public async Task GetResponsibilityTypesByAssetId()
		{
			MockAssetRepository
				.Setup(repository => repository.GetAssetTypeByUID(Guid.Parse(DataConstants.ValidGUID)))
				.Returns(new AssetType { ID = 1, Object = "object", });

			MockCompanyContext
				.Setup(context => context.HasAssetTypePermission("object", 1, Permission.ReadAsset))
				.Returns(true);

			var result = await ResponsibilitiesController.GetResponsibilityTypesByAssetTypeAsync(Guid.Parse(DataConstants.ValidGUID));

			result.ShouldBeOKContent<IEnumerable<ResponsibilityTypeViewModel>>();
		}

		[Fact]
		public async Task GetResponsibilityTypeAsync()
		{
			var result = await ResponsibilitiesController.GetResponsibilityTypeAsync(Guid.NewGuid());

			result.Should().BeOfType(typeof(OkNegotiatedContentResult<>));
		}

		[Fact]
		public async Task GetResponsibilityTypeAllocationsAsync()
		{
			var result = await ResponsibilitiesController.GetResponsibilityTypeAllocationsAsync(Guid.NewGuid());

			result.ShouldBeOKContent<IEnumerable<ResponsibilityTypeAllocationViewModel>>();
		}

		[Fact]
		public async Task GetResponsibilityTypeAllocationsByAssetAsync()
		{
			var result = await ResponsibilitiesController.GetResponsibilityTypeAllocationsByAssetAsync(Guid.NewGuid());

			result.ShouldBeOKContent<IEnumerable<ResponsibilityTypeAllocationViewModel>>();
		}

		[Fact]
		public void PostResponsibilityTypeAllocations()
		{
			Guid assetTypeUid = Guid.Parse(DataConstants.ValidGUID);
			List<int> permissions = new List<int>() { 1, 2, 4 };

			List<ResponsibilityTypeAllocationInsertModel> responsibilityTypeAllocationInsertModels = new List<ResponsibilityTypeAllocationInsertModel>();
			responsibilityTypeAllocationInsertModels.Add(new ResponsibilityTypeAllocationInsertModel() { AssetTypeUid = assetTypeUid, Permissions = permissions });

			var result = ResponsibilitiesController.PostResponsibilityTypeAllocations(assetTypeUid, responsibilityTypeAllocationInsertModels);

			result.ShouldBeOKContent<List<ResponsibilityTypeAllocationResponseModel>>();
		}

		[Fact]
		public void PutResponsibilityTypeAllocations()
		{
			Guid assetTypeUid = Guid.Parse(DataConstants.ValidGUID);
			List<int> permissions = new List<int>() { 1, 2, 4 };

			List<ResponsibilityTypeAllocationInsertModel> responsibilityTypeAllocationInsertModels = new List<ResponsibilityTypeAllocationInsertModel>();
			responsibilityTypeAllocationInsertModels.Add(new ResponsibilityTypeAllocationInsertModel() { AssetTypeUid = assetTypeUid, Permissions = permissions });

			var result = ResponsibilitiesController.PutResponsibilityTypeAllocations(assetTypeUid, responsibilityTypeAllocationInsertModels);

			result.ShouldBeOKContent<List<ResponsibilityTypeAllocationResponseModel>>();
		}

		[Fact]
		public async Task DeleteResponsibilityTypeAllocationsAsync()
		{
			Guid uid = Guid.Parse(DataConstants.ValidGUID);
			string objectAsset = "Object";

			AssetType assetType = new AssetType() { uid = uid, Class = AssetTypeClass.BusinessAsset, Object = objectAsset };

			List<AssetType> assetTypes = new List<AssetType>();
			assetTypes.Add(assetType);

			List<ResponsibilityTypeAllocationDeleteItemModel> responsibilityTypeAllocationDeleteItemModels = new List<ResponsibilityTypeAllocationDeleteItemModel>();
			responsibilityTypeAllocationDeleteItemModels.Add(new ResponsibilityTypeAllocationDeleteItemModel() { AssetTypeUid = uid });

			ResponsibilityTypeAllocationDeleteModel responsibilityTypeAllocationDeleteModels = new ResponsibilityTypeAllocationDeleteModel()
			{ Items = responsibilityTypeAllocationDeleteItemModels };

			MockCompanyContext
				.Setup(x => x.Filter(It.IsAny<Expression<Func<AssetType, bool>>>()))
				.Returns(assetTypes.AsQueryable());

			var result = await ResponsibilitiesController.DeleteResponsibilityTypeAllocationsAsync(uid, responsibilityTypeAllocationDeleteModels);

			result.ShouldBeOKContent<List<ResponsibilityTypeAllocationResponseModel>>();
		}

		[Fact]
		public async Task GetResponsibilityRulesForTypeAsync()
		{
			var result = await ResponsibilitiesController.GetResponsibilityRulesForTypeAsync(Guid.NewGuid());

			result.ShouldBeOKContent<IEnumerable<ResponsibilityTypeRuleViewModel>>();
		}

		[Fact]
		public async Task GetResponsibilityRulesStats()
		{
			var result = await ResponsibilitiesController.GetResponsibilityRulesStats(Guid.NewGuid());

			result.ShouldBeOKContent<ResponsibilityTypeRuleStatsViewModel>();
		}

		[Fact]
		public async Task GetResponsibilities()
		{
			var result = await ResponsibilitiesController.GetResponsibilities();

			result.ShouldBeOKContent<AssetResponsibilitiesApiModel>();
		}

		[Fact]
		public void InsertResponsibilityTypes()
		{
			Guid uid = Guid.Parse(DataConstants.ValidGUID);

			List<ResponsibilityTypeInsertModel> responsibilityTypeInsertModels = new List<ResponsibilityTypeInsertModel>();
			responsibilityTypeInsertModels.Add(new ResponsibilityTypeInsertModel() { Uid = uid });

			var result = ResponsibilitiesController.InsertResponsibilityTypes(responsibilityTypeInsertModels);

			result.ShouldBeOKContent<List<ResponsibilityTypeUpsertResult>>();
		}

		[Fact]
		public async Task GetOwnershipOfAsset()
		{
			Guid assetUid = Guid.Parse(DataConstants.ValidGUID);
			Guid responsibilityUid = Guid.Parse(DataConstants.ValidGUID);

			List<OwnershipApiModel> ownershipApiModels = new List<OwnershipApiModel>();
			ownershipApiModels.Add(new OwnershipApiModel() { ResponsibilityUid = responsibilityUid });

			MockResponsibilityRepository
				.Setup(repository => repository.GetOwnership(assetUid))
				.ReturnsAsync(ownershipApiModels);

			var result = await ResponsibilitiesController.GetOwnershipOfAsset(DataConstants.ValidGUID);

			result.ShouldBeOKContent<IEnumerable<OwnershipApiModel>>();
		}

		[Fact]
		public async Task GetAssetHasOwnership()
		{
			Guid assetUid = Guid.Parse(DataConstants.ValidGUID);

			MockResponsibilityRepository
				.Setup(repository => repository.HasOwnership(assetUid))
				.ReturnsAsync(true);

			var result = await ResponsibilitiesController.GetAssetHasOwnership(DataConstants.ValidGUID);

			result.ShouldBeOKContent<bool>();
		}

		[Fact]
		public async Task UpdateResponsibilityTypes()
		{
			Guid uid = Guid.NewGuid();
			string responsibilityTypeName = "testName";

			List<ResponsibilityTypeUpsertModel> responsibilityTypeUpsertModels = new List<ResponsibilityTypeUpsertModel>();
			responsibilityTypeUpsertModels.Add(new ResponsibilityTypeUpsertModel() { Uid = uid, Name = responsibilityTypeName });

			MockResponsibilityRepository
				.Setup(x => x.UpsertResponsibilityTypes(responsibilityTypeUpsertModels, It.IsAny<ApiExecution>()))
				.Returns(new List<ResponsibilityTypeUpsertResult>() { new ResponsibilityTypeUpsertResult() { Uid = uid } });

			var result = await ResponsibilitiesController.UpdateResponsibilityTypes(responsibilityTypeUpsertModels);

			result.ShouldBeOKContent<List<ResponsibilityTypeUpsertResult>>();
		}

		[Fact]
		public void DeleteResponsibilityTypes()
		{
			Guid uid = Guid.NewGuid();

			ResponsibilityTypeDeleteModel responsibilityTypeDeleteModel = new ResponsibilityTypeDeleteModel() { Uid = uid };

			MockResponsibilityRepository
				.Setup(x => x.DeleteResponsibilityTypes(responsibilityTypeDeleteModel))
				.Returns(new ResponsibilityTypeDeleteResult() { Uid = uid });

			var result = ResponsibilitiesController.DeleteResponsibilityTypes(responsibilityTypeDeleteModel);

			result.ShouldBeOKContent<ResponsibilityTypeDeleteResult>();
		}

		[Fact]
		public void AddResponsibilitiesOverride()
		{
			Guid assetUid = Guid.NewGuid();
			Guid resposibilityUid = Guid.NewGuid();
			Guid resourceUid = Guid.NewGuid();

			List<Guid> resourcesUids = new List<Guid>();
			resourcesUids.Add(resourceUid);

			ResponsibilityOverridePostModel responsibilityOverridePostModel = new ResponsibilityOverridePostModel()
			{ ResourceUid = resourcesUids };

			MockCompanyContext
				.Setup(context => context.HasAssetPermission(It.IsAny<long>(), Permission.AddResponsibilities))
				.Returns(true);

			MockResponsibilityRepository
				.Setup(repository => repository.GetSecurityAssetModelsForResources(resourcesUids, assetUid, resposibilityUid))
				.Returns(new List<SecurityAssetModel>() { new SecurityAssetModel() { uid = resourcesUids.FirstOrDefault(), SecurityAsset = "TestAsset" } });

			var result = ResponsibilitiesController.AddResponsibilitiesOverride(assetUid, resposibilityUid, responsibilityOverridePostModel);

			result.ShouldBeOKContent<ConfirmResponse>();
		}

		[Fact]
		public async Task BulkAddResponsibilitiesOverride()
		{
			var bulkResponsibilityOverridePostModels = new List<BulkResponsibilityOverridePostModel>();

			var result = await ResponsibilitiesController.BulkAddResponsibilitiesOverride(bulkResponsibilityOverridePostModels);

			result.ShouldBeOKContent<ApiExecutionRecievedResponse>();
		}

		[Fact]
		public void DeleteResponsibilitiesOverride()
		{
			Guid assetUid = Guid.NewGuid();
			Guid responsibilityUid = Guid.NewGuid();
			Guid resourceUid = Guid.NewGuid();

			List<Guid> resourcesUids = new List<Guid>();
			resourcesUids.Add(resourceUid);

			List<ResponsibilityOverrideDeleteModel> responsibilityOverrideDeleteModels = new List<ResponsibilityOverrideDeleteModel>();
			responsibilityOverrideDeleteModels.Add(new ResponsibilityOverrideDeleteModel() { ResourceUid = resourceUid });

			MockCompanyContext
				.Setup(context => context.HasAssetPermission(It.IsAny<long>(), Permission.DeleteResponsibilities))
				.Returns(true);

			MockResponsibilityRepository
				.Setup(repository => repository.GetSecurityAssetModelsForResources(resourcesUids, assetUid, responsibilityUid))
				.Returns(new List<SecurityAssetModel>() { new SecurityAssetModel() { uid = resourcesUids.FirstOrDefault(), SecurityAsset = "TestAsset", Exists = true } });

			var result = ResponsibilitiesController.DeleteResponsibilitiesOverride(assetUid, responsibilityUid, responsibilityOverrideDeleteModels);

			result.ShouldBeOKContent<ConfirmResponse>();
		}

		[Fact]
		public async Task PostResponsibilityRules()
		{
			Guid responsibilityTypeUid = Guid.NewGuid();

			List<ResponsibilityRuleUpsertModel> responsibilityRules = new List<ResponsibilityRuleUpsertModel>();

			var result = await ResponsibilitiesController.PostResponsibilityRules(responsibilityTypeUid, responsibilityRules);

			result.ShouldBeOKContent<List<ResponsibilityRuleUpsertResponseModel>>();
		}

		[Fact]
		public async Task PutResponsibilityRules()
		{
			Guid responsibilityTypeUid = Guid.NewGuid();

			List<ResponsibilityRuleUpsertModel> responsibilityRules = new List<ResponsibilityRuleUpsertModel>();

			var result = await ResponsibilitiesController.PutResponsibilityRules(responsibilityTypeUid, responsibilityRules);

			result.ShouldBeOKContent<List<ResponsibilityRuleUpsertResponseModel>>();
		}

		[Fact]
		public async Task TestResponsibilityRules()
		{
			string testType = "when";

			ResponsibilityRuleUpsertModel responsibilityRule = new ResponsibilityRuleUpsertModel();

			var result = await ResponsibilitiesController.TestResponsibilityRules(testType, responsibilityRule);

			result.ShouldBeOKContent<ResponsibilityRuleTestResponseModel>();
		}

		[Fact]
		public async Task DeleteResponsibilitiesOverrideByGroupOrResourceAsync()
		{
			Guid assetUid = Guid.NewGuid();

			MockAssetRepository
				.Setup(repository => repository.GetAssetByUID(assetUid))
				.Returns(new Asset() { uid = assetUid, Object = "Resource" });

			var result = await ResponsibilitiesController.DeleteResponsibilitiesOverrideByGroupOrResourceAsync(assetUid);

			result.Should().BeOfType(typeof(OkResult));
		}

		[Fact]
		public async Task DeleteResponsibilitiesOverrideByTypeAsync()
		{
			Guid responsibilityTypeUid = Guid.NewGuid();

			var result = await ResponsibilitiesController.DeleteResponsibilitiesOverrideByTypeAsync(responsibilityTypeUid);

			result.Should().BeOfType(typeof(OkResult));
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
