using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Controllers.V2;
using d360.web.Utilities;
using MediatR;
using Moq;

namespace igx.UnitTests.V2ControllerTests
{
    public partial class ResponsibilitiesControllerTests: BaseTest
    {
        protected ResponsibilitiesControllerTests()
        {
            Controller = new ResponsibilitiesController(
                GetCoreComponentSet(),
                GetApplicationUriProvider(),
                GetAssetRepository(),
                GetMediator(), 
                GetResponsibilityRepository()
            );
        }

        protected ResponsibilitiesController Controller { get; }

        protected Mock<IApplicationUriProvider> MockApplicationUriProvider { get; }

        protected Mock<IMediator> MockMediator { get; }

        protected Mock<ICompanyContext> MockCompanyContext { get; }

        protected Mock<ICommunityContext> MockCommunityContext { get; }

        protected Mock<ISettingsRepository> MockSettingsRepository { get; }

        protected Mock<IAssetRepository> MockAssetRepository { get; }

        protected Mock<IResponsibilityRepository> MockResponsibilityRepository { get; }
    }
}