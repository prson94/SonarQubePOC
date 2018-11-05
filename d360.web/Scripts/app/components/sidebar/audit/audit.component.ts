import { Component, Input, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
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
                            
                            <p-table #dt [loading]="isLoading" loadingIcon="fa fa-spinner" [scrollable]="true" scrollWidth="100%" [lazy]="true" (onLazyLoad)="loadAuditsLazy($event)" [totalRecords]="totalRecords" [value]="audits" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['ResourceName','Date','Action','Field','NewValue','PreviousValue','ActionObject','ActionObjectTypeName','ActionObjectName','ActionDescription','Version']" [pageLinks]="3" [paginator]="true" [rows]="rowsPerPage" [rowsPerPageOptions]="defaultPagingOptions" [(selection)]="selected">
                                <ng-template pTemplate="colgroup" let-columns>
                                    <colgroup>
                                        <col style="width:150px">
                                        <col style="width:200px">
                                        <col style="width:100px">
                                        <col style="width:200px">
                                        <col style="width:250px">
                                        <col style="width:250px">
                                        <col style="width:100px">
                                        <col style="width:100px">
                                        <col style="width:100px">
                                        <col style="width:250px">
                                        <col style="width:100px">
                                    </colgroup>
                                </ng-template>
                                <ng-template pTemplate="header">
                                    <tr>
                                        <th [pSortableColumn]="'ResourceName'" style="width: 150px">
                                            User
                                            <d3s-sortIcon [field]="'ResourceName'"></d3s-sortIcon>
                                        </th>
                                        <th [pSortableColumn]="'Date'" style="width: 200px">
                                            Date
                                            <d3s-sortIcon [field]="'Date'"></d3s-sortIcon>
                                        </th>
                                        <th [pSortableColumn]="'Action'" style="width: 100px">
                                            Action
                                            <d3s-sortIcon [field]="'Action'"></d3s-sortIcon>
                                        </th>
                                        <th [pSortableColumn]="'Field'" style="width: 200px">
                                            Field
                                            <d3s-sortIcon [field]="'Field'"></d3s-sortIcon>
                                        </th>
                                        <th [pSortableColumn]="'NewValue'" style="width: 250px">
                                            New Value
                                            <d3s-sortIcon [field]="'NewValue'"></d3s-sortIcon>
                                        </th>
                                        <th [pSortableColumn]="'PreviousValue'" style="width: 250px">
                                            Previous Value
                                            <d3s-sortIcon [field]="'PreviousValue'"></d3s-sortIcon>
                                        </th>
                                        <th [pSortableColumn]="'ActionObject'" style="width: 100px">
                                            Object
                                            <d3s-sortIcon [field]="'ActionObject'"></d3s-sortIcon>
                                        </th>
                                        <th [pSortableColumn]="'ActionObjectTypeName'" style="width: 100px">
                                            Type
                                            <d3s-sortIcon [field]="'ActionObjectTypeName'"></d3s-sortIcon>
                                        </th>
                                        <th [pSortableColumn]="'ActionObjectName'" style="width: 100px">
                                            Item
                                            <d3s-sortIcon [field]="'ActionObjectName'"></d3s-sortIcon>
                                        </th>
                                        <th [pSortableColumn]="'ActionDescription'" style="width: 250px">
                                            Audit Description
                                            <d3s-sortIcon [field]="'ActionDescription'"></d3s-sortIcon>
                                        </th>
                                        <th [pSortableColumn]="'Version'" style="width: 100px">
                                            Revision
                                            <d3s-sortIcon [field]="'Version'"></d3s-sortIcon>
                                        </th>
                                    </tr>
                                    <tr [hidden]="false">
                                        <th><d3s-column-filter [field]="'ResourceName'"></d3s-column-filter></th>
                                        <th><d3s-column-filter [field]="'Date'"></d3s-column-filter></th>
                                        <th><d3s-column-filter [field]="'Action'"></d3s-column-filter></th>
                                        <th><d3s-column-filter [field]="'Field'"></d3s-column-filter></th>
                                        <th><d3s-column-filter [field]="'NewValue'"></d3s-column-filter></th>
                                        <th><d3s-column-filter [field]="'PreviousValue'"></d3s-column-filter></th>
                                        <th><d3s-column-filter [field]="'ActionObject'"></d3s-column-filter></th>
                                        <th><d3s-column-filter [field]="'ActionObjectTypeName'"></d3s-column-filter></th>
                                        <th><d3s-column-filter [field]="'ActionObjectName'"></d3s-column-filter></th>
                                        <th><d3s-column-filter [field]="'ActionDescription'"></d3s-column-filter></th>
                                        <th><d3s-column-filter [field]="'Version'"></d3s-column-filter></th>
                                    </tr>
                                </ng-template>
                                <ng-template pTemplate="body" let-item>
                                    <tr [pSelectableRow]="item">
                                        <td>{{item.ResourceName}}</td>
                                        <td>
                                            <span>{{item.Date | date: 'medium'}}</span>
                                        </td>
                                        <td>{{item.Action}}</td>
                                        <td>{{item.Field}}</td>
                                        <td>
                                            <div *ngIf="item.NewValue" [innerHtml]="item.NewValue"></div>
                                        </td>
                                        <td>
                                            <div *ngIf="item.PreviousValue" [innerHtml]="item.PreviousValue"></div>
                                        </td>
                                        <td>{{item.ActionObject}}</td>
                                        <td>{{item.ActionObjectTypeName}}</td>
                                        <td>{{item.ActionObjectName}}</td>
                                        <td>{{item.ActionDescription}}</td>
                                        <td>{{item.Version}}</td>
                                    </tr>
                                </ng-template>
                                <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                    <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                                </ng-template>
                            </p-table>                            
                        </div>
                    </div>
                </div>
        `, changeDetection: ChangeDetectionStrategy.OnPush
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
        private objectDetailService: ObjectDetailService,
        private changeDetectorRef: ChangeDetectorRef
    ) {
        super();
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.objectID = +params['objectId']; // (+) converts string 'id' to a number
            this.objectType = params['objectType'];

            this.objectDetailService.getObject(this.objectID, this.objectType).then(res => {
                if (res) this.objectName = res.Name ? res.Name : res.DisplayValue;
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
                this.changeDetectorRef.markForCheck();
            });
    }

    public loadAuditsLazy(event: LazyLoadEvent) {
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

    public export() {
        this.auditService.exportToExcel(this.objectID, this.objectType, this.objectName, this.filters);
    }
}
