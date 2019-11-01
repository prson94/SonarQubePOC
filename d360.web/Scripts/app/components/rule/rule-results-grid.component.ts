import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange, ViewChild } from '@angular/core';
import { RulesService } from '../../services/rules.service';
import { BaseComponent } from '../shared/base.component';
import { LazyLoadEvent } from 'primeng/api';
import { Table } from 'primeng/table';
import { Rule, RuleResult, RuleResultPagedResults, RuleResultFilter } from '../../models/rule.model';
import { SortOrder } from '../../models/enums.model';
import { GridDefinition, GridColumn, GridField, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression } from '../../models/grid-definition.model';
import { RuleColumnFilterComponent } from './rule-column-filter.component'

@Component({
    selector: 'd3s-rule-results-grid',
    template: `                 
                <header>
                    <span *ngIf="showTitle; else noTitle">Results</span>
                    <ng-template #noTitle>&nbsp;</ng-template>
                    <d3s-tile-actions [hasAdd]="false" [hasExport]="true" (exportClick)="doExport()" [hasRefresh]="true" (refreshClick)="getData();"></d3s-tile-actions>
                </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading">
                        <div *ngIf="showSimpleFilter">                                                
                            <input type="text" style="width: 100%;" maxlength="200" (keyup)="checkSimpleSearchEnter($event,dt);" [(ngModel)]="simpleTextFilter" placeholder="Search..." autofocus autocomplete="off" />                            
                        </div>
                        <d3s-rule-column-filter [hidden]="showSimpleFilter" [(attributeFilter)]="attributes" [(relationshipFilter)]="relationships" [(filters)]="filters" [fields]="filtercolumns" (filterChanged)="filterGridData($event)"></d3s-rule-column-filter>
                        <p-table #dt 
                            [value]="items" 
                            selectionMode="single" 
                            [metaKeySelection]="true" 
                            [globalFilterFields]="['RunDate','EffectiveDate','PassFraction','RowsPassed','RowsFailed','Passed','FusionAttribute']" 
                            [pageLinks]="3" 
                            [paginator]="true" 
                            [rows]="rowsPerPage" 
                            [rowsPerPageOptions]="[5,10,20]"  
                            [lazy]="true"  
                            (onLazyLoad)="loadRuleResultsLazy($event)" 
                            [totalRecords]="totalRecords" 
                            [scrollable]="true" 
                            scrollWidth="100%">
                            <ng-template pTemplate="colgroup" >
                                <colgroup>
                                    <col style="width: 150px">
                                    <col style="width: 120px">
                                    <col style="width: 150px">
                                    <col style="width: 150px">
                                    <col style="width: 150px">
                                    <col style="width: 150px">
                                    <col style="width: 200px">
                                    <col style="width: 200px">
                                </colgroup>
                            </ng-template> 
                            <ng-template pTemplate="header">
                                <tr>
                                    <th [pSortableColumn]="'RunDate'">
                                        Run Date
                                        <d3s-sortIcon [field]="'RunDate'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'EffectiveDate'">
                                        Effective Date
                                        <d3s-sortIcon [field]="'EffectiveDate'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'PassFraction'">
                                        Pass Fraction
                                        <d3s-sortIcon [field]="'PassFraction'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'RowsPassed'">
                                        Rows Passed
                                        <d3s-sortIcon [field]="'RowsPassed'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'RowsFailed'">
                                        Rows Failed
                                        <d3s-sortIcon [field]="'RowsFailed'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'Passed'">
                                        Passed
                                        <d3s-sortIcon [field]="'Passed'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'FusionAttribute'">
                                        Fusion
                                        <d3s-sortIcon [field]="'FusionAttribute'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="''"></th>
                                </tr>
                                <tr [hidden]="showSimpleFilter">
                                    <th></th>
                                    <th></th>
                                    <th></th>
                                    <th></th>
                                    <th></th>
                                    <th></th>
                                    <th></th>
                                    <th></th>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="body" let-item>
                                <tr [pSelectableRow]="item">
                                    <td>
                                            <span>{{item.RunDate | date : 'short'}}</span>
                                    </td>
                                    <td>
                                            <span>{{item.EffectiveDate | date : 'shortDate'}}</span>
                                    </td>
                                    <td>{{item.PassFraction}}</td>
                                    <td>{{item.RowsPassed}}</td>
                                    <td>{{item.RowsFailed}}</td>
                                    <td>
                                            <i *ngIf="item.Passed" class="fa fa-check enabled" title="Passed"></i>
                                            <i *ngIf="!item.Passed" class="fa fa-times disabled" title="Failed"></i>
                                    </td>
                                    <td>{{item.FusionAttribute}}</td>
                                    <td></td>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="summary">
                                <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                            </ng-template>
                        </p-table>
                </span>                
                `,
    providers: [RulesService],
})

export class RuleResultsGridComponent extends BaseComponent implements OnInit {

    @Input() implementationId: number;
    @Input() showTitle: boolean = true;

    simpleTextFilter: string;
    showSimpleFilter: boolean = false;

    private rowsPerPage: number = 5;
    private totalRecords: number = 0;
    private results: RuleResultPagedResults;
    private items;
    columns: GridColumn[] = [];
    filtercolumns: GridFilterColumn[] = [];

    @ViewChild(RuleColumnFilterComponent, {static:false}) private filtersComponent: RuleColumnFilterComponent;

    currentPageNumber: number = 0;
    sortField: string = "";
    sortOrder: SortOrder = SortOrder.None;
    filters: GridFilterExpression[] = [];
    relationships: GridRelationshipFilterExpression;
    attributes: GridAttributeFilterExpression;

    searchValue: string = "";
    simpleSearchID: number = 0;
    searchDelayMilliSeconds: number = 300;

    constructor(private ruleService: RulesService) {
        super();
    }

    ngOnInit() {

    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['implementationId'] && this.implementationId) {
            this.filters = [];
            this.getData();
        }
    }


    public filterGridData(filterData) {
        this.currentPageNumber = 0;
        this.getData();
    }

    private getData() {
        if (!this.implementationId) {
            console.log("ERROR - NO RULE ID");
            return;
        }

        //remove any invalid filters
        if (this.filters && this.filters.length > 0) {
            for (var i = this.filters.length - 1; i >= 0; i--) {
                if (!this.filters[i].field || !this.filters[i].value) {
                    this.filters.splice(i, 1);
                }
            }
        }

        this.ruleService.getResultsByRule(this.implementationId, this.currentPageNumber, this.rowsPerPage, this.sortField, this.sortOrder, this.filters, this.relationships, this.attributes, this.simpleTextFilter)
            .subscribe(res => {
                this.results = res;
                if (this.results != null) {
                    this.totalRecords = this.results.total;
                    this.items = this.results.results;
                }

            });

    }

    private loadRuleResultsLazy(event: LazyLoadEvent) {
        //event.first = First row offset
        //event.rows = Number of rows per page
        //event.sortField = Field name to sort with
        //event.sortOrder = Sort order as number, 1 for asc and -1 for dec
        //filters: FilterMetadata object having field as key and filter value, filter matchMode as value
        console.log(event);
        this.sortOrder = event.sortOrder;
        this.sortField = event.sortField == undefined ? "" : event.sortField;
        this.rowsPerPage = event.rows;
        this.currentPageNumber = event.first / event.rows;

        this.getData();
    }

    private checkSimpleSearchEnter(event, dt: Table) {
        if (event.keyCode == 13) this.doSimpleSearch(dt);
        else {
            if (this.simpleSearchID > 0) {
                window.clearTimeout(this.simpleSearchID);
                this.simpleSearchID = 0;
            }

            this.simpleSearchID = window.setTimeout(() => this.doSimpleSearch(dt), this.searchDelayMilliSeconds);

        }
    }

    private doSimpleSearch(dt: Table) {
        if (dt) dt.reset();
        this.getData();
    }

    private doExport() {
        this.ruleService.getResultsByRuleExcel(this.implementationId);
    }

    resetFilters() {
        this.simpleTextFilter = '';
        this.filtersComponent.resetFilters();
    }
};