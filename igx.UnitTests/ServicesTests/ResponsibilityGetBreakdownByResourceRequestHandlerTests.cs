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
using igx.UnitTests.V2ControllerTests;
using MediatR;
using Moq;
using Xunit;

namespace igx.UnitTests.ServicesTests
{
    public class ResponsibilityGetBreakdownByResourceRequestHandlerTests
    {
        private ResponsibilityGetBreakdownByResourceRequestHandler TestedObject { get; }

        private Mock<IAssetService> MockAssetService { get; }

        private Mock<IResponsibilityDapperRepository> MockResponsibilityDapperRepository { get; }

        private Mock<IMediator> MockMediator { get; }

        public ResponsibilityGetBreakdownByResourceRequestHandlerTests()
        {
            MockResponsibilityDapperRepository = new Mock<IResponsibilityDapperRepository>();
            MockAssetService = new Mock<IAssetService>();
            MockMediator = new Mock<IMediator>();
            TestedObject = new ResponsibilityGetBreakdownByResourceRequestHandler(MockMediator.Object, MockResponsibilityDapperRepository.Object, MockAssetService.Object);
        }

        [Theory, AutoData]
        public void RequestModelTest(Guid resourceUid, Guid? resourceTypeUid)
        {
            var actual = new ResponsibilityGetBreakdownByResourceRequest();
            actual.ResourceUid = resourceUid;
            actual.ResponsibilityTypeUid = resourceTypeUid;

            // assert
            actual.ResourceUid.Should().Be(resourceUid);
            actual.ResponsibilityTypeUid.Should().Be(resourceTypeUid);
        }

        [Theory, AutoData]
        internal async Task Valid(ResponsibilityGetBreakdownByResourceRequest request, CancellationToken cancellationToken)
        {
            var repositoryResult = AutoFixtureHelpers.CreateClassWithRecursiveDataEnumerable<ResponsibilityBreakdownByResourceAggregate>().ToArray();

            // assign
            MockResponsibilityDapperRepository.Setup(x => x.GetResponsibilityBreakdownByResourceAsync(request.ResourceUid, request.ResponsibilityTypeUid)).ReturnsAsync(repositoryResult);
            MockAssetService.Setup(x => x.GetAssetName(It.IsAny<AssetType>())).Returns<AssetType>(entity => $"{entity.uid} asset name");
            MockMediator.SetupMediator<ResourceIsExistsRequest, IsEntityExistsResponse>(x =>
            {
                x.Uid.Should().Be(request.ResourceUid);
            }, It.IsAny<IsEntityExistsResponse>(), cancellationToken);
            MockMediator.SetupMediator<ResponsibilityTypeIsExistsRequest, IsEntityExistsResponse>(x =>
            {
                x.Uid.Should().Be(request.ResponsibilityTypeUid);
            }, It.IsAny<IsEntityExistsResponse>(), cancellationToken);

            // act
            var actualResult = await TestedObject.Handle(request, cancellationToken);

            // assert
            MockResponsibilityDapperRepository.Verify(x => x.GetResponsibilityBreakdownByResourceAsync(request.ResourceUid, request.ResponsibilityTypeUid), Times.Once);
            MockMediator.VerifyRequest<ResourceIsExistsRequest, IsEntityExistsResponse>(cancellationToken, Times.Once);
            MockMediator.VerifyRequest<ResponsibilityTypeIsExistsRequest, IsEntityExistsResponse>(cancellationToken, Times.Once);

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