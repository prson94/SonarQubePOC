using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using d360.core.entities;

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
    }
}
