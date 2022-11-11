using System.Collections.Generic;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer.repositories
{
	internal sealed class NavigationRepository : DapperRepositoryBase<ICompanyDbConnectionProvider>, INavigationRepository
	{
		public NavigationRepository(IDapperQueryComposer<ICompanyDbConnectionProvider> queryComposer) : base(queryComposer)
		{
		}

		public async Task<IReadOnlyList<AdminConfigurationItem>> GetAdminConfigurationItems()
		{
			return await QueryComposer.QueryListAsync<AdminConfigurationItem>(@"
				with cte_assetType as
				(
					select
						assetType.Class,
						assetType.Name,
						assetType.uid as Uid,
						assetType.ID,
						cast(null as uniqueidentifier) as ParentUid
					from dbo.AssetType assetType
					where not exists  (
						select	IT.SubjectAssetTypeID
						from	IntersectType IT
								inner join [Predicate] P on IT.ObjectAssetTypeID = assetType.ID and P.ID = IT.PredicateID and P.Type = 3
					)

					union all
	
					select
						child.Class,
						child.Name,
						child.uid as Uid,
						child.ID,
						parent.uid as ParentUid
					from cte_assetType parent
						join dbo.IntersectType IT on IT.SubjectAssetTypeID = parent.ID
						join dbo.[Predicate] p on P.ID = IT.PredicateID and P.Type = 3
						join dbo.AssetType child on child.Id = IT.ObjectAssetTypeID
				)
				select 
					Class,
					Name,
					Uid,
					ParentUid
				from cte_assetType
				where Class in (
					1, -- Business Assets
					2, -- Models
					6, -- Policies
					7, -- Rules,
					8, -- Technical Asset,
					15 -- Diagram Asset
				)
			");
		}
	}
}
