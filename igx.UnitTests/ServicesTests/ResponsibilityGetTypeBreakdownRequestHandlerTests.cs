using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using d360.core.entities;
using d360.model.DataAccessLayer;
using d360.web.Services;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace igx.UnitTests.ServicesTests
{
    public class ResponsibilityGetTypeBreakdownRequestHandlerTests
    {
        private ResponsibilityGetTypeBreakdownRequestHandler TestedObject { get; }
        
        private Mock<IResponsibilityDapperRepository> MockResponsibilityDapperRepository { get; }

        private Mock<IMediator> MockMediator { get; }

        public ResponsibilityGetTypeBreakdownRequestHandlerTests()
        {
            MockMediator = new Mock<IMediator>();
            MockResponsibilityDapperRepository = new Mock<IResponsibilityDapperRepository>();
            TestedObject = new ResponsibilityGetTypeBreakdownRequestHandler(MockMediator.Object, MockResponsibilityDapperRepository.Object);
        }

        [Theory, AutoData]
        public async Task ReturnsProperResultAsync(Guid? typeUid, IReadOnlyList<ResponsibilityBreakdownResponse> repositoryResult)
        {
            // assign
            MockResponsibilityDapperRepository.Setup(x => x.GetResponsibilityTypeBreakdownAsync(typeUid)).ReturnsAsync(repositoryResult);
            var request = new ResponsibilityGetTypeBreakdownRequest();
            request.ResponsibilityTypeUid = typeUid;

            // act
            var actualResult = await TestedObject.Handle(request, CancellationToken.None);

            // assert
            actualResult.Should().NotBeNull();
            actualResult.Data.Should().Equal(repositoryResult);
        }

        [Theory, AutoData]
        public async Task RethrowsExceptionAsync(Guid? typeUid, Exception exception)
        {
            // assign
            MockResponsibilityDapperRepository.Setup(x => x.GetResponsibilityTypeBreakdownAsync(typeUid)).ThrowsAsync(exception);
            var request = new ResponsibilityGetTypeBreakdownRequest();
            request.ResponsibilityTypeUid = typeUid;

            // act
            try
            {
                await TestedObject.Handle(request, CancellationToken.None);
                Assert.True(false, "Exception was not thrown");
            }
            catch (Exception actualException)
            {
                // assert
                actualException.Should().Be(exception);
            }
        }

    }
}
