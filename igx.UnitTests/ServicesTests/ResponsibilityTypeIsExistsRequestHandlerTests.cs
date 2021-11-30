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
    public class ResponsibilityTypeIsExistsRequestHandlerTests
    {
        private ResponsibilityTypeIsExistsRequestHandler TestedObject { get; }

        private Mock<IResponsibilityTypeRepository> MockResponsibilityTypeRepository { get; }

        public ResponsibilityTypeIsExistsRequestHandlerTests()
        {
            MockResponsibilityTypeRepository = new Mock<IResponsibilityTypeRepository>();
            TestedObject = new ResponsibilityTypeIsExistsRequestHandler(MockResponsibilityTypeRepository.Object);
        }

        [Theory, AutoData]
        internal async Task ValidUid_CallsRepository_AndReturnProperResult(Guid uid)
        {
            // assign
            var entity = AutoFixtureHelpers.CreateClassWithRecursiveData<ResponsibilityType>();
            MockResponsibilityTypeRepository.Setup(x => x.GetByUidAsync(uid)).ReturnsAsync(entity);
            var request = new ResponsibilityTypeIsExistsRequest();
            request.Uid = uid;
            request.ThrowNotFoundException = false;

            // act
            var actualResult = await TestedObject.Handle(request, CancellationToken.None);

            // assert
            MockResponsibilityTypeRepository.Verify(x => x.GetByUidAsync(uid), Times.Once);
            actualResult.Should().NotBeNull();
            actualResult.IsExists.Should().Be(true);
        }

        [Theory, AutoData]
        internal async Task InvalidUid_CallsRepository_AndReturnProperResult(Guid uid)
        {
            // assign
            MockResponsibilityTypeRepository.Setup(x => x.GetByUidAsync(uid)).ReturnsAsync((ResponsibilityType)null);
            var request = new ResponsibilityTypeIsExistsRequest();
            request.Uid = uid;
            request.ThrowNotFoundException = false;

            // act
            var actualResult = await TestedObject.Handle(request, CancellationToken.None);

            // assert
            MockResponsibilityTypeRepository.Verify(x => x.GetByUidAsync(uid), Times.Once);
            actualResult.Should().NotBeNull();
            actualResult.IsExists.Should().Be(false);
        }

        [Theory, AutoData]
        internal async Task InvalidUid_CallsRepository_AndThrowException(Guid uid)
        {
            // assign
            MockResponsibilityTypeRepository.Setup(x => x.GetByUidAsync(uid)).ReturnsAsync((ResponsibilityType)null);
            var request = new ResponsibilityTypeIsExistsRequest();
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
                notFoundException.Message.Should().Be($"ResponsibilityType UID=\'{uid}\' does not exist");
            }

            // assert
            MockResponsibilityTypeRepository.Verify(x => x.GetByUidAsync(uid), Times.Once);
        }

        [Theory, AutoData]
        internal async Task NullUid_SkipsRepositoryCall()
        {
            // assign
            var request = new ResponsibilityTypeIsExistsRequest();
            request.Uid = null;

            // act
            var actualResult = await TestedObject.Handle(request, CancellationToken.None);

            // assert
            MockResponsibilityTypeRepository.Verify(x => x.GetByUidAsync(It.IsAny<Guid>()), Times.Never);
            actualResult.Should().NotBeNull();
            actualResult.IsExists.Should().Be(true);
        }

        [Theory, AutoData]
        public async Task RethrowsExceptionAsync(Guid uid, Exception exception)
        {
            // assign
            MockResponsibilityTypeRepository.Setup(x => x.GetByUidAsync(uid)).ThrowsAsync(exception);
            var request = new ResponsibilityTypeIsExistsRequest();
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
            MockResponsibilityTypeRepository.Verify(x => x.GetByUidAsync(uid), Times.Once);
        }

    }
}