import { ChangeDetectorRef, Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { forkJoin, Observable, of, ReplaySubject, Subject, Subscription } from 'rxjs';
import { SortOrder } from '../../../models/enums.model';
import { WorkflowMonitorService } from '../../../services/workflowmonitor.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { AuthenticationService } from '../../../services/authentication.service';
import { map } from 'rxjs/operators';
import { LazyLoadEvent } from 'primeng/api';
import { BaseComponent } from '../../shared/base.component';
import { WorkflowService } from '../../../services/workflow.service';
import {
	WorkflowAssignmentItem,
	WorkflowAssignments,
	WorkflowIssueType,
	WorkflowTypeModel
} from '../../../models/workflow.model';
import {
	AdvancedFilterFieldCondition,
	AdvancedFilterFieldType,
	Filters,
	LookupValuesAPIModel,
	LookupValuesAPIParameters
} from '../../assets-grid/advanced-filtering/advanced-filtering.models';
import { FieldType, FieldTypeAPIModelField } from '../../../models/fieldtype-api.model';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';
import { PopupMenuItem } from '../../shared/controls/popup-menu/popup-menu.component';

/*global $localize*/

class WorkflowAssignmentGrid extends WorkflowAssignmentItem {
	filteredAssignees: string[];
	allAssignees: string[];
	daysOpen: number;
}

@Component({
	selector: 'd3s-assignment-grid',
	templateUrl: './assignment-grid.component.html',
	styleUrls: ['./assignment-grid.component.less']
})

export class AssignmentGridComponent extends BaseComponent implements OnInit, OnDestroy {
	@Input() isRequestsFlow: boolean = false;
	@Input() assetTypeUid: string;
	@Input() assetUid: string;
	currentResourceUid: string = null;
	title: string = $localize`WorkFlow Items`;
	items: WorkflowAssignmentGrid[] = [];
	totalRecords: number;
	rowsPerPage: number = 10;
	currentPageNumber: number = 1;
	sortField: string;
	sortOrder: SortOrder = SortOrder.Descending;
	isAdmin: boolean = false;
	selectedCount: number = 0;
	assignments: WorkflowAssignmentItem[] = [];
	simpleFilter: string = '';
	advancedFilter: string = '';
	singleActionTypeUidSelected: boolean = false;
	singleActionTypeUidFilter: { title: string, value: string };
	assigneeSearchInputList: { title: string, value: string }[];
	actionFormFields: FieldTypeAPIModelField[] = [];
	showDeletionModal: boolean = false;
	@Output() selectionChange: EventEmitter<WorkflowAssignmentItem[]> = new EventEmitter<WorkflowAssignmentItem[]>();
	private destroy: Subject<void> = new Subject<void>();
	menuItems: PopupMenuItem[] = [new PopupMenuItem({
		title: $localize`Delete`
	})];
	isExportInProgress: boolean = false;
	filterFields$: Observable<AdvancedFilterFieldType[]>;
	protected readonly JSON: JSON = JSON;
	emptyGridMessage: string;
	loadDataSub: Subscription;
	areTypesLoaded: boolean = false;

	private actionTypeCount: number = 0;

	constructor(private wfMonitorService: WorkflowMonitorService,
				private workflowService: WorkflowService,
				private changeDetectorRef: ChangeDetectorRef,
				protected settingsService: CompanySettingsService,
				private fieldsService: FieldsObservableService,
				private authenticationService: AuthenticationService) {
		super(settingsService);
		this.currentResourceUid = this.settingsService.CurrentResourceUid;
		this.loadData();
	}

	ngOnInit(): void {
		this.isAdmin = this.authenticationService.isAdmin;
		this.emptyGridMessage = this.isRequestsFlow ? $localize`No requests found` : $localize`No assignments found`;
		this.loadActionTypeCount();
	}

	loadRowsPerPage(event: LazyLoadEvent): void {
		const rowsPerPageStorage: string = localStorage.getItem(this.storageKey);
		this.rowsPerPage = rowsPerPageStorage != null ? Number(rowsPerPageStorage) : event?.rows;
	}

	ngOnDestroy(): void {
		if (this.loadDataSub) {
			this.loadDataSub.unsubscribe();
		}
		this.destroy.next();
		this.destroy.complete();
	}

	canExportRecords() {
		return this.totalRecords <= this.maxExportRows;
	}

	export() {
		this.isExportInProgress = true;
		const initiatorUid: string = this.isRequestsFlow ? this.currentResourceUid : null;
		this.workflowService.getWorkflowAssignments(this.currentPageNumber, this.maxExportRows, this.simpleFilter, this.advancedFilter, initiatorUid, this.assetUid, this.assetTypeUid, this.sortField, this.sortOrder, this.isRequestsFlow, this.getExportFileName(), () => {
			this.isExportInProgress = false;
		});
	}

	gridSelectionChange(event: WorkflowAssignmentItem[]): void {
		this.assignments = event;
		this.selectedCount = this.assignments == null ? 0 : this.assignments.length;
		this.selectionChange.emit(event);
	}

	setRowsPerPage(event): void {
		if (event?.rows) {
			localStorage.setItem(this.storageKey, event.rows);
		}
	}

	private loadData(): void {
		if (!this.currentResourceUid) {
			return;
		}
		this.isLoading = true;
		const initiatorUid: string = this.isRequestsFlow ? this.currentResourceUid : null;
		const sources: Observable<WorkflowAssignments | FieldTypeAPIModelField[]>[] = [
			this.workflowService.getWorkflowAssignments(this.currentPageNumber, this.rowsPerPage, this.simpleFilter, this.advancedFilter, initiatorUid, this.assetUid, this.assetTypeUid, this.sortField, this.sortOrder, this.isRequestsFlow),
			this.singleActionTypeUidSelected && this.singleActionTypeUidFilter ? this.fieldsService.getFieldsV2(null, this.singleActionTypeUidFilter.value, null) : of([])
		];
		if (this.loadDataSub) {
			this.loadDataSub.unsubscribe();
		}
		this.loadDataSub = forkJoin(sources).subscribe((results: [WorkflowAssignments, FieldTypeAPIModelField[]]) => {
			this.items = results[0].items as WorkflowAssignmentGrid[];
			this.totalRecords = +results[0].total;
			if (this.items != null && this.items.length > 0) {
				this.assignments = [this.items[0]];
				this.selectedCount = 1;
				this.selectionChange.emit(this.assignments);
				this.createDisplayColumnData();
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
		this.loadRowsPerPage(event);
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

	clickMenuIcon(item: WorkflowAssignmentItem): void {
		if (item) {
			this.gridSelectionChange([item]);
		}
	}

	onSimpleSearch(): void {
		this.currentPageNumber = 1;
		this.loadData();
	}

	clickMenuItem(event: { value: string, action: string, event, data: PopupMenuItem }): void {
		const key = event.value.toLowerCase();
		if (key === $localize`Delete`.toLowerCase()) {
			this.showDeletionModal = true;
		}
	}

	deleteAssignments = (): void => {
		this.isLoading = true;
		let itemIds: string[] = [];
		if (Array.isArray(this.assignments)) {
			itemIds = this.assignments.map((i: WorkflowAssignmentItem) => i.workflowItemUid);
		} else if (this.assignments != null) {
			itemIds.push((this.assignments as WorkflowAssignmentItem).workflowItemUid);
		}
		this.wfMonitorService.deleteItemsByUid(itemIds).subscribe(
			() => {
				this.showDeletionModal = false;
				this.loadWorkflowAssignmentItems({ rows: this.rowsPerPage, first: 0 });
			}
		);
	};

	onFiltersLoaded(): void {
		this.currentPageNumber = 1;
		this.loadData();
	}

	advancedFiltersChanged($event: Filters): void {
		this.advancedFilter = $event.filter;
		const advancedFilterData: AdvancedFilterFieldCondition[] = $event.data;
		this.assigneeSearchInputList = [];
		for (const item of advancedFilterData) {
			if (item.field === 'actionTypeUid') {
				this.singleActionTypeUidSelected = item.value?.length === 1;
				this.singleActionTypeUidFilter = item.value?.[0];
			}
			if (item.field === 'assignee' && item.operator === 'Equals') {
				for (const value of item.value) {
					if (!this.assigneeSearchInputList.includes(value)) {
						this.assigneeSearchInputList.push(value);
					}
				}
			}
		}
		this.currentPageNumber = 1;
		this.loadData();
	}

	createFilterFields(): void {
		const filterFieldsSubject: ReplaySubject<AdvancedFilterFieldType[]> = new ReplaySubject(1);
		const lookupFieldTypePrimaryFilter: FieldType = new FieldType('Lookup');
		lookupFieldTypePrimaryFilter.Lookup.IsPrimaryFilter = !this.isRequestsFlow;
		const filterFieldList: AdvancedFilterFieldType[] = [{
			Name: 'workflowName',
			FriendlyName: $localize`Workflow Name`,
			Type: new FieldType('Lookup'),
			Category: '',
			ValueLoader: this.getFilteredWorkflowNames
		}];
		if (!this.assetUid) {
			filterFieldList.push({
				Name: 'assetDisplayValue',
				FriendlyName: $localize`Associated with`,
				Type: new FieldType('Text'),
				Category: ''
			});
		}
		filterFieldList.push({
			Name: 'StartedOn',
			FriendlyName: $localize`Initiated`,
			Type: new FieldType('DateTime'),
			Category: ''
		}, {
			Name: 'assignee',
			FriendlyName: $localize`Assignee`,
			Type: lookupFieldTypePrimaryFilter,
			Category: '',
			ValueLoader: this.getFilteredAssignees
		}, {
			Name: 'Status',
			FriendlyName: $localize`Status`,
			Type: lookupFieldTypePrimaryFilter,
			Category: '',
			ValueLoader: this.getFilteredStatuses
		});
		if (!this.isRequestsFlow) {
			filterFieldList.push(
				{
					Name: 'actionTypeUid',
					FriendlyName: $localize`Action`,
					Type: this.actionTypeCount > 0 ? lookupFieldTypePrimaryFilter : new FieldType('Lookup'),
					Category: '',
					ValueLoader: this.getFilteredActions
				},
				{
					Name: 'CompletedOn',
					FriendlyName: $localize`Completed`,
					Type: new FieldType('DateTime'),
					Category: ''
				},
				{
					Name: 'initiatorUid',
					FriendlyName: $localize`Initiator`,
					Type: new FieldType('Lookup'),
					Category: '',
					ValueLoader: this.getFilteredInitiator
				},
				{
					Name: 'initiatingObjectType',
					FriendlyName: $localize`Type`,
					Type: new FieldType('Lookup'),
					Category: '',
					ValueLoader: this.getFilteredTypes
				},
				{
					Name: 'assetTypeUid',
					FriendlyName: $localize`Type Name`,
					Type: new FieldType('Lookup'),
					Category: '',
					ValueLoader: this.getFilteredTypeNames
				}
			);
		}
		this.filterFields$ = filterFieldsSubject.asObservable();
		filterFieldsSubject.next(filterFieldList);
		filterFieldsSubject.complete();
	}

	get storageKey(): string {
		if (this.assetTypeUid) {
			return 'assetsAssignmentGrid' + this.settingsService.CurrentResourceID;
		} else if (this.assetUid) {
			return 'assetAssignmentGrid' + this.settingsService.CurrentResourceID;
		} else if (this.isRequestsFlow) {
			return 'requestGrid' + this.settingsService.CurrentResourceID;
		} else {
			return 'assignmentGrid' + this.settingsService.CurrentResourceID;
		}
	}

	private setDisplayAssignees(): void {
		for (const item of this.items) {
			const assigneesList: {
				Name: string,
				uid: string
			}[] = JSON.parse(item.assigneesJson) ?? [];
			const displayAssigneesList: { Name: string, uid: string }[] = [];
			if (this.assigneeSearchInputList?.length > 0) {
				for (const assigneeSearchInput of this.assigneeSearchInputList) {
					for (const assignee of assigneesList) {
						if (assignee.Name === assigneeSearchInput.title) {
							displayAssigneesList.push(assignee);
							break;
						}
					}
				}
				item.filteredAssignees = displayAssigneesList.map((assignee) => assignee.Name);
			} else {
				item.filteredAssignees = assigneesList.slice(0, 2).map((assignee) => assignee.Name);
			}
			item.allAssignees = assigneesList.map((assignee) => assignee.Name);
		}
	}

	private getFilteredWorkflowNames = (params: LookupValuesAPIParameters): Observable<LookupValuesAPIModel> => {
		return this.workflowService.getTypes().pipe(
			map((workflowTypeList: WorkflowTypeModel[]) => {
				let workflowNameList: string[] = [];
				for (const workflowType of workflowTypeList) {
					if (workflowType?.Name && !workflowNameList.includes(workflowType.Name)) {
						workflowNameList.push(workflowType.Name);
					}
				}
				workflowNameList = workflowNameList.filter((s) => s.toLowerCase().indexOf(params.filter?.toLowerCase() ?? '') !== -1);
				return {
					items: workflowNameList,
					count: workflowNameList.length
				};
			}));
	};

	private getFilteredStatuses = (params: LookupValuesAPIParameters): Observable<LookupValuesAPIModel> => {
		const statusValues: string[] = ['Pending', 'Complete'];
		const values: string[] = statusValues.filter((s) => s.toLowerCase().indexOf(params.filter?.toLowerCase() ?? '') !== -1);
		return of({
			items: values,
			count: values.length
		});
	};

	private getFilteredAssignees = (params: LookupValuesAPIParameters): Observable<LookupValuesAPIModel> => {
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
	};

	private getFilteredActions = (params: LookupValuesAPIParameters): Observable<LookupValuesAPIModel> => {
		const queryParams: Record<string, unknown> = { '_hasAssignments': true };
		if (this.assetUid) {
			queryParams['_assetUid'] = this.assetUid;
		} else if (this.assetTypeUid) {
			queryParams['_assetTypeUid'] = this.assetTypeUid;
		}
		return this.workflowService.getWorkflowIssueTypes(null, null, queryParams).pipe(
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
	};

	private getFilteredInitiator = (params: LookupValuesAPIParameters): Observable<LookupValuesAPIModel> => {
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
	};

	private getFilteredTypes = (params: LookupValuesAPIParameters): Observable<LookupValuesAPIModel> => {
		const typeValues: string[] = ['Action', 'Business Asset', 'Model', 'Policy', 'Relationship', 'Rule', 'Technical Asset'];
		const values: string[] = typeValues.filter((s) => s.toLowerCase().indexOf(params.filter?.toLowerCase() ?? '') !== -1);
		return of({
			items: values,
			count: values.length
		});
	};

	private getFilteredTypeNames = (params: LookupValuesAPIParameters): Observable<LookupValuesAPIModel> => {
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
	};

	private createDisplayColumnData() {
		this.setDisplayAssignees();
	}

	private getExportFileName(): string {
		let fileName: string = '';
		if (this.singleActionTypeUidSelected) {
			fileName += `${this.singleActionTypeUidFilter.title} `;
		}
		if (this.isRequestsFlow) {
			fileName += 'Filtered Request List';
		} else {
			fileName += 'Filtered Assignment List';
		}
		return fileName;
	}

	protected readonly Object = Object;

	private loadActionTypeCount() {
		const queryParams: Record<string, unknown> = { '_hasAssignments': true };
		if (this.assetUid) {
			queryParams['_assetUid'] = this.assetUid;
		} else if (this.assetTypeUid) {
			queryParams['_assetTypeUid'] = this.assetTypeUid;
		}
		this.workflowService.getWorkflowIssueTypes(null, null, queryParams).subscribe((response: WorkflowIssueType[]): void => {
			this.actionTypeCount = response?.length;
			this.createFilterFields();
			this.areTypesLoaded = true;
		});
	}
}
