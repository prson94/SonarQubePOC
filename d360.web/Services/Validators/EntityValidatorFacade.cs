using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace d360.web.Services
{
    public class EntityValidatorFacade : IEntityValidatorFacade
    {
        private IMediator Mediator { get; }

        public EntityValidatorFacade(IMediator mediator)
        {
            Mediator = mediator;
        }

        public Task ResponsibilityTypeIsExists(Guid? responsibilityTypeUid, CancellationToken cancellationToken = default)
        {
            var request = new ResponsibilityTypeIsExistsRequest()
            {
                // pass uid
                Uid = responsibilityTypeUid,
                // request throw if entity not found
                ThrowNotFoundException = true
            };
            return Mediator.Send(request, cancellationToken);
        }

        public Task ResourceIsExists(Guid? resourceUid, CancellationToken cancellationToken = default)
        {
            var request = new ResourceIsExistsRequest()
            {
                // pass uid
                Uid = resourceUid,
                // request throw if entity not found
                ThrowNotFoundException = true
            };
            return Mediator.Send(request, cancellationToken);
        }
    }
}