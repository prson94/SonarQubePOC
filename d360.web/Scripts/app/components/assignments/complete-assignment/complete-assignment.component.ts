import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
	selector: 'd3s-complete-assignment',
	templateUrl: './complete-assignment.component.html',
	styleUrls: ['./complete-assignment.component.less']
})
export class CompleteAssignmentComponent extends BaseComponent implements OnInit, OnDestroy {

	isModalVisible: boolean = false;
	isAssignmentProgressSelected: boolean = false;
	modalTitle: string = 'Assignment';
	sidePanelOpen: boolean = true;
	assetId: number
	sidePanelStorageKey: string = 'CompleteAssignment_' + this.settingsService.CurrentResourceID;

	constructor(protected settingsService: CompanySettingsService) {
		super(settingsService);
	}

	ngOnDestroy(): void {

    }

	ngOnInit(): void {
		this.isAssignmentProgressSelected = false;
	}

	openModal(details: { workflowId; stepId; assetId }): void {
		if(details && details.assetId) {
			this.assetId = details.assetId
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

}
