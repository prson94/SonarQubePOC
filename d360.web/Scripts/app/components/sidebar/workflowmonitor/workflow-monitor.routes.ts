import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MonitorWorkflowComponent } from './monitor-workflow.component';

const routes: Routes = [
	{ path: '', component: MonitorWorkflowComponent }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class WorkflowMonitorRoutingModule { }