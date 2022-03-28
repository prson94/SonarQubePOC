using System.Threading;
using System.Threading.Tasks;

using d360.core.entities;
using d360.model.DataAccessLayer.repositories;

namespace d360.model.DataAccessLayer
{
    internal sealed class ApplicationHealthDapperRepository : DapperRepositoryBase<ICompanyDbConnectionProvider>, IApplicationHealthDapperRepository
    {
        public ApplicationHealthDapperRepository(IDapperQueryComposer<ICompanyDbConnectionProvider> queryComposer) : base(queryComposer)
        {
        }

        public async Task<ApplicationHealthDetailsEntity> GetDetailsAsync(CancellationToken cancellationToken = default)
        {
            var sql = "SELECT count(1) FROM[queue].[Task] WITH(NOLOCK); "
                      + "SELECT count(1) FROM[api].[Execution] WITH(NOLOCK) WHERE CompletedOn IS NULL; "
                      + "SELECT count(1) FROM[workflow].[Item] WITH(NOLOCK) WHERE CompletedOn IS NULL; ";
            var grid = await QueryComposer.QueryMultipleAsync(sql);

            var result = new ApplicationHealthDetailsEntity
            {
                QueueTaskCount = await grid.ReadSingleAsync<int>(),
                ApiExecutionPendingCount = await grid.ReadSingleAsync<int>(),
                WorkflowItemPendingCount = await grid.ReadSingleAsync<int>()
            };

            return result;
        }
    }
}
