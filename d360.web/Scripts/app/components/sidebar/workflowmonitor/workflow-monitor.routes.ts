import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { MonitorWorkflowComponent } from './monitor-workflow.component';

const routes: Routes = [
    { path: ':objectType/:objectId', component: MonitorWorkflowComponent },
    { path: ':assetUid', component: MonitorWorkflowComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class WorkflowMonitorRoutingModule { }