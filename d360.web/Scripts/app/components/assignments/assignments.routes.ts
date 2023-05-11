import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AssignmentsContainerComponent } from './assignments-container.component';
import { AssignmentListComponent } from './assignment-list/assignment-list.component';
import { WorkflowVersionListComponent } from './workflow-version-list/workflow-version-list.component';

const routes: Routes = [
	{
		path: '',
		component: AssignmentsContainerComponent,
		children: [
			{ path: '', component: AssignmentListComponent },
			{ path: 'by-workflow-version', component: WorkflowVersionListComponent }
		]
	}
];

@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule]
})
export class AssignmentsRoutingModule {
}
