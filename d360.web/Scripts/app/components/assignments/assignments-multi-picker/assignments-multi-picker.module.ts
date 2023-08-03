import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AssignmentsMultiPickerComponent } from './assignments-multi-picker.component';
import { SiteModalModule } from '../../shared/modal/gov-modal.module';
import { CoreModule } from '../../shared/core.module';
import { AssetDetailModule } from '../../shared/asset-detail/asset-detail.module';
import { SidePanelModule } from '../../shared/sidepanel/side-panel.module';
import { TableModule } from 'primeng/table';
import { PropertyGroupModule } from '../../shared/controls/property-group/property-group.component';



@NgModule({
	declarations: [
		AssignmentsMultiPickerComponent
	],
	exports: [
		AssignmentsMultiPickerComponent
	],
	imports: [
		CommonModule,
		SiteModalModule,
		CoreModule,
		AssetDetailModule,
		SidePanelModule,

		TableModule,
		PropertyGroupModule
	]
})
export class AssignmentsMultiPickerModule { }
