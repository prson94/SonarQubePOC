using System.Threading;
using System.Threading.Tasks;
using d360.model.DataAccessLayer;
using MediatR;

namespace d360.web.Services
{
    internal sealed class ResponsibilityGetTypeBreakdownRequestHandler : IRequestHandler<ResponsibilityGetTypeBreakdownRequest, ResponsibilityGetTypeBreakdownResponse>
    {
        private IResponsibilityDapperRepository ResponsibilityRepository { get; }

        public ResponsibilityGetTypeBreakdownRequestHandler(IResponsibilityDapperRepository responsibilityRepository)
        {
            ResponsibilityRepository = responsibilityRepository;
        }

        public async Task<ResponsibilityGetTypeBreakdownResponse> Handle(ResponsibilityGetTypeBreakdownRequest request, CancellationToken cancellationToken)
        {
            var result = new ResponsibilityGetTypeBreakdownResponse();
            result.Data = await ResponsibilityRepository.GetResponsibilityTypeBreakdownAsync(request.ResourceTypeUid);
            return result;
        }
    }
}