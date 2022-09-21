using d360.core.entities.Graph;
using d360.core.resources;
using d360.model.DataAccessLayer;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Net;
using System.Threading.Tasks;

namespace d360.model.validators
{
	public class GraphFilterValidator
	{
		private readonly ICompanyContext CompanyContext;
		private readonly IAssetRepository AssetRepository;
		private readonly IResponsibilityTypeRepository ResponsibilityTypeRepository;

		public GraphFilterValidator(
			ICompanyContext companyContext,
			IAssetRepository assetRepository,
			IResponsibilityTypeRepository responsibilityTypeRepository)
		{
			this.CompanyContext = companyContext;
			this.AssetRepository = assetRepository;
			this.ResponsibilityTypeRepository = responsibilityTypeRepository;
		}

		private static HashSet<int> VALID_NUMBER_OF_HOPS = new HashSet<int> { 1, 2, 3, 4, 5 };
		private static HashSet<int> VALID_ANCESTRY = new HashSet<int> { 1 /* All parents */, 2 /* Direct parents */ };

		enum DiagramType
		{
			Lineage = 1,
			Impact = 2
		}

		public async Task<WorkHttpStatus> ValidateFilterCreateOrUpdate(GraphFilter filter)
		{
			var settings = filter.StructuredSettings;

			if (string.IsNullOrEmpty(filter.Name))
			{
				return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.BadRequest, GraphFilterErrors.InvalidName);
			}
 
			if (settings.NumberOfHops == null || !VALID_NUMBER_OF_HOPS.Contains(settings.NumberOfHops.Value))
			{
				return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.BadRequest, GraphFilterErrors.InvalidNumberOfHops);
			}

			if (settings.DiagramType == null || !Enum.IsDefined(typeof(DiagramType), settings.DiagramType.Value))
			{
				return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.BadRequest, GraphFilterErrors.InvalidDiagramType);
			}

			if (settings.AncestryMode == null || !VALID_ANCESTRY.Contains(settings.AncestryMode.Value))
			{
				return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.BadRequest, GraphFilterErrors.InvalidAncestryMode);
			}

			foreach (var assetType in settings.AssetTypes)
			{
				var assetTypeInDb = AssetRepository.GetAssetTypeByUID(assetType.Uid ?? Guid.Empty);
				var isDeleted = assetTypeInDb.State == core.enums.State.Deleted || assetTypeInDb.State == core.enums.State.PendingDelete;
				if (assetTypeInDb == null || isDeleted)
				{
					return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.BadRequest, GraphFilterErrors.InvalidAssetType);
				}
			}

			foreach (var responsibilityType in settings.ResponsibilityTypes)
			{
				var responsibilityTypeInDb = await ResponsibilityTypeRepository.GetByUidAsync(responsibilityType.Uid ?? Guid.Empty);
				if (responsibilityTypeInDb == null)
				{
					return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.BadRequest, GraphFilterErrors.InvalidResponsibilityType);
				}
			}

			foreach (var predicate in settings.Predicates)
			{
				var predicateInDb = await CompanyContext.Predicates.SingleOrDefaultAsync(x => x.UID == predicate.Uid);
				if (predicateInDb == null)
				{
					return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.BadRequest, GraphFilterErrors.InvalidPredicate);
				}
			}

			return null;
		}
	}
}
