using d360.extensions;
using d360.model;
using d360.web.Controllers;
using d360.web.Models.Theme;
using d360.web.Services;
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
		protected readonly CommunityFeatureFlagService CommunityFlags;
		protected readonly Mock<ISecurityContextProvider> MockSecurityContext;
		protected readonly Mock<List<ICatalog>> MockCatalogs;
		protected readonly Mock<ILogger> MockLog;
		protected readonly Mock<IMailProvider> MockMailProvider;
		protected readonly Mock<IWorkspaces> MockWorkspace;
		protected readonly Mock<IThemeManager> MockThemeRepository;
		protected readonly Mock<IRuntimeInfo> RuntimeInfo;
		protected readonly ICoreComponentSet CoreComponentSet;
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
			MockThemeRepository = new Mock<IThemeManager>();
			RuntimeInfo = new Mock<IRuntimeInfo>();
			MockCache = new Mock<ICachingProvider>();
			CommunityFlags = new CommunityFeatureFlagService(MockCache.Object, MockCommunity.Object, MockSecurityContext.Object);

			CoreComponentSet = new CoreComponentSet(
				MockCache.Object,
				MockCommunity.Object,
				CommunityFlags,
				MockCompanyContext.Object,
				MockSecurityContext.Object,
				MockCatalogs.Object,
				MockLog.Object,
				MockMailProvider.Object, 
				MockThemeRepository.Object,
				RuntimeInfo.Object, 
				MockWorkspace.Object);
		}
	}
}