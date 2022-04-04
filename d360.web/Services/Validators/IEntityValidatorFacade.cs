using System;
using System.Threading;
using System.Threading.Tasks;

namespace d360.web.Services
{
    public interface IEntityValidatorFacade
    {
        Task ResponsibilityTypeIsExists(Guid? responsibilityTypeUid, CancellationToken cancellationToken = default);

        Task ResourceIsExists(Guid? resourceUid, CancellationToken cancellationToken = default);
    }
}
