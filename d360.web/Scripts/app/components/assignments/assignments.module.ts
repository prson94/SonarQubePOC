import { NgModule } from '@angular/core'
import { CommonModule } from '@angular/common'
import { AssignmentsContainerComponent } from './assignments-container.component'
import { AssignmentListComponent } from './assignment-list/assignment-list.component'
import { RouterModule } from '@angular/router'
import { AssignmentsRoutingModule } from './assignments.routes'
import { AdvancedFiltersModule } from '../assets-grid/advanced-filtering/advanced-filtering.module'
import { AngularSplitModule } from 'angular-split'
import { AssetDetailModule } from '../shared/asset-detail/asset-detail.module'
import { ButtonModule } from '../../directives/ig-button-directive'
import { CheckboxModule } from 'primeng/checkbox'
import { D3SSortIconModule } from '../shared/turbotable-sorticon.component'
import { DirectivesModule } from '../../directives/directives.module'
import { IgBadgeModule } from '../shared/controls/badge/badge.module'
import { PopupMenuModule } from '../shared/controls/popup-menu/popup-menu.component'
import { SearchFieldModule } from '../shared/controls/search-field/search-field.component'
import { SemanticsModule } from '../semantic/semantics.module'
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component'
import { SharedModule } from 'primeng/api'
import { SidePanelModule } from '../shared/sidepanel/side-panel.module'
import { TableModule } from 'primeng/table'
import { TooltipModule } from 'primeng/tooltip'
import { FormsModule } from '@angular/forms'
import { WorkflowMonitorModule } from '../workflowmonitor/workflowmonitor.module'
import { AssignmentGridComponent } from './assignment-grid/assignment-grid.component'
import { D3SColumnFilterModule } from '../shared/turbotable-column-filter.component'
import { AssignmentInformationComponent } from './assignment-information/assignment-information.component'
import { AssignmentProgressComponent } from './assignment-progress/assignment-progress.component'
import { PropertyGroupModule } from '../shared/controls/property-group/property-group.component';
import { WorkflowVersionListComponent } from './workflow-version-list/workflow-version-list.component'


@NgModule({
	declarations: [
		AssignmentGridComponent,
		AssignmentInformationComponent,
		AssignmentListComponent,
		AssignmentProgressComponent,
		AssignmentsContainerComponent,
  WorkflowVersionListComponent
	],
	imports: [
		AdvancedFiltersModule,
		AngularSplitModule,
		AssetDetailModule,
		AssignmentsRoutingModule,
		ButtonModule,
		CheckboxModule,
		CommonModule,
		D3SColumnFilterModule,
		D3SSortIconModule,
		DirectivesModule,
		FormsModule,
		IgBadgeModule,
		PopupMenuModule,
		PropertyGroupModule,
		RouterModule,
		SearchFieldModule,
		SemanticsModule,
		SharedGridPagingInfoModule,
		SharedModule,
		SidePanelModule,
		TableModule,
		TooltipModule,
		WorkflowMonitorModule
	]
})
export class AssignmentsModule {
}
