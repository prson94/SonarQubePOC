using OptimaJet.Workflow.Core.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OptimaJet.Workflow.Core.Model;

namespace d360.workflow.extensions
{
    public class NotifyOwnerAction : IWorkflowActionProvider
    {
        public void ExecuteAction(string name, ProcessInstance processInstance, WorkflowRuntime runtime, string actionParameter)
        {
            throw new NotImplementedException();
        }

        public bool ExecuteCondition(string name, ProcessInstance processInstance, WorkflowRuntime runtime, string actionParameter)
        {
            throw new NotImplementedException();
        }

        public List<string> GetActions()
        {
            return new List<string>() { "Email", "SMS" };
        }
    }
}
