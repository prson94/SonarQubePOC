using d360.extensions;
using d360.featureflags;
using d360.model;
using d360.web.Controllers;
using d360.web.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using repositories;
using System.Collections.Generic;

namespace igx.UnitTests.V2ControllerTests
{
	public abstract class CoreComponentSetControllerTestBase : BaseTest
	{
		protected readonly Mock<ICompanyContext> MockCompanyContext;
		protected readonly Mock<ICommunity> MockCommunity;
		protected readonly Mock<ISecurityContextProvider> MockSecurityContext;
		protected readonly Mock<List<ICatalog>> MockCatalogs;
		protected readonly Mock<ILogger> MockLog;
		protected readonly Mock<IMailProvider> MockMailProvider;
		protected readonly Mock<IWorkspaces> MockWorkspace;
		protected readonly Mock<IThemeRepository> MockThemeRepository;
		protected readonly Mock<IRuntimeInfo> RuntimeInfo;
		protected readonly ICoreComponentSet CoreComponentSet;
		protected readonly Mock<IFeatureFlagService> MockFlags;
		protected readonly Mock<ICachingProvider> MockCache;

		protected CoreComponentSetControllerTestBase()
		{
			
			MockCompanyContext = new Mock<ICompanyContext>();
			MockCommunity = new Mock<ICommunity>();
			MockSecurityContext = new Mock<ISecurityContextProvider>();
			MockCatalogs = new Mock<List<ICatalog>>();
			MockCatalogs.Object.AddRange(GetCatalogs());
			MockLog = new Mock<ILogger>();
			MockMailProvider = new Mock<IMailProvider>();
			MockWorkspace = new Mock<IWorkspaces>();
			MockThemeRepository = new Mock<IThemeRepository>();
			RuntimeInfo = new Mock<IRuntimeInfo>();
			MockFlags = new Mock<IFeatureFlagService>();
			MockCache = new Mock<ICachingProvider>();

			CoreComponentSet = new CoreComponentSet(
				MockCache.Object,
				MockCommunity.Object, 
				MockCompanyContext.Object,
				MockSecurityContext.Object,
				MockCatalogs.Object,
				MockLog.Object,
				MockMailProvider.Object, 
				MockThemeRepository.Object,
				MockFlags.Object, 
				RuntimeInfo.Object, 
				MockWorkspace.Object);
		}
	}
}