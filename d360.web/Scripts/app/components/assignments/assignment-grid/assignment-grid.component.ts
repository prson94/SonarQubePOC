import {
	ChangeDetectorRef,
	Component,
	EventEmitter,
	OnDestroy,
	OnInit,
	Output
} from '@angular/core'
import { GridFilterExpression } from '../../../models/grid-definition.model'
import { WorkflowMonitorItem } from '../../../models/workflowmonitor.model'
import { Subject, Subscription } from 'rxjs'
import { SortOrder } from '../../../models/enums.model'
import { WorkflowMonitorService } from '../../../services/workflowmonitor.service'
import { NumberOfRowsByCategoryService } from '../../../services/number-of-rows-by-category.service'
import { StateService } from '../../../services/state.service'
import { CompanySettingsService } from '../../../services/settings.service'
import { AuthenticationService } from '../../../services/authentication.service'
import { takeUntil } from 'rxjs/operators'
import { clone } from 'lodash-es'
import { StringHelpers } from '../../../static/string-helpers'
import { LazyLoadEvent } from 'primeng/api'
import { BaseComponent } from '../../shared/base.component'

@Component({
  selector: 'd3s-assignment-grid',
  templateUrl: './assignment-grid.component.html',
  styleUrls: ['./assignment-grid.component.less']
})
export class AssignmentGridComponent extends BaseComponent implements OnInit, OnDestroy {

	title: string = $localize`WorkFlow Items`;
	items: WorkflowMonitorItem[] = [];
	subItems: Subscription;
	totalRecords: number;
	rowsPerPage: number = 10;
	sortField: string = undefined;
	sortOrder: SortOrder = SortOrder.Descending;
	isAdmin: boolean = false;
	selectedCount: number = 0;
	selection: WorkflowMonitorItem[];
	@Output() selectionChange = new EventEmitter();
	@Output() hideDetails = new EventEmitter();
	private destroy = new Subject<void>();

	exportMessage: string = "";

	constructor(private wfMonitorService: WorkflowMonitorService,
				public numberOfRowsByCategoryService: NumberOfRowsByCategoryService,
				public stateService: StateService,
				private changeDetectorRef: ChangeDetectorRef,
				protected settingsService: CompanySettingsService,
				private authenticationService: AuthenticationService) {
		super(settingsService);

		this.exportMessage = $localize`Export not available for over ${this.maxExportRows} rows`;
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

	export() {
		const filter: GridFilterExpression[] = this.getFilter();
		if (filter == null || filter.length < 1) {
			return;
		}
		this.wfMonitorService.exportToExcel(this.rowsPerPage, this.stateService.workflowItemFilters.currentPageNumber, this.sortField, this.sortOrder, filter);
	}

	gridSelectionChange($event) {
		if (Array.isArray($event) && $event.length === 1) {
			this.stateService.workflowItemFilters.itemId = $event[0].Id;
		} else {
			this.stateService.workflowItemFilters.itemId = 0;
		}
		this.selection = $event;
		this.selectedCount = this.selection == null ? 0 : this.selection.length;
		this.selectionChange.emit($event);
	}

	getFilter(): GridFilterExpression[] {
		let filter: GridFilterExpression[] = [];

		if ((!this.stateService.workflowItemFilters) ||
			((!this.stateService.workflowItemFilters.columFilters || this.stateService.workflowItemFilters.columFilters.length < 1) &&
				(!this.stateService.workflowItemFilters.workflowTypeFilters ||
					StringHelpers.isNullOrEmpty(this.stateService.workflowItemFilters.workflowTypeFilters.value)))) {
			return filter;
		}

		if (this.stateService.workflowItemFilters.columFilters && this.stateService.workflowItemFilters.columFilters.length > 0)
		{filter = clone(this.stateService.workflowItemFilters.columFilters);}

		if (this.stateService.workflowItemFilters.workflowTypeFilters &&
			!StringHelpers.isNullOrEmpty(this.stateService.workflowItemFilters.workflowTypeFilters.value))
		{filter.push(this.stateService.workflowItemFilters.workflowTypeFilters);}

		return filter;

	}

	private loadData() {
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

					this.selection = item ? [item] : [this.items[0]];
					this.selectedCount = 1;
					this.selectionChange.emit(this.selection);
				}
				else {
					this.selectedCount = 0;
					this.selectionChange.emit(null);
				}
				this.isLoading = false;
				this.changeDetectorRef.markForCheck();
			});
	}

	loadWorkflowMonitorItems(event: LazyLoadEvent) {

		this.rowsPerPage = event.rows;
		this.sortOrder = event.sortField == null ? SortOrder.Descending : event.sortOrder;
		this.sortField = event.sortField == null ? "" : event.sortField;
		this.rowsPerPage = event.rows;
		this.stateService.workflowItemFilters.currentPageNumber = event.first / event.rows;
		this.loadData();
	}

	selectAll() {
		if (this.selection) {
			if (this.selection.length === this.items.length) {
				this.gridSelectionChange([this.items[0]]);
			} else {
				this.gridSelectionChange(this.items);
			}
		}
	}

	canExportRecords() {
		return this.totalRecords <= this.maxExportRows;
	}

}
