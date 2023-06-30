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

/*global $localize*/

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
	sortField: string;
	sortOrder: SortOrder = SortOrder.Descending;
	selectedCount: number = 0;
	selectedAssignmentByVersion: AssignmentByVersion[] = [];
	simpleFilter: string = '';
	@Output() selectionChange = new EventEmitter();
	private destroy = new Subject<void>();
	private currentPageNumber: number = 1;

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
			this.loadAssignmentsByVersion({ rows: this.rowsPerPage, first: 0 });
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
		this.subscription = this.workflowService.getAssignmentsByVersion(this.currentPageNumber, this.rowsPerPage, this.simpleFilter)
			.subscribe((response) => {
				this.assignmentsByVersion = response.items.map((assignmentByVersion: AssignmentByVersion) => {
					const assignmentWithStatusByVersion = assignmentByVersion as AssignmentWithStatusByVersion;
					assignmentWithStatusByVersion.statusCount = assignmentByVersion.Awaiting + assignmentByVersion.Incomplete;
					return assignmentWithStatusByVersion;
				});
				this.totalRecords = response.total;
				if (this.assignmentsByVersion.length > 0) {
					this.selectedAssignmentByVersion = [this.assignmentsByVersion[0]];
					this.selectedCount = 1;
					this.selectionChange.emit(this.selectedAssignmentByVersion);
				} else {
					this.selectedCount = 0;
					this.selectionChange.emit(null);
				}
				this.isLoading = false;
				this.changeDetectorRef.markForCheck();
			});
	}

	loadAssignmentsByVersion(lazyLoadEvent: LazyLoadEvent): void {
		this.rowsPerPage = lazyLoadEvent.rows;
		this.sortField = lazyLoadEvent.sortField ?? '';
		this.sortOrder = lazyLoadEvent.sortField ? lazyLoadEvent.sortOrder : SortOrder.Descending;
		this.currentPageNumber = (lazyLoadEvent.first / lazyLoadEvent.rows) + 1;
		this.loadData();
	}

	onSimpleSearch(searchTerm: string): void {
		this.currentPageNumber = 1;
		this.loadData();
	}
}
