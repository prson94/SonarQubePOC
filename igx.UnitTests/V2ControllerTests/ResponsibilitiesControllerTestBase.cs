using System;
using System.Net.Http;
using System.Web.Http;
using d360.model.DataAccessLayer;
using d360.web.Controllers.V2;
using d360.web.Services;
using igx.UnitTests.Core;
using Moq;

namespace igx.UnitTests.V2ControllerTests
{
	public abstract class ResponsibilitiesControllerTestBase : CoreComponentSetControllerTestBase
	{
		protected ResponsibilitiesController ResponsibilitiesController;

		protected readonly Mock<IResponsibilityRepository> MockResponsibilityRepository;
		protected readonly Mock<IResourceRepository> MockResourceRepository;
		protected readonly Mock<IAssetService> MockAssetService;
		protected readonly Mock<IAssetRepository> MockAssetRepository;


		protected ResponsibilitiesControllerTestBase()
		{
			MockResponsibilityRepository = GetResponsibilityRepositoryMock();
			MockResourceRepository = new Mock<IResourceRepository>();
			MockAssetService = new Mock<IAssetService>();
			MockAssetRepository = new Mock<IAssetRepository>();

			ResponsibilitiesController = new ResponsibilitiesController(CoreComponentSet, MockAssetRepository.Object, MockResponsibilityRepository.Object, MockResourceRepository.Object, MockAssetService.Object)
			{
				Request = new HttpRequestMessage() { RequestUri = new Uri(DataConstants.UrlString) },
				Configuration = new HttpConfiguration()
			};
		}
	}
}