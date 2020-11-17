import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { WorkflowComponent } from './workflow.component';
import { WorkflowFormComponent } from './workflow-form.component';
import { WorkflowRaiseIssueComponent} from './workflow-raise-issue.component';
import { WorkflowViewDetailsComponent } from './workflow-view-details.component';
import { WorkflowNewDetailComponent } from './workflow-new-details.component';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

const routes: Routes = [
    {
        path: '',
        component: WorkflowComponent,
        children: [
            {
                path: ':workflowUid', component: WorkflowViewDetailsComponent
            },
            {
                path: SiteUrlHelpers.SITE_URL_WORKFLOW_RAISE_ISSUE, component: WorkflowRaiseIssueComponent
            },   
            {
                path: SiteUrlHelpers.SITE_URL_WORKFLOW_LIST_V2 + '/:workflowTypeId/:version/:stepId/:fromMail', component: WorkflowNewDetailComponent
            }, 
            {
                path: SiteUrlHelpers.SITE_URL_WORKFLOW_LIST_V2 + '/:workflowTypeId/:version/:stepId', component: WorkflowNewDetailComponent
            },            
            {
                path: SiteUrlHelpers.SITE_URL_WORKFLOW_FORM + '/:workflowId/:stepId/:itemId', component: WorkflowFormComponent
            },                   
            {
                path: SiteUrlHelpers.SITE_URL_WORKFLOW_V2_VIEW_STATUS + '/:workflowId', component: WorkflowViewDetailsComponent
            },
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class WorkflowRoutingModule { }