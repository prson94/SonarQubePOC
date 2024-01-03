using d360.core.entities;
using Dapper;
using DocumentFormat.OpenXml.EMMA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace repositories.azure
{
	public class Catalog : Repository, ICatalog
	{
		public Catalog(DapperConnectionProvider provider): base(provider) { }

		public Task CreateSemanticType()
		{
			throw new NotImplementedException();
		}

		public async Task<List<AssetType>> ReadAncestryAsync(Guid assetUid, CancellationToken cancellationToken = default)
		{
			const string sql = @"
WITH cte AS (  
	select	*, 
			0 as lvl
	from	AssetType
	where	[uid] = @assetUid
	union all
	select	a.*,
			cte.lvl - 1 
	from	IntersectType it
            inner join [Predicate] p on it.PredicateID = p.ID and p.Type IN (3) 
			inner join cte on cte.ID = it.ObjectAssetTypeID 
            inner join AssetType a on a.ID = it.SubjectAssetTypeID
)
select		*
from		cte
order by	lvl";

			IEnumerable<AssetType> results;
			using (var connection = ConnectionProvider.Connect())
			{
				results = await connection.QueryAsync<AssetType>(sql, new { assetUid });
			}
			return results.ToList();
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

		public async Task<IEnumerable<AssetTypeApiViewModel>> ReadAssetTypes(int pageNum = 0, int pageSize = 5000)
		{
			var dbArgs = new DynamicParameters();

			var sql = $@"
SELECT     A.[Name]
			,ISNULL(A.[Description],'') as Description
			,A.[Class] as ClassID
			,ISNULL(A.[Notes],'') as Notes
			,A.SourceID
			,A.[uid]
			,A.HierarchyMaximumDepth
			,A.DisplayFormat
			,A.UseAsTransformation
			,0 as 'CanOwnFusion'
			,A.AutoDisplayParent
			,A.FlowObjectType
			,A.CanEditParent
			,A.IsDescriptionEnabled
			,A.IsDescriptionVisibleByDefault
			,A.DescriptionButtonName
			,cast(iif(A.DefaultPermissions = 1, 1, 0) as bit) as IsDefaultReadAccessEnabled
			,P.[Path]
			,AT.IconBackColor as BackColor
			,AT.Icon as Icon
			,AT.IconForeColor as ForeColor
FROM        AssetType A
			cross apply dbo.GetAssetTypeTextPathById(A.ID, ' / ') P
			left join [dbo].[AssetTypeStyle] AT on (A.ID = AT.ID)
where       A.[State] = 1
order by    P.[Path];";

			IEnumerable<AssetTypeApiViewModel> model;

			using (var connection = ConnectionProvider.Connect())
			{
				model = await connection.QueryAsync<AssetTypeApiViewModel>(sql, dbArgs);
			}

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
