import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { RulesService } from '../../services/index';
import { BaseComponent } from '../shared/base.component';
import { LazyLoadEvent } from 'primeng/primeng';
import { Rule, RuleResult, RuleResultPagedResults, RuleResultFilter } from '../../models/rule.model';
import { SortOrder } from '../../models/enums.model';
import { GridDefinition, GridColumn, GridField, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression } from '../../models/grid-definition.model';

@Component({
    selector: 'd3s-rule-results-grid',
    template: `                 
                <div class="tile tile-detail">
                    <header>Values<d3s-tile-actions [hasAdd]="false" [hasExport]="true" (exportClick)="doExport()"></d3s-tile-actions></header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">
                        <d3s-fusion-attribute-summary-filters [filterColumns]="filtercolumns" [filters]="filters" (filtersChange)="doFilterResults($event)"></d3s-fusion-attribute-summary-filters>                 
                        <p-dataTable [lazy]="true" [totalRecords]="results?.total" scrollable="true" scrollWidth="100%" [value]="results?.results" selectionMode="single" [rows]="rowsPerPage" [paginator]="true" [pageLinks]="4" (onLazyLoad)="loadRuleResultsLazy($event)" [rowsPerPageOptions]="[5,10,20]" [responsive]="true" [stacked]="stacked">                                                                       
                            <p-column field="EffectiveDate" header="Effective Date" [sortable]="true"></p-column>  
                            <p-column field="RowsPassed" header="Rows Passed" [sortable]="true" [style]="{width:'20%'}"></p-column>
                            <p-column field="RowsFailed" header="Rows Failed" [sortable]="true" [style]="{width:'20%'}"></p-column>
                            <p-column field="Passed" header="Passed" [sortable]="true" [style]="{width:'10%'}">
                                <template let-item="rowData" pTemplate type="body">
                                    <i *ngIf="item.Passed" class="fa fa-check enabled" title="Passed"></i>
                                    <i *ngIf="!item.Passed" class="fa fa-times disabled" title="Failed"></i>
                                </template>
                            </p-column>
                            <p-column field="CreatedOn" header="Created On" [sortable]="true" [style]="{width:'15%'}"></p-column>
                        </p-dataTable>                   
                    </span>
                </div>
                `,
    providers: [RulesService],
})

export class RuleResultsGridComponent extends BaseComponent implements OnInit {

    @Input() ruleId: number;

    //@Input() rule: any;
    //@Output() ruleChange = new EventEmitter();

    private filters: RuleResultFilter[] = [];
    
    private rowsPerPage: number = 10;
    private results: RuleResultPagedResults;
    columns: GridColumn[] = [];
    filtercolumns: GridFilterColumn[] = [];

    currentPageNumber: number = 0;
    sortField: string = "";
    sortOrder: SortOrder = SortOrder.None;

    
    constructor(private ruleService: RulesService) {
        super();
    }

    ngOnInit() {

    }
    
    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['ruleId'] && this.ruleId) {
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

    private doFilterResults(event) {        
        this.filters = event;
        this.currentPageNumber = 0;        
        this.getData();
    }

    private getData() {
        if (!this.ruleId) {
            console.log("ERROR - NO RULE ID");
            return;
        }

        //remove any invalid filters
        if (this.filters && this.filters.length > 0) {
            for (var i = this.filters.length - 1; i >= 0; i--) {
                if (!this.filters[i].dataField || !this.filters[i].value) {
                    //console.log("REMOVING FILTER", i);
                    this.filters.splice(i, 1);
                }
            }
        }


        this.ruleService.getResultsByRule(this.ruleId, this.currentPageNumber, this.rowsPerPage, this.sortField, this.sortOrder, this.filters)
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

    private doExport() {
        this.ruleService.getResultsByRuleExcel(this.ruleId);
    }
};