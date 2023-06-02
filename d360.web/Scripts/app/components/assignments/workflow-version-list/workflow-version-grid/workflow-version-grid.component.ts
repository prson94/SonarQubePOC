import { ChangeDetectorRef, Component, EventEmitter, OnDestroy, OnInit, Output } from '@angular/core';
import { WorkflowMonitorItem } from '../../../../models/workflowmonitor.model';
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

@Component({
  selector: 'd3s-workflow-version-grid',
  templateUrl: './workflow-version-grid.component.html',
  styleUrls: ['./workflow-version-grid.component.less']
})
export class WorkflowVersionGridComponent extends BaseComponent implements OnInit, OnDestroy {
	title: string = $localize`WorkFlow Items`;
	items: WorkflowMonitorItem[] = [];
	subItems: Subscription;
	totalRecords: number;
	rowsPerPage: number = 10;
	sortField: string = undefined;
	sortOrder: SortOrder = SortOrder.Descending;
	isAdmin: boolean = false;
	selectedCount: number = 0;
	assignments: WorkflowMonitorItem[] = [];
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
			this.loadWorkflowMonitorItems({ rows: this.rowsPerPage, first: 0 });
		});
	}

	ngOnDestroy(): void {
		if (this.subItems) {
			this.subItems.unsubscribe();
		}
		this.destroy.next();
		this.destroy.complete();
	}

	export(): void {
		this.wfMonitorService.exportToExcel(this.rowsPerPage, this.stateService.workflowItemFilters.currentPageNumber, this.sortField, this.sortOrder);
	}

	gridSelectionChange(event: WorkflowMonitorItem[]): void {
		if (Array.isArray(event) && event.length === 1) {
			this.stateService.workflowItemFilters.itemId = event[0].Id;
		} else {
			this.stateService.workflowItemFilters.itemId = 0;
		}
		this.assignments = event;
		this.selectedCount = this.assignments == null ? 0 : this.assignments.length;
		this.selectionChange.emit(event);
	}

	private loadData(): void {
		this.isLoading = true;
		this.subItems = this.wfMonitorService.getWorkFlowMonitorItems(this.rowsPerPage, this.stateService.workflowItemFilters.currentPageNumber, this.sortField, this.sortOrder)
			.subscribe((result) => {
				this.items = result.Items;
				this.totalRecords = +result.Total;
				if (this.items != null && this.items.length > 0) {
					let item: WorkflowMonitorItem;
					if (this.stateService.workflowItemFilters.itemId !== 0) {
						item = this.items.find((x) => x.Id === this.stateService.workflowItemFilters.itemId);
					}

					this.assignments = item ? [item] : [this.items[0]];
					this.selectedCount = 1;
					this.selectionChange.emit(this.assignments);
				} else {
					this.selectedCount = 0;
					this.selectionChange.emit(null);
				}
				this.isLoading = false;
				this.changeDetectorRef.markForCheck();
			});
	}

	loadWorkflowMonitorItems(event: LazyLoadEvent): void {
		this.rowsPerPage = event.rows;
		this.sortOrder = event.sortField == null ? SortOrder.Descending : event.sortOrder;
		this.sortField = event.sortField == null ? '' : event.sortField;
		this.rowsPerPage = event.rows;
		this.stateService.workflowItemFilters.currentPageNumber = event.first / event.rows;
		this.loadData();
	}

	selectAll(): void {
		if (this.assignments) {
			if (this.assignments.length === this.items.length) {
				this.gridSelectionChange([this.items[0]]);
			} else {
				this.gridSelectionChange(this.items);
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
		if (Array.isArray(this.assignments)) {
			itemIds = this.assignments.map((i) => i.Id);
		} else if (this.assignments != null) {
			itemIds.push((this.assignments as WorkflowMonitorItem).Id);
		}
		this.wfMonitorService.deleteItems(itemIds).subscribe(
			(res) => {
				this.showDeletionModal = false;
				this.loadWorkflowMonitorItems({ rows: this.rowsPerPage, first: 0 });
			}
		);
	}
}
