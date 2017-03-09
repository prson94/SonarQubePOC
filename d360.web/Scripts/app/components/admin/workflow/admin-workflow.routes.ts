import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminWorkflowComponent } from './admin-workflow.component';
import { AdminWorkflowNewComponent } from './admin-workflow-new.component';

const routes: Routes = [
    { path: '', component: AdminWorkflowComponent },
    { path: 'new', component: AdminWorkflowNewComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminWorkflowRoutingModule { }

