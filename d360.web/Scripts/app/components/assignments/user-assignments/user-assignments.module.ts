import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserAssignmentsComponent } from './user-assignments.component';
import { TableModule } from 'primeng/table';
import { CoreModule } from '../../shared/core.module';
import { IgBadgeModule } from '../../shared/controls/badge/badge.module';
import { RouterModule } from '@angular/router';
import { AssignmentsModule } from '../assignments.module';
import { AssignmentsMultiPickerModule } from '../assignment-multi-picker/assignment-multi-picker.module';

@NgModule({
	declarations: [
		UserAssignmentsComponent
	],
	imports: [
		CommonModule,
		TableModule,
		CoreModule,
		IgBadgeModule,
		RouterModule,
		AssignmentsModule,
		AssignmentsMultiPickerModule
	],
	exports: [
		UserAssignmentsComponent
	]
})
export class UserAssignmentsModule { }
