import { NgModule } from '@angular/core';
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { TooltipModule } from 'primeng/tooltip';
import { FieldTypeDetailsComponent } from './field-type-details.component';
import { PropertyGroupModule } from '../../controls/property-group/property-group.component';
import { PipesModule } from '../../../../pipes/pipes.module';
import { CoreModule } from '../../core.module';


@NgModule({
	imports: [
		CommonModule,
		FormsModule,
		CoreModule,
		PipesModule,
		PropertyGroupModule,
		TooltipModule
	],
	declarations: [
		FieldTypeDetailsComponent
	],
	exports: [
		FieldTypeDetailsComponent
	]
})
export class FieldTypeDetailModule { }