using d360.core.entities;
using d360.model;
using MediatR;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace d360.web.Services
{
    public class GetResponsibilityTypeRelationRule
        : IRequestHandler<GetResponsibilityTypeRelationRule.Argument, ResponsibilityRuleGetModel>
    {
        private readonly ICompanyContext company;

        public GetResponsibilityTypeRelationRule(ICompanyContext company)
        {
            this.company = company;
        }

        public async Task<ResponsibilityRuleGetModel> Handle(Argument request, CancellationToken cancellationToken)
        {
            var model = company.GetById<ResponsibilityTypeRelationRule>(request.Id);
            model.SetDefinitionFromRaw();

            var assetTypeUid = company.AssetTypes.SingleOrDefault(x => x.Object == model.Object && x.ObjectID == model.ObjectID).uid;
            var mappedModel = new ResponsibilityRuleGetModel
            {
                ID = model.ID,
                ResponsibilityTypeID = model.ResponsibilityTypeID,
                Object = model.Object,
                ObjectID = model.ObjectID,
                ApplyToType = model.ApplyToType,
                AssetTypeUid = assetTypeUid,
                Context = model.Context,
                IsVisible = model.IsVisible,
                Name = model.Name,
                StructuredDefinition = model.StructuredDefinition
            };

            return mappedModel;
        }

        public class Argument : IRequest<ResponsibilityRuleGetModel>
        {
            public int Id { get; set; }
        }
    }
}
