using d360.extensions;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Controllers;
using d360.web.Utilities;
using LaunchDarkly.Sdk.Server;
using Moq;

namespace igx.UnitTests.V2ControllerTests
{
	public abstract class CoreComponentSetControllerTestBase : BaseTest
	{
		protected readonly Mock<ICompanyContext> MockCompanyContext;
		protected readonly Mock<ICommunityContext> MockCommunityContext;
		protected readonly Mock<IMailProvider> MockMailProvider;
		protected readonly Mock<ISettingsRepository> MockSettingsRepository;
		protected readonly Mock<IThemeRepository> MockThemeRepository;
		protected readonly Mock<IRuntimeInfo> RuntimeInfo;
		protected readonly ICoreComponentSet CoreComponentSet;

		protected CoreComponentSetControllerTestBase()
		{
			MockCompanyContext = new Mock<ICompanyContext>();
			MockCommunityContext = new Mock<ICommunityContext>();
			MockMailProvider = new Mock<IMailProvider>();
			MockSettingsRepository = new Mock<ISettingsRepository>();
			MockThemeRepository = new Mock<IThemeRepository>();
			RuntimeInfo = new Mock<IRuntimeInfo>();
			var ldClient = new LdClient("");

			CoreComponentSet = new CoreComponentSet(MockCommunityContext.Object, MockCompanyContext.Object, MockMailProvider.Object, MockSettingsRepository.Object
				, MockThemeRepository.Object, ldClient, RuntimeInfo.Object);
		}
	}
}