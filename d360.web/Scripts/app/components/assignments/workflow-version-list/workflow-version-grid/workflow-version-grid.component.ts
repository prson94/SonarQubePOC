import { ChangeDetectorRef, Component, EventEmitter, OnDestroy, OnInit, Output } from '@angular/core';
import { Subject, Subscription } from 'rxjs';
import { SortOrder } from '../../../../models/enums.model';
import { WorkflowMonitorService } from '../../../../services/workflowmonitor.service';
import { NumberOfRowsByCategoryService } from '../../../../services/number-of-rows-by-category.service';
import { StateService } from '../../../../services/state.service';
import { CompanySettingsService } from '../../../../services/settings.service';
import { AuthenticationService } from '../../../../services/authentication.service';
import { takeUntil } from 'rxjs/operators';
import { LazyLoadEvent } from 'primeng/api';
import { BaseComponent } from '../../../shared/base.component';
import { WorkflowService } from '../../../../services/workflow.service';
import { WorkflowByType, WorkflowWithStatus } from '../../../../models/workflow.model';

@Component({
	selector: 'd3s-workflow-version-grid',
	templateUrl: './workflow-version-grid.component.html',
	styleUrls: ['./workflow-version-grid.component.less']
})
export class WorkflowVersionGridComponent extends BaseComponent implements OnInit, OnDestroy {
	title: string = $localize`WorkFlow Items`;
	workflowsWithStatus: WorkflowWithStatus[] = [];
	subscription: Subscription;
	totalRecords: number;
	rowsPerPage: number = 10;
	sortField: string = undefined;
	sortOrder: SortOrder = SortOrder.Descending;
	isAdmin: boolean = false;
	selectedCount: number = 0;
	selectedWorkflowsByType: WorkflowByType[] = [];
	simpleFilter: string = '';
	showDeletionModal: boolean = false;
	@Output() selectionChange = new EventEmitter();
	@Output() hideDetails = new EventEmitter();
	private destroy = new Subject<void>();
	theDeleteCallback: Function;
	menuItems: any[] = [
		{ title: $localize`Delete` }
	];

	constructor(private wfMonitorService: WorkflowMonitorService,
				public numberOfRowsByCategoryService: NumberOfRowsByCategoryService,
				public stateService: StateService,
				private workflowService: WorkflowService,
				private changeDetectorRef: ChangeDetectorRef,
				protected settingsService: CompanySettingsService,
				private authenticationService: AuthenticationService) {
		super(settingsService);
		this.theDeleteCallback = this.deleteAssignments.bind(this);
	}

	ngOnInit(): void {
		this.isAdmin = this.authenticationService.isAdmin;
		this.setRowsPerPage();
		this.numberOfRowsByCategoryService.defineNumberOfRows(this.defaultInitialItemsPerPage);
	}

	setRowsPerPage(): void {
		this.numberOfRowsByCategoryService.rowsPerPage.pipe(
			takeUntil(this.destroy)
		).subscribe((rowsPerPage) => {
			this.rowsPerPage = rowsPerPage[this.title] || this.defaultInitialItemsPerPage;
			this.isLoading = true;
			this.loadWorkflowsByType({ rows: this.rowsPerPage, first: 0 });
		});
	}

	ngOnDestroy(): void {
		if (this.subscription) {
			this.subscription.unsubscribe();
		}
		this.destroy.next();
		this.destroy.complete();
	}

	export(): void {
		this.wfMonitorService.exportToExcel(this.rowsPerPage, this.stateService.workflowItemFilters.currentPageNumber, this.sortField, this.sortOrder);
	}

	gridSelectionChange(event: WorkflowByType[]): void {
		if (Array.isArray(event) && event.length === 1) {
			this.stateService.workflowItemFilters.itemId = event[0].TypeID;
		} else {
			this.stateService.workflowItemFilters.itemId = 0;
		}
		this.selectedWorkflowsByType = event;
		this.selectedCount = this.selectedWorkflowsByType == null ? 0 : this.selectedWorkflowsByType.length;
		this.selectionChange.emit(event);
	}

	private loadData(): void {
		this.isLoading = true;
		this.workflowsWithStatus = [];
		this.subscription = this.workflowService.getWorkflowsByTypeList()
			.subscribe((workflowsByType) => {
				workflowsByType.forEach(workflowByType => {
					let existingWorkflowWithStatus = this.workflowsWithStatus.find(workflowWithStatus => workflowWithStatus.Name === workflowByType.Name && workflowWithStatus.Version === workflowByType.Version);
					if (!existingWorkflowWithStatus) {
						existingWorkflowWithStatus = workflowByType as WorkflowWithStatus;
						existingWorkflowWithStatus.incomplete = 0;
						existingWorkflowWithStatus.complete = 0;
						existingWorkflowWithStatus.awaiting = 0;
						this.workflowsWithStatus.push(existingWorkflowWithStatus);
					}
					if (workflowByType.Status === 'Incomplete') {
						existingWorkflowWithStatus.incomplete++;
					} else if (workflowByType.Status === 'Complete') {
						existingWorkflowWithStatus.complete++;
					} else if (workflowByType.Status === 'Waiting on user action') {
						existingWorkflowWithStatus.awaiting++;
					}
				});
			});
		if (this.workflowsWithStatus.length > 0) {
			this.totalRecords = this.workflowsWithStatus.length;
			this.selectedWorkflowsByType = [this.workflowsWithStatus[0]];
			this.selectedCount = 1;
			this.selectionChange.emit(this.selectedWorkflowsByType);
		} else {
			this.selectedCount = 0;
			this.selectionChange.emit(null);
		}
		this.isLoading = false;
		this.changeDetectorRef.markForCheck();
	}

	loadWorkflowsByType(event: LazyLoadEvent): void {
		this.rowsPerPage = event.rows;
		this.sortOrder = event.sortField == null ? SortOrder.Descending : event.sortOrder;
		this.sortField = event.sortField == null ? '' : event.sortField;
		this.rowsPerPage = event.rows;
		this.stateService.workflowItemFilters.currentPageNumber = event.first / event.rows;
		this.loadData();
	}

	selectAll(): void {
		if (this.selectedWorkflowsByType) {
			if (this.selectedWorkflowsByType.length === this.workflowsWithStatus.length) {
				this.gridSelectionChange([this.workflowsWithStatus[0]]);
			} else {
				this.gridSelectionChange(this.workflowsWithStatus);
			}
		}
	}

	clickMenuIcon(item: any): void {
		if (item) {
			this.gridSelectionChange([item]);
		}
	}

	onSimpleSearch(event: any): void {
		console.log(event);
	}

	clickMenuItem(event: any, item: any): void {
		const key = event.value.toLowerCase();
		if (key === $localize`Delete`.toLowerCase()) {
			this.showDeletionModal = true;
		}
	}

	public deleteAssignments(): void {
		this.isLoading = true;
		let itemIds = [];
		if (Array.isArray(this.selectedWorkflowsByType)) {
			itemIds = this.selectedWorkflowsByType.map((i) => i.TypeID);
		} else if (this.selectedWorkflowsByType != null) {
			itemIds.push((this.selectedWorkflowsByType as WorkflowByType).TypeID);
		}
		this.wfMonitorService.deleteItems(itemIds).subscribe(
			(res) => {
				this.showDeletionModal = false;
				this.loadWorkflowsByType({ rows: this.rowsPerPage, first: 0 });
			}
		);
	}
}
