import { Component, OnDestroy, OnInit, ChangeDetectorRef, ChangeDetectionStrategy, Input, OnChanges, SimpleChanges, Output, EventEmitter } from "@angular/core";
import { BaseComponent } from "../shared/base.component";
import { LazyLoadEvent } from "primeng/api";
import { WorkflowMonitorService } from "../../services/workflowmonitor.service";
import { Subject, Subscription } from "rxjs";
import { WorkflowMonitorItem } from "../../models/workflowmonitor.model";
import { SortOrder } from "../../models/enums.model";
import {  GridFilterExpression } from "../../models/grid-definition.model";
import { StateService } from "../../services/state.service";
import { StringHelpers } from "../../static/string-helpers";
import { AuthenticationService } from '../../services/authentication.service';
import * as _ from "lodash";
import { CompanySettingsService } from "../../services/settings.service";
import { NumberOfRowsByCategoryService } from "../../services/number-of-rows-by-category.service";
import { takeUntil } from "rxjs/operators";


@Component({
    selector: 'd3s-workflowmonitor-list',
    providers: [WorkflowMonitorService],
    templateUrl: 'workflowmonitor-list.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,  
})

export class WorkflowMonitorListComponent extends BaseComponent  implements OnInit, OnDestroy,OnChanges {
    @Input() predefinedFilters: GridFilterExpression[] = [];
    @Input() showHeader: boolean = true;
    title: string = 'WorkFlow Items';
    items: WorkflowMonitorItem[] = [];;
    subItems : Subscription
    totalRecords: number;
    rowsPerPage: number = 10;
    sortField: string = undefined;
    sortOrder: SortOrder = SortOrder.Descending;
    usePredefinedFilters: boolean = false;
    isAdmin: boolean = false;
    showConfirmDelete: boolean = false;
    selectedCount: number = 0;
    selection: any;
    @Output() selectionChange = new EventEmitter();
    @Output() hideDetails = new EventEmitter();
    private destroy = new Subject<void>();

    constructor(private wfMonitorService: WorkflowMonitorService,
        public numberOfRowsByCategoryService: NumberOfRowsByCategoryService,
        public stateService: StateService,
        private changeDetectorRef: ChangeDetectorRef,
        protected settingsService: CompanySettingsService,
        private authenticationService: AuthenticationService) {
        super(settingsService);
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
            this.loadWorkflowMonitorItems({ rows: this.rowsPerPage, first: 0});
        });
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['predefinedFilters']) {
            this.usePredefinedFilters = (this.predefinedFilters && this.predefinedFilters.length > 0);
            this.isLoading = true;
            this.loadWorkflowMonitorItems({ rows: this.rowsPerPage, first: 0});
        }
     }

    ngOnDestroy(): void {
        if (this.subItems) {
            this.subItems.unsubscribe();
        }
        this.destroy.next();
        this.destroy.complete();
    }

    export() {
        let filter: GridFilterExpression[] = this.getFilter();
        if (filter == null || filter.length < 1) {
            return;
        }
        this.wfMonitorService.exportToExcel(this.rowsPerPage, this.stateService.workflowItemFilters.currentPageNumber, this.sortField, this.sortOrder, filter);
    }

    gridSelectionChange($event) {
        if (Array.isArray($event) && $event.length == 1) {
            this.stateService.workflowItemFilters.itemId = $event[0].Id;
        } else {
            this.stateService.workflowItemFilters.itemId = 0;
        }
        this.selection = $event;
        this.selectedCount = this.selection == null ? 0 : this.selection.length;
        this.selectionChange.emit($event)
    }

    getFilter(): GridFilterExpression[] {
        let filter: GridFilterExpression[] = [];

        if (this.predefinedFilters && this.predefinedFilters.length > 0) {
            filter = _.clone(this.predefinedFilters);
            return filter;
        }

        if ((!this.stateService.workflowItemFilters) ||
            ((!this.stateService.workflowItemFilters.columFilters || this.stateService.workflowItemFilters.columFilters.length < 1) &&
                (!this.stateService.workflowItemFilters.workflowTypeFilters ||
                    StringHelpers.isNullOrEmpty(this.stateService.workflowItemFilters.workflowTypeFilters.value)))) {
            return filter;
        } 

        if (this.stateService.workflowItemFilters.columFilters && this.stateService.workflowItemFilters.columFilters.length > 0)
            filter = _.clone(this.stateService.workflowItemFilters.columFilters);

        if (this.stateService.workflowItemFilters.workflowTypeFilters &&
            !StringHelpers.isNullOrEmpty(this.stateService.workflowItemFilters.workflowTypeFilters.value))
            filter.push(this.stateService.workflowItemFilters.workflowTypeFilters);

        return filter;

    }

    private loadData() {
        let filter: GridFilterExpression[] = this.getFilter();
        if (filter == null || filter.length < 1) {
            this.items = [];
            this.totalRecords = 0;
            this.selectionChange.emit(null);
            this.isLoading = false;
            return;
        }

        this.isLoading = true;
        this.subItems = this.wfMonitorService.getWorkFlowMonitorItems(this.rowsPerPage, this.stateService.workflowItemFilters.currentPageNumber, this.sortField, this.sortOrder,filter)
            .subscribe(result => {
                this.items = result.Items;
                this.totalRecords = +result.Total;
                if (this.items != null && this.items.length > 0) {
                    let item: any;
                    if (this.stateService.workflowItemFilters.itemId != 0) {
                        item = this.items.find(x => x.Id == this.stateService.workflowItemFilters.itemId)
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

    confirmDelete(showConfirm: boolean) {
        this.showConfirmDelete = showConfirm;
        this.hideDetails.emit(showConfirm);
    }

    public deleteItems() {
        this.isLoading = true;
        let itemIds = [];
        if (Array.isArray(this.selection)) {
            itemIds = this.selection.map(i => i.Id);
        } else if (this.selection != null) {
            itemIds.push(this.selection.Id);
        }
        this.wfMonitorService.deleteItems(itemIds).subscribe(
            (res) => {
                this.confirmDelete(false);
                this.loadWorkflowMonitorItems({ rows: this.rowsPerPage, first: 0 });
            }
        );
    }

    OnFilterChange() {
        this.stateService.workflowItemFilters.currentPageNumber = 0;
        this.loadData();
    }

    loadWorkflowMonitorItems(event: LazyLoadEvent) {
     
        this.rowsPerPage = event.rows;
        this.sortOrder = event.sortField == undefined ? SortOrder.Descending: event.sortOrder;
        this.sortField = event.sortField == undefined ? "" : event.sortField;
        this.rowsPerPage = event.rows;
        this.stateService.workflowItemFilters.currentPageNumber =  event.first / event.rows;
        this.loadData();
    }

    getMetaKey() {
        if (window.navigator && window.navigator.platform.indexOf("Mac") >= 0) {
            return "\u2318";
        } else {
            return "Ctrl"
        }
    }

    selectAll() {
        if (this.selection) {
            if (this.selection.length == this.items.length) {
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



