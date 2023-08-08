import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AssignmentSidebarComponent } from './assignment-sidebar.component';
import { AssignmentsModule } from '../../assignments/assignments.module';
import { AssignmentSidebarRoutes } from './assignment-sidebar-routes.module';


@NgModule({
	declarations: [
		AssignmentSidebarComponent
	],
	imports: [
		AssignmentSidebarRoutes,
		AssignmentsModule,
		CommonModule
	]
})
export class AssignmentSidebarModule {
}
