using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http.Results;
using AutoFixture.Xunit2;
using d360.core.entities;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Controllers.V2;
using d360.web.Services;
using FluentAssertions;
using igx.UnitTests.ServicesTests;
using MediatR;
using Moq;
using Xunit;

namespace igx.UnitTests.V2ControllerTests
{
    public class ResponsibilitiesController_GetResponsibilityTypeBreakdown_Tests
    {
        [Theory, AutoData]
        public async Task SmokeTestAsync(
            Guid? typeUid
        )
        {
            // assign
            var mockResponsibilityRepository = new Mock<IResponsibilityRepository>();
            var mockAssetRepository = new Mock<IAssetRepository>();
            var mockSettingsRepository = new Mock<ISettingsRepository>();
            var mockCommunityContext = new Mock<ICommunityContext>();
            var mockCompanyContext = new Mock<ICompanyContext>();
            var mockMediator = new Mock<IMediator>();

            var businessLayerResponse = AutoFixtureHelpers.CreateClassWithRecursiveData<ResponsibilityGetTypeBreakdownResponse>();

            mockMediator.Setup(x => x.Send(It.Is<ResponsibilityGetTypeBreakdownRequest>(r => r.TypeUid == typeUid), default)).ReturnsAsync(businessLayerResponse);
            var controller = new ResponsibilitiesController(mockCommunityContext.Object, mockCompanyContext.Object, mockResponsibilityRepository.Object, mockAssetRepository.Object,
                mockSettingsRepository.Object, mockMediator.Object);

            // act
            var actualResponse = await controller.GetResponsibilityTypeBreakdown(typeUid);

            // assert
            var okResult = actualResponse.Should().BeOfType<OkNegotiatedContentResult<IReadOnlyList<ResponsibilityBreakdownResponse>>>().Subject;
            okResult.Content.Should().Equal(businessLayerResponse.Data);
        }
    }
}