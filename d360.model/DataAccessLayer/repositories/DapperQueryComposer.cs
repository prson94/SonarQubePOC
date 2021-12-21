using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace d360.model.DataAccessLayer.repositories
{
    internal sealed class DapperQueryComposer<TDbConnectionProvider> : IDapperQueryComposer<TDbConnectionProvider>
        where TDbConnectionProvider : IDbConnectionProvider
    {
        private TDbConnectionProvider ConnectionProvider { get; }

        public DapperQueryComposer(TDbConnectionProvider connectionProvider)
        {
            ConnectionProvider = connectionProvider;
        }

        public Task<SqlMapper.GridReader> StoredProcedureMultipleResultsAsync(
            string procedureName,
            SqlMapper.IDynamicParameters parameters,
            int? commandTimeout)
        {
            return ConnectionProvider.Connection.QueryMultipleAsync(procedureName, parameters, null, commandTimeout, CommandType.StoredProcedure);
        }

        public Task<TItem> StoredProcedureSingleAsync<TItem>(
            string procedureName,
            SqlMapper.IDynamicParameters parameters,
            int? commandTimeout = null)
        {
            return ConnectionProvider.Connection.QueryFirstOrDefaultAsync<TItem>(procedureName, parameters, null, commandTimeout, CommandType.StoredProcedure);
        }

        public async Task<IReadOnlyList<TItem>> StoredProcedureMultipleAsync<TItem>(
            string procedureName,
            SqlMapper.IDynamicParameters parameters,
            int? commandTimeout = null)
        {
            var result = await ConnectionProvider.Connection.QueryAsync<TItem>(procedureName, parameters, null, commandTimeout, CommandType.StoredProcedure);
            return result as IReadOnlyList<TItem> ?? new List<TItem>(result);
        }

        public Task<SqlMapper.GridReader> QueryMultipleAsync(
            string sql,
            object parameters,
            int? commandTimeout = null)
        {
            return ConnectionProvider.Connection.QueryMultipleAsync(sql, parameters, null, commandTimeout);
        }
    }
}