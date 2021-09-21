import { Input, Component, SimpleChange, ViewChild, OnDestroy } from '@angular/core';
import { RulesService } from '../../services/rules.service';
import { BaseComponent } from '../shared/base.component';
import { LazyLoadEvent } from 'primeng/api';
import { Table } from 'primeng/table';
import { RuleResultPagedResults } from '../../models/rule.model';
import { SortOrder } from '../../models/enums.model';
import { GridColumn, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression } from '../../models/grid-definition.model';
import { RuleColumnFilterComponent } from './rule-column-filter.component'
import { Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { AdvancedFilterFieldType, Filters } from '../assets-grid/advanced-filtering/advanced-filtering.models';
import { ActivatedRoute } from '@angular/router';
import { FieldType } from "../../models/fieldtype-api.model";
import { Observable, of } from "rxjs";
import { CompanySettingsService } from '../../services/settings.service';
import { CompanySettingEnum } from '../../models/settings.model';

@Component({
    selector: 'd3s-rule-results-grid',
    templateUrl: './rule-results-grid.component.html',
    providers: [RulesService, CompanySettingsService],
    styleUrls: ['rule-results-grid.component.less']
})

export class RuleResultsGridComponent extends BaseComponent implements OnDestroy {

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

    @ViewChild(RuleColumnFilterComponent, { static: false }) private filtersComponent: RuleColumnFilterComponent;

    currentPageNumber: number = 0;
    sortField: string = "";
    sortOrder: SortOrder = SortOrder.None;
    filters: GridFilterExpression[] = [];
    relationships: GridRelationshipFilterExpression;

    searchValue: string = "";
    simpleSearchID: number = 0;
    searchDelayMilliSeconds: number = 300;
    isLoading: boolean = false;
    isDebugMode = false;

    getRuleResultsSub: Subscription;

    ruleResultsExportTooltip: string = "Export to Excel";
    ruleResultsExportEnabled: boolean = true;
    ruleResultsExportLimit: number = 0;

    constructor(
        private ruleService: RulesService,
        private route: ActivatedRoute,
        private settings: CompanySettingsService
    ) {
        super();

        this.route.queryParams.subscribe((params) => {
            if (params["debug"]) {
                this.isDebugMode = true;
            }
        });
    }

    public filterGridData(filterData) {
        this.currentPageNumber = 0;
        this.getData();
    }

    ngOnDestroy() {
        if (this.getRuleResultsSub) {
            this.getRuleResultsSub.unsubscribe();
        }
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

        this.settings.getSettings()
            .subscribe((data) => {
                this.ruleResultsExportLimit = data.MaxExcelExportRows;

                this.isLoading = true;
                if (this.getRuleResultsSub) {
                    this.getRuleResultsSub.unsubscribe();
                }
                this.getRuleResultsSub = this.ruleService
                    .getResultsByRule(this.ruleUid, this.currentPageNumber, this.rowsPerPage, this.sortField, this.sortOrder, false, null, this.simpleTextFilter, this.newAdvancedFilters?.filter)
                    .pipe(debounceTime(300))
                    .subscribe((res) => {
                        this.results = res;
                        if (this.results != null) {
                            this.totalRecords = this.results.total;
                            if (this.totalRecords > this.ruleResultsExportLimit) {
                                this.ruleResultsExportTooltip = `Number of items is greater than ${this.ruleResultsExportLimit}.`;
                                this.ruleResultsExportEnabled = false;
                            }
                            else {
                                this.ruleResultsExportTooltip = "Export to Excel";
                                this.ruleResultsExportEnabled = true;
                            }
                            this.items = this.results.items;
                            this.items.forEach((item) => {
                                var date = new Date(item.RunDate as string);
                                date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
                                item.RunDate = date;
                            });
                            this.isLoading = false;
                        }
                    },
                        err => {
                            this.isLoading = false;
                        }
                    );
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

    doExport() {
        this.ruleService.getResultsByRule(this.ruleUid, this.currentPageNumber, this.rowsPerPage, this.sortField, this.sortOrder, true, this.ruleId, this.simpleTextFilter, this.newAdvancedFilters?.filter);
    }

    formatPath(s: string) {
        return s ? s.replace(/ > /g, '<i class="fa fa-angle-right assetpathseparator"></i>') : s;
    }

    resetFilters() {
        this.simpleTextFilter = '';
        this.filtersComponent.resetFilters();
    }

    onFiltersLoaded() {
        this.getData();
    }

    newAdvancedFilters: Filters;
    advancedFiltersChanged($event) {
        this.newAdvancedFilters = $event;
        this.getData();
    }

    getFieldsObs(): Observable<AdvancedFilterFieldType[]> {
        var fields: AdvancedFilterFieldType[] = [];
        fields.push({
            Name: "EvaluatedAssetClass", FriendlyName: "Asset Class", Type: new FieldType("Lookup"), Category: "", RemovePopulatedOperator: true
        });
        fields.push({
            Name: "EvaluatedAssetTypePath", FriendlyName: "Asset Type", Type: new FieldType("Path"), Category: ""
        });
        fields.push({
            Name: "EvaluatedAssetDisplayPath", FriendlyName: "Asset", Type: new FieldType("Path"), Category: ""
        });
        fields.push({
            Name: "RunDate", FriendlyName: "Run Date", Type: new FieldType("DateTime"), Category: ""
        });
        fields.push({
            Name: "EffectiveDate", FriendlyName: "Effective Date", Type: new FieldType("Date"), Category: ""
        });
        let passFraction = new FieldType("Decimal");
        passFraction.Decimal.Validation.MinimumValue = 0;
        passFraction.Decimal.Validation.MaximumValue = 1;
        fields.push({
            Name: "PassFraction", FriendlyName: "Pass Fraction", Type: passFraction, Category: ""
        });
        let notNegativeNumber = new FieldType("Number");
        notNegativeNumber.Number.Validation.MinimumValue = 0;
        fields.push({
            Name: "PassCount", FriendlyName: "Rows Passed", Type: notNegativeNumber, Category: ""
        });
        fields.push({
            Name: "FailCount", FriendlyName: "Rows Failed", Type: notNegativeNumber, Category: ""
        });
        fields.push({
            Name: "TotalCount", FriendlyName: "Total Rows", Type: notNegativeNumber, Category: ""
        });
        fields.push({
            Name: "Outdated", FriendlyName: "Outdated Rule Result", Type: new FieldType("Boolean"), Category: ""
        });
        var staticObs = of(fields);
        return staticObs;
    }
}
