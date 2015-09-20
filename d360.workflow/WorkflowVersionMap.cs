using d360.workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.workflow
{
    public static class WorkflowVersionMap
    {
        static Dictionary<WorkflowIdentity, Activity> map;

        // Current version identities.
        public static WorkflowIdentity SuggestNewArtifactIdentity_v1000;
        public static WorkflowIdentity CertifyArtifactIdentity_v1000;

        public static WorkflowIdentity SuggestNewArtifactIdentity_vCurrent;
        public static WorkflowIdentity CertifyArtifactIdentity_vCurrent;

        static WorkflowVersionMap()
        {
            map = new Dictionary<WorkflowIdentity, Activity>();

            CertifyArtifactIdentity_v1000 = new WorkflowIdentity { Name = "CertifyArtifactWorkflow v1.0.0.0", Version = new Version(1, 0, 0, 0) };
            map.Add(CertifyArtifactIdentity_v1000, new CertifyArtifact_v1000());
            CertifyArtifactIdentity_vCurrent = new WorkflowIdentity { Name = "CertifyArtifactWorkflow v1.0.0.1", Version = new Version(1, 0, 0, 1) };
            map.Add(CertifyArtifactIdentity_vCurrent, new CertifyArtifact_v1001());


            //SuggestNewArtifactIdentity_v1000 = new WorkflowIdentity     { Name = "SuggestNewArtifactWorkflow v1.0.0.0",     Version = new Version(1, 0, 0, 0)   };
            //map.Add(SuggestNewArtifactIdentity_v1000, new SuggestNewArtifact());
            SuggestNewArtifactIdentity_vCurrent = new WorkflowIdentity { Name = "SuggestNewArtifactWorkflow v1.0.0.0", Version = new Version(1, 0, 0, 0) };
            map.Add(SuggestNewArtifactIdentity_vCurrent, new SuggestNewArtifact_v1000());
        }

        public static Activity GetWorkflowDefinition(WorkflowIdentity identity)
        {
            return map[identity];
        }

        public static string GetIdentityDescription(WorkflowIdentity identity)
        {
            return identity.ToString();
        }
    }
}
