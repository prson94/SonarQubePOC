import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DataCatalogGridComponent } from './data-catalog-grid.component';
import { DataCatalogComponent } from './data-catalog.component';
import { RouterModule, Routes } from '@angular/router';
import { TableModule } from 'primeng/table';
import { AngularSplitModule } from 'angular-split';
import { SidePanelModule } from '../shared/sidepanel/side-panel.module';
import { SearchFieldModule } from '../shared/controls/search-field/search-field.component';
import { AdvancedFiltersModule } from '../assets-grid/advanced-filtering/advanced-filtering.module';
import { FormsModule } from '@angular/forms';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { D3SSortIconModule } from '../shared/turbotable-sorticon.component';
import { DataCatalogSidePanelWrapperModule } from './sidepanel-wrapper/data-catalog-sidepanel-wrapper.module';
import { PopupMenuModule } from '../shared/controls/popup-menu/popup-menu.component';
import { ButtonModule } from 'primeng/button';
import { DirectivesModule } from '../../directives/directives.module';

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
		DirectivesModule,

		TableModule,
		AngularSplitModule,
		SidePanelModule,
		SearchFieldModule,
		AdvancedFiltersModule,
		SharedGridPagingInfoModule,
		D3SSortIconModule,
		DataCatalogSidePanelWrapperModule,
		PopupMenuModule,
		ButtonModule
	],
	exports: [RouterModule]
})
export class DataCatalogModule { }
