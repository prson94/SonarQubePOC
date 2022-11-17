using d360.model.DataAccessLayer;
using d360.web.Filters;
using Microsoft.Web.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers.V2
{
	[
		ApiVersion("2.0"),
		RoutePrefix("api/v{version:apiVersion}/navigation"),
		Authorize,
		ApiExplorerSettings(IgnoreApi = true)
	]
	public class NavigationController : BaseV2ApiController
	{
		private readonly INavigationRepository NavigationRepository;

		public NavigationController(ICoreComponentSet set, INavigationRepository navigationRepository) : base(set)
		{
			NavigationRepository = navigationRepository;
		}

		[HttpGet]
		[Route("adminConfiguration")]
		[RequireAdminPermissions]
		public async Task<IReadOnlyList<AdminConfigurationItem>> GetAdminConfigurationItemsAsync()
		{
			return await this.NavigationRepository.GetAdminConfigurationItems();
		}
	}
}
