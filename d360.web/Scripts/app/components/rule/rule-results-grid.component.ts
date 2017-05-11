import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { RulesService } from '../../services/rules.service';
import { BaseComponent } from '../shared/base.component';
import { LazyLoadEvent, DataTable } from 'primeng/primeng';
import { Rule, RuleResult, RuleResultPagedResults, RuleResultFilter } from '../../models/rule.model';
import { SortOrder } from '../../models/enums.model';
import { GridDefinition, GridColumn, GridField, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression } from '../../models/grid-definition.model';
import { RuleColumnFilterComponent } from './rule-column-filter.component'

@Component({
    selector: 'd3s-rule-results-grid',
    template: `                 
                <header>
                    Results
                    <d3s-tile-actions [hasAdd]="false" [hasExport]="true" (exportClick)="doExport()" hasFilterMode="true" [filterMode]="showSimpleFilter" (filterModeChange)="showSimpleFilter=$event;resetFilters();" [hasRefresh]="true" (refreshClick)="getData();"></d3s-tile-actions>
                </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading">
                        <div *ngIf="showSimpleFilter">                                                
                            <input type="text" style="width: 100%;" maxlength="200" (keyup)="checkSimpleSearchEnter($event,dt);" [(ngModel)]="simpleTextFilter" placeholder="Search..." autofocus autocomplete="off" />                            
                        </div>
                        <d3s-rule-column-filter [hidden]="showSimpleFilter" [(attributeFilter)]="attributes" [(relationshipFilter)]="relationships" [(filters)]="filters" [fields]="filtercolumns" (filterChanged)="filterGridData($event)"></d3s-rule-column-filter>
                        <p-dataTable #dt [lazy]="true" [totalRecords]="results?.total" scrollable="true" scrollWidth="100%" [value]="results?.results" selectionMode="single" [rows]="rowsPerPage" paginator="true" pageLinks="3" (onLazyLoad)="loadRuleResultsLazy($event)" [rowsPerPageOptions]="[5,10,20]" [responsive]="true" [stacked]="stacked">                                                                       
                            <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                            <p-column field="RunDate" header="Run Date" [sortable]="true" [style]="{width:'150px'}">
                                <template let-col let-item="rowData" pTemplate type="body">
                                    <span>{{item.RunDate | date : 'short'}}</span>
                                </template>
                            </p-column>
                            <p-column field="EffectiveDate" header="Effective Date" [sortable]="true" [style]="{width:'120px'}">
                                <template let-col let-item="rowData" pTemplate type="body">
                                    <span>{{item.EffectiveDate | date : 'short'}}</span>
                                </template>
                            </p-column>
                            <p-column field="PassFraction" header="Pass Fraction" [sortable]="true" [style]="{width:'150px'}"></p-column>
                            <p-column field="RowsPassed" header="Rows Passed" [sortable]="true" [style]="{width:'150px'}"></p-column>
                            <p-column field="RowsFailed" header="Rows Failed" [sortable]="true" [style]="{width:'150px'}"></p-column>
                            <p-column field="Passed" header="Passed" [sortable]="true" [style]="{width:'150px'}">
                                <template let-item="rowData" pTemplate type="body">
                                    <i *ngIf="item.Passed" class="fa fa-check enabled" title="Passed"></i>
                                    <i *ngIf="!item.Passed" class="fa fa-times disabled" title="Failed"></i>
                                </template>
                            </p-column>
                            <p-column field="FusionAttribute" header="Fusion" [sortable]="true" [style]="{width:'200px'}"></p-column>
                            <p-column *ngFor="let q of results?.qualifiers" [field]="q.Field" [header]="q.Header" [sortable]="true" [style]="{width:'200px'}"></p-column>
                        </p-dataTable>
                </span>                
                `,
    providers: [RulesService],
})

export class RuleResultsGridComponent extends BaseComponent implements OnInit {

    @Input() implementationId: number;

    //@Input() rule: any;
    //@Output() ruleChange = new EventEmitter();

    simpleTextFilter: string;
    showSimpleFilter: boolean = true;
    
    private rowsPerPage: number = 10;
    private results: RuleResultPagedResults;
    columns: GridColumn[] = [];
    filtercolumns: GridFilterColumn[] = [];

    @ViewChild(RuleColumnFilterComponent) private filtersComponent: RuleColumnFilterComponent;

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
            //this.getFieldsDefinition();
            this.getData();
        }
    }
    
    //getFieldsDefinition() {
    //    this.isLoading = true;

    //    //this.gridDefinitionService.getGridDefinition(this.fusionObjectID, this.fusionObject, this.fusionId, 'FusionID')
    //    //    .then(result => {
    //    //        if (result) {
    //    //            this.columns = result.Columns;
    //    //            this.filtercolumns = result.FilterColumns;
    //    //        }                
    //    //        this.isLoading = false;
    //    //    });
    //}

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
                    //console.log("REMOVING FILTER", i);
                    this.filters.splice(i, 1);
                }
            }
        }

        this.ruleService.getResultsByRule(this.implementationId, this.currentPageNumber, this.rowsPerPage, this.sortField, this.sortOrder, this.filters, this.relationships, this.attributes, this.simpleTextFilter)
            .then(res => {
                this.results = res;

                //if (!this.rule && this.results && this.results.results && this.results.results.length > 0) {
                //    this.rule = this.results.results[0];
                //    this.ruleChange.emit(this.rule);
                //}
            });

    }

    private loadRuleResultsLazy(event: LazyLoadEvent) {
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

    private checkSimpleSearchEnter(event, dt: DataTable) {
        if (event.keyCode == 13) this.doSimpleSearch(dt);
        else {
            if (this.simpleSearchID > 0) {
                window.clearTimeout(this.simpleSearchID);
                this.simpleSearchID = 0;
            }

            this.simpleSearchID = window.setTimeout(() => this.doSimpleSearch(dt), this.searchDelayMilliSeconds);

        }
    }

    private doSimpleSearch(dt: DataTable) {
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