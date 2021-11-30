using System.Threading;
using System.Threading.Tasks;
using d360.model.DataAccessLayer;
using MediatR;

namespace d360.web.Services
{
    internal sealed class ResponsibilityTypeIsExistsRequestHandler : IRequestHandler<ResponsibilityTypeIsExistsRequest, IsEntityExistsResponse>
    {
        private IResponsibilityTypeRepository ResponsibilityTypeRepository { get; }

        public ResponsibilityTypeIsExistsRequestHandler(IResponsibilityTypeRepository responsibilityTypeRepository)
        {
            ResponsibilityTypeRepository = responsibilityTypeRepository;
        }

        public async Task<IsEntityExistsResponse> Handle(ResponsibilityTypeIsExistsRequest request, CancellationToken cancellationToken)
        {
            var result = new IsEntityExistsResponse();

            if (request.Uid != null)
            {
                var responsibilityType = await ResponsibilityTypeRepository.GetByUidAsync(request.Uid.Value);
                if (responsibilityType == null)
                {
                    if (request.ThrowNotFoundException)
                    {
                        throw new NotFoundBusinessLayerException($"ResponsibilityType UID=\'{request.Uid}\' does not exist");
                    }

                    result.IsExists = false;
                }
            }

            return result;
        }
    }
}