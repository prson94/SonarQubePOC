import { NgModule } from '@angular/core'
import { CommonModule } from '@angular/common'
import { AssignmentsContainerComponent } from './assignments-container.component'
import { AssignmentListComponent } from './assignment-list/assignment-list.component'
import { RouterModule } from '@angular/router'
import { AssignmentsRoutingModule } from './assignments.routes'


@NgModule({
	declarations: [
		AssignmentsContainerComponent,
		AssignmentListComponent
	],
	imports: [
		CommonModule,
		RouterModule,
		AssignmentsRoutingModule
	]
})
export class AssignmentsModule {
}
