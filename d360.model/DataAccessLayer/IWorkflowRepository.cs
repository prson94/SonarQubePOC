using d360.core.entities.Workflow;
using d360.core.enums;
using d360.core.enums.Workflow;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace d360.model.DataAccessLayer
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

        Task<IEnumerable<WorkflowReassignmentAssetApiModel>> GetWorkflowReassignmentAssets(int assetTypeId, string query, int resultCount = 100, CancellationToken? cancellationToken = null);
    }
}
