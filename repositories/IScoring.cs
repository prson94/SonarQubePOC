using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
	public interface IScoring
	{
		Task<RepositoryResponse<List<Guid>>> ReadAssetUidsAssociatedToPolicyAsync(Guid uid);
		Task<RepositoryResponse<List<Guid>>> ReadAssetUidsAssociatedToRoleAsync(Guid uid);
	}
}
