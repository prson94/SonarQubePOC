import { WorkflowRaiseIssueComponent} from './workflow-raise-issue.component';
import { WorkflowWorkIssueComponent } from './workflow-work-issue.component';
import { RouterModule } from '@angular/router';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const WorkflowRoutes = [
    {
        path: SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT + '/' + SiteUrlHelpers.SITE_URL_WORKFLOW_RAISE_ISSUE, component: WorkflowRaiseIssueComponent
    },    
    {
        path: SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT + '/' + SiteUrlHelpers.SITE_URL_WORKFLOW_VIEW_ISSUE + '/:workflowId', component: WorkflowWorkIssueComponent
    }   
];

export const routing = RouterModule.forChild(WorkflowRoutes);