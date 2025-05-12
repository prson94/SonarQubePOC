using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
	public interface IScoring
	{
		Platform Platform { get; }

		Task<RepositoryResponse<List<Guid>>> ReadAssetUidsAssociatedToPolicyAsync(Guid uid);
		Task<RepositoryResponse<List<Guid>>> ReadAssetUidsAssociatedToRoleAsync(Guid uid);
	}
}
