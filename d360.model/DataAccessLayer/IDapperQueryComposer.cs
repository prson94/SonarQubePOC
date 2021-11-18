using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace d360.model.DataAccessLayer
{
    internal interface IDapperQueryComposer
    {
        Task<SqlMapper.GridReader> StoredProcedureMultipleResultsAsync(
            string procedureName,
            SqlMapper.IDynamicParameters parameters,
            IDbConnection connection,
            int? commandTimeout);

        Task<TItem> StoredProcedureSingleAsync<TItem>(
            string procedureName,
            SqlMapper.IDynamicParameters parameters,
            IDbConnection connection,
            int? commandTimeout = null);

        Task<IReadOnlyList<TItem>> StoredProcedureMultipleAsync<TItem>(
            string procedureName,
            SqlMapper.IDynamicParameters parameters,
            IDbConnection connection,
            int? commandTimeout = null);
    }

    // ReSharper disable once UnusedTypeParameter
    internal interface IDapperQueryComposer<TDbConnectionProvider>
        where TDbConnectionProvider : IDbConnectionProvider
    {
        Task<SqlMapper.GridReader> StoredProcedureMultipleResultsAsync(
            string procedureName,
            SqlMapper.IDynamicParameters parameters,
            int? commandTimeout = null);

        Task<TItem> StoredProcedureSingleAsync<TItem>(
            string procedureName,
            SqlMapper.IDynamicParameters parameters,
            int? commandTimeout = null);

        Task<IReadOnlyList<TItem>> StoredProcedureMultipleAsync<TItem>(
            string procedureName,
            SqlMapper.IDynamicParameters parameters,
            int? commandTimeout = null);
    }
}