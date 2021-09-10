import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { forkJoin } from "rxjs";
import { ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SearchStateService } from './search-state.service';
import { SearchResultsObject, SearchCategories, AdvancedSearchFilter, SearchAggregationFilter, SearchSelecton } from '../../models/search-result.model';
import { CurrentCompanySettings } from '../../static/company-settings'
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { DataProfileService } from '../../services/dataprofile.service';
import { SidePanelButton } from "../../models/side-panel.model";

import { CheckTree } from "../shared/small-widgets/check-tree/check-tree.component";
import { CheckTreeNode } from '../shared/small-widgets/check-tree/checktreenode';
import { PopupMenuItem } from '../shared/controls/popup-menu/popup-menu.component';

declare var CompanySettings;

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
    public isExactMatch: boolean = true;
    public searchTypes: string[] = CurrentCompanySettings.defaultSearchTypes ? CurrentCompanySettings.defaultSearchTypes.split(',') : [];
    public advancedFilters: AdvancedSearchFilter[] = [];

    public resultsPerPage: number = 25;
    public fromNumber: number = 0;
    public sub: any;
    public selection: SearchSelecton;

    public sidePanelOpen: boolean = false;
    public sidePanelLoading: boolean = false;
    public sidePanelTab: string;
    public sidePanelStorageKey: string = "searchresults";
    public hasProfiling: boolean = false;
    public dataProfile: any;

    public extraButtons: SidePanelButton[] = [new SidePanelButton({
        label: 'Filters',
        tooltip: 'Filters',
        disabledTooltip: null,
        nothingSelectedMessage: 'Filters not available',
        notApplicableMessage: 'Filters not available',
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

    newFilterOptions: any[] = [
        { field: "Name", value: 'any' },
        { field: "Description", value: 'any' },
        { field: "Tags", value: 'any' }
    ];

    @ViewChild('title', { static: false }) title: ElementRef;
    @ViewChild('catagoryFilter', { static: false }) catagoryFilter: CheckTree;

    constructor(private route: ActivatedRoute,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected secondaryNavService: SecondaryNavService,
        public searchStateService: SearchStateService,
        private dataProfileService: DataProfileService) {
        super();
        this.secondaryNavService = secondaryNavService;
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Search Results');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Search Results'));

        this.secondaryNavService.clearItems();
        this.secondaryNavService.clearButtons();
        this.secondaryNavService.clearCurrentObject();
        this.secondaryNavService.setCurrentArea('Search Results', 'fa-search', null);
        this.secondaryNavService.showHeader(false);
        this.searchStateService.advancedFilters = [];

        this.sub = this.route.queryParams.subscribe((params) => {
            this.searchText = params['query'] ? params['query'] : '';
            this.isExactMatch = params['exactMatch'] ? params['exactMatch'] != '0' : (CompanySettings.SearchExactMatch && CompanySettings.SearchExactMatch == 'true');
            if (params['types'] != undefined) {
                this.searchTypes = params['types'].split(',').filter((x): x is string => x.length > 0);
            }
            let keepFilter = params['f'] ? (params['f'] == 1 ? true : false) : false;
            this.searchStateService.loadState(this.searchText, this.searchTypes, keepFilter);
            if (params['explain'] != undefined) {
                this.searchStateService.setExplain(params['explain'] == 'please');
            }
            if (this.searchText.length > 0) {
                this.doSearch();
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
        if (this.sidePanelTab === "filters" || this.selection == null) {
            return true;
        }
        if (this.sidePanelTab === "detail") {
            return this.selection?.AssetUid !== null;
        }
        if (this.sidePanelTab === "dataprofile") {
            return this.selection.HasProfiling;
        }
    }

    //Advanced filters changed
    filterChanged(options) {
        this.doSearch(true);
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
}