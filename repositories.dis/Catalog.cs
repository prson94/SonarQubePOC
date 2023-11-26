using d360.core.entities;
using repositories.dis.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace repositories.dis
{
	public class Catalog : Repository, ICatalog
	{
		public string BaseUrl { get { return "https://data-catalog-dev.govern.cloud.precisely.services"; } }

		public Task CreateSemanticType()
		{
			throw new NotImplementedException();
		}

		public Task<List<AssetType>> ReadAncestryAsync(Guid assetUid, CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}

		public async Task<AssetPathResults> ReadAssetPaths(int assetTypeId, bool includeTotal = false, int pageNum = 0, int pageSize = 5000)
		{
			var model = new AssetPathResults();
			var payload = await Get_PayloadFromService<PagesListModel<GetAssetModel>>($"{BaseUrl}/assets?filter=assetTypeId:eq(652e0aaa5a7fc6c78691b1f4)");

			model.items = payload.data.Select(a => new AssetPathResult { 
				path = string.Join(" > ", a.path.Select(p => string.Join(" / ", p.segments))),
				uid = Guid.NewGuid()//a.id
			});
			model.total = payload.data.Count;
			return model;
		}

		public Task ReadAssetTypeDefinition()
		{
			throw new NotImplementedException();
		}

		public Task ReadProfiles()
		{
			throw new NotImplementedException();
		}

		public Task ReadRelationTypeDefinition()
		{
			throw new NotImplementedException();
		}

		public Task ReadSemanticTypes()
		{
			throw new NotImplementedException();
		}

		public Task RemoveSemanticType()
		{
			throw new NotImplementedException();
		}

		public Task UpdateSemanticType()
		{
			throw new NotImplementedException();
		}
	}
}
