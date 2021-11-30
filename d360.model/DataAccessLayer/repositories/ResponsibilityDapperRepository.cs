using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using d360.core.entities;
using Dapper;

namespace d360.model.DataAccessLayer.repositories
{
    internal sealed class ResponsibilityDapperRepository : DapperRepositoryBase<ICompanyDbConnectionProvider>, IResponsibilityDapperRepository
    {
        public ResponsibilityDapperRepository(IDapperQueryComposer<ICompanyDbConnectionProvider> queryComposer) : base(queryComposer)
        {
            
        }

        public async Task<IReadOnlyList<ResponsibilityBreakdownResponse>> GetResponsibilityTypeBreakdownAsync(Guid? responsibilityTypeUid)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@responsibilityTypeUid", responsibilityTypeUid, DbType.Guid);

            return await QueryComposer.StoredProcedureMultipleAsync<ResponsibilityBreakdownResponse>("[dbo].[GetResponsibilityTypeBreakdown]", parameters);
        }

        public async Task<IReadOnlyList<ResponsibilityBreakdownByResourceAggregate>> GetResponsibilityBreakdownByResourceAsync(Guid resourceUid, Guid? responsibilityTypeUid)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@resourceUid", resourceUid, DbType.Guid);
            parameters.Add("@responsibilityTypeUid", responsibilityTypeUid, DbType.Guid);

            var grid = await QueryComposer.StoredProcedureMultipleResultsAsync("[dbo].[GetResponsibilityBreakdownByResource]", parameters).ConfigureAwait(false);

            var result = await grid.ReadListAsync<ResponsibilityBreakdownByResourceAggregate>().ConfigureAwait(false);
            var assetTypes = await grid.ReadListAsync<AssetType>().ConfigureAwait(false);
            var responsibilityTypes = await grid.ReadListAsync<ResponsibilityType>().ConfigureAwait(false);
            foreach (var aggregate in result)
            {
                aggregate.AssetType = assetTypes.First(x => x.uid == aggregate.AssetTypeUid);
                aggregate.ResponsibilityType = responsibilityTypes.First(x => x.UID == aggregate.ResponsibilityTypeUid);
            }

            return result;
        }
    }

    public class ResponsibilityBreakdownByResourceAggregate
    {
        public Guid AssetTypeUid { get; set; }

        public Guid ResponsibilityTypeUid { get; set; }

        public int AssetCount { get; set; }

        // nested entities 

        public AssetType AssetType { get; set; }

        public ResponsibilityType ResponsibilityType { get; set; }
    }

    internal static class GridReaderExtensions
    {
        public static async Task<IReadOnlyList<T>> ReadListAsync<T>(this SqlMapper.GridReader gridReader)
        {
            var enumerable = await gridReader.ReadAsync<T>().ConfigureAwait(false);
            return enumerable.ToArray();
        }
    }
}