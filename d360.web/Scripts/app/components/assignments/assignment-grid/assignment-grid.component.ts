import { ChangeDetectorRef, Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { forkJoin, Observable, of, ReplaySubject, Subject } from 'rxjs';
import { SortOrder } from '../../../models/enums.model';
import { WorkflowMonitorService } from '../../../services/workflowmonitor.service';
import { NumberOfRowsByCategoryService } from '../../../services/number-of-rows-by-category.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { AuthenticationService } from '../../../services/authentication.service';
import { map } from 'rxjs/operators';
import { LazyLoadEvent } from 'primeng/api';
import { BaseComponent } from '../../shared/base.component';
import { WorkflowService } from '../../../services/workflow.service';
import { WorkflowAssignmentItem, WorkflowIssueType, WorkflowTypeModel } from '../../../models/workflow.model';
import {
	AdvancedFilterFieldType,
	Filters,
	LookupValuesAPIModel,
	LookupValuesAPIParameters
} from '../../assets-grid/advanced-filtering/advanced-filtering.models';
import { FieldType } from '../../../models/fieldtype-api.model';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';

@Component({
	selector: 'd3s-assignment-grid',
	templateUrl: './assignment-grid.component.html',
	styleUrls: ['./assignment-grid.component.less']
})
export class AssignmentGridComponent extends BaseComponent implements OnInit, OnDestroy {
	@Input() isRequestsFlow: boolean = false;
	currentResourceUid: string = null;
	title: string = $localize`WorkFlow Items`;
	items: WorkflowAssignmentItem[] = [];
	totalRecords: number;
	rowsPerPage: number = 10;
	currentPageNumber: number = 1;
	sortField: string = undefined;
	sortOrder: SortOrder = SortOrder.Descending;
	isAdmin: boolean = false;
	selectedCount: number = 0;
	assignments: WorkflowAssignmentItem[] = [];
	simpleFilter: string = '';
	advancedFilter: string = '';
	singleActionTypeUidSelected: boolean = false;
	singleActionTypeUidFilter: string = '';
	actionFormFields: any[] = [];
	showDeletionModal: boolean = false;
	@Output() selectionChange: EventEmitter<WorkflowAssignmentItem[]> = new EventEmitter<WorkflowAssignmentItem[]>();
	@Output() hideDetails: EventEmitter<any> = new EventEmitter();
	private destroy: Subject<void> = new Subject<void>();
	theDeleteCallback: Function;
	menuItems: any[] = [
		{ title: $localize`Delete` }
	];
	isExportInProgress: boolean = false;
	filterFields$: Observable<AdvancedFilterFieldType[]>;
	private filterFieldsSubject: ReplaySubject<AdvancedFilterFieldType[]> = new ReplaySubject(1);
	protected readonly JSON: JSON = JSON;

	constructor(private wfMonitorService: WorkflowMonitorService,
				private workflowService: WorkflowService,
				public numberOfRowsByCategoryService: NumberOfRowsByCategoryService,
				private changeDetectorRef: ChangeDetectorRef,
				protected settingsService: CompanySettingsService,
				private fieldsService: FieldsObservableService,
				private authenticationService: AuthenticationService) {
		super(settingsService);
		this.theDeleteCallback = this.deleteAssignments.bind(this);
		this.settingsService.getUserVariables().subscribe((res) => {
			this.currentResourceUid = res.CurrentResourceUid;
			this.loadData();
		});
	}

	ngOnInit(): void {
		this.isAdmin = this.authenticationService.isAdmin;
		this.createFilterFields();
		this.numberOfRowsByCategoryService.defineNumberOfRows(this.defaultInitialItemsPerPage);
	}

	ngOnDestroy(): void {
		this.destroy.next();
		this.destroy.complete();
	}

	canExportRecords() {
		return this.totalRecords <= this.maxExportRows;
	}

	export() {
		this.isExportInProgress = true;
		let initiatorUid: string = this.isRequestsFlow ? this.currentResourceUid : null;
		this.workflowService.getWorkflowAssignments(this.currentPageNumber, this.maxExportRows, this.simpleFilter, this.advancedFilter, initiatorUid, this.sortField, this.sortOrder, true, () => {
			this.isExportInProgress = false;
		});
	}

	gridSelectionChange(event: WorkflowAssignmentItem[]): void {
		this.assignments = event;
		this.selectedCount = this.assignments == null ? 0 : this.assignments.length;
		this.selectionChange.emit(event);
	}

	private loadData(): void {
		if (!this.currentResourceUid) {
			return;
		}
		this.isLoading = true;
		let initiatorUid: string = this.isRequestsFlow ? this.currentResourceUid : null;
		let sources: Observable<any>[] = [
			this.workflowService.getWorkflowAssignments(this.currentPageNumber, this.rowsPerPage, this.simpleFilter, this.advancedFilter, initiatorUid, this.sortField, this.sortOrder, false, null),
			this.singleActionTypeUidSelected && this.singleActionTypeUidFilter ? this.fieldsService.getFieldsV2(null, this.singleActionTypeUidFilter, null) : of([])
		];
		forkJoin(sources).subscribe((results: any[]) => {
			this.items = results[0].items;
			this.totalRecords = +results[0].total;
			if (this.items != null && this.items.length > 0) {
				this.assignments = [this.items[0]];
				this.selectedCount = 1;
				this.selectionChange.emit(this.assignments);
			} else {
				this.selectedCount = 0;
				this.selectionChange.emit(null);
			}
			this.actionFormFields = results[1] && results[1].length > 0 ? results[1] : [];
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
		this.currentPageNumber = 1;
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
		let itemIds: string[] = [];
		if (Array.isArray(this.assignments)) {
			itemIds = this.assignments.map((i: WorkflowAssignmentItem) => i.workflowItemUid);
		} else if (this.assignments != null) {
			itemIds.push((this.assignments as WorkflowAssignmentItem).workflowItemUid);
		}
		this.wfMonitorService.deleteItemsByUid(itemIds).subscribe(
			(res) => {
				this.showDeletionModal = false;
				this.loadWorkflowAssignmentItems({ rows: this.rowsPerPage, first: 0 });
			}
		);
	}

	getFilterValues(lookupType: string, params: LookupValuesAPIParameters): Observable<LookupValuesAPIModel> {
		if (lookupType === 'status') {
			let statusValues: string[] = ['Pending', 'Complete'];
			const values: string[] = statusValues.filter((s) => s.toLowerCase().indexOf(params.filter?.toLowerCase() ?? '') !== -1);
			return of({
				items: values,
				count: values.length
			});
		}
		if (lookupType === 'type') {
			let typeValues: string[] = ['Action', 'Business Asset', 'Model', 'Policy', 'Relationship', 'Rule', 'Technical Asset'];
			const values: string[] = typeValues.filter((s) => s.toLowerCase().indexOf(params.filter?.toLowerCase() ?? '') !== -1);
			return of({
				items: values,
				count: values.length
			});
		}
		if (lookupType === 'assignee') {
			return this.workflowService.getPossibleAssignees().pipe(
				map((assignees: { uid: string, Name: string }[]) => {
					let possibleAssigneeList: {
						'name': string,
						'value': string
					}[] = assignees?.map((assignee: { uid: string, Name: string }): {
						'name': string,
						'value': string
					} => {
						return { 'name': assignee.Name, 'value': assignee.uid };
					}) ?? [];
					possibleAssigneeList = possibleAssigneeList.filter((s) => s?.name.toLowerCase().indexOf(params.filter?.toLowerCase() ?? '') !== -1);
					return {
						items: possibleAssigneeList,
						count: possibleAssigneeList.length
					};
				}));
		}
		if (lookupType === 'initiator') {
			return this.workflowService.getPossibleInitiators().pipe(
				map((initiators: { uid: string, Name: string }[]) => {
					let possibleInitiatorsList: {
						'name': string,
						'value': string
					}[] = initiators?.map((assignee: { uid: string, Name: string }): {
						'name': string,
						'value': string
					} => {
						return { 'name': assignee.Name, 'value': assignee.uid };
					}) ?? [];
					possibleInitiatorsList = possibleInitiatorsList.filter((s) => s?.name.toLowerCase().indexOf(params.filter?.toLowerCase() ?? '') !== -1);
					return {
						items: possibleInitiatorsList,
						count: possibleInitiatorsList.length
					};
				}));
		}
		if (lookupType === 'workflowName') {
			return this.workflowService.getTypes().pipe(
				map((workflowTypeList: WorkflowTypeModel[]) => {
					let workflowNameList: string[] = workflowTypeList?.map((workflowTypeModel: WorkflowTypeModel) => workflowTypeModel.Name) ?? [];
					workflowNameList = workflowNameList.filter((s) => s.toLowerCase().indexOf(params.filter?.toLowerCase() ?? '') !== -1);
					return {
						items: workflowNameList,
						count: workflowNameList.length
					};
				}));
		}
		if (lookupType === 'action') {
			return this.workflowService.getWorkflowIssueTypes(null, null, { '_limitToActiveWorkflows': true }).pipe(
				map((workflowIssueTypeList: WorkflowIssueType[]) => {
					let workflowActionList: {
						'name': string,
						'value': string
					}[] = workflowIssueTypeList?.map((workflowIssueTypeList: WorkflowIssueType): {
						'name': string,
						'value': string
					} => {
						return { 'name': workflowIssueTypeList.Name, 'value': workflowIssueTypeList.Uid };
					}) ?? [];
					workflowActionList = workflowActionList.filter((s) => s?.name.toLowerCase().indexOf(params.filter?.toLowerCase() ?? '') !== -1);
					return {
						items: workflowActionList,
						count: workflowActionList.length
					};
				}));
		}
		if (lookupType === 'typeName') {
			return this.workflowService.getRelevantAssetTypes().pipe(
				map((assetType: { uid: string, name: string }[]) => {
					let assetTypeList: { 'name': string, 'value': string }[] = assetType?.map((assetType): {
						'name': string,
						'value': string
					} => {
						return { 'name': assetType.name, 'value': assetType.uid };
					}) ?? [];
					assetTypeList = assetTypeList.filter((s) => s?.name.toLowerCase().indexOf(params.filter?.toLowerCase() ?? '') !== -1);
					return {
						items: assetTypeList,
						count: assetTypeList.length
					};
				}));
		}
	}

	onFiltersLoaded(): void {
		this.currentPageNumber = 1;
		this.loadData();
	}

	advancedFiltersChanged($event: Filters): void {
		this.advancedFilter = $event.filter;
		let advancedFilterData: any[] = $event.data;
		for (let i: number = 0; i < advancedFilterData?.length; i++) {
			if (advancedFilterData[i].field === 'actionTypeUid') {
				this.singleActionTypeUidSelected = advancedFilterData[i].value?.length === 1;
				this.singleActionTypeUidFilter = advancedFilterData[i].value && advancedFilterData[i].value[0]?.value;
			}
		}
		this.currentPageNumber = 1;
		this.loadData();
	}

	getAssignees(assigneeList: any[], count?: number): string {
		let assigneeNames: string = '';
		if (assigneeList && assigneeList.length > 0) {
			const assigneeNameList: any[] = assigneeList.map((assignee) => assignee.Name)?.sort();
			assigneeNames = count ? assigneeNameList?.slice(0, count)?.join(', ') : assigneeNameList?.join(', ');
		}
		return assigneeNames;
	}

	createFilterFields(): void {
		let filterFieldList: AdvancedFilterFieldType[] = [];
		const lookupFieldTypePrimaryFilter: FieldType = new FieldType('Lookup');
		lookupFieldTypePrimaryFilter.Lookup.IsPrimaryFilter = !this.isRequestsFlow;
		filterFieldList = [
			{
				Name: 'Status',
				FriendlyName: $localize`Status`,
				Type: lookupFieldTypePrimaryFilter,
				Category: '',
				ValueLoader: this.getFilterValues.bind(this, 'status')
			},
			{
				Name: 'assignee',
				FriendlyName: $localize`Assignee`,
				Type: lookupFieldTypePrimaryFilter,
				Category: '',
				ValueLoader: this.getFilterValues.bind(this, 'assignee')
			},
			{
				Name: 'actionTypeUid',
				FriendlyName: $localize`Action`,
				Type: lookupFieldTypePrimaryFilter,
				Category: '',
				ValueLoader: this.getFilterValues.bind(this, 'action')
			},
			{
				Name: 'assetDisplayValue',
				FriendlyName: $localize`Associated with`,
				Type: new FieldType('Text'),
				Category: ''
			},
			{
				Name: 'CompletedOn',
				FriendlyName: $localize`Completed`,
				Type: new FieldType('DateTime'),
				Category: ''
			},
			{
				Name: 'StartedOn',
				FriendlyName: $localize`Initiated`,
				Type: new FieldType('DateTime'),
				Category: ''
			},
			{
				Name: 'initiatorUid',
				FriendlyName: $localize`Initiator`,
				Type: new FieldType('Lookup'),
				Category: '',
				ValueLoader: this.getFilterValues.bind(this, 'initiator')
			},
			{
				Name: 'initiatingObjectType',
				FriendlyName: $localize`Type`,
				Type: new FieldType('Lookup'),
				Category: '',
				ValueLoader: this.getFilterValues.bind(this, 'type')
			},
			{
				Name: 'assetTypeUid',
				FriendlyName: $localize`Type Name`,
				Type: new FieldType('Lookup'),
				Category: '',
				ValueLoader: this.getFilterValues.bind(this, 'typeName')
			},
			{
				Name: 'workflowName',
				FriendlyName: $localize`Workflow Name`,
				Type: new FieldType('Lookup'),
				Category: '',
				ValueLoader: this.getFilterValues.bind(this, 'workflowName')
			}
		];
		this.filterFields$ = this.filterFieldsSubject.asObservable();
		this.filterFieldsSubject.next(filterFieldList);
		this.filterFieldsSubject.complete();
	}
}
