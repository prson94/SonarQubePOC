using d360.core.entities;
using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
	public interface ISearch
	{
		Task<RepositoryResponse<SearchModel>> ReadResultsAsync(
			string phrase,
			bool includeFields, bool includePath, bool includeScore, bool aggregationOnly,
			List<AssetTypeClass> limitedToClasses = null, List<string> limitedToTypes = null,
			int offset = 0, int take = 250);
	}
}
