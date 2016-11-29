import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AdminWorkflowComponent } from './admin-workflow.component';

const routes: Routes = [
    { path: '', component: AdminWorkflowComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AdminWorkflowRoutingModule { }

