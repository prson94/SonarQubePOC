using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;

namespace d360.model.DataAccessLayer
{
    // ReSharper disable once UnusedTypeParameter
    internal interface IDapperQueryComposer<TDbConnectionProvider>
        where TDbConnectionProvider : IDbConnectionProvider
    {
        Task<SqlMapper.GridReader> StoredProcedureMultipleResultsAsync(
            string procedureName,
            object parameters,
            int? commandTimeout = null);

        Task<TItem> StoredProcedureSingleAsync<TItem>(
            string procedureName,
            object parameters,
            int? commandTimeout = null);

        Task<IReadOnlyList<TItem>> StoredProcedureMultipleAsync<TItem>(
            string procedureName,
            object parameters,
            int? commandTimeout = null
        );

        Task<T> QuerySingleOrDefaultAsync<T>(
            string sql,
            object parameters,
            int? commandTimeout = null
        );

        Task<SqlMapper.GridReader> QueryMultipleAsync(
            string sql,
            object parameters,
            int? commandTimeout = null
        );

        Task<IReadOnlyList<T>> QueryListAsync<T>(
            string sql,
            object parameters,
            int? commandTimeout = null
        );
    }
}
