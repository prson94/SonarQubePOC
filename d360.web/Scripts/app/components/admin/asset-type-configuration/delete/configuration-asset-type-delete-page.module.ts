import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { DirectivesModule } from '../../../../directives/directives.module';
import { CoreModule } from '../../../shared/core.module';
import { SiteModalModule } from '../../../shared/modal/gov-modal.module';
import { ConfigurationAssetTypeDeletePageComponent } from './configuration-asset-type-delete-page.component';

@NgModule({
	imports: [
		CommonModule,
		ButtonModule,
		DirectivesModule,
		CoreModule,
		TooltipModule,

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
