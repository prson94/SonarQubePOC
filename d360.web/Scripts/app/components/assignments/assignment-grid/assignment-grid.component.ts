import { ChangeDetectorRef, Component, EventEmitter, OnDestroy, OnInit, Output } from '@angular/core';
import { Observable, of, ReplaySubject, Subject, Subscription } from 'rxjs';
import { SortOrder } from '../../../models/enums.model';
import { WorkflowMonitorService } from '../../../services/workflowmonitor.service';
import { NumberOfRowsByCategoryService } from '../../../services/number-of-rows-by-category.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { AuthenticationService } from '../../../services/authentication.service';
import { takeUntil } from 'rxjs/operators';
import { LazyLoadEvent } from 'primeng/api';
import { BaseComponent } from '../../shared/base.component';
import { WorkflowService } from '../../../services/workflow.service';
import { WorkflowAssignmentItem } from '../../../models/workflow.model';
import {
	AdvancedFilterFieldType, Filters, LookupValuesAPIModel,
	LookupValuesAPIParameters
} from '../../assets-grid/advanced-filtering/advanced-filtering.models';
import { FieldType } from '../../../models/fieldtype-api.model';

@Component({
	selector: 'd3s-assignment-grid',
	templateUrl: './assignment-grid.component.html',
	styleUrls: ['./assignment-grid.component.less']
})
export class AssignmentGridComponent extends BaseComponent implements OnInit, OnDestroy {

	title: string = $localize`WorkFlow Items`;
	items: WorkflowAssignmentItem[] = [];
	subItems: Subscription;
	totalRecords: number;
	rowsPerPage: number = 10;
	currentPageNumber: number = 1;
	sortField: string = undefined;
	sortOrder: SortOrder = SortOrder.Descending;
	isAdmin: boolean = false;
	selectedCount: number = 0;
	assignments: WorkflowAssignmentItem[] = [];
	simpleFilter: string = '';
	advancedFilter: string = "";
	showDeletionModal: boolean = false;
	@Output() selectionChange: EventEmitter<WorkflowAssignmentItem[]> = new EventEmitter<WorkflowAssignmentItem[]>();
	@Output() hideDetails = new EventEmitter();
	private destroy = new Subject<void>();
	theDeleteCallback: Function;
	menuItems: any[] = [
		{ title: $localize`Delete` }
	];
	isExportInProgress: boolean = false;
	filterFields$: Observable<AdvancedFilterFieldType[]>;
	private filterFieldsSubject: ReplaySubject<AdvancedFilterFieldType[]> = new ReplaySubject(1);
	statusValues: string[] = ["Pending", "Complete"];

	filterFieldList: AdvancedFilterFieldType[] = [
		{
			Name: 'Status',
			FriendlyName: $localize`Status`,
			Type: new FieldType("Lookup"),
			Category: "",
			ValueLoader: this.getFilterValues.bind(this, "Status"),
			RemovePopulatedOperator: true
		},
		{
			Name: 'assetDisplayValue',
			FriendlyName: $localize`Associated with`,
			Type: new FieldType("Text"),
			Category: ""
		},
		{
			Name: 'CompletedOn',
			FriendlyName: $localize`Completed`,
			Type: new FieldType("DateTime"),
			Category: ""
		},
		{
			Name: 'StartedOn',
			FriendlyName: $localize`Initiated`,
			Type: new FieldType("DateTime"),
			Category: ""
		},
	];

	constructor(private wfMonitorService: WorkflowMonitorService,
				private workflowService: WorkflowService,
				public numberOfRowsByCategoryService: NumberOfRowsByCategoryService,
				private changeDetectorRef: ChangeDetectorRef,
				protected settingsService: CompanySettingsService,
				private authenticationService: AuthenticationService) {
		super(settingsService);
		this.theDeleteCallback = this.deleteAssignments.bind(this);
	}

	ngOnInit(): void {
		this.isAdmin = this.authenticationService.isAdmin;
		this.setRowsPerPage();
		this.filterFields$ = this.filterFieldsSubject.asObservable();
		this.filterFieldsSubject.next(this.filterFieldList);
		this.filterFieldsSubject.complete();
		this.numberOfRowsByCategoryService.defineNumberOfRows(this.defaultInitialItemsPerPage);
	}

	setRowsPerPage(): void {
		this.numberOfRowsByCategoryService.rowsPerPage.pipe(
			takeUntil(this.destroy)
		).subscribe((rowsPerPage) => {
			this.rowsPerPage = rowsPerPage[this.title] || this.defaultInitialItemsPerPage;
			this.isLoading = true;
			this.loadWorkflowAssignmentItems({ rows: this.rowsPerPage, first: 0 });
		});
	}

	ngOnDestroy(): void {
		if (this.subItems) {
			this.subItems.unsubscribe();
		}
		this.destroy.next();
		this.destroy.complete();
	}

	canExportRecords() {
		return this.totalRecords <= this.maxExportRows;
	}

	export() {
		this.isExportInProgress = true;
		this.workflowService.getWorkflowAssignments(this.currentPageNumber, this.maxExportRows, this.simpleFilter, this.advancedFilter, this.sortField, this.sortOrder, true, () => {
			this.isExportInProgress = false;
		});
	}

	gridSelectionChange(event: WorkflowAssignmentItem[]): void {
		this.assignments = event;
		this.selectedCount = this.assignments == null ? 0 : this.assignments.length;
		this.selectionChange.emit(event);
	}

	private loadData(): void {
		this.isLoading = true;
		this.subItems = this.workflowService.getWorkflowAssignments(this.currentPageNumber, this.rowsPerPage, this.simpleFilter, this.advancedFilter, this.sortField, this.sortOrder, false, null)
			.subscribe((result) => {
				this.items = result.items;
				this.totalRecords = +result.total;
				if (this.items != null && this.items.length > 0) {
					this.assignments = [this.items[0]];
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

	loadWorkflowAssignmentItems(event: LazyLoadEvent): void {
		this.rowsPerPage = event.rows;
		this.sortOrder = event.sortField == null ? SortOrder.Descending : event.sortOrder;
		this.sortField = event.sortField == null ? '' : event.sortField;
		this.currentPageNumber = (event.first / event.rows) + 1;
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
		this.loadData();
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
		// if (Array.isArray(this.assignments)) {
		// 	itemIds = this.assignments.map((i: WorkflowAssignmentItem) => i.Id);
		// } else if (this.assignments != null) {
		// 	itemIds.push((this.assignments as WorkflowAssignmentItem).Id);
		// }
		this.wfMonitorService.deleteItems(itemIds).subscribe(
			(res) => {
				this.showDeletionModal = false;
				this.loadWorkflowAssignmentItems({ rows: this.rowsPerPage, first: 0 });
			}
		);
	}

	getFilterValues(params: LookupValuesAPIParameters): Observable<LookupValuesAPIModel> {
		if (params === "Status") {
			return of({
				items: this.statusValues,
				count: this.statusValues.length
			});
		}
	}

	onFiltersLoaded(): void {
		this.loadData()
	}

	advancedFiltersChanged($event: Filters): void {
		this.advancedFilter = $event.filter;
		this.loadData();
	}

}
