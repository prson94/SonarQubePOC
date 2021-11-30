using System.Threading;
using System.Threading.Tasks;
using d360.model.DataAccessLayer;
using MediatR;

namespace d360.web.Services
{
    internal sealed class ResourceIsExistsRequestHandler : IRequestHandler<ResourceIsExistsRequest, IsEntityExistsResponse>
    {
        private IResourceRepository ResourceRepository { get; }

        public ResourceIsExistsRequestHandler(IResourceRepository resourceRepository)
        {
            ResourceRepository = resourceRepository;
        }

        public async Task<IsEntityExistsResponse> Handle(ResourceIsExistsRequest request, CancellationToken cancellationToken)
        {
            var result = new IsEntityExistsResponse();

            if (request.Uid != null)
            {
                var responsibilityType = await ResourceRepository.GetByUidAsync(request.Uid.Value);
                if (responsibilityType == null)
                {
                    if (request.ThrowNotFoundException)
                    {
                        throw new NotFoundBusinessLayerException($"Resource UID=\'{request.Uid}\' does not exist");
                    }

                    result.IsExists = false;
                }
            }

            return result;
        }
    }
}