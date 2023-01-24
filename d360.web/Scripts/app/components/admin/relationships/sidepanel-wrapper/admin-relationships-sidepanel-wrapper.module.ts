import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminRelationshipsSidePanelWrapperComponent } from './admin-relationships-sidepanel-wrapper.component';
import { SidePanelModule } from '../../../shared/sidepanel/side-panel.module';
import { AngularSplitModule } from 'angular-split';
import { RelationshipTypeDetailModule } from '../relationship-type-detail/relationship-type-detail.module';



@NgModule({
	declarations: [
		AdminRelationshipsSidePanelWrapperComponent
	],
	imports: [
		CommonModule,
		SidePanelModule,
		AngularSplitModule,
		RelationshipTypeDetailModule
	],
	exports: [
		AdminRelationshipsSidePanelWrapperComponent
	]
})
export class AdminRelationshipsSidePanelWrapperModule { }
