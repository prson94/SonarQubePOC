using d360.core.entities;
using System;
using System.Collections.Generic;

namespace d360.model.helpers.filters
{
    public interface IFilterDataProvider
    {
        (List<IntersectType>, List<Asset>, List<AssetType>) GetDataForRelationshipsParsing(List<Guid> IntersectUids, List<Guid> AssetUids);
        int GetFieldLookupValue(string lookupObjectType, int lookupObjectId, int fieldTypeIdForLookupValue, string value);
        FieldType GetFieldTypeById(int? fieldTypeId);
        bool IsFieldFromRelationship(int fieldTypeId);
    }
}