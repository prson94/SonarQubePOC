using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Controllers.V2;
using d360.web.Services;
using d360.web.Utilities;
using MediatR;
using Moq;

namespace igx.UnitTests.V2ControllerTests
{
    public partial class ResponsibilitiesControllerTests: BaseTest
    {
	    private readonly Mock<IResourceRepository> mockResourceRepository;
	    private readonly Mock<IAssetService> mockAssetService;

	    protected ResponsibilitiesControllerTests()
        {
            MockResponsibilityRepository = new Mock<IResponsibilityRepository>();
            MockAssetRepository = new Mock<IAssetRepository>();
            MockSettingsRepository = new Mock<ISettingsRepository>();
            MockCommunityContext = new Mock<ICommunityContext>();
            MockCompanyContext = new Mock<ICompanyContext>();
            MockMediator = new Mock<IMediator>();
            MockApplicationUriProvider = new Mock<IApplicationUriProvider>();
            mockResourceRepository = new Mock<IResourceRepository>();
            mockAssetService = new Mock<IAssetService>();

            Controller = new ResponsibilitiesController(
                GetCoreComponentSet(),
                MockAssetRepository.Object,
                MockMediator.Object,
                MockResponsibilityRepository.Object,
                mockResourceRepository.Object,
                mockAssetService.Object
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