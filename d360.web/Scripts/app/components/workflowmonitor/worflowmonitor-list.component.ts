import { Component, OnDestroy, OnInit, ChangeDetectorRef, ChangeDetectionStrategy, Input, OnChanges, SimpleChanges, Output, EventEmitter } from "@angular/core";
import { BaseComponent } from "../shared/base.component";
import { LazyLoadEvent } from "primeng/primeng";
import { WorkflowMonitorService } from "../../services/workflowmonitor.service";
import { Subscription } from "rxjs";
import { WorkflowMonitorItem } from "../../models/workflowmonitor.model";
import { SortOrder } from "../../models/enums.model";
import {  GridFilterExpression } from "../../models/grid-definition.model";
import { StateService } from "../../services/state.service";
import { StringHelpers } from "../../static/string-helpers";
import * as _ from "lodash";


@Component({
    selector: 'd3s-workflowmonitor-list',
    providers: [WorkflowMonitorService],
    template: ` <d3s-loading [isLoading]="isEditing"></d3s-loading>                                                
                <header>
                  WorkFlow Items
                  <d3s-tile-actions  [hasExport]="true"  (exportClick)="export()" ></d3s-tile-actions>
                </header>                    
                <div class="row" >                    
                <div class="col s12">                                                
                    <d3s-workflowmonitor-list-filter  [(columnFilters)]="stateService.workflowItemFilters.columFilters" [(workflowTypeFilters)]="stateService.workflowItemFilters.workflowTypeFilters"
                       (filterChange)= "OnFilterChange()"   (exportToExcel)="export()"     [selectAll]="true" >
                    </d3s-workflowmonitor-list-filter>
                </div>
                    <div class="col s12">                
                        <p-dataTable [loading]="isLoading" loadingIcon="fa-spinner" styleClass="overridePaginator" 
                        #dt lazy="true" [totalRecords]="totalRecords"  scrollable="true" scrollWidth="100%" [value]="items" selectionMode="single" 
                        [selection]="selection" (selectionChange)="gridSelectionChange($event)"
                        [rows]="rowsPerPage" paginator="true" pageLinks="3"  (onLazyLoad)="loadWorkflowMonitorItems($event)" [rowsPerPageOptions]="defaultPagingOptions">
                        <p-footer *ngIf="totalRecords">
                        <d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info>
                        </p-footer>
                        <p-column field="WorkflowName" header="Workflow Name" sortable="true" [filter]="!showSimpleFilter"></p-column>  
                        <p-column field="Type" header="Type" sortable="true" [filter]="!showSimpleFilter"></p-column>  
                        <p-column field="TypeName" header="Type Name" sortable="true" [filter]="!showSimpleFilter"></p-column>  
                        <p-column field="Asset" header="Asset" sortable="true" [filter]="!showSimpleFilter"></p-column>  
                        <p-column field="Initiator" header="Initiator" sortable="true" [filter]="!showSimpleFilter"></p-column>  
                                
                        <p-column field="StartedOn" header="Started" sortable="true" [filter]="!showSimpleFilter">
                        <ng-template let-col let-data="rowData" pTemplate type="body">
                        <span>{{data.StartedOn | date: 'shortDate'}}</span>
                        </ng-template>
                        </p-column>  
                        <p-column field="CompletedOn" header="Completed" sortable="true" [filter]="!showSimpleFilter">
                        <ng-template let-col let-data="rowData" pTemplate type="body">
                        <span>{{data.CompletedOn | date: 'shortDate'}}</span>
                        </ng-template>
                        </p-column> 
                      </p-dataTable>                          
                    </div>
                </div>                                  
                `,
    changeDetection: ChangeDetectionStrategy.OnPush,  
})

export class WorkflowMonitorListComponent extends BaseComponent  implements OnInit, OnDestroy,OnChanges {

    
    private items: WorkflowMonitorItem[] = [];;
    private subItems : Subscription
    private totalRecords: number;
    private rowsPerPage: number = 10;
    private sortField: string = undefined;
    private sortOrder: SortOrder = SortOrder.Descending;
     selection: any;
    @Output() selectionChange = new EventEmitter();

    constructor(private wfMonitorService: WorkflowMonitorService,
        private stateService: StateService,
        private changeDetectorRef: ChangeDetectorRef) {
        super();
    }

    ngOnInit(): void {
     }

    ngOnChanges(changes: SimpleChanges): void {
      }

    ngOnDestroy(): void {
        this.subItems.unsubscribe();
    }

    export() {
        let filter: GridFilterExpression[] = this.getFilter();
        if (filter == null || filter.length < 1) {
            return;
        }
        this.wfMonitorService.exportToExcel(this.rowsPerPage, this.stateService.workflowItemFilters.currentPageNumber, this.sortField, this.sortOrder, filter);
    }

    private gridSelectionChange($event) {
        this.stateService.workflowItemFilters.itemId = $event ? $event.Id : 0;
        this.selection = $event;
        this.selectionChange.emit($event)
    }
    private getFilter(): GridFilterExpression[] {
        let filter: GridFilterExpression[] = [];
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

                    this.selection = item ? item : this.items[0];
                    this.selectionChange.emit(this.selection);
                }
                else {
                    this.selectionChange.emit(null);
                }
                this.isLoading = false;
                this.changeDetectorRef.markForCheck();
            });
    }



    OnFilterChange() {
        this.stateService.workflowItemFilters.currentPageNumber = 0;
        this.loadData();
    }
    private loadWorkflowMonitorItems(event: LazyLoadEvent) {
        debugger;
        this.rowsPerPage = event.rows;
        this.sortOrder = event.sortField == undefined ? SortOrder.Descending: event.sortOrder;
        this.sortField = event.sortField == undefined ? "" : event.sortField;
        this.rowsPerPage = event.rows;
        this.stateService.workflowItemFilters.currentPageNumber = event.first == 0 ? this.stateService.workflowItemFilters.currentPageNumber : event.first / event.rows;
        this.loadData();
    }
}


