using System;
using System.Collections.Generic;
using System.Linq;
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
        public class DeleteResponsibilityRules: ResponsibilitiesControllerTests
        {
            [Theory, AutoData]
            public async Task SmokeTestAsync(
                Guid typeUid,
                IReadOnlyList<ResponsibilityRuleDeleteModel> responsibilityRulesDeletes
            )
            {
                // assign
                var businessLayerResponse = AutoFixtureHelpers.CreateClassWithRecursiveData<ResponsibilityDeleteRulesResponse>();
                var expectedUids = responsibilityRulesDeletes.Select(x => x.Uid).ToList();

                MockMediator.SetupMediator<ResponsibilityDeleteRulesRequest, ResponsibilityDeleteRulesResponse>(
                    businessLayerRequest =>
                    {
                        businessLayerRequest.TypeUid.Should().Be(typeUid);
                        businessLayerRequest.RuleDeleteUidCollection.Should().BeEquivalentTo(expectedUids);
                    },
                    businessLayerResponse
                );

                // act
                var actualResponse = await Controller.DeleteResponsibilityRules(typeUid, responsibilityRulesDeletes);

                // assert
                var okResult = actualResponse.Should().BeOfType<OkNegotiatedContentResult<IReadOnlyList<ResponsibilityRuleDeleteResponse>>>().Subject;
                okResult.Content.Should().Equal(businessLayerResponse.Data);
                MockMediator.Verify(x => x.Send(It.IsAny<ResponsibilityDeleteRulesRequest>(), default), Times.Once);
            }
        }
    }
}