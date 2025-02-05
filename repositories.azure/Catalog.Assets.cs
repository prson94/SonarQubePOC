using d360.core.entities;
using Dapper;
using System.Linq;
using System.Threading.Tasks;

namespace repositories.azure
{
	public partial class Catalog
	{
		public async Task<AssetDetail> ReadAssetDetail(long id)
		{
			var dbArgs = new DynamicParameters();
			dbArgs.Add("@id", id);

			var sql = @"
select	ID,
		DisplayValue,
		AssetTypeID,
		State,
		Object,
		ObjectID,
		TypeName,
		Type,
		TypeID,
		uid
from	AssetDetail
where   ID = @id";

			AssetDetail model = null;
			using (var connection = ConnectionProvider.Connect())
			{
				model = (
					await connection.QueryAsync<AssetDetail>(sql, dbArgs)
					).SingleOrDefault();
			}
			return model;
		}

		public async Task<AssetDetail> ReadAssetDetail(string @object, int objectId)
		{
			var dbArgs = new DynamicParameters();
			dbArgs.Add("@o", @object);
			dbArgs.Add("@oid", objectId);

			var sql = @"
select	ID,
		DisplayValue,
		AssetTypeID,
		State,
		Object,
		ObjectID,
		TypeName as AssetTypeName,
		Type,
		TypeID,
		uid,
		assetTypeUid
from	AssetDetail
where   [ObjectID] = @oid and [Object] = @o";

			AssetDetail model = null;
			using (var connection = ConnectionProvider.Connect())
			{
				model = (
					await connection.QueryAsync<AssetDetail>(sql, dbArgs)
					).SingleOrDefault();
			}
			return model;
		}

		public async Task<AssetPathResults> ReadAssetPaths(int assetTypeId, bool includeTotal = false, int pageNum = 0, int pageSize = 5000)
		{
			var dbArgs = new DynamicParameters();

			dbArgs.Add("@assetTypeId", assetTypeId);
			dbArgs.Add("@pageNum", pageNum);
			dbArgs.Add("@pageSize", pageSize);
			dbArgs.Add("@offset", pageSize * (pageNum - 1));

			var sql = $@"

				DROP TABLE IF EXISTS #tempassetPath;
				create table #tempassetPath (id int identity(1,1), AssetId bigint);
				create index ix_tempassetPath on #tempassetPath	(AssetId);

				insert into #tempassetPath
				select a.ID
				from Asset A
				where A.assetTypeId = @assetTypeId
				order by A.ID
				OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY
				option (recompile);

				select	A.[uid],
						AP.[keypath] as [path]
				from #tempassetPath TempPA
				inner join Asset A on A.ID = TempPA.AssetId
				inner join AssetPath AP on a.ID=ap.ID
				order by TempPA.ID
				option (recompile);";

			var model = new AssetPathResults();

			using (var connection = ConnectionProvider.Connect())
			{
				var countSql = "select count(1) from Asset where AssetTypeId = @assetTypeId";
				if (includeTotal)
				{
					model.total = await connection.QueryFirstAsync<int>(countSql, dbArgs);
				}
				model.items = await connection.QueryAsync<AssetPathResult>(sql, dbArgs);
			}

			return model;
		}

	}
}
