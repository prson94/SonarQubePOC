using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using d360.core;
using d360.core.entities;

namespace d360.model.DataAccessLayer
{
    public interface IFieldsRepository
    {
        void DeleteFields(List<FieldType> currentFieldTypes, List<string> fieldNamesToDelete);
        Task<Tuple<FieldTypesApiViewModel, WorkHttpStatus>> GetFieldTypes(IEnumerable<KeyValuePair<string, string>> queryParams);
        List<FieldType> GetFieldTypes(TypeIdentifierInfoModel typeIdentifierInfoModel);
        List<Tuple<string, Guid>> GetFieldInterSetUID(List<FieldType> ExistingFieldType);
        bool HasExistingItems(TypeIdentifierInfoModel typeIdentifierInfoModel);
        WorkHttpStatus UpdateFields(FieldTypesApiEditModel model, TypeIdentifierInfoModel typeIdentifierInfoModel);
        IEnumerable<string> GetCustomFields(SystemObjects objectType, int objectId);
        bool hasResponsibilityUsingField(TypeIdentifierInfoModel typeIdentifierInfoModel, List<FieldType> fieldTypes);
        List<FieldType> GetFieldDefinitionForComplexLookupFieldType(FieldType fieldType, Guid assetUid, bool handleFiltersAsString);
    }
}