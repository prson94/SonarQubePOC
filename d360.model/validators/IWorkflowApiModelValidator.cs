using System;
using System.Collections.Generic;

namespace d360.model.validators
{
    public interface IWorkflowApiModelValidator
    {
        bool IsValidGuidCountForWorkflowGetTypeModel(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        bool IsValidGuidForWorkflowGetTypeModel(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        bool IsValidGuidCountForWorkflowGetVersionModel(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        bool IsValidGuidForWorkflowGetVersionModel(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        bool IsValidOrderByFieldForWorkflowGetVersionModel(IEnumerable<KeyValuePair<string, string>> queryParams);

        bool IsValidAssetType(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        bool IsValidActionType(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        bool IsValidRelationshipType(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        bool IsValidWorkflowType(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        bool IsValidWorkflowVersion(Guid workflowVersionUID);
        
        bool IsValidWorkflowInstance(Guid workflowVersionUID);
        
        bool IsValidWorkflowVersion(IEnumerable<KeyValuePair<string, string>> queryParams);

        bool IsValidGuidCountForGetWorkflowModel(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        bool IsValidGuidForGetWorkflowModel(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        bool IsValidAsset(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        bool IsValidAction(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        bool IsValidRelationship(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        bool IsValidOrderByFieldForGetWorkflowModel(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        bool IsValidDirectionForWorkflowGetModel(IEnumerable<KeyValuePair<string, string>> queryParams);
    }
}
