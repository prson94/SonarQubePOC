using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using d360.core.entities;
using d360.model.DataAccessLayer;
using d360.model.DataAccessLayer.repositories;
using d360.web.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace igx.UnitTests.ServicesTests
{
    public class ResponsibilityGetBreakdownByResourceRequestHandlerTests
    {
        private ResponsibilityGetBreakdownByResourceRequestHandler TestedObject { get; }

        private Mock<IAssetService> MockAssetService { get; }

        private Mock<IResponsibilityDapperRepository> MockResponsibilityDapperRepository { get; }

        public ResponsibilityGetBreakdownByResourceRequestHandlerTests()
        {
            MockResponsibilityDapperRepository = new Mock<IResponsibilityDapperRepository>();
            MockAssetService = new Mock<IAssetService>();
            TestedObject = new ResponsibilityGetBreakdownByResourceRequestHandler(MockResponsibilityDapperRepository.Object, MockAssetService.Object);
        }

        [Theory, AutoData]
        public void RequestModelTest(Guid resourceUid, Guid? resourceTypeUid)
        {
            var actual = new ResponsibilityGetBreakdownByResourceRequest();
            actual.ResourceUid = resourceUid;
            actual.ResourceTypeUid = resourceTypeUid;

            // assert
            actual.ResourceUid.Should().Be(resourceUid);
            actual.ResourceTypeUid.Should().Be(resourceTypeUid);
        }

        [Theory, AutoData]
        internal async Task Valid(ResponsibilityGetBreakdownByResourceRequest request, CancellationToken cancellationToken)
        {
            var repositoryResult = AutoFixtureHelpers.CreateClassWithRecursiveDataEnumerable<ResponsibilityBreakdownByResourceAggregate>().ToArray();

            // assign
            MockResponsibilityDapperRepository.Setup(x => x.GetResponsibilityBreakdownByResourceAsync(request.ResourceUid, request.ResourceTypeUid)).ReturnsAsync(repositoryResult);
            MockAssetService.Setup(x => x.GetAssetName(It.IsAny<AssetType>())).Returns<AssetType>(entity => $"{entity.uid} asset name");

            // act
            var actualResult = await TestedObject.Handle(request, cancellationToken);

            // assert
            MockResponsibilityDapperRepository.Verify(x => x.GetResponsibilityBreakdownByResourceAsync(request.ResourceUid, request.ResourceTypeUid), Times.Once);
            actualResult.Should().NotBeNull();
            
            var expectedItems = repositoryResult.Select(x => new ResponsibilityGetBreakdownByResourceModel()
            {
                AssetCount = x.AssetCount,
                AssetTypeUid = x.AssetType.uid,
                Class = x.AssetType.Class.ToString(),
                Name = $"{x.AssetType.uid} asset name"
            }).ToArray();

            actualResult.ItemCollection.Should().BeEquivalentTo(expectedItems);
            
        }
    }
}