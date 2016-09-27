import { WorkflowRaiseIssueComponent} from './workflow-raise-issue.component';
import { RouterModule } from '@angular/router';

export const WorkflowRoutes = [
    { path: 'a/workflow/raiseissue', component: WorkflowRaiseIssueComponent }
];

export const routing = RouterModule.forChild(WorkflowRoutes);