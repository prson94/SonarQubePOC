import { WorkflowRaiseIssueComponent} from './workflow-raise-issue.component';
import { WorkflowWorkItemComponent } from './workflow-work-item.component';
import { WorkflowViewStatusComponent } from './workflow-view-status.component';
import { RouterModule } from '@angular/router';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

export const WorkflowRoutes = [
    {
        path: SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT + '/' + SiteUrlHelpers.SITE_URL_WORKFLOW_RAISE_ISSUE, component: WorkflowRaiseIssueComponent
    },    
    {
        path: SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT + '/' + SiteUrlHelpers.SITE_URL_WORKFLOW_VIEW_ITEM + '/:workflowType/:workflowId', component: WorkflowWorkItemComponent
    },    
    {
        path: SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT + '/' + SiteUrlHelpers.SITE_URL_WORKFLOW_VIEW_STATUS + '/:workflowId', component: WorkflowViewStatusComponent
    }   
];

export const routing = RouterModule.forChild(WorkflowRoutes);