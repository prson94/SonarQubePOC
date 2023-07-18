import { Component, Input, OnChanges, OnInit } from '@angular/core';
import { WorkflowService } from '../../../../services/workflow.service';
import { VersionStepHistory, WorkflowActivityType, WorkflowTypeNew } from '../../../../models/workflow.model';
import { BaseComponent } from '../../../shared/base.component';
import { CompanySettingsService } from '../../../../services/settings.service';
import { Router } from '@angular/router';
import { PopupMenuItem } from '../../../shared/controls/popup-menu/popup-menu.component';
import { WorkflowMonitorService } from '../../../../services/workflowmonitor.service';

/*global $localize*/

@Component({
	selector: 'd3s-pending-assignments-step',
	templateUrl: './pending-assignments-step.component.html',
	styleUrls: ['./pending-assignments-step.component.less']
})
export class PendingAssignmentsStepComponent extends BaseComponent implements OnInit, OnChanges {
	@Input() versionStepId: number;
	@Input() workflowTypeNew: WorkflowTypeNew;
	history: VersionStepHistory[];
	selectedHistoryItem: VersionStepHistory;
	WorkflowActivityType = WorkflowActivityType;
	menuItems: PopupMenuItem[] = [new PopupMenuItem({
		title: $localize`Delete`
	})];
	showDeletionModal: boolean = false;

	constructor(
		protected settingsService: CompanySettingsService,
		private workflowService: WorkflowService,
		private workflowMonitorService: WorkflowMonitorService,
		private router: Router) {
		super(settingsService);
	}


	ngOnInit() {
		this.loadWorkflowVersionStepHistory();
	}

	ngOnChanges() {
		this.loadWorkflowVersionStepHistory();
	}

	deleteAssignment = (): void => {
		if (this.selectedHistoryItem != null) {
			this.isLoading = true;
			this.workflowMonitorService.deleteItemsByUid([this.selectedHistoryItem.WorkflowItemUid]).subscribe(
				() => {
					this.showDeletionModal = false;
					this.loadWorkflowVersionStepHistory();
				}
			);
		}
	};

	loadWorkflowVersionStepHistory() {
		this.history = [];
		if (this.versionStepId != null) {
			this.isLoading = true;
			this.workflowService.getWorkflowVersionStepHistory(this.versionStepId)
				.subscribe((r) => {
					this.history = r;
					this.isLoading = false;
				});
		}
	}

	export() {
		this.workflowService.exportVersionStepHistory(this.versionStepId);
	}

	navigate(url: string) {
		this.router.navigateByUrl(this.federateUrl(url));
	}

	clickMenuItem(event: { value: string, action: string, event, data: PopupMenuItem }): void {
		const key = event.value.toLowerCase();
		if (key === $localize`Delete`.toLowerCase()) {
			this.showDeletionModal = true;
		}
	}

	clickMenuIcon(item: VersionStepHistory): void {
		if (item) {
			this.selectedHistoryItem = item;
		}
	}
}
