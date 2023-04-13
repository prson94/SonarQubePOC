import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RelationshipTypeDetailPageComponent } from './relationship-type-detail-page.component';
import { PropertyGroupModule } from '../../../shared/controls/property-group/property-group.component';
import { CoreModule } from '../../../shared/core.module';
import { DirectivesModule } from '../../../../directives/directives.module';
import { RouterModule, Routes } from '@angular/router';
import { SidePanelModule } from '../../../shared/sidepanel/side-panel.module';
import { AngularSplitModule } from 'angular-split';
import { RelationshipTypeDetailModule } from '../relationship-type-detail/relationship-type-detail.module';

const routes: Routes = [
	{ path: '', component: RelationshipTypeDetailPageComponent },
];

@NgModule({
	declarations: [
		RelationshipTypeDetailPageComponent
	],
	imports: [
		CommonModule,
		CoreModule,
		PropertyGroupModule,
		DirectivesModule,
		RouterModule.forChild(routes),
		SidePanelModule,
		AngularSplitModule,
		RelationshipTypeDetailModule
	],
	exports: [
		RelationshipTypeDetailPageComponent
	]
})
export class RelationshipTypeDetailPageModule { }
