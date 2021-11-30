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
        public class GetResponsibilityBreakdownByResource : ResponsibilitiesControllerTests
        {
            [Theory, AutoData]
            public async Task Valid(Guid resourceUid, Guid? resourceTypeUid, ResponsibilityGetBreakdownByResourceResponse businessLayerResponse)
            {
                // assign
                //var businessLayerResponse = AutoFixtureHelpers.CreateClassWithRecursiveData<ResponsibilityGetBreakdownByResourceResponse>();

                MockMediator.SetupMediator<ResponsibilityGetBreakdownByResourceRequest, ResponsibilityGetBreakdownByResourceResponse>(businessLayerRequest =>
                {
                    businessLayerRequest.ResourceUid.Should().Be(resourceUid);
                    businessLayerRequest.ResponsibilityTypeUid.Should().Be(resourceTypeUid);
                }, businessLayerResponse);

                // act
                var actualResponse = await Controller.GetResponsibilityBreakdownByResource(resourceUid, resourceTypeUid);

                // assert
                var okResult = actualResponse.Should().BeOfType<OkNegotiatedContentResult<IReadOnlyList<ResponsibilityGetBreakdownByResourceModel>>>().Subject;
                okResult.Content.Should().BeEquivalentTo(businessLayerResponse.ItemCollection);
            }
        }
    }
}