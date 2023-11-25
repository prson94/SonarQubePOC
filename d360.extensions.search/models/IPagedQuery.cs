using System.Collections.Generic;

namespace d360.extensions.search.models
{
	internal interface IPagedQuery<T>
	{
		List<T> GetByAssetID(long AssetID);
	}
}
