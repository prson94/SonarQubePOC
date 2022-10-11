using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using d360.core.entities;
using Dapper;

namespace d360.model.helpers.filters
{

    [ExcludeFromCodeCoverage]
    public class FilterDataProvider : IFilterDataProvider
    {
        private readonly ICompanyContext companyContext;

        public FilterDataProvider(ICompanyContext ctx)
        {
            companyContext = ctx;
        }

        public bool IsFieldFromRelationship(int fieldTypeId)
        {
            return GetFieldTypeById(fieldTypeId)?.Type == "FieldFromRelationship";
        }

        public FieldType GetFieldTypeById(int? fieldTypeId)
        {
            return companyContext.FieldTypes.FirstOrDefault(x => x.ID == fieldTypeId);
        }

        public int GetFieldLookupValue(string lookupObjectType, int lookupObjectId, int fieldTypeIdForLookupValue, string value)
        {
            return companyContext.GetFieldLookupValue(lookupObjectType, lookupObjectId, fieldTypeIdForLookupValue, value);
        }

        public (List<IntersectType>, List<Asset>, List<AssetType>) GetDataForRelationshipsParsing(List<Guid> IntersectUids, List<Guid> AssetUids)
        {
            var intersectTypes = companyContext.IntersectTypes.Where(x => IntersectUids.Contains(x.uid)).AsNoTracking().ToList();
            var filterAssets = companyContext.Assets.Where(x => AssetUids.Contains(x.uid)).Include(x => x.AssetType).AsNoTracking().ToList();
            var filterAssetTypes = companyContext.AssetTypes.Where(x => AssetUids.Contains(x.uid)).AsNoTracking().ToList();
            
            return (intersectTypes, filterAssets, filterAssetTypes);
        }

		public (List<Guid>, List<AssetTypeKeyFieldMap>) GetPathSegmentsMappingInfo(int assetTypeID, List<Guid> assetTypeUids)
		{
			var gridReader = companyContext.Database.Connection.QueryMultiple(@"
								;with cte as (
								select IT.SubjectAssetTypeID from [IntersectType] IT
								inner join [Predicate] P on P.ID = IT.PredicateID AND P.Type IN (3,4)
								where IT.ObjectAssetTypeID = @assetTypeid
								union all
								select IT.SubjectAssetTypeID from cte 
								inner join [IntersectType] IT ON IT.ObjectAssetTypeID = cte.SubjectAssetTypeId
								inner join [Predicate] P on P.ID = IT.PredicateID AND P.Type IN (3,4)
								)
								select at.uid from cte
								inner join AssetType at on at.ID = cte.SubjectAssetTypeID

								select at.uid as AssetTypeUid, ft.ID as FieldTypeId from FieldType ft
								inner join AssetType at on at.ID = ft.AssetTypeID
								where at.uid in @assetTypeUids and ft.IsPartOfKey = 1 and [Type] = 'Text'", new { assetTypeID, assetTypeUids });


			List<Guid> levels = gridReader.Read<Guid>().ToList();
			var typeKeyFields = gridReader.Read<AssetTypeKeyFieldMap>().ToList();
			return (levels, typeKeyFields);
		}
    }
}
