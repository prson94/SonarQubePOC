using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using d360.core.entities;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace igx.UnitTests.ServicesTests
{
    public class ResponsibilityGetTypeBreakdownRequestHandlerTests
    {
        [Theory, AutoData]
        public async Task ReturnsProperResultAsync(Mock<IResponsibilityDapperRepository> mockResponsibilityDapperRepository, Guid? typeUid, IReadOnlyList<ResponsibilityBreakdownResponse> repositoryResult)
        {
            // assign
            mockResponsibilityDapperRepository.Setup(x => x.GetResponsibilityTypeBreakdownAsync(typeUid)).ReturnsAsync(repositoryResult);
            var request = new ResponsibilityGetTypeBreakdownRequest();
            request.ResourceTypeUid = typeUid;
            var testedClass = new ResponsibilityGetTypeBreakdownRequestHandler(mockResponsibilityDapperRepository.Object);

            // act
            var actualResult = await testedClass.Handle(request, CancellationToken.None);

            // assert
            actualResult.Should().NotBeNull();
            actualResult.Data.Should().Equal(repositoryResult);
        }

        [Theory, AutoData]
        public async Task RethrowsExceptionAsync(Mock<IResponsibilityDapperRepository> mockResponsibilityDapperRepository, Guid? typeUid, Exception exception)
        {
            // assign
            mockResponsibilityDapperRepository.Setup(x => x.GetResponsibilityTypeBreakdownAsync(typeUid)).ThrowsAsync(exception);
            var request = new ResponsibilityGetTypeBreakdownRequest();
            request.ResourceTypeUid = typeUid;
            var testedClass = new ResponsibilityGetTypeBreakdownRequestHandler(mockResponsibilityDapperRepository.Object);

            // act
            try
            {
                var actualResult = await testedClass.Handle(request, CancellationToken.None);
                Assert.True(false, "Exception is not thrown");
            }
            catch (Exception actualException)
            {
                // assert
                actualException.Should().Be(exception);
            }
        }

    }
}
