import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DataCatalogGridComponent } from './data-catalog-grid.component';
import { DataCatalogComponent } from './data-catalog.component';
import { RouterModule, Routes } from '@angular/router';
import { TableModule } from 'primeng/table';
import { DataCatalogSidePanelWrapperComponent } from './sidepanel-wrapper/data-catalog-sidepanel-wrapper.component';
import { AngularSplitModule } from 'angular-split';
import { SidePanelModule } from '../shared/sidepanel/side-panel.module';

const routes: Routes = [
	{ path: '', component: DataCatalogComponent }
];

@NgModule({
	declarations: [
		DataCatalogGridComponent,
		DataCatalogComponent,

		DataCatalogSidePanelWrapperComponent
	],
	imports: [
		RouterModule.forChild(routes),
		CommonModule,

		TableModule,
		AngularSplitModule,
		SidePanelModule
	],
	exports: [RouterModule]
})
export class DataCatalogModule { }
