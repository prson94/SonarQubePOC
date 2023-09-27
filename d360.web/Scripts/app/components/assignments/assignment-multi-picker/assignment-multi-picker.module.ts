import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AssignmentsMultiPickerComponent } from './assignment-multi-picker.component';
import { SiteModalModule } from '../../shared/modal/gov-modal.module';
import { CoreModule } from '../../shared/core.module';
import { SidePanelModule } from '../../shared/sidepanel/side-panel.module';
import { TableModule } from 'primeng/table';
import { PropertyGroupModule } from '../../shared/controls/property-group/property-group.component';
import { AssignmentsModule } from '../assignments.module';
import { AssetDetailModule } from '../../shared/asset-detail/asset-detail.module';
import { SharedGridSelectionInfoModule } from '../../shared/grid-selection-info.component';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SearchFieldModule } from '../../shared/controls/search-field/search-field.component'


@NgModule({
	declarations: [
		AssignmentsMultiPickerComponent
	],
	exports: [
		AssignmentsMultiPickerComponent
	],
	imports: [
		AssetDetailModule,
		AssignmentsModule,
		CommonModule,
		CoreModule,
		PropertyGroupModule,
		SharedGridSelectionInfoModule,
		SidePanelModule,
		SiteModalModule,
		TableModule,
		SharedGridPagingInfoModule,
		SearchFieldModule
	]
})
export class AssignmentsMultiPickerModule {
}
