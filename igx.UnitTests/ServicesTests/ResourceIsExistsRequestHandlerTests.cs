using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using d360.core.entities;
using d360.model.DataAccessLayer;
using d360.web.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace igx.UnitTests.ServicesTests
{
    public class ResourceIsExistsRequestHandlerTests
    {
        private ResourceIsExistsRequestHandler TestedObject { get; }

        private Mock<IResourceRepository> MockResourceRepository { get; }

        public ResourceIsExistsRequestHandlerTests()
        {
            MockResourceRepository = new Mock<IResourceRepository>();
            TestedObject = new ResourceIsExistsRequestHandler(MockResourceRepository.Object);
        }

        [Theory, AutoData]
        internal async Task ValidUid_CallsRepository_AndReturnProperResult(Guid uid, GlobalReportingResource entity)
        {
            // assign
            MockResourceRepository.Setup(x => x.GetByUidAsync(uid)).ReturnsAsync(entity);
            var request = new ResourceIsExistsRequest();
            request.Uid = uid;
            request.ThrowNotFoundException = false;

            // act
            var actualResult = await TestedObject.Handle(request, CancellationToken.None);

            // assert
            MockResourceRepository.Verify(x => x.GetByUidAsync(uid), Times.Once);
            actualResult.Should().NotBeNull();
            actualResult.IsExists.Should().Be(true);
        }

        [Theory, AutoData]
        internal async Task InvalidUid_CallsRepository_AndReturnProperResult(Guid uid)
        {
            // assign
            MockResourceRepository.Setup(x => x.GetByUidAsync(uid)).ReturnsAsync((GlobalReportingResource)null);
            var request = new ResourceIsExistsRequest();
            request.Uid = uid;
            request.ThrowNotFoundException = false;

            // act
            var actualResult = await TestedObject.Handle(request, CancellationToken.None);

            // assert
            MockResourceRepository.Verify(x => x.GetByUidAsync(uid), Times.Once);
            actualResult.Should().NotBeNull();
            actualResult.IsExists.Should().Be(false);
        }

        [Theory, AutoData]
        internal async Task InvalidUid_CallsRepository_AndThrowException(Guid uid)
        {
            // assign
            MockResourceRepository.Setup(x => x.GetByUidAsync(uid)).ReturnsAsync((GlobalReportingResource)null);
            var request = new ResourceIsExistsRequest();
            request.Uid = uid;
            request.ThrowNotFoundException = true;

            // act
            try
            {
                var actualResult = await TestedObject.Handle(request, CancellationToken.None);
                Assert.True(false, "Exception was not thrown");
            }
            catch (Exception actualException)
            {
                // assert
                var notFoundException = actualException.Should().BeOfType<NotFoundBusinessLayerException>().Subject;
                notFoundException.Message.Should().Be($"Resource UID=\'{uid}\' does not exist");
            }

            // assert
            MockResourceRepository.Verify(x => x.GetByUidAsync(uid), Times.Once);
        }

        [Theory, AutoData]
        internal async Task NullUid_SkipsRepositoryCall()
        {
            // assign
            var request = new ResourceIsExistsRequest();
            request.Uid = null;

            // act
            var actualResult = await TestedObject.Handle(request, CancellationToken.None);

            // assert
            MockResourceRepository.Verify(x => x.GetByUidAsync(It.IsAny<Guid>()), Times.Never);
            actualResult.Should().NotBeNull();
            actualResult.IsExists.Should().Be(true);
        }

        [Theory, AutoData]
        public async Task RethrowsExceptionAsync(Guid uid, Exception exception)
        {
            // assign
            MockResourceRepository.Setup(x => x.GetByUidAsync(uid)).ThrowsAsync(exception);
            var request = new ResourceIsExistsRequest();
            request.Uid = uid;
            request.ThrowNotFoundException = true;

            // act
            try
            {
                var actualResult = await TestedObject.Handle(request, CancellationToken.None);
                Assert.True(false, "Exception was not thrown");
            }
            catch (Exception actualException)
            {
                // assert
                actualException.Should().Be(exception);
                
            }

            // assert
            MockResourceRepository.Verify(x => x.GetByUidAsync(uid), Times.Once);
        }

    }
}