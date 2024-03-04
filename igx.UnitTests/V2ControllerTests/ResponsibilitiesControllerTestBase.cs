using d360.web.Controllers.V2;
using igx.UnitTests.Core;
using Moq;
using repositories;
using System;
using System.Net.Http;
using System.Web.Http;

namespace igx.UnitTests.V2ControllerTests
{
	public abstract class ResponsibilitiesControllerTestBase : CoreComponentSetControllerTestBase
	{
		protected ResponsibilitiesController ResponsibilitiesController;
		protected readonly Mock<IResponsibilityRepository> MockResponsibilityRepository;
		protected readonly Mock<IResourceRepository> MockResourceRepository;
		protected readonly Mock<IAssetRepository> MockAssetRepository;

		protected ResponsibilitiesControllerTestBase()
		{

			MockAssetRepository = new Mock<IAssetRepository>();
			MockResourceRepository = new Mock<IResourceRepository>();
			MockResponsibilityRepository = new Mock<IResponsibilityRepository>();

			ResponsibilitiesController = new ResponsibilitiesController(CoreComponentSet, MockAssetRepository.Object, MockResponsibilityRepository.Object, MockResourceRepository.Object)
			{
				Request = new HttpRequestMessage() { RequestUri = new Uri(DataConstants.UrlString) },
				Configuration = new HttpConfiguration()
			};
		}
	}
}