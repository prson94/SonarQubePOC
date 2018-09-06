import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { WorkflowMonitorComponent } from './workflowmonitor.component';

const routes: Routes = [
    { path: '', component: WorkflowMonitorComponent },    
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class WorkflowMonitorRoutingModule { }

