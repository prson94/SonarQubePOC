using System.Threading;
using System.Threading.Tasks;
using d360.model.DataAccessLayer;
using MediatR;

namespace d360.web.Services
{
    internal sealed class ResponsibilityGetTypeBreakdownRequestHandler : IRequestHandler<ResponsibilityGetTypeBreakdownRequest, ResponsibilityGetTypeBreakdownResponse>
    {
        private IMediator Mediator { get; }

        private IResponsibilityDapperRepository ResponsibilityRepository { get; }

        public ResponsibilityGetTypeBreakdownRequestHandler(IMediator mediator, IResponsibilityDapperRepository responsibilityRepository)
        {
            Mediator = mediator;
            ResponsibilityRepository = responsibilityRepository;
        }

        public async Task<ResponsibilityGetTypeBreakdownResponse> Handle(ResponsibilityGetTypeBreakdownRequest request, CancellationToken cancellationToken)
        {
            // assert parameters
            await Mediator.EntityValidators().ResponsibilityTypeIsExists(request.ResponsibilityTypeUid, cancellationToken);

            // act
            var result = new ResponsibilityGetTypeBreakdownResponse();
            result.Data = await ResponsibilityRepository.GetResponsibilityTypeBreakdownAsync(request.ResponsibilityTypeUid);
            return result;
        }
    }
}