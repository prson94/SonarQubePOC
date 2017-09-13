import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { AuditService } from '../../../services/audit.service';
import { Audit } from '../../../models/audit.model';
import { LazyLoadEvent } from 'primeng/primeng';
import { SortOrder } from '../../../models/enums.model';
import { GridFilterExpression } from '../../../models/grid-definition.model';
import { BaseComponent } from '../../shared/base.component';
import { ObjectDetailService } from '../../../services/object-detail.service';

@Component({
    selector: 'd3s-audit',
    providers: [AuditService, ObjectDetailService],
    template: `                
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">   
                            <header>Audit History for {{objectName}}<d3s-tile-actions [hasAdd]="false" [hasExport]="true" (exportClick)="export()"></d3s-tile-actions></header>                                                                                           
                            <p-dataTable #dt scrollable="true" scrollWidth="100%" lazy="true" [totalRecords]="totalRecords" [value]="audits" selectionMode="single" [rows]="rowsPerPage" paginator="true" pageLinks="3" [(selection)]="selected" (onLazyLoad)="loadAuditsLazy($event)" [rowsPerPageOptions]="defaultPagingOptions">
                                <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                                <p-column field="ResourceName" header="User" sortable="true" [style]="{'width':'150px'}" filter="true"></p-column>                                                                                    
                                <p-column field="Date" header="Date" sortable="true" [style]="{'width':'200px'}" filter="true">
                                    <ng-template let-col let-data="rowData" pTemplate type="body">
                                        <span>{{data.Date | date: 'medium'}}</span>
                                    </ng-template>
                                </p-column>
                                <p-column field="Action" header="Action" sortable="true" [style]="{'width':'100px'}" filter="true"></p-column>                                                            
                                <p-column field="Field" header="Field" sortable="true" [style]="{'width':'200px'}" filter="true"></p-column>                                
                                <p-column field="NewValue" header="New Value" sortable="true" [style]="{'width':'250px'}" filter="true">
                                    <ng-template let-col let-data="rowData" pTemplate type="body">
                                        <div *ngIf="data.NewValue" [innerHtml]="data.NewValue"></div>
                                    </ng-template>                                                        
                                </p-column>
                                <p-column field="PreviousValue" header="Previous Value" sortable="true" [style]="{'width':'250px'}" filter="true">
                                    <ng-template let-col let-data="rowData" pTemplate type="body">
                                        <div *ngIf="data.PreviousValue" [innerHtml]="data.PreviousValue"></div>
                                    </ng-template>                                                        
                                </p-column>
                                <p-column field="ActionObject" header="Object" sortable="true" [style]="{'width':'100px'}" filter="true"></p-column>
                                <p-column field="ActionObjectTypeName" header="Type" sortable="true" [style]="{'width':'100px'}" filter="true"></p-column>
                                <p-column field="ActionObjectName" header="Item" sortable="true" [style]="{'width':'100px'}" filter="true"></p-column>
                                <p-column field="ActionDescription" header="Audit Description" sortable="true" [style]="{'width':'250px'}" filter="true"></p-column>                                                                                        
                                <p-column field="Version" header="Revision" sortable="true"  [style]="{'width':'100px'}" filter="true"></p-column>
                            </p-dataTable>       
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>                                                  
                        </div>
                    </div>
                </div>
        `
})

export class AuditComponent extends BaseComponent implements OnInit, OnDestroy {
    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() objectName: string;

    totalRecords: number;
    rowsPerPage: number = 10;
    audits: Audit[] = [];
    private sub: any;

    selected: Audit;
    currentPageNumber: number = 0;
    sortField: string = undefined;
    sortOrder: SortOrder = SortOrder.None;
    filters: GridFilterExpression[] = [];

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private auditService: AuditService,
        private headerBreadcrumbService: HeaderBreadcrumbService,
        private objectDetailService: ObjectDetailService
    ) {
        super();
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.objectID = +params['objectId']; // (+) converts string 'id' to a number
            this.objectType = params['objectType'];

            this.objectDetailService.getObject(this.objectID, this.objectType).then(res => {
                if (res) this.objectName = res.Name;                
            });
        });
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }

    private getData() {
        this.isLoading = true;
        this.auditService.getAuditData(this.objectID, this.objectType, this.currentPageNumber, this.rowsPerPage, this.sortOrder, this.sortField, this.filters)
            .then(result => {
                this.isLoading = false;
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

    private export() {
        this.auditService.exportToExcel(this.objectID, this.objectType, this.objectName, this.filters);
    }
}
