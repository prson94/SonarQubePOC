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
                        <p-table #dt [loading]="isLoading" loadingIcon="fa fa-spinner" [value]="items" selectionMode="single" [lazy]="true" [totalRecords]="totalRecords"  [scrollable]="true" scrollWidth="100%" [metaKeySelection]="true" 
                            [globalFilterFields]="['WorkflowName','Type','TypeName','Asset','Initiator','StartedOn','CompletedOn']" [pageLinks]="3" [paginator]="true" 
                            [rows]="rowsPerPage" [rowsPerPageOptions]="defaultPagingOptions" (onLazyLoad)="loadWorkflowMonitorItems($event)" [selection]="selection" 
                            (selectionChange)="gridSelectionChange($event)">
                            <ng-template pTemplate="header">
                                <tr>
                                    <th [pSortableColumn]="'WorkflowName'">
                                        Workflow Name
                                        <d3s-sortIcon [field]="'WorkflowName'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'Type'">
                                        Type
                                        <d3s-sortIcon [field]="'Type'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'TypeName'">
                                        Type Name
                                        <d3s-sortIcon [field]="'TypeName'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'Asset'">
                                        Asset
                                        <d3s-sortIcon [field]="'Asset'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'Initiator'">
                                        Initiator
                                        <d3s-sortIcon [field]="'Initiator'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'StartedOn'">
                                        Started
                                        <d3s-sortIcon [field]="'StartedOn'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'CompletedOn'">
                                        Completed
                                        <d3s-sortIcon [field]="'CompletedOn'"></d3s-sortIcon>
                                    </th>
                                </tr>
                                <tr [hidden]="showSimpleFilter">
                                    <th><d3s-column-filter [field]="'WorkflowName'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'Type'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'TypeName'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'Asset'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'Initiator'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'StartedOn'" [datatype]="'text'"></d3s-column-filter></th>
                                    <th><d3s-column-filter [field]="'CompletedOn'" [datatype]="'text'"></d3s-column-filter></th>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="body" let-item>
                                <tr [pSelectableRow]="item">
                                    <td>{{item.WorkflowName}}</td>
                                    <td>{{item.Type}}</td>
                                    <td>{{item.TypeName}}</td>
                                    <td>{{item.Asset}}</td>
                                    <td>{{item.Initiator}}</td>
                                    <td>
                                        <span>{{item.StartedOn | date: 'shortDate'}}</span>
                                    </td>
                                    <td>
                                        <span>{{item.CompletedOn | date: 'shortDate'}}</span>
                                    </td>
                                </tr>
                            </ng-template>
                            <ng-template *ngIf="totalRecords" pTemplate="summary">
                                <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                            </ng-template>
                        </p-table>                        
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
        this.stateService.workflowItemFilters.currentPageNumber =  event.first / event.rows;
        this.loadData();
    }
}


