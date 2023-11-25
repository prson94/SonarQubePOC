using d360.extensions;
using d360.featureflags;
using d360.model;
using d360.web.Controllers;
using d360.web.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using repositories;

namespace igx.UnitTests.V2ControllerTests
{
	public abstract class CoreComponentSetControllerTestBase : BaseTest
	{
		protected readonly Mock<ICompanyContext> MockCompanyContext;
		protected readonly Mock<ICommunityContext> MockCommunityContext;
		protected readonly Mock<ILogger> MockLog;
		protected readonly Mock<IMailProvider> MockMailProvider;
		protected readonly Mock<ISettingsRepository> MockSettingsRepository;
		protected readonly Mock<IThemeRepository> MockThemeRepository;
		protected readonly Mock<IRuntimeInfo> RuntimeInfo;
		protected readonly ICoreComponentSet CoreComponentSet;
		protected readonly Mock<IFeatureFlagService> MockFlags;

		protected CoreComponentSetControllerTestBase()
		{
			
			MockCompanyContext = new Mock<ICompanyContext>();
			MockCommunityContext = new Mock<ICommunityContext>();
			MockLog = new Mock<ILogger>();
			MockMailProvider = new Mock<IMailProvider>();
			MockSettingsRepository = new Mock<ISettingsRepository>();
			MockThemeRepository = new Mock<IThemeRepository>();
			RuntimeInfo = new Mock<IRuntimeInfo>();
			MockFlags = new Mock<IFeatureFlagService>();

			CoreComponentSet = new CoreComponentSet(
				MockCommunityContext.Object, 
				MockCompanyContext.Object,
				MockLog.Object,
				MockMailProvider.Object, 
				MockSettingsRepository.Object, 
				MockThemeRepository.Object,
				MockFlags.Object, 
				RuntimeInfo.Object);
		}
	}
}