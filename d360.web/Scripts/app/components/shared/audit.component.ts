///<reference path="../../es6-shim.d.ts"/>
import {Component, Input} from '@angular/core';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService, AuditService  } from '../../services/index';
import { Audit } from '../../models/audit.model';
import { DataTable, Column, LazyLoadEvent} from 'primeng/primeng';
import { SortOrder } from '../../models/enums.model';
import { GridFilterExpression } from '../../models/grid-definition.model';

@Component({
    selector: 'd3s-audit',
    directives: [DataTable, Column],
    providers: [AuditService],
    template: `
                <div *ngIf="isLoading" style="width:100%; text-align:center;">
                    <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
                <div class="row" *ngIf="!isAuditVisible">
                    <div class="col s12">
                        <div class="tile tile-detail" *ngIf="!isLoading">   
                            <header *ngIf="!isLoading">Audit History for {{objectName}}</header>       
                            <p-dataTable  [lazy]="true" [totalRecords]="totalRecords" [value]="audits" selectionMode="single" [rows]="rowsPerPage" [paginator]="true" [pageLinks]="4" [(selection)]="selected" (onLazyLoad)="loadAuditsLazy($event)" [rowsPerPageOptions]="[5,10,20]">
                                <p-column field="ResourceName" header="User" [sortable]="true" [filter]="true"></p-column>                                                                                    
                                <p-column field="Date" header="Date" [sortable]="true" [filter]="true">
                                    <template let-col let-data="rowData">
                                        <span>{{data.Date | date: 'medium'}}</span>
                                    </template>
                                </p-column>
                                <p-column field="Action" header="Action" [sortable]="true" [filter]="true"></p-column>                                                            
                                <p-column field="ActionObject" header="Action Object" [sortable]="true" [filter]="true"></p-column>
                                <p-column field="ActionObjectTypeName" header="Type" [sortable]="true" [filter]="true"></p-column>
                                <p-column field="ActionObjectName" header="Item" [sortable]="true" [filter]="true"></p-column>
                                <p-column field="ActionDescription" header="Audit Description" [sortable]="true" [filter]="true"></p-column>                                                        
                                <p-column field="Field" header="Field" [sortable]="true" [filter]="true"></p-column>                                
                                <p-column field="NewValue" header="New Value" [sortable]="true" [filter]="true">
                                    <template let-col let-data="rowData">
                                        <div [innerHtml]="data?.NewValue"></div>
                                    </template>                                                        
                                </p-column>
                                <p-column field="PreviousValue" header="Previous Value" [sortable]="true" [filter]="true">
                                    <template let-col let-data="rowData">
                                        <div [innerHtml]="data?.PreviousValue"></div>
                                    </template>                                                        
                                </p-column>
                                <p-column field="Version" header="Revision #" [sortable]="true" [filter]="true"></p-column>
                            </p-dataTable> 
                        </div>
                    </div>
                </div>
        `    
})

export class AuditComponent {
    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() objectName: string;

    totalRecords: number;
    rowsPerPage: number = 20;
    audits: Audit[] = [];
    isLoading: boolean = false;
    selected: Audit;
    currentPageNumber: number = 0;
    sortField: string = undefined;
    sortOrder: SortOrder = SortOrder.None;
    filters: GridFilterExpression[] = [];


    constructor(private auditService: AuditService, private headerBreadcrumbService: HeaderBreadcrumbService) {
      
    }

    ngOnInit() {        
     //   this.getData(this.currentPageNumber);
    }

    private getData() {
        //    this.isLoading = true;
        this.auditService.getAuditData(this.objectID, this.objectType, this.currentPageNumber, this.rowsPerPage, this.sortOrder, this.sortField, this.filters)
            .then(result => {
         //       this.isLoading = false;
                this.audits = result.results;
                this.totalRecords = result.total;
            });
    }   
    
    private loadAuditsLazy(event: LazyLoadEvent) {
        //event.first = First row offset
        //event.rows = Number of rows per page
        //event.sortField = Field name to sort with
        //event.sortOrder = Sort order as number, 1 for asc and -1 for dec
        //filters: FilterMetadata object having field as key and filter value, filter matchMode as value        
        this.filters.splice(0, this.filters.length);

        for (var key in event.filters) {
            var filter = event.filters[key];

            var gridFilter = new GridFilterExpression();
            gridFilter.condition = "CONTAINS"
            gridFilter.field = key;
            gridFilter.value = filter.value;
            this.filters.push(gridFilter);
        }
        

        this.sortOrder = event.sortOrder;
        this.sortField = event.sortField == undefined ? "" : event.sortField;
        this.rowsPerPage = event.rows;
        this.currentPageNumber = event.first / event.rows;        
        this.getData();
    }
}