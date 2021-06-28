import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { WorkflowComponent } from './workflow.component';
import { WorkflowFormComponent } from './workflow-form.component';
import { WorkflowRaiseIssueComponent} from './workflow-raise-issue.component';
import { WorkflowViewDetailsComponent } from './workflow-view-details.component';
import { WorkflowNewDetailComponent } from './workflow-new-details.component';

const routes: Routes = [
    {
        path: '',
        component: WorkflowComponent,
        children: [
            {
                path: 'raiseissue', component: WorkflowRaiseIssueComponent
            },  
            {
                path: 'workflowlistnew/:workflowTypeId/:version/:stepId/:fromMail', component: WorkflowNewDetailComponent
            }, 
            {
                path: 'workflowlistnew/:workflowTypeId/:version/:stepId', component: WorkflowNewDetailComponent
            },            
            {
                path: 'form/:workflowId/:stepId/:itemId', component: WorkflowFormComponent
            }, 
            {
                path: 'details/:workflowInstance', component: WorkflowViewDetailsComponent
            },
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class WorkflowRoutingModule { }