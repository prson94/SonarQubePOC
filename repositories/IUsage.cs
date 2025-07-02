using d360.core.entities;
using d360.core.entities.Usage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
	public interface IUsage
	{
		int CompanyId { get; set; }
		string WorkspaceId { get; set; }
		Task<RepositoryResponse<PagedApiBaseViewModel<dynamic>>> ReadUsageDetailAsync(IEnumerable<KeyValuePair<string, string>> queryParams);

		Task<RepositoryResponse<bool>> CreateUsageAsync(UsageEntry value, string ipAddress);

	}
}
