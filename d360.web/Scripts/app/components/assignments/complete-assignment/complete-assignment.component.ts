import { Component, OnInit } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { CompanySettingsService } from '../../../services/settings.service';
import { AssignmentItemStep } from '../../../models/workflow.model';

@Component({
	selector: 'd3s-complete-assignment',
	templateUrl: './complete-assignment.component.html',
	styleUrls: ['./complete-assignment.component.less']
})
export class CompleteAssignmentComponent extends BaseComponent implements OnInit {

	isModalVisible: boolean = false;
	isAssignmentProgressSelected: boolean = false;
	modalTitle: string = 'Assignment';
	sidePanelOpen: boolean = false;
	workflowItemUid: string;
	stepUid: string;
	assetId: number;
	sidePanelStorageKey: string = 'CompleteAssignment_' + this.settingsService.CurrentResourceID;
	sidePanel: string = 'asset-details';
	assignmentItemStep: AssignmentItemStep;

	constructor(protected settingsService: CompanySettingsService) {
		super(settingsService);
	}

	ngOnInit(): void {
		this.isAssignmentProgressSelected = false;
	}

	openModal(details: {
		workflowItemUid: string,
		stepUid: string,
		assetId: number
	}): void {
		if (details) {
			this.assetId = details.assetId;
			this.stepUid = details.stepUid;
			this.workflowItemUid = details.workflowItemUid;
		}
		this.isModalVisible = true;
	}

	submit(): void {

	}

	showAssignment(): void {
		this.isAssignmentProgressSelected = false;
		this.modalTitle = 'Assignment';
	}

	showAssignmentProgress(): void {
		this.isAssignmentProgressSelected = true;
		this.modalTitle = 'Assignment Progress and Information';
	}

	stepClickChanged(assignmentItemStep: AssignmentItemStep): void {
		this.sidePanel = 'step-details';
		this.assignmentItemStep = assignmentItemStep;
	}
}
