import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AssignmentsContainerComponent } from './assignments-container.component';
import { AssignmentListComponent } from './assignment-list/assignment-list.component';
import { ByWorkflowVersionComponent } from './by-workflow-version/by-workflow-version.component';

const routes: Routes = [
	{
		path: '',
		component: AssignmentsContainerComponent,
		children: [
			{ path: '', component: AssignmentListComponent },
			{ path: 'by-workflow-version', component: ByWorkflowVersionComponent }
		]
	}
];

@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule]
})
export class AssignmentsRoutingModule {
}
