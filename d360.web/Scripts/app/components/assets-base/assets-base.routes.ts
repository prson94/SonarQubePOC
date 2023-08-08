import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuditComponent } from '../sidebar/audit/audit.component';
import { RelationshipsComponent } from '../sidebar/relationships/relationships.component';
import { DiagramComponent } from '../sidebar/visualization/diagram.component';
import { MonitorWorkflowComponent } from '../sidebar/workflowmonitor/monitor-workflow.component';
import { AssetsBaseComponent } from './assets-base.component';
import { AssignmentSidebarComponent } from '../sidebar/assignments/assignment-sidebar.component';

const routes: Routes = [
    { path: ':assetTypeUid', component: AssetsBaseComponent },
	{ path: ':assetTypeUid/workflowmonitor', component: MonitorWorkflowComponent },
	{ path: ':assetTypeUid/assignments', component: AssignmentSidebarComponent },
	{ path: ':assetTypeUid/diagrams', component: DiagramComponent },
	{ path: ':uid/log', component: AuditComponent },
	{ path: ':uid/relationships', component: RelationshipsComponent },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class AssetsBaseRoutingModule { }
