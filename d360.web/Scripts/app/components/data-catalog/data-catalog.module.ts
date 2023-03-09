import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DataCatalogGridComponent } from './data-catalog-grid.component';
import { DataCatalogComponent } from './data-catalog.component';
import { RouterModule, Routes } from '@angular/router';
import { TableModule } from 'primeng/table';
import { DataCatalogSidePanelWrapperComponent } from './sidepanel-wrapper/data-catalog-sidepanel-wrapper.component';
import { AngularSplitModule } from 'angular-split';
import { SidePanelModule } from '../shared/sidepanel/side-panel.module';
import { SearchFieldModule } from '../shared/controls/search-field/search-field.component';
import { AdvancedFiltersModule } from '../assets-grid/advanced-filtering/advanced-filtering.module';
import { FormsModule } from '@angular/forms';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { D3SSortIconModule } from '../shared/turbotable-sorticon.component';
import { DataCatalogSidePanelWrapperModule } from './sidepanel-wrapper/data-catalog-sidepanel-wrapper.module';

const routes: Routes = [
	{ path: '', component: DataCatalogComponent }
];

@NgModule({
	declarations: [
		DataCatalogGridComponent,
		DataCatalogComponent
	],
	imports: [
		RouterModule.forChild(routes),
		CommonModule,
		FormsModule,

		TableModule,
		AngularSplitModule,
		SidePanelModule,
		SearchFieldModule,
		AdvancedFiltersModule,
		SharedGridPagingInfoModule,
		D3SSortIconModule,
		DataCatalogSidePanelWrapperModule
	],
	exports: [RouterModule]
})
export class DataCatalogModule { }
