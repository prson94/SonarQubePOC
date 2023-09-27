import { Component, Input, OnChanges, OnInit, ViewChild } from '@angular/core';
import { WorkflowService } from '../../../../services/workflow.service';
import { VersionStepHistory, WorkflowActivityType, WorkflowDiagramModel } from '../../../../models/workflow.model';
import { BaseComponent } from '../../../shared/base.component';
import { CompanySettingsService } from '../../../../services/settings.service';
import { Router } from '@angular/router';
import { PopupMenuItem } from '../../../shared/controls/popup-menu/popup-menu.component';
import { WorkflowMonitorService } from '../../../../services/workflowmonitor.service';
import { LinkClickInterceptor } from '../../../../services/href-click-service';
import { AuthenticationService } from '../../../../services/authentication.service';
import { Table } from 'primeng/table';

/*global $localize*/

@Component({
	selector: 'd3s-pending-assignments-step',
	templateUrl: './pending-assignments-step.component.html',
	styleUrls: ['./pending-assignments-step.component.less']
})
export class PendingAssignmentsStepComponent extends BaseComponent implements OnInit, OnChanges {
	@Input() versionStepId: number;
	@Input() workflowTypeVersion: number;
	@Input() workflowTypeUid: string;

	history: VersionStepHistory[];
	selectedHistoryItem: VersionStepHistory;
	WorkflowActivityType = WorkflowActivityType;
	menuItems: PopupMenuItem[] = [new PopupMenuItem({
		title: $localize`Show Assignment Details`
	})];
	showDeletionModal: boolean = false;
	workflowDiagramModel: WorkflowDiagramModel;
	simpleFilter: string = '';
	@ViewChild('pendingAssignments') pendingAssignments: Table;
	modalSubtitle: string;

	constructor(
		protected settingsService: CompanySettingsService,
		private workflowService: WorkflowService,
		private authenticationService: AuthenticationService,
		private workflowMonitorService: WorkflowMonitorService,
		private router: Router,
		private linkClickInterceptor: LinkClickInterceptor) {
		super(settingsService);
	}


	ngOnInit() {
		this.authenticationService.checkCurrentUserAdmin().subscribe((isAdmin: boolean): void => {
			if (isAdmin) {
				this.menuItems.push(new PopupMenuItem({
					title: $localize`Delete`
				}));
			}
		});
		this.loadWorkflowVersionStepHistory();
		this.loadWorkflowDiagram();
	}

	ngOnChanges() {
		this.loadWorkflowVersionStepHistory();
		this.loadWorkflowDiagram();
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
			const startedOn = new Date(Date.parse(this.selectedHistoryItem?.StartedOn));
			this.modalSubtitle = `<b>${this.workflowDiagramModel?.Type?.Name}</b>&nbsp;on&nbsp;${(this.selectedHistoryItem?.Name ?? '---')} initiated on ${startedOn.toLocaleDateString()} ${startedOn.toLocaleTimeString([], {
				hour: '2-digit',
				minute: '2-digit'
			})}`;

			this.showDeletionModal = true;
		}
		if (key === $localize`Show Assignment Details`.toLowerCase()) {
			this.linkClickInterceptor.sendEvent(event.event, {
				workflowItemUid: this.selectedHistoryItem.WorkflowItemUid,
				workflowTypeVersion: this.workflowTypeVersion
			}, '');
		}
	}

	clickMenuIcon(item: VersionStepHistory): void {
		if (item) {
			this.selectedHistoryItem = item;
		}
	}

	onSimpleSearch(): void {
		this.pendingAssignments.filterGlobal(this.simpleFilter, 'contains');
	}

	onClickAsset(event: MouseEvent, item: VersionStepHistory): void {
		this.linkClickInterceptor.sendEvent(event, {
			AssetId: item.ObjectID
		}, item.NgUrl);
	}

	private loadWorkflowDiagram() {
		this.workflowService.getWorkflowDiagram(0, this.workflowTypeUid, this.workflowTypeVersion).subscribe((response) => {
			this.workflowDiagramModel = response;
		});
	}
}
