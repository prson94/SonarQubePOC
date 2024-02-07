using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using d360.core.entities.Workflow;

namespace repositories
{
    public interface IWorkflowRepository
    {
        Task<IEnumerable<WorkflowTypeApiViewModel>> GetWorkflowTypes(IEnumerable<KeyValuePair<string, string>> queryParams);
        
        Task<WorkflowVersionsApiViewModel> GetWorkflowVersions(IEnumerable<KeyValuePair<string, string>> queryParams);

        Task<IEnumerable<WorkflowVersionStepsApiViewModel>> GetWorkflowVersionSteps(Guid uid);

        d360.core.entities.Workflow.Type GetWorkflowTypeByUID(Guid workflowTypUid);
        
        WorkflowVersion GetWorkflowVersionByUID(Guid workflowVerionUid);

        Task<IEnumerable<WorkflowInstanceApiViewModel>> GetWorkflowInstances(Guid workflowUid);

        WorkflowItem GetWorkflowItemByUID(Guid workflowItemUid);

        Task<WorkflowsApiViewModel> GetWorkflows(IEnumerable<KeyValuePair<string, string>> queryParams);

        Task<IEnumerable<WorkflowReassignmentAssetApiModel>> GetWorkflowReassignmentAssets(long workflowItemId, string query, int resultCount = 100, CancellationToken? cancellationToken = null);

		Task<WorkflowAssignmentApiModel> GetWorkflowAssignmentList(IEnumerable<KeyValuePair<string, string>> queryParams, CancellationToken cancellationToken);		

		Task<WorkflowItemDetails> GetWorkflowItemDetails(Guid workflowItemUid);		

		Task<IEnumerable<dynamic>> GetPossibleAssignees();

		Task<IEnumerable<dynamic>> GetPossibleInitiators();

		Task<IEnumerable<dynamic>> GetRelevantAssetTypes();		

		Task<WorkflowInstanceDetailsByVersionAPIModel> GetWorkflowInstanceDetailsByVersion(IEnumerable<KeyValuePair<string, string>> queryParams);

		Task<long> GetAssetAssignmentCount(string type, Guid uid);
		Task<WorkflowUserGroupedAssignments> GetWorkflowAssignmentListGroupedForUser(Guid resourceUid, IEnumerable<KeyValuePair<string, string>> queryParams, CancellationToken? cancellationToken = null);

		Task<WorkflowItemStepStateAPIModel> GetAssignmentStateForCurrentUser(Guid workflowItemStepUid);

	}
}
