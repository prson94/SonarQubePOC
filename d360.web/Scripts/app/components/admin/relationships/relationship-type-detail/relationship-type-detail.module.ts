import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RelationshipTypeDetailComponent } from './relationship-type-detail.component';
import { PropertyGroupModule } from '../../../shared/controls/property-group/property-group.component';
import { CoreModule } from '../../../shared/core.module';



@NgModule({
	declarations: [
		RelationshipTypeDetailComponent
	],
	imports: [
		CommonModule,
		CoreModule,
		PropertyGroupModule
	],
	exports: [
		RelationshipTypeDetailComponent
	]
})
export class RelationshipTypeDetailModule { }
