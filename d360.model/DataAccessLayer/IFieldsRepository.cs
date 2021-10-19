using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using d360.core;
using d360.core.entities;
using d360.core.Models;
using Dapper;

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
        List<FieldType> GetFieldDefinitionForComplexLookupFieldType(FieldType fieldType, Guid assetUid, bool forUiFiltering = false);
        Task<(List<GridColumn>, List<GridField>, List<dynamic>, int, List<dynamic>)> GetComplexRelationLookupGrid(FieldTypeLookup ftl, List<FieldType> fields, DynamicParameters dbArgs, string simpleFilter, string advancedFilter, string orderBy = "", string direction = "asc", bool countOnly = false);
        Task<(List<GridColumn>, List<GridField>, List<dynamic>, int)> GetRefListFromRelationshipGrid(List<FieldType> fields, DynamicParameters dbArgs, string simpleFilter, string advancedFilter, string orderBy = "", string direction = "asc", bool countOnly = false);
        Task<(List<GridColumn>, List<GridField>, List<dynamic>, int)> GetOwnershipLookupGrid(FieldTypeLookup ftl, List<FieldType> fields, DynamicParameters dbArgs, string simpleFilter, string advancedFilter, string orderBy = "", string direction = "asc", bool countOnly = false);
    }
}