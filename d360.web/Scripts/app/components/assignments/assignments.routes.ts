import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AssignmentsContainerComponent } from './assignments-container.component';
import { AssignmentListComponent } from './assignment-list/assignment-list.component';
import { ByWorkflowVersionListComponent } from './by-workflow-version-list/by-workflow-version-list.component';
import {
	AssignmentDetailsContainerComponent
} from './assignment-details-container/assignment-details-container.component';
import { AssignmentDetailsGuard } from '../../guards/feature-flag.service';

const routes: Routes = [
	{
		path: '',
		component: AssignmentsContainerComponent,
		children: [
			{ path: '', component: AssignmentListComponent },
			{ path: 'by-workflow-version', component: ByWorkflowVersionListComponent },
			{
				path: ':assignmentUid',
				component: AssignmentDetailsContainerComponent,
				canActivate: [AssignmentDetailsGuard]
			}
		]
	}
];

@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule]
})
export class AssignmentsRoutingModule {
}
