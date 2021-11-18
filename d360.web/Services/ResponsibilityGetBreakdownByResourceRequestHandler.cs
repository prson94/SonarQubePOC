using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using d360.model.DataAccessLayer;
using d360.model.DataAccessLayer.repositories;
using MediatR;

namespace d360.web.Services
{
    internal sealed class ResponsibilityGetBreakdownByResourceRequestHandler : IRequestHandler<ResponsibilityGetBreakdownByResourceRequest, ResponsibilityGetBreakdownByResourceResponse>
    {
        private IResponsibilityDapperRepository ResponsibilityRepository { get; }

        private IAssetService AssetService { get; }

        public ResponsibilityGetBreakdownByResourceRequestHandler(IResponsibilityDapperRepository responsibilityRepository, IAssetService assetService)
        {
            ResponsibilityRepository = responsibilityRepository;
            AssetService = assetService;
        }

        public async Task<ResponsibilityGetBreakdownByResourceResponse> Handle(ResponsibilityGetBreakdownByResourceRequest request, CancellationToken cancellationToken)
        {
            var aggregateCollection = await ResponsibilityRepository.GetResponsibilityBreakdownByResourceAsync(request.ResourceUid, request.ResourceTypeUid);

            var response = new ResponsibilityGetBreakdownByResourceResponse();
            response.ItemCollection = aggregateCollection.Select(Convert).ToArray();

            return response;
        }

        private ResponsibilityGetBreakdownByResourceModel Convert(ResponsibilityBreakdownByResourceAggregate aggregate)
        {
            var result = new ResponsibilityGetBreakdownByResourceModel();
            result.Name = AssetService.GetAssetName(aggregate.AssetType);
            result.Class = aggregate.AssetType.Class.ToString();
            result.AssetTypeUid = aggregate.AssetType.uid;
            result.AssetCount = aggregate.AssetCount;
            return result;
        }
    }
}