import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { Observable, forkJoin, ReplaySubject } from "rxjs";
import { ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SearchStateService } from './search-state.service';
import { SearchResultsObject, SearchCategories, SearchSelecton, SearchFieldFilter, SearchConnector, SearchOperator } from '../../models/search-result.model';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { DataProfileService } from '../../services/dataprofile.service';
import { SidePanelButton } from "../../models/side-panel.model";
import { AdvancedFilterFieldConditionCollection, AdvancedFilterFieldCondition, AdvancedFilterFieldType } from "../assets-grid/advanced-filtering/advanced-filtering.models";
import { DatePipe } from "@angular/common";
import { CheckTree } from "../shared/small-widgets/check-tree/check-tree.component";
import { CheckTreeNode } from '../shared/small-widgets/check-tree/checktreenode';
import { PopupMenuItem } from '../shared/controls/popup-menu/popup-menu.component';
import { FieldType } from '../../models/fieldtype-api.model';
import { AdvancedFilteringComponent } from '../assets-grid/advanced-filtering/advanced-filtering.component';
import { Operator } from '../../models/operator.model';
import { SelectItem } from '../../models/form.model';
import { CompanySettingsService } from '../../services/settings.service';
import { CompanySettingEnum } from '../../models/settings.model';

@Component({
    selector: 'd3s-search',
    templateUrl: './search.component.html',
    providers: [DataProfileService],
    styleUrls: ["search.component.less"],
})

export class SearchComponent extends BaseComponent implements OnInit {
    public searchResults: SearchResultsObject;
    public categories: SearchCategories[] = [];
    public searchText: string;
    public searchTypes: string[] = [];

    public resultsPerPage: number = 25;
    public fromNumber: number = 0;
    public sub: any;
    public selection: SearchSelecton;

    public sidePanelOpen: boolean = true;
    public sidePanelLoading: boolean = false;
    public sidePanelTab: string;
    public sidePanelStorageKey: string = "searchresults";
    public hasProfiling: boolean = false;
    public dataProfile: any;
    public advancedFiltersLoaded: boolean = false;

    public extraButtons: SidePanelButton[] = [new SidePanelButton({
        label: 'Filters',
        tooltip: 'Filters',
        disabledTooltip: null,
        nothingSelectedMessage: 'Filters not available',
        notApplicableMessage: 'Filters not available',
        multipleSelectedMessage: 'Filters not available',
        key: 'filters',
        icon: 'fa-filter',
        disabled: false,
        visible: true,
        needsSelection: false,
        panelMenu: [
            new PopupMenuItem({
                title: "Expand All",
                callback: () => this.filterExpandAll()
            }),
            new PopupMenuItem({
                title: "Collapse All",
                callback: () => this.filterCollapseAll()
            })
        ]
    })];

    public filterFields$: Observable<AdvancedFilterFieldType[]>;
    private filterFieldsSubject: ReplaySubject<AdvancedFilterFieldType[]> = new ReplaySubject(1);

    @ViewChild("title", { static: false }) title: ElementRef;
    @ViewChild("catagoryFilter", { static: false }) catagoryFilter: CheckTree;
    @ViewChild("advancedFilter", { static: false }) advancedFilter: AdvancedFilteringComponent;

    constructor(private route: ActivatedRoute,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected secondaryNavService: SecondaryNavService,
        public searchStateService: SearchStateService,
        private dataProfileService: DataProfileService,
        protected settingsService: CompanySettingsService,
        private datePipe: DatePipe) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.filterFields$ = this.filterFieldsSubject.asObservable();
    }

    ngOnInit() {
        this.advancedFiltersLoaded = false;
        this.setFieldsObsservable();

        this.setBrowserTitle(this.titleService, 'Search Results');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Search Results'));

        this.secondaryNavService.clearItems();
        this.secondaryNavService.clearButtons();
        this.secondaryNavService.clearCurrentObject();
        this.secondaryNavService.setCurrentArea('Search Results', 'fa-search', null);
        this.secondaryNavService.showHeader(false);
        this.searchStateService.advancedFilters = [];

        this.searchTypes = this.settingsService.getSettingById(CompanySettingEnum.DefaultSearchTypes).ScalarValue.split(',');

        this.sub = this.route.queryParams.subscribe((params) => {
            this.searchText = params['query'] ? params['query'] : '';
            if (params['types'] != undefined) {
                this.searchTypes = params['types'].split(',').filter((x): x is string => x.length > 0);
            }
            let keepFilter = params['f'] ? (params['f'] == 1 ? true : false) : false;
            this.searchStateService.loadState(this.searchText, this.searchTypes, keepFilter);
            if (params['explain'] != undefined) {
                this.searchStateService.setExplain(params['explain'] == 'please');
            }
        });
    }

    resultSelected($event) {
        this.selection = $event;
        if (this.selection && this.selection.HasProfiling) {
            this.sidePanelLoading = true;
            this.dataProfileService.getDataProfiles(this.selection.AssetUid).subscribe(
                (r) => {
                    if (r && r.items && r.items.length > 0) {
                        this.dataProfile = r.items[0];

                        forkJoin(
                            this.dataProfileService.getMatchCounts(this.dataProfile.assetUid, 'Structure'),
                            this.dataProfileService.getMatchCounts(this.dataProfile.assetUid, 'Data')
                        ).subscribe((res) => {
                            this.dataProfile['matches'] = {
                                structure: res[0],
                                data: res[1]
                            };
                        });
                    }
                    this.sidePanelLoading = false;
                });
        } else {
            this.dataProfile = null;
        }
    }

    get panelApplies(): boolean {
        let ret = false;
        if (this.sidePanelTab === "filters" || this.selection == null) {
            ret = true;
        } else if (this.sidePanelTab === "detail") {
            ret = this.selection?.AssetUid !== null;
        } else if (this.sidePanelTab === "dataprofile") {
            ret = this.selection.HasProfiling;
        }
        return ret;
    }

    //Class/assettype selection changed
    public filterCheckTree(selectedNodes: CheckTreeNode[]) {
        this.doSearch(true);
    }
    public doSearch(resetPage: boolean = false) {
        this.resultSelected(null);
        this.searchStateService.search(this.searchText, resetPage);
    }

    paginate(data) {
        /*
            event.page: New page number
            event.first: Index of first record
            event.rows: Number of rows to display in new page            
            event.pageCount: Total number of pages
        */
        this.resultSelected(null);
        this.searchStateService.page(data.first, data.size);
    }

    public filterClear() {
        this.catagoryFilter?.clearSelection();
    }
    public filterExpandAll() {
        this.catagoryFilter?.expandAll();
    }
    public filterCollapseAll() {
        this.catagoryFilter?.collapseAll();
    }

    private setFieldsObsservable() {
        var fields: AdvancedFilterFieldType[] = [];
        fields.push({
            Name: "Name", FriendlyName: "Name", Type: new FieldType("Text"), Category: "", RemovePopulatedOperator: true
        });
        fields.push({
            Name: "Description", FriendlyName: "Description", Type: new FieldType("Text"), Category: "", RemovePopulatedOperator: true
        });
        fields.push({
            Name: "Tags", FriendlyName: "Tags", Type: new FieldType("Tag"), Category: "", RemovePopulatedOperator: true
        });
        this.filterFieldsSubject.next(fields);
        this.filterFieldsSubject.complete();
    }

    public getSavedFilters(): string {
        let state: AdvancedFilterFieldConditionCollection = new AdvancedFilterFieldConditionCollection();
        state.connector = ` ${this.parseConnectorToString(this.searchStateService.connector)} `;
        state.filters = this.searchStateService.advancedFilters.map((af) => {
            const op: any = af.Operator === SearchOperator.NotContains ? Operator[Operator.NotContains] : Operator[Operator.Contains];
            let condition: AdvancedFilterFieldCondition = new AdvancedFilterFieldCondition(this.datePipe);
            condition.field = af.Field;
            condition.exact = af.MatchWords;
            if (af.Field === "Tags") {
                condition.value = af.Values.map((v) => { return { title: v, value: v }; });
                condition.fieldType = "Tag";
            } else {
                condition.value = af.Values[0];
                condition.fieldType = "Text";
            }
            condition.connectingOperator = this.parseConnectorToString(af.Connector);
            condition.operator = op;
            condition.isRelationship = false;
            condition.markForDeletion = false;
            condition.isDefaultFilter = false;
            return condition;
        });
        return JSON.stringify(state);
    }

    public onFiltersLoaded() {
        this.advancedFiltersLoaded = true;
        if (this.searchText.length > 0) {
            this.doSearch();
        }
    }

    private parseOperator(op: string): SearchOperator {
        if (Operator[`${op}`] === Operator.Contains) {
            return SearchOperator.Contains;
        } else if (Operator[`${op}`] === Operator.NotContains) {
            return SearchOperator.NotContains;
        } else {
            return null;
        }
    }

    private parseConnector(conn: string): SearchConnector {
        const c = conn.trim();
        if (c === "or") {
            return SearchConnector.Or;
        } else if (c === "and") {
            return SearchConnector.And;
        } else {
            return null;
        }
    }

    private parseConnectorToString(conn: SearchConnector): string {
        if (conn === SearchConnector.And) {
            return "and";
        } else if (conn === SearchConnector.Or) {
            return "or";
        }
        return "";
    }

    public advancedFiltersChanged($event) {
        if (this.advancedFiltersLoaded) {
            const flts: SearchFieldFilter[] = this.advancedFilter.conditions.filters
                .filter((x) => x.field && x.operator && x.markForDeletion !== true)
                .map((f) => {
                    return {
                        Field: f.field,
                        Values: Array.isArray(f.value) ? (f.value as SelectItem[]).map((i) => i.value) : [f.value],
                        MatchWords: f.exact,
                        Operator: this.parseOperator(f.operator + ""),
                        Connector: this.parseConnector(f.connectingOperator)
                    };
                });
            this.searchStateService.advancedFilters = flts;
            this.searchStateService.connector = this.parseConnector(this.advancedFilter.conditions.connector);
            this.doSearch(true);
        }
    }
}