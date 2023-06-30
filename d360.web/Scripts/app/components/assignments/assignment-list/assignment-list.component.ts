import { Component, OnInit, ViewChild } from '@angular/core';
import { CompanySettingsService } from '../../../services/settings.service';
import { SidePanelService } from '../../../services/side-panel.service';
import { IOutputData } from 'angular-split';
import { BaseComponent } from '../../shared/base.component';
import { SidePanelButton } from '../../../models/side-panel.model';
import { AssignmentGridComponent } from '../assignment-grid/assignment-grid.component';
import { CompleteAssignmentComponent } from '../complete-assignment/complete-assignment.component';
import { AssignmentItemStep, WorkflowAssignmentItem } from '../../../models/workflow.model';
import { AssignmentProgressComponent } from '../assignment-progress/assignment-progress.component';
import { Router } from '@angular/router';
import { AuthenticationService } from '../../../services/authentication.service';

/*global $localize*/

@Component({
	selector: 'd3s-assignment-list',
	templateUrl: './assignment-list.component.html',
	styleUrls: ['./assignment-list.component.less']
})
export class AssignmentListComponent extends BaseComponent implements OnInit {

	@ViewChild(AssignmentProgressComponent) assignmentProgressComponent: AssignmentProgressComponent;
	isRequestsFlow: boolean = false; // flag checks if url is requests
	flowContext: string = 'Assignment'; // to store flow context
	sidePanelStorageKey: string;
	showSidePanel: boolean = true;
	sidePanelOpen: boolean = false;
	sidePanelTab: string = 'information';
	showAssignmentHeader: boolean = true;
	isAdmin: boolean = false;
	resourceUid: any;
	secondarySidePanelOpen: boolean = false;
	selectedWorkflowItems: WorkflowAssignmentItem[] = [];
	assignmentItemStep: AssignmentItemStep;
	sidePanelButtons: SidePanelButton[] = [];
	sidePanelMultiSelectButtons: SidePanelButton[] = [
		new SidePanelButton({
			label: $localize`${this.selectedWorkflowItems?.length} Assignments Selected`,
			tooltip: $localize`${this.selectedWorkflowItems?.length} Assignments Selected`,
			disabledTooltip: null,
			nothingSelectedMessage: $localize`Select multiple assignments from the list to display its information`,
			notApplicableMessage: $localize`Information data is not available for the selected assignments`,
			multipleSelectedMessage: $localize`multiple assignments selected`,
			key: 'delete',
			icon: 'fa-info-circle',
			disabled: false,
			visible: true,
			needsSelection: true
		})
	];

	@ViewChild('assignmentGridComponent') assignmentGridComponent: AssignmentGridComponent;
	@ViewChild('completeAssignmentComponent') completeAssignmentComponent: CompleteAssignmentComponent;
	secondarySidePanelObjectUid: string;
	secondarySidePanelObjectType: string;

	constructor(
		public sidePanelService: SidePanelService,
		private router: Router,
		private authenticationService: AuthenticationService,
		protected settingsService: CompanySettingsService) {
		super(settingsService);
	}

	ngOnInit(): void {
		this.isAdmin = this.authenticationService.isAdmin;
		if (this.router.url === '/requests') {
			this.flowContext = 'Request';
			this.isRequestsFlow = true;
		}
		this.setFlowSpecificDetails();
	}

	setFlowSpecificDetails(): void {
		this.sidePanelStorageKey = this.flowContext + '_' + this.settingsService.CurrentResourceID;
		this.sidePanelButtons = [
			new SidePanelButton({
				label: $localize`${this.flowContext} Progress`,
				tooltip: $localize`${this.flowContext} Progress`,
				disabledTooltip: null,
				nothingSelectedMessage: $localize`Select ${this.flowContext} from the list to display its progress`,
				notApplicableMessage: $localize`Progress data is not available for the selected Assignment`,
				multipleSelectedMessage: $localize`Select a single ${this.flowContext} to display it’s progress`,
				key: 'progress',
				icon: 'fa-step-forward',
				disabled: false,
				visible: true,
				needsSelection: true
			}), new SidePanelButton({
				label: $localize`${this.flowContext} Information`,
				tooltip: $localize`${this.flowContext} Information`,
				disabledTooltip: null,
				nothingSelectedMessage: $localize`Select ${this.flowContext} from the list to display its information`,
				notApplicableMessage: $localize`Information data is not available for the selected Assignment`,
				multipleSelectedMessage: $localize`Select a single ${this.flowContext} to display it’s information`,
				key: 'information',
				icon: 'fa-info-circle',
				disabled: false,
				visible: true,
				needsSelection: true
			})
		];
	}

	selectRow(rows: WorkflowAssignmentItem[]): void {
		this.secondarySidePanelOpen = false;
		if (this.isAdmin && !this.isRequestsFlow && rows?.length > 1) {
			this.sidePanelMultiSelectButtons[0].label = $localize`${this.selectedWorkflowItems?.length} Assignments Selected`;
			this.sidePanelMultiSelectButtons[0].tooltip = $localize`${this.selectedWorkflowItems?.length} Assignments Selected`;
			this.sidePanelTab = 'delete';
		}
	}

	getSidePanelWidth(): number {
		return this.sidePanelService.getSidePanelWidth(this.sidePanelOpen, this.sidePanelStorageKey);
	}

	getSidePanelMaxWidth(): number {
		return this.sidePanelService.getSidePanelMaxWidth(this.sidePanelOpen);
	}

	getSidePanelMinWidth(): number {
		return this.sidePanelService.getSidePanelMinWidth(this.sidePanelOpen);
	}

	onSidePanelDragEnd(sidePanelStorageKey: string, event: IOutputData): void {
		this.sidePanelService.onSidePanelDragEnd(sidePanelStorageKey, event);
	}

	workflowSelectionChanged(workflowMonitorItems: WorkflowAssignmentItem[]): void {
		this.selectedWorkflowItems = workflowMonitorItems;
		this.selectRow(workflowMonitorItems);
	}

	sidePanelLinkClicked(value: { objectType: string, objectUid: string }): void {
		this.secondarySidePanelOpen = true;
		this.secondarySidePanelObjectUid = value.objectUid;
		this.secondarySidePanelObjectType = value.objectType;
	}

	deleteAssignments(event: boolean): void {
		if (event) {
			this.assignmentGridComponent.showDeletionModal = true;
		}
	}

	stepClicked(assignmentItemStep: AssignmentItemStep) {
		this.secondarySidePanelOpen = true;
		this.assignmentItemStep = assignmentItemStep;
		this.secondarySidePanelObjectType = 'step-details';
	}

	closeSecondarySidePanel() {
		this.secondarySidePanelOpen = false;
		this.assignmentProgressComponent.deselectWorkflowSteps();
	}
}
