import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule } from "@angular/forms";
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { CheckboxModule } from 'primeng/checkbox';
import { DirectivesModule } from '../../../../directives/directives.module';
import { CoreModule } from '../../../shared/core.module';
import { SiteModalModule } from '../../../shared/modal/gov-modal.module';
import { ConfigurationAssetTypeDeletePageComponent } from './configuration-asset-type-delete-page.component';

@NgModule({
	imports: [
		CommonModule,
		FormsModule,
		ButtonModule,
		DirectivesModule,
		CoreModule,
		TooltipModule,
		CheckboxModule,

		SiteModalModule,
	],
	declarations: [
		ConfigurationAssetTypeDeletePageComponent
	],
	exports: [
		ConfigurationAssetTypeDeletePageComponent
	],
})
export class ConfigurationAssetTypeDeletePageComponentModule { }
