import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AssignmentsContainerComponent } from './assignments-container.component'
import { AssignmentListComponent } from './assignment-list/assignment-list.component'

const routes: Routes = [
	{
		path: '',
		component: AssignmentsContainerComponent,
		children: [
			{ path: '', component: AssignmentListComponent}
		]
	},
];

@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule],
})
export class AssignmentsRoutingModule { }