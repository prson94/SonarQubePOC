using d360.core.entities.Workflow;
using d360.core.enums;
using d360.core.enums.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
   public interface IWorkflowRepository
    {
         Task<IEnumerable<WorkflowTypeApiViewModel>> GetWorkflowTypes(IEnumerable<KeyValuePair<string, string>> queryParams);
        Task<WorkflowVersionsApiViewModel> GetWorkflowVersions(IEnumerable<KeyValuePair<string, string>> queryParams);

        Task<IEnumerable<WorkflowVersionSteps>> GetWorkflowVersionSteps(Guid uid);

        Task<IEnumerable<WorkflowInstanceApiViewModel>> GetWorkflowInstances(Guid workflowUid);
    }
}
