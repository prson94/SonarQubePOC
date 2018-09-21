import { Component, OnDestroy, OnInit, ChangeDetectorRef, ChangeDetectionStrategy, Input, OnChanges, SimpleChanges, Output, EventEmitter } from "@angular/core";
import { BaseComponent } from "../shared/base.component";
import { LazyLoadEvent } from "primeng/primeng";
import { WorkflowMonitorService } from "../../services/workflowmonitor.service";
import { Subscription } from "rxjs";
import { WorkflowMonitorItem } from "../../models/workflowmonitor.model";
import { SortOrder } from "../../models/enums.model";
import {  GridFilterExpression } from "../../models/grid-definition.model";


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
                    <d3s-workflowmonitor-top-level-filter
                       (filterChange)= "OnFilterChange($event)"   (exportToExcel)="export()"     (selectionChange)="OnWorkflowTypesChange($event)" [selectAll]="true" >
                    </d3s-workflowmonitor-top-level-filter>
                </div>
                    <div class="col s12">                
                        <p-dataTable [loading]="isLoading" loadingIcon="fa-spinner" 
                        #dt lazy="true" [totalRecords]="totalRecords"  scrollable="true" scrollWidth="100%" [value]="items" selectionMode="single" 
                        [selection]="selection" (selectionChange)="selection = $event; selectionChange.emit($event)"
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
    private currentPageNumber: number = 0;
    private sortField: string = undefined;
    private sortOrder: SortOrder = SortOrder.Descending;
    private filter: GridFilterExpression[] = [];
     selection: any;
    @Output() selectionChange = new EventEmitter();

    workflowTypes: any[];

    constructor(private wfMonitorService: WorkflowMonitorService,
        private changeDetectorRef: ChangeDetectorRef) {
        super();
    }

    ngOnInit(): void {
      
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.getData();
    }

    ngOnDestroy(): void {
        this.subItems.unsubscribe();
    }

    export() {
        this.wfMonitorService.exportToExcel(this.rowsPerPage, this.currentPageNumber, this.sortField, this.sortOrder, this.filter);
    }
    private getData() {

        if (this.filter == null || this.filter.length < 1) {
            this.items = [];
            this.totalRecords = 0;
            this.selectionChange.emit(null);
            return;
        }

        this.isLoading = true;
        this.subItems = this.wfMonitorService.getWorkFlowMonitorItems(this.rowsPerPage, this.currentPageNumber, this.sortField, this.sortOrder, this.filter)
            .subscribe(result => {

                this.items = result.Items;
                this.totalRecords = +result.Total;

                if (this.items != null && this.items.length > 0) {
                    //select first row by default
                    this.selection = this.items[0];
                    this.selectionChange.emit(this.selection);
                }
                else {
                    this.selectionChange.emit(null);
                }
                this.isLoading = false;
                this.changeDetectorRef.markForCheck();
                console.log(this.totalRecords);
            });
    }

    OnWorkflowTypesChange($event) {
          this.workflowTypes = $event;
        this.getData();
    }

    OnFilterChange($event) {
        this.filter = $event;
        this.currentPageNumber = 0;
        this.getData();
    }
    private loadWorkflowMonitorItems(event: LazyLoadEvent) {
        this.rowsPerPage = event.rows;
        this.sortOrder = event.sortField == undefined ? SortOrder.Descending: event.sortOrder;
        this.sortField = event.sortField == undefined ? "" : event.sortField;
        this.rowsPerPage = event.rows;
        this.currentPageNumber = event.first / event.rows;
        this.getData();
    }
}


