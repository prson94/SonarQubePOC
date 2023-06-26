import { ChangeDetectorRef, Component, EventEmitter, OnDestroy, OnInit, Output } from '@angular/core';
import { Subject, Subscription } from 'rxjs';
import { SortOrder } from '../../../../models/enums.model';
import { NumberOfRowsByCategoryService } from '../../../../services/number-of-rows-by-category.service';
import { StateService } from '../../../../services/state.service';
import { CompanySettingsService } from '../../../../services/settings.service';
import { takeUntil } from 'rxjs/operators';
import { LazyLoadEvent } from 'primeng/api';
import { BaseComponent } from '../../../shared/base.component';
import { WorkflowService } from '../../../../services/workflow.service';
import { AssignmentByVersion } from '../../../../models/workflow.model';

class AssignmentWithStatusByVersion extends AssignmentByVersion {
	statusCount: number;
}

@Component({
	selector: 'd3s-by-workflow-version-grid',
	templateUrl: './by-workflow-version-grid.component.html',
	styleUrls: ['./by-workflow-version-grid.component.less']
})
export class ByWorkflowVersionGridComponent extends BaseComponent implements OnInit, OnDestroy {
	title: string = $localize`WorkFlow Items`;
	assignmentsByVersion: AssignmentWithStatusByVersion[] = [];
	subscription: Subscription;
	totalRecords: number;
	rowsPerPage: number = 10;
	sortField: string = undefined;
	sortOrder: SortOrder = SortOrder.Descending;
	selectedCount: number = 0;
	selectedAssignmentByVersion: AssignmentByVersion[] = [];
	simpleFilter: string = '';
	showDeletionModal: boolean = false;
	@Output() selectionChange = new EventEmitter();
	@Output() hideDetails = new EventEmitter();
	private destroy = new Subject<void>();
	menuItems: any[] = [
		{ title: $localize`Delete` }
	];

	constructor(public numberOfRowsByCategoryService: NumberOfRowsByCategoryService,
				public stateService: StateService,
				private workflowService: WorkflowService,
				private changeDetectorRef: ChangeDetectorRef,
				protected settingsService: CompanySettingsService) {
		super(settingsService);
	}

	ngOnInit(): void {
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

	gridSelectionChange(event: AssignmentByVersion[]): void {
		// if (Array.isArray(event) && event.length === 1) {
		// 	this.stateService.workflowItemFilters.itemId = event[0].TypeID;
		// } else {
		// 	this.stateService.workflowItemFilters.itemId = 0;
		// }
		this.selectedAssignmentByVersion = event;
		this.selectedCount = this.selectedAssignmentByVersion == null ? 0 : this.selectedAssignmentByVersion.length;
		this.selectionChange.emit(event);
	}

	private loadData(): void {
		this.isLoading = true;
		this.assignmentsByVersion = [];
		this.subscription = this.workflowService.getAssignmentsByVersion()
			.subscribe(response => {
				this.assignmentsByVersion = response.map(assignmentByVersion => {
					const assignmentWithStatusByVersion = assignmentByVersion as AssignmentWithStatusByVersion;
					assignmentWithStatusByVersion.statusCount = assignmentByVersion.Awaiting + assignmentByVersion.Incomplete;
					return assignmentWithStatusByVersion;
				});
			});
		if (this.assignmentsByVersion.length > 0) {
			this.totalRecords = this.assignmentsByVersion.length;
			this.selectedAssignmentByVersion = [this.assignmentsByVersion[0]];
			this.selectedCount = 1;
			this.selectionChange.emit(this.selectedAssignmentByVersion);
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
		if (this.selectedAssignmentByVersion) {
			if (this.selectedAssignmentByVersion.length === this.assignmentsByVersion.length) {
				this.gridSelectionChange([this.assignmentsByVersion[0]]);
			} else {
				this.gridSelectionChange(this.assignmentsByVersion);
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
}
