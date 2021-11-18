using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http.Results;
using AutoFixture.Xunit2;
using d360.core.entities;
using d360.web.Services;
using FluentAssertions;
using igx.UnitTests.ServicesTests;
using Moq;
using Xunit;

namespace igx.UnitTests.V2ControllerTests
{
    public partial class ResponsibilitiesControllerTests
    {
        public class GetResponsibilityTypeBreakdown : ResponsibilitiesControllerTests
        {
            [Theory, AutoData]
            public async Task SmokeTestAsync(
                Guid? typeUid
            )
            {
                // assign
                var businessLayerResponse = AutoFixtureHelpers.CreateClassWithRecursiveData<ResponsibilityGetTypeBreakdownResponse>();

                MockMediator.Setup(x => x.Send(It.Is<ResponsibilityGetTypeBreakdownRequest>(r => r.TypeUid == typeUid), default)).ReturnsAsync(businessLayerResponse);

                // act
                var actualResponse = await Controller.GetResponsibilityTypeBreakdown(typeUid);

                // assert
                var okResult = actualResponse.Should().BeOfType<OkNegotiatedContentResult<IReadOnlyList<ResponsibilityBreakdownResponse>>>().Subject;
                okResult.Content.Should().Equal(businessLayerResponse.Data);
            }
        }
    }
}