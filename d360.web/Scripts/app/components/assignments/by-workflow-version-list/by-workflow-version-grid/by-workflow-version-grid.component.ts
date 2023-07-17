import { ChangeDetectorRef, Component, EventEmitter, OnDestroy, OnInit, Output } from '@angular/core';
import { Observable, of, ReplaySubject, Subject, Subscription } from 'rxjs';
import { SortOrder } from '../../../../models/enums.model';
import { NumberOfRowsByCategoryService } from '../../../../services/number-of-rows-by-category.service';
import { StateService } from '../../../../services/state.service';
import { CompanySettingsService } from '../../../../services/settings.service';
import { map, takeUntil } from 'rxjs/operators';
import { LazyLoadEvent } from 'primeng/api';
import { BaseComponent } from '../../../shared/base.component';
import { WorkflowService } from '../../../../services/workflow.service';
import { AssignmentByVersion, AssignmentVersionItem, WorkflowTypeModel } from '../../../../models/workflow.model';
import {
	AdvancedFilterFieldType,
	Filters,
	LookupValuesAPIModel,
	LookupValuesAPIParameters
} from '../../../assets-grid/advanced-filtering/advanced-filtering.models';
import { FieldType } from '../../../../models/fieldtype-api.model';

/*global $localize*/

@Component({
	selector: 'd3s-by-workflow-version-grid',
	templateUrl: './by-workflow-version-grid.component.html',
	styleUrls: ['./by-workflow-version-grid.component.less']
})
export class ByWorkflowVersionGridComponent extends BaseComponent implements OnInit, OnDestroy {
	title: string = $localize`WorkFlow Items`;
	assignmentVersionItems: AssignmentVersionItem[] = [];
	subscription: Subscription;
	totalRecords: number;
	rowsPerPage: number = 10;
	sortField: string;
	sortOrder: SortOrder = SortOrder.Descending;
	selectedCount: number = 0;
	selectedAssignmentVersionItems: AssignmentVersionItem[] = [];
	simpleFilter: string = '';
	@Output() selectionChange: EventEmitter<AssignmentVersionItem[]> = new EventEmitter<AssignmentVersionItem[]>();
	filterFields$: Observable<AdvancedFilterFieldType[]>;
	private destroy: Subject<void> = new Subject<void>();
	private currentPageNumber: number = 1;
	private advancedFilter: string = '';
	private filterFieldsSubject: ReplaySubject<AdvancedFilterFieldType[]> = new ReplaySubject(1);

	constructor(public numberOfRowsByCategoryService: NumberOfRowsByCategoryService,
				public stateService: StateService,
				private workflowService: WorkflowService,
				private changeDetectorRef: ChangeDetectorRef,
				protected settingsService: CompanySettingsService) {
		super(settingsService);
	}

	ngOnInit(): void {
		this.setRowsPerPage();
		this.createFilterFields();
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

	gridSelectionChange(event: AssignmentVersionItem[]): void {
		this.selectedAssignmentVersionItems = event;
		this.selectedCount = this.selectedAssignmentVersionItems == null ? 0 : this.selectedAssignmentVersionItems.length;
		this.selectionChange.emit(event);
	}

	loadAssignmentsByVersion(lazyLoadEvent: LazyLoadEvent): void {
		this.rowsPerPage = lazyLoadEvent.rows;
		this.sortField = lazyLoadEvent.sortField ?? '';
		this.sortOrder = lazyLoadEvent.sortField ? lazyLoadEvent.sortOrder : SortOrder.Descending;
		this.currentPageNumber = (lazyLoadEvent.first / lazyLoadEvent.rows) + 1;
		this.loadData();
	}

	onSimpleSearch(): void {
		this.currentPageNumber = 1;
		this.loadData();
	}

	onFiltersLoaded(): void {
		this.currentPageNumber = 1;
		this.loadData();
	}

	advancedFiltersChanged($event: Filters): void {
		this.advancedFilter = $event.filter;
		this.onSimpleSearch();
	}

	private loadData(): void {
		this.isLoading = true;
		this.assignmentVersionItems = [];
		this.subscription = this.workflowService.getAssignmentsByVersion(this.currentPageNumber, this.rowsPerPage, this.simpleFilter, this.advancedFilter, this.sortField, this.sortOrder)
			.subscribe((response: AssignmentByVersion): void => {
				this.assignmentVersionItems = response.items;
				this.totalRecords = response.total;
				if (this.assignmentVersionItems.length > 0) {
					this.selectedAssignmentVersionItems = [this.assignmentVersionItems[0]];
					this.selectedCount = 1;
					this.selectionChange.emit(this.selectedAssignmentVersionItems);
				} else {
					this.selectedCount = 0;
					this.selectionChange.emit(null);
				}
				this.isLoading = false;
				this.changeDetectorRef.markForCheck();
			});
	}

	private createFilterFields(): void {
		const lookupFieldTypePrimaryFilter: FieldType = new FieldType('Lookup');
		lookupFieldTypePrimaryFilter.Lookup.IsPrimaryFilter = true;
		const filterFieldList: AdvancedFilterFieldType[] = [{
			Name: 'WorkflowName',
			FriendlyName: $localize`Workflow Name`,
			Type: new FieldType('Lookup'),
			Category: '',
			ValueLoader: this.getFilteredWorkflowNames
		}, {
			Name: 'Status',
			FriendlyName: $localize`Status`,
			Type: lookupFieldTypePrimaryFilter,
			Category: '',
			ValueLoader: this.getFilteredStatuses
		}, {
			Name: 'Version',
			FriendlyName: $localize`Version`,
			Type: new FieldType('Number'),
			Category: ''
		}];
		this.filterFields$ = this.filterFieldsSubject.asObservable();
		this.filterFieldsSubject.next(filterFieldList);
		this.filterFieldsSubject.complete();
	}

	private getFilteredStatuses = (lookupValuesAPIParameters: LookupValuesAPIParameters): Observable<LookupValuesAPIModel> => {
		const values: string[] = ['Incomplete', 'Awaiting'].filter((s) => s.toLowerCase().indexOf(lookupValuesAPIParameters.filter?.toLowerCase() ?? '') !== -1);
		return of({
			items: values,
			count: values.length
		});
	};

	private getFilteredWorkflowNames = (lookupValuesAPIParameters: LookupValuesAPIParameters): Observable<LookupValuesAPIModel> => {
		return this.workflowService.getTypes().pipe(
			map((workflowTypeList: WorkflowTypeModel[]) => {
				let workflowNameList: string[] = workflowTypeList?.map((workflowTypeModel: WorkflowTypeModel) => workflowTypeModel.Name) ?? [];
				workflowNameList = workflowNameList.filter((s) => s.toLowerCase().indexOf(lookupValuesAPIParameters.filter?.toLowerCase() ?? '') !== -1);
				return {
					items: workflowNameList,
					count: workflowNameList.length
				};
			}));
	};
}
