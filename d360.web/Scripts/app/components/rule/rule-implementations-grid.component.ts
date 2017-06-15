import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { RulesService } from '../../services/rules.service';
import { BaseComponent } from '../shared/base.component';
import { LazyLoadEvent, DataTable } from 'primeng/primeng';
import { Rule, RuleImplementation, RuleImplementationPagedResults, RuleImplementationFilter } from '../../models/rule.model';
import { SortOrder } from '../../models/enums.model';
import { GridDefinition, GridColumn, GridField, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression } from '../../models/grid-definition.model';
import { RuleColumnFilterComponent } from './rule-column-filter.component'
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-rule-implementations-grid',
    template: `
                <header>
                    Implementations
                    <d3s-tile-actions [hasExport]="true" (exportClick)="doExport()" hasFilterMode="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
                </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div *ngIf="!isLoading">
                    <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter"> 
                    <p-dataTable #dt [value]="results" [globalFilter]="gb" selectionMode="single" [(selection)]="selected" [rows]="rowsPerPage" paginator="true" pageLinks="3" [rowsPerPageOptions]="[5,10,20]" [responsive]="true" [stacked]="stacked" (onRowDblclick)="selected=$event.data;showRuleImplementation(selected);">
                        <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                        <p-column field="CreatedOn" header="Create Date" [sortable]="true" [filter]="!showSimpleFilter" [style]="{width:'120px'}">
                            <template let-col let-item="rowData" pTemplate type="body">
                                <span>{{item.CreatedOn | date : 'shortDate'}}</span>
                            </template>
                        </p-column>
                        <p-column field="UpdatedOn" header="Update Date" [sortable]="true" [style]="{width:'120px'}" [filter]="!showSimpleFilter">
                            <template let-col let-item="rowData" pTemplate type="body">
                                <span>{{item.UpdatedOn | date : 'shortDate'}}</span>
                            </template>
                        </p-column>
                        <p-column field="Name" header="Name" [sortable]="true" [style]="{width:'150px'}" [filter]="!showSimpleFilter">
                            <template pTemplate type="body" let-item="rowData">
                                <a (click)="showRuleImplementation(item);">{{item.Name}}</a>
                            </template>
                        </p-column>
                        <p-column field="SourceID" header="Source Identifier" [sortable]="true" [style]="{width:'150px'}" [filter]="!showSimpleFilter"></p-column>
                        <p-column field="SourceUri" header="" [sortable]="true" [style]="{width:'35px'}" [filter]="!showSimpleFilter">
                            <template let-item="rowData" pTemplate type="body">
                                <a *ngIf="item.SourceUri == null"><i class="fa fa-info" title="Source Uri"></i></a>
                                <a *ngIf="item.SourceUri != null" [href]="item.SourceUri"><i class="fa fa-info" title="Source Uri"></i></a>
                            </template>
                        </p-column>
                    </p-dataTable>
                </div>                
                `,
    providers: [RulesService],
})

export class RuleImplementationsGridComponent extends BaseComponent implements OnInit {

    @Input() ruleId: number;
    
    private selected: RuleImplementation;
    private rowsPerPage: number = 10;
    private results: RuleImplementation[];//RuleImplementationPagedResults;
    columns: GridColumn[] = [];
    fields: GridField[] = [];
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
    
    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private ruleService: RulesService) {
        super();
    }

    ngOnInit() {
       
    }
    
    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['ruleId'] && this.ruleId) {
            this.filters = [];
            this.getData();
        }
    }
    
    public filterGridData(filterData) {     
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
                if (!this.filters[i].field || !this.filters[i].value) {
                    //console.log("REMOVING FILTER", i);
                    this.filters.splice(i, 1);
                }
            }
        }

        this.ruleService.getRuleImplementations(this.ruleId)//, this.currentPageNumber, this.rowsPerPage, this.sortField, this.sortOrder, this.filters, this.simpleTextFilter)
            .then(res => {
                this.results = res;
            });

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
        this.ruleService.getResultsByRuleExcel(this.ruleId);
    }

    resetFilters() {
        this.filtersComponent.resetFilters();
    }

    private showRuleImplementation(impl) {
        this.router.navigateByUrl(SiteUrlHelpers.getDeepObjectUrl('ruleimplementation', impl.RuleTypeID, impl.RuleID, impl.ID));
    }
};