import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AssignmentsContainerComponent } from './assignments-container.component';
import { AssignmentListComponent } from './assignment-list/assignment-list.component';
import { RouterModule } from '@angular/router';
import { AssignmentsRoutingModule } from './assignments.routes';
import { AdvancedFiltersModule } from '../assets-grid/advanced-filtering/advanced-filtering.module';
import { AngularSplitModule } from 'angular-split';
import { AssetDetailModule } from '../shared/asset-detail/asset-detail.module';
import { ButtonModule } from '../../directives/ig-button-directive';
import { CheckboxModule } from 'primeng/checkbox';
import { D3SSortIconModule } from '../shared/turbotable-sorticon.component';
import { DirectivesModule } from '../../directives/directives.module';
import { IgBadgeModule } from '../shared/controls/badge/badge.module';
import { PopupMenuModule } from '../shared/controls/popup-menu/popup-menu.component';
import { SearchFieldModule } from '../shared/controls/search-field/search-field.component';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedModule } from 'primeng/api';
import { SidePanelModule } from '../shared/sidepanel/side-panel.module';
import { TableModule } from 'primeng/table';
import { TooltipModule } from 'primeng/tooltip';
import { FormsModule } from '@angular/forms';
import { AssignmentGridComponent } from './assignment-grid/assignment-grid.component';
import { D3SColumnFilterModule } from '../shared/turbotable-column-filter.component';
import { AssignmentInformationComponent } from './assignment-information/assignment-information.component';
import { AssignmentProgressComponent } from './assignment-progress/assignment-progress.component';
import { PropertyGroupModule } from '../shared/controls/property-group/property-group.component';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { AssignmentMultiDeleteComponent } from './assignment-multi-delete/assignment-multi-delete.component';
import {
	AssignmentInformationGeneralComponent
} from './assignment-information/assignment-information-general/assignment-information-general.component';
import {
	AssignmentInformationRequestComponent
} from './assignment-information/assignment-information-request/assignment-information-request.component';
import { CoreModule } from '../shared/core.module';
import {
	AssignmentProgressStepComponent
} from './assignment-progress/assignment-progress-step/assignment-progress-step.component';
import { CompleteAssignmentComponent } from './complete-assignment/complete-assignment.component';
import { SiteModalModule } from '../shared/modal/gov-modal.module';
import {
	AssignmentProgressStepDetailsComponent
} from './assignment-progress-step-details/assignment-progress-step-details.component';
import { WorkflowDiagramModule } from '../shared/diagram/workflow/workflow-diagram.module';
import { WorkflowInformationComponent } from './workflow-information/workflow-information.component';
import {
	WorkflowInformationGeneralComponent
} from './workflow-information/workflow-information-general/workflow-information-general.component';
import {
	WorkflowInformationDiagramComponent
} from './workflow-information/workflow-information-diagram/workflow-information-diagram.component';
import {
	AssignmentStepFieldChangeDetailsComponent
} from './assignment-progress-step-details/assignment-step-field-change-details/assignment-step-field-change-details.component';
import {
	AssignmentStepEmailDetailsComponent
} from './assignment-progress-step-details/assignment-step-email-details/assignment-step-email-details.component';
import { PipesModule } from '../../pipes/pipes.module';
import {
	AssignmentStepHttpDetailsComponent
} from './assignment-progress-step-details/assignment-step-http-details/assignment-step-http-details.component';
import {
	AssignmentStepHttpResponseDetailsComponent
} from './assignment-progress-step-details/assignment-step-http-response-details/assignment-step-http-response-details.component';
import {
	AssignmentStepRelationshipChangeDetailsComponent
} from './assignment-progress-step-details/assignment-step-relationship-change-details/assignment-step-relationship-change-details.component';


@NgModule({
	declarations: [
		AssignmentGridComponent,
		AssignmentInformationComponent,
		AssignmentInformationGeneralComponent,
		AssignmentInformationRequestComponent,
		AssignmentListComponent,
		AssignmentMultiDeleteComponent,
		AssignmentProgressComponent,
		AssignmentProgressStepComponent,
		AssignmentProgressStepDetailsComponent,
		AssignmentsContainerComponent,
		CompleteAssignmentComponent,
		WorkflowInformationComponent,
		WorkflowInformationDiagramComponent,
		WorkflowInformationGeneralComponent,
		AssignmentStepFieldChangeDetailsComponent,
		AssignmentStepEmailDetailsComponent,
		AssignmentStepHttpDetailsComponent,
		AssignmentStepHttpResponseDetailsComponent,
		AssignmentStepRelationshipChangeDetailsComponent
	],
	imports: [
		AdvancedFiltersModule,
		AngularSplitModule,
		AssetDetailModule,
		AssignmentsRoutingModule,
		ButtonModule,
		CheckboxModule,
		CommonModule,
		CoreModule,
		D3SColumnFilterModule,
		D3SSortIconModule,
		DirectivesModule,
		FormsModule,
		IgBadgeModule,
		PopupMenuModule,
		PropertyGroupModule,
		RouterModule,
		SearchFieldModule,
		SharedDeleteFormModule,
		SharedGridPagingInfoModule,
		SharedModule,
		SidePanelModule,
		SiteModalModule,
		TableModule,
		TooltipModule,
		WorkflowDiagramModule,
		PipesModule
	]
})
export class AssignmentsModule {
}
