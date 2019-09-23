import {Component, Input} from '@angular/core';
import { LazyLoadEvent } from 'primeng/primeng';
import { Table } from 'primeng/table';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

import {SortOrder} from '../../models/enums.model';
import {FusionExecutionResult} from '../../models/fusion.model';

import {FusionService} from '../../services/fusion.service';

import {BaseComponent} from '../shared/base.component';

@Component({
    selector: 'd3s-fusion-execution-results',
    template: `
        <header>Execution History - Result Details
            <d3s-tile-actions [hasExport]="true" (exportClick)="export()"></d3s-tile-actions>
        </header>


        <input type="text" [hidden]="!showSimpleFilter" pInputText size="100"
               (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..."
               class="grid-simple-filter">
        <p-table #dt [value]="results" [loading]="isLoading" loadingIcon="fa fa-spinner" selectionMode="single"
                 [metaKeySelection]="true"
                 [globalFilterFields]="['FusionAttributeType','FusionAttribute','Action','FieldName','OldValue','NewValue']"
                 [pageLinks]="3" [paginator]="true" [rows]="5" [rowsPerPageOptions]="[5,10,20]" [(selection)]="selected"
                 (onLazyLoad)="loadResultsLazy($event)" lazy="true" [totalRecords]="resultCount"
                 [scrollable]="true">
            <ng-template pTemplate="colgroup">
                <colgroup>
                    <col style="width:100px">
                    <col style="width:100px">
                    <col style="width:100px">
                    <col style="width:125px">
                    <col style="width:175px">
                    <col style="width:175px">
                </colgroup>
            </ng-template>
            <ng-template pTemplate="header">
                <tr>
                    <th [pSortableColumn]="'FusionAttributeType'" style="width: 100px">
                        Type
                        <d3s-sortIcon [field]="'FusionAttributeType'"></d3s-sortIcon>
                    </th>
                    <th [pSortableColumn]="'FusionAttribute'" style="width: 100px">
                        Attribute
                        <d3s-sortIcon [field]="'FusionAttribute'"></d3s-sortIcon>
                    </th>
                    <th [pSortableColumn]="'Action'" style="width: 100px">
                        Action
                        <d3s-sortIcon [field]="'Action'"></d3s-sortIcon>
                    </th>
                    <th [pSortableColumn]="'FieldName'" style="width: 125px">
                        Field
                        <d3s-sortIcon [field]="'FieldName'"></d3s-sortIcon>
                    </th>
                    <th [pSortableColumn]="'OldValue'" style="width: 175px">
                        Old Value
                        <d3s-sortIcon [field]="'OldValue'"></d3s-sortIcon>
                    </th>
                    <th [pSortableColumn]="'NewValue'" style="width: 175px">
                        New Value
                        <d3s-sortIcon [field]="'NewValue'"></d3s-sortIcon>
                    </th>
                </tr>
            </ng-template>
            <ng-template pTemplate="body" let-item>
                <tr [pSelectableRow]="item">
                    <td>{{item.FusionAttributeType}}</td>
                    <td>{{item.FusionAttribute}}</td>
                    <td>{{item.Action}}</td>
                    <td>{{item.FieldName}}</td>
                    <td>{{item.OldValue}}</td>
                    <td>{{item.NewValue}}</td>
                </tr>
            </ng-template>
            <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows"
                                      [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
            </ng-template>
        </p-table>
    `,
    providers: [FusionService],
})

export class FusionExecutionResultsComponent extends BaseComponent {
    @Input() executionId: number;
    @Input() rowsPerPage: number = 20;

    private results: FusionExecutionResult[] = [];
    private selected: FusionExecutionResult;
    private resultCount: number = 0;
    private currentPageNumber: number = 0;
    private sortField: string = "";
    private sortOrder: SortOrder = SortOrder.None;
    private simpleTextFilter: string = "";
    private simpleSearchID: number = 0;
    private searchDelayMilliSeconds: number = 300;

    destroySubject$: Subject<void> = new Subject();

    constructor(private fusionService: FusionService) {
        super();

        this.isLoading = true;
    }

    private export() {
        this.fusionService.getFusionExecutionResultsExport(this.executionId, this.simpleTextFilter);
    }

    private getData() {
        this.isLoading = true;

        this.fusionService
            .getFusionExecutionResults(
                this.executionId,
                this.sortField,
                this.sortOrder,
                this.rowsPerPage,
                this.currentPageNumber,
                this.simpleTextFilter
            )
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                res => {
                    this.results = res.results;
                    this.resultCount = res.total;
                    this.isLoading = false;
                    this.selected = this.results.length > 0 ? this.results[0] : null;
                }
            );
    }

    private loadResultsLazy(event: LazyLoadEvent) {
        //event.first = First row offset
        //event.rows = Number of rows per page
        //event.sortField = Field name to sort with
        //event.sortOrder = Sort order as number, 1 for asc and -1 for dec
        //filters: FilterMetadata object having field as key and filter value, filter matchMode as value        
        this.sortOrder = event.sortOrder;
        this.sortField = event.sortField == undefined ? "" : event.sortField;
        this.rowsPerPage = event.rows;
        this.currentPageNumber = event.first / event.rows;
        this.getData();
    }

    private checkSimpleSearchEnter(event, dt: Table) {
        if (event.keyCode == 13) {
            this.doSimpleSearch(dt);
        } else {
            if (this.simpleSearchID > 0) {
                window.clearTimeout(this.simpleSearchID);
                this.simpleSearchID = 0;
            }

            this.simpleSearchID = window.setTimeout(() => this.doSimpleSearch(dt), this.searchDelayMilliSeconds);
        }
    }

    private doSimpleSearch(dt: Table) {
        if (dt) {
            dt.reset();
        }

        this.getData();
    }
}
