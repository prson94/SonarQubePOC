import { Input, Component, SimpleChange, ViewChild } from '@angular/core';
import { RulesService } from '../../services/rules.service';
import { BaseComponent } from '../shared/base.component';
import { LazyLoadEvent } from 'primeng/api';
import { Table } from 'primeng/table';
import { RuleResultPagedResults } from '../../models/rule.model';
import { SortOrder } from '../../models/enums.model';
import { GridColumn, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression  } from '../../models/grid-definition.model';
import { RuleColumnFilterComponent } from './rule-column-filter.component'
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-rule-results-grid',
    templateUrl: './rule-results-grid.component.html',
    providers: [RulesService],
})

export class RuleResultsGridComponent extends BaseComponent {

    @Input() ruleId: number;
    @Input() ruleUid: string;
    @Input() showTitle: boolean = true;    

    simpleTextFilter: string;
    showSimpleFilter: boolean = false;

    rowsPerPage: number = 25;
    totalRecords: number = 0;
    results: RuleResultPagedResults;
    items;
    columns: GridColumn[] = [];
    filtercolumns: GridFilterColumn[] = [];

    @ViewChild(RuleColumnFilterComponent, {static:false}) private filtersComponent: RuleColumnFilterComponent;

    currentPageNumber: number = 0;
    sortField: string = "";
    sortOrder: SortOrder = SortOrder.None;
    filters: GridFilterExpression[] = [];
    relationships: GridRelationshipFilterExpression;

    searchValue: string = "";
    simpleSearchID: number = 0;
    searchDelayMilliSeconds: number = 300;
    isLoading: boolean = false;

    constructor(private ruleService: RulesService) {
        super();
    }    

    public filterGridData(filterData) {
        this.currentPageNumber = 0;
        this.getData();        
    }

    getData() {

        if (!this.ruleId) {
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

        this.isLoading = true;
        this.ruleService.getResultsByRule(this.ruleUid, this.currentPageNumber, this.rowsPerPage, this.sortField, this.sortOrder)
            .subscribe(res => {
                this.results = res;
                if (this.results != null) {
                    this.totalRecords = this.results.total;
                    this.items = this.results.items;  
                    this.isLoading = false;
                }                
            },
            err => {
                this.isLoading = false;
            });       
    }

    loadRuleResultsLazy(event: LazyLoadEvent) {
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

    checkSimpleSearchEnter(event, dt: Table) {
        if (event.keyCode == 13) this.doSimpleSearch(dt);
        else {
            if (this.simpleSearchID > 0) {
                window.clearTimeout(this.simpleSearchID);
                this.simpleSearchID = 0;
            }

            this.simpleSearchID = window.setTimeout(() => this.doSimpleSearch(dt), this.searchDelayMilliSeconds);
        }
    }

    doSimpleSearch(dt: Table) {
        if (dt) dt.reset();
        this.getData();
    }

    doExport() {
        this.ruleService.getResultsByRule(this.ruleUid, this.currentPageNumber, this.rowsPerPage, this.sortField, this.sortOrder, true, this.ruleId);        
    }

    formatPath(s: string) {
        return s ? s.replace(/ > /g, '<i class="fa fa-angle-right assetpathseparator"></i>') : s;
    }

    resetFilters() {
        this.simpleTextFilter = '';
        this.filtersComponent.resetFilters();
    }
}
