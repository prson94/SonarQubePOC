using System.Threading;
using System.Threading.Tasks;

using d360.core.entities;

namespace repositories
{
    public interface IApplicationHealthDapperRepository
    {
        Task<ApplicationHealthDetailsEntity> GetDetailsAsync(CancellationToken cancellationToken = default);
    }
}
