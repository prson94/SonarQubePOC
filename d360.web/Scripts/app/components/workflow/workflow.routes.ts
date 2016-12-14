import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { WorkflowComponent } from './workflow.component';
import { WorkflowRaiseIssueComponent} from './workflow-raise-issue.component';
import { WorkflowWorkItemComponent } from './workflow-work-item.component';
import { WorkflowViewStatusComponent } from './workflow-view-status.component';
import { WorkflowDetailComponent } from './workflow-detail.component';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

const routes: Routes = [
    {
        path: '',
        component: WorkflowComponent,
        children: [
            {
                path: SiteUrlHelpers.SITE_URL_WORKFLOW_RAISE_ISSUE, component: WorkflowRaiseIssueComponent
            },
            {
                path: SiteUrlHelpers.SITE_URL_WORKFLOW_VIEW_ITEM + '/:workflowType/:workflowId', component: WorkflowWorkItemComponent
            },
            {
                path: SiteUrlHelpers.SITE_URL_WORKFLOW_VIEW_STATUS + '/:workflowId', component: WorkflowViewStatusComponent
            },
            {
                path: SiteUrlHelpers.SITE_URL_WORKFLOW_LIST + '/:workflowType', component: WorkflowDetailComponent
            }   
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class WorkflowRoutingModule { }