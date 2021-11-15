
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.Xunit2;
using d360.core.entities;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Services;
using FluentAssertions;
using Moq;
using Resources;
using Xunit;

namespace igx.UnitTests.ServicesTests
{
    public class ResponsibilityDeleteRulesRequestHandlerTests
    {
        [Theory, AutoData]
        public async Task ReturnsProperResultAsync(
            Mock<IResponsibilityRepository> mockResponsibilityRepository,
            Mock<ICompanyContext> mockCompanyContext,
            Guid typeUid,
            IReadOnlyList<Guid> ruleDeleteUidCollection,
            IReadOnlyList<ResponsibilityRuleDeleteResponse> responsibilityRuleDeleteResponseList)
        {
            ResponsibilityType responsibilityType = AutoFixtureHelpers.CreateClassWithRecursiveData<ResponsibilityType>();

            // assign
            mockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid)).Returns(responsibilityType);
            mockResponsibilityRepository.Setup(x => x.DeleteResponsibilityRulesAsync(typeUid, ruleDeleteUidCollection)).ReturnsAsync(responsibilityRuleDeleteResponseList);
            mockCompanyContext.Setup(x => x.CurrentResourceIsAdmin).Returns(true);

            var request = new ResponsibilityDeleteRulesRequest();
            request.TypeUid = typeUid;
            request.RuleDeleteUidCollection = ruleDeleteUidCollection;

            var testedClass = new ResponsibilityDeleteRulesRequestHandler(mockResponsibilityRepository.Object, mockCompanyContext.Object);

            // act
            var actualResult = await testedClass.Handle(request, CancellationToken.None);

            // assert
            actualResult.Should().NotBeNull();
            actualResult.Data.Should().Equal(responsibilityRuleDeleteResponseList);
        }

        [Theory, AutoData]
        public async Task Throws_UnauthorizedException_Async(
            Mock<IResponsibilityRepository> mockResponsibilityRepository,
            Mock<ICompanyContext> mockCompanyContext,
            Guid typeUid,
            IReadOnlyList<Guid> ruleDeleteUidCollection,
            IReadOnlyList<ResponsibilityRuleDeleteResponse> responsibilityRuleDeleteResponseList)
        {
            // assign
            ResponsibilityType responsibilityType = AutoFixtureHelpers.CreateClassWithRecursiveData<ResponsibilityType>();

            mockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid)).Returns(responsibilityType);
            mockResponsibilityRepository.Setup(x => x.DeleteResponsibilityRulesAsync(typeUid, ruleDeleteUidCollection)).ReturnsAsync(responsibilityRuleDeleteResponseList);
            mockCompanyContext.Setup(x => x.CurrentResourceIsAdmin).Returns(false);

            var request = new ResponsibilityDeleteRulesRequest();
            request.TypeUid = typeUid;
            request.RuleDeleteUidCollection = ruleDeleteUidCollection;

            var testedClass = new ResponsibilityDeleteRulesRequestHandler(mockResponsibilityRepository.Object, mockCompanyContext.Object);

            // act
            try
            {
                var actualResult = await testedClass.Handle(request, CancellationToken.None);
                Assert.True(false, "Exception is not thrown");
            }
            catch (Exception actualException)
            {
                // assert
                var unauthorizedBusinessLayerException = actualException.Should().BeOfType<ForbiddenBusinessLayerException>().Subject;
                unauthorizedBusinessLayerException.Message.Should().Be(ApiMessages.ForbiddenUserNotAuthorizedMessage);
            }
        }

        [Theory, AutoData]
        public async Task Throws_ResponsibilityTypeNotFound_Async(
            Mock<IResponsibilityRepository> mockResponsibilityRepository,
            Mock<ICompanyContext> mockCompanyContext,
            Guid typeUid,
            IReadOnlyList<Guid> ruleDeleteUidCollection,
            IReadOnlyList<ResponsibilityRuleDeleteResponse> responsibilityRuleDeleteResponseList)
        {
            // assign
            ResponsibilityType responsibilityType = null;

            mockResponsibilityRepository.Setup(x => x.GetResponsibilityTypeByUID(typeUid)).Returns(responsibilityType);
            mockResponsibilityRepository.Setup(x => x.DeleteResponsibilityRulesAsync(typeUid, ruleDeleteUidCollection)).ReturnsAsync(responsibilityRuleDeleteResponseList);
            mockCompanyContext.Setup(x => x.CurrentResourceIsAdmin).Returns(true);

            var request = new ResponsibilityDeleteRulesRequest();
            request.TypeUid = typeUid;
            request.RuleDeleteUidCollection = ruleDeleteUidCollection;

            var testedClass = new ResponsibilityDeleteRulesRequestHandler(mockResponsibilityRepository.Object, mockCompanyContext.Object);

            // act
            try
            {
                var actualResult = await testedClass.Handle(request, CancellationToken.None);
                Assert.True(false, "Exception is not thrown");
            }
            catch (Exception actualException)
            {
                // assert
                var unauthorizedBusinessLayerException = actualException.Should().BeOfType<NotFoundBusinessLayerException>().Subject;
                unauthorizedBusinessLayerException.Message.Should().Be(ResponsibilityApiMessages.InvalidResponsibilityUid);
            }
        }
    }

    public static class AutoFixtureHelpers
    {
        public static T CreateClassWithRecursiveData<T>()
        {
            var fixture = new Fixture();
            fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => fixture.Behaviors.Remove(b));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            return fixture.Create<T>();
        }
    }
}