import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { SidePanelService } from '../../../services/side-panel.service';
import { IOutputData } from 'angular-split';
import { BaseComponent } from '../../shared/base.component';
import { SidePanelButton } from '../../../models/side-panel.model';
import { SecondaryNavItem } from '../../../models/secondaryNav.model';
import { AssignmentGridComponent } from '../assignment-grid/assignment-grid.component';
import { CompleteAssignmentComponent } from '../complete-assignment/complete-assignment.component';
import { AssignmentItemStep, WorkflowAssignmentItem } from '../../../models/workflow.model';
import { AssignmentProgressComponent } from '../assignment-progress/assignment-progress.component';

@Component({
	selector: 'd3s-assignment-list',
	templateUrl: './assignment-list.component.html',
	styleUrls: ['./assignment-list.component.less']
})
export class AssignmentListComponent extends BaseComponent implements OnInit, OnDestroy {

	@ViewChild(AssignmentProgressComponent) assignmentProgressComponent: AssignmentProgressComponent;
	showSidePanel: boolean = true;
	sidePanelOpen: boolean = false;
	sidePanelTab: string = 'information';
	sidePanelStorageKey: string = 'AssignmentList_' + this.settingsService.CurrentResourceID;
	resourceUid: any;
	secondarySidePanelOpen: boolean = false;
	selectedWorkflowItems: WorkflowAssignmentItem[] = [];
	assignmentItemStep: AssignmentItemStep;
	sidePanelButtons: SidePanelButton[] = [
		new SidePanelButton({
			label: $localize`Assignment Progress`,
			tooltip: $localize`Assignment Progress`,
			disabledTooltip: null,
			nothingSelectedMessage: $localize`Select an Assignment from the list to display its progress`,
			notApplicableMessage: $localize`Progress data is not available for the selected Assignment`,
			multipleSelectedMessage: $localize`Select a single Assignment to display it’s progress`,
			key: 'progress',
			icon: 'fa-step-forward',
			disabled: false,
			visible: true,
			needsSelection: true
		}), new SidePanelButton({
			label: $localize`Assignment Information`,
			tooltip: $localize`Assignment Information`,
			disabledTooltip: null,
			nothingSelectedMessage: $localize`Select an Assignment from the list to display its information`,
			notApplicableMessage: $localize`Information data is not available for the selected Assignment`,
			multipleSelectedMessage: $localize`Select a single Assignment to display it’s information`,
			key: 'information',
			icon: 'fa-info-circle',
			disabled: false,
			visible: true,
			needsSelection: true
		})
	];
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

	constructor(headerBreadcrumbService: HeaderBreadcrumbService,
				private titleService: Title,
				public sidePanelService: SidePanelService,
				secondaryNavService: SecondaryNavService,
				protected settingsService: CompanySettingsService) {
		super(settingsService);
		this.secondaryNavService = secondaryNavService;
		this.breadcrumbsService = headerBreadcrumbService;
	}

	ngOnInit(): void {
		this.clearSidebar();
		this.displayBreadCrumbs();
	}

	selectRow(rows: WorkflowAssignmentItem[]): void {
		this.secondarySidePanelOpen = false;
		if (rows && rows.length > 1) {
			this.sidePanelMultiSelectButtons[0].label = $localize`${this.selectedWorkflowItems?.length} Assignments Selected`;
			this.sidePanelMultiSelectButtons[0].tooltip = $localize`${this.selectedWorkflowItems?.length} Assignments Selected`;
			this.sidePanelTab = 'delete';
		}
	}

	displayBreadCrumbs(): void {
		this.setBrowserTitle(this.titleService, 'Assignments');
		this.breadcrumbsService.clearBreadcrumbs();
		this.breadcrumbsService.clearCurrentObjectInfo();
		this.secondaryNavService.clearItems();
		this.secondaryNavService.clearCurrentObject();
		this.secondaryNavService.setCurrentArea('Assignments', 'fa-list-ul', $localize`Assignments`);
		this.secondaryNavService.showHeader(true);
		this.fieldNav = new SecondaryNavItem(
			$localize`By Workflow Version`,
			'byWorkflowVersion',
			null,
			'/assignments/by-workflow-version', null, 0);
		this.secondaryNavService.showItem(this.fieldNav);
	}

	ngOnDestroy(): void {

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

	openCompleteAssignment(): void {
		this.completeAssignmentComponent.openModal(null);
	}

	stepClicked(value: { assignmentItemStep: AssignmentItemStep; open: boolean }) {
		this.secondarySidePanelOpen = value.open;
		this.assignmentItemStep = value.assignmentItemStep;
		this.secondarySidePanelObjectType = 'step-details';
	}

	closeSecondarySidePanel() {
		this.secondarySidePanelOpen = false;
		this.assignmentProgressComponent.deselectWorkflowSteps();
	}
}
