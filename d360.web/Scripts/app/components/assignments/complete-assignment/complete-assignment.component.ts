import { Component, OnDestroy, OnInit } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { CompanySettingsService } from '../../../services/settings.service';
import { AssignmentItemStep, WorkflowItemStep } from '../../../models/workflow.model';

@Component({
	selector: 'd3s-complete-assignment',
	templateUrl: './complete-assignment.component.html',
	styleUrls: ['./complete-assignment.component.less']
})
export class CompleteAssignmentComponent extends BaseComponent implements OnInit, OnDestroy {

	isModalVisible: boolean = false;
	isAssignmentProgressSelected: boolean = false;
	modalTitle: string = 'Assignment';
	sidePanelOpen: boolean = false;
	workflowUid: string;
	stepUid: string;
	assetUid: string;
	sidePanelStorageKey: string = 'CompleteAssignment_' + this.settingsService.CurrentResourceID;
	sidePanel: string = 'asset-details';
	assignmentItemStep: AssignmentItemStep;

	constructor(protected settingsService: CompanySettingsService) {
		super(settingsService);
	}

	ngOnDestroy(): void {

	}

	ngOnInit(): void {
		this.isAssignmentProgressSelected = false;
	}

	openModal(details: {
		workflowUid: string,
		stepUid: string,
		assetUid: string
	}): void {
		if (details) {
			this.assetUid = details.assetUid;
			this.stepUid = details.stepUid;
			this.workflowUid = details.workflowUid;
		}
		this.isModalVisible = true;
	}

	submit(): void {
		console.log('submit');
	}

	showAssignment(): void {
		this.isAssignmentProgressSelected = false;
		this.modalTitle = 'Assignment';
	}

	showAssignmentProgress(): void {
		this.isAssignmentProgressSelected = true;
		this.modalTitle = 'Assignment Progress and Information';
	}

	stepClickChanged(value: { assignmentItemStep: AssignmentItemStep, open: boolean }): void {
		if (value.open) {
			this.sidePanel = 'step-details';
			this.assignmentItemStep = value.assignmentItemStep;
		} else {
			this.sidePanel = 'asset-details';
		}
	}
}
