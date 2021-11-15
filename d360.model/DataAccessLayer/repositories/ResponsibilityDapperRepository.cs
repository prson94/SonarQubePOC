using System;
using System.Collections.Generic;
using System.Data;
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

        public async Task<IReadOnlyList<ResponsibilityBreakdownResponse>> GetResponsibilityTypeBreakdownAsync(Guid? typeUid)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@typeUID", typeUid, DbType.Guid);

            return await QueryComposer.StoredProcedureMultipleAsync<ResponsibilityBreakdownResponse>("[dbo].[GetResponsibilityTypeBreakdown]", parameters);
        }
    }
}