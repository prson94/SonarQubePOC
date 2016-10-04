import { WorkflowRaiseIssueComponent} from './workflow-raise-issue.component';
import { WorkflowWorkIssueComponent } from './workflow-work-issue.component';
import { RouterModule } from '@angular/router';

export const WorkflowRoutes = [
    { path: 'a/workflow/raiseissue', component: WorkflowRaiseIssueComponent },    
    { path: 'a/workflow/work/issue/:workflowId', component: WorkflowWorkIssueComponent }                        
];

export const routing = RouterModule.forChild(WorkflowRoutes);