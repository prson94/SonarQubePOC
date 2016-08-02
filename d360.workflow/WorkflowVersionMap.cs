using System;
using System.Activities;
using System.Collections.Generic;

namespace d360.workflow
{
    public static class WorkflowVersionMap
    {
        static Dictionary<WorkflowIdentity, Activity> map;

        // Current version identities.
        public static WorkflowIdentity SuggestNewArtifactIdentity_v1000;
        public static WorkflowIdentity SuggestNewArtifactIdentity_vCurrent;

        public static WorkflowIdentity CertifyArtifactIdentity_v1000;
        public static WorkflowIdentity CertifyArtifactIdentity_vCurrent;

        public static WorkflowIdentity WorkIssue_v1000;
        public static WorkflowIdentity WorkIssue_v1001;
        public static WorkflowIdentity WorkIssue_vCurrent;

        public static WorkflowIdentity ChallengeArtifact_v1000;
        public static WorkflowIdentity ChallengeArtifact_vCurrent;

        public static WorkflowIdentity SuggestNewArtifactMultiStepIdentity_vCurrent;

        static WorkflowVersionMap()
        {
            map = new Dictionary<WorkflowIdentity, Activity>();

            CertifyArtifactIdentity_v1000 = new WorkflowIdentity { Name = "CertifyArtifactWorkflow v1.0.0.0", Version = new Version(1, 0, 0, 0) };
            map.Add(CertifyArtifactIdentity_v1000, new CertifyArtifact_v1000());

            CertifyArtifactIdentity_vCurrent = new WorkflowIdentity { Name = "CertifyArtifactWorkflow v1.0.0.1", Version = new Version(1, 0, 0, 1) };
            map.Add(CertifyArtifactIdentity_vCurrent, new CertifyArtifact_v1001());
                        
            SuggestNewArtifactIdentity_vCurrent = new WorkflowIdentity { Name = "SuggestNewArtifactWorkflow v1.0.0.0", Version = new Version(1, 0, 0, 0) };
            map.Add(SuggestNewArtifactIdentity_vCurrent, new SuggestNewArtifact_v1000());

            WorkIssue_v1000 = new WorkflowIdentity { Name = "WorkIssue v1.0.0.0", Version = new Version(1, 0, 0, 0) };
            map.Add(WorkIssue_v1000, new WorkIssue_v1000());

            WorkIssue_v1001 = new WorkflowIdentity { Name = "WorkIssue v1.0.0.1", Version = new Version(1, 0, 0, 1) };
            map.Add(WorkIssue_v1001, new WorkIssue_v1001());

            WorkIssue_vCurrent = new WorkflowIdentity { Name = "WorkIssue v1.0.0.2", Version = new Version(1, 0, 0, 2) };
            map.Add(WorkIssue_vCurrent, new WorkIssue_v1002());

            ChallengeArtifact_v1000 = new WorkflowIdentity { Name = "ChallengeArtifact v1.0.0.0", Version = new Version(1, 0, 0, 0) };
            map.Add(ChallengeArtifact_v1000, new ChallengeArtifact_v1000());

            ChallengeArtifact_vCurrent = new WorkflowIdentity { Name = "ChallengeArtifact v1.0.0.1", Version = new Version(1, 0, 0, 1) };
            map.Add(ChallengeArtifact_vCurrent, new ChallengeArtifact_v1001());
            
            SuggestNewArtifactMultiStepIdentity_vCurrent = new WorkflowIdentity { Name = "SuggestNewArtifactWorkflowMultiStep v1.0.0.0", Version = new Version(1, 0, 0, 0) };
            map.Add(SuggestNewArtifactMultiStepIdentity_vCurrent, new SuggestNewArtifactMultiStep_v1000());
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
