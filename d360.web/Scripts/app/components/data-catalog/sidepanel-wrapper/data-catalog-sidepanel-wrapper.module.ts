import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { AngularSplitModule } from 'angular-split';
import { FormsModule } from '@angular/forms';
import { SidePanelModule } from '../../shared/sidepanel/side-panel.module';
import { DataCatalogSidePanelWrapperComponent } from './data-catalog-sidepanel-wrapper.component';
import { AssetDetailModule } from '../../shared/asset-detail/asset-detail.module';

@NgModule({
	declarations: [
		DataCatalogSidePanelWrapperComponent
	],
	exports: [
		DataCatalogSidePanelWrapperComponent
	],
	imports: [
		CommonModule,
		FormsModule,

		TableModule,
		AngularSplitModule,
		SidePanelModule,
		AssetDetailModule
	]
})
export class DataCatalogSidePanelWrapperModule { }
