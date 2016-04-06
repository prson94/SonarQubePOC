using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.workflow
{
    public enum WorkflowAction
    {
        SuggestNewArtifact = 1,
        CertifyArtifact = 2,
        WorkIssue = 3,
        ChallengeArtifact = 4
    }
}
