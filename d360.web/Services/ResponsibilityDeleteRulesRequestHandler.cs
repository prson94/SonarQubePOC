using System.IO;
using System.Threading;
using System.Threading.Tasks;
using d360.model;
using d360.model.DataAccessLayer;
using MediatR;
using Resources;

namespace d360.web.Services
{
    internal sealed class ResponsibilityDeleteRulesRequestHandler : IRequestHandler<ResponsibilityDeleteRulesRequest, ResponsibilityDeleteRulesResponse>
    {
        private IResponsibilityRepository ResponsibilityRepository { get; }
        private ICompanyContext Company { get; }

        public ResponsibilityDeleteRulesRequestHandler(IResponsibilityRepository responsibilityRepository, ICompanyContext company)
        {
            ResponsibilityRepository = responsibilityRepository;
            Company = company;
        }

        public async Task<ResponsibilityDeleteRulesResponse> Handle(ResponsibilityDeleteRulesRequest request, CancellationToken cancellationToken)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new ForbiddenBusinessLayerException();

            var responsibility = ResponsibilityRepository.GetResponsibilityTypeByUID(request.TypeUid);
            if (responsibility == null)
            {
                throw new NotFoundBusinessLayerException(ResponsibilityApiMessages.InvalidResponsibilityUid);
            }

            var result = new ResponsibilityDeleteRulesResponse();
            result.Data = await ResponsibilityRepository.DeleteResponsibilityRulesAsync(request.TypeUid, request.RuleDeleteUidCollection);
            return result;
        }
    }
}