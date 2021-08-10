using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.helpers.filters
{
    public class FilterDataProvider : IFilterDataProvider
    {
        private ICompanyContext companyContext;

        public FilterDataProvider(ICompanyContext ctx)
        {
            this.companyContext = ctx;
        }

        public bool IsFieldFromRelationship(int fieldTypeId)
        {
            return this.GetFieldTypeById(fieldTypeId)?.Type == "FieldFromRelationship";
        }

        public FieldType GetFieldTypeById(int? fieldTypeId)
        {
            return this.companyContext.FieldTypes.FirstOrDefault(x => x.ID == fieldTypeId);
        }

        public int GetFieldLookupValue(string lookupObjectType, int lookupObjectId, int fieldTypeIdForLookupValue, string value)
        {
            return this.companyContext.GetFieldLookupValue(lookupObjectType, lookupObjectId, fieldTypeIdForLookupValue, value);
        }


        public (List<IntersectType>, List<Asset>, List<AssetType>) GetDataForRelationshipsParsing(List<Guid> IntersectUids, List<Guid> AssetUids)
        {
            var intersectTypes = this.companyContext.IntersectTypes.Where(x => IntersectUids.Contains(x.uid)).AsNoTracking().ToList();
            var filterAssets = this.companyContext.Assets.Where(x => AssetUids.Contains(x.uid)).Include(x => x.AssetType).AsNoTracking().ToList();
            var filterAssetTypes = this.companyContext.AssetTypes.Where(x => AssetUids.Contains(x.uid)).AsNoTracking().ToList();
            return (intersectTypes, filterAssets, filterAssetTypes);
        }
    }
}
