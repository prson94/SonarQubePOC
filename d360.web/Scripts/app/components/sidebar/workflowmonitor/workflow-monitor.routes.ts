import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { WorkflowMonitorComponent } from './workflow-monitor.component';

const routes: Routes = [
    { path: ':objectType/:objectId', component: WorkflowMonitorComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class WorkflowMonitorRoutingModule { }