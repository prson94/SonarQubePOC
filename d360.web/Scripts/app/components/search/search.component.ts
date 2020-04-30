import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { TypeaheadSearchService } from '../../services/typeahead-search.service';
import { SearchStateService } from './search-state.service';
import { SearchResultsObject, SearchCategories, AdvancedSearchFilter, SearchAggregationFilter } from '../../models/search-result.model';
import { CurrentCompanySettings } from '../../static/company-settings'
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { CheckTreeNode } from '../shared/small-widgets/check-tree/checktreenode';

declare var CompanySettings;

@Component({
    selector: 'd3s-search',
    template: ` <div class="search-page-full">
                    <div #title class="title-bar search">
                        <div class="title">
                            <span class="d3s-icon asset-icon"><i class="fa fa-search"></i></span>
                            <h1>Search Results</h1>
                            <d3s-search-input
                                (search)="inputSearch($event)"
                                [(isExactMatch)]="isExactMatch"
                                (isExactMatchChange)="exactMatchChance(isExactMatch)"
                                [(searchTypes)]="searchTypes"
                                [(searchText)]="searchText"
                                [style.width]="'100%'"
                                [style.height.px]="32"></d3s-search-input>
                        </div>
                    </div>
                <d3s-search-results [from]="fromNumber" 
                    [loading]="isLoading" [itemsPerPage]="resultsPerPage"
                    [results]="searchResults"
                    [selectedFilters] = "searchStateService.advancedFilters"
                    (selectedCategoryChange)="filterCheckTree($event);"
                    (advFilterChanged)="advancedFilterChanged($event);">
                </d3s-search-results>
                </div>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                `,
    providers: [TypeaheadSearchService],
})

export class SearchComponent extends BaseComponent implements OnInit {
    public searchResults: SearchResultsObject;
    public categories: SearchCategories[] = [];
    public searchText: string;
    public isExactMatch: boolean = true;
    public searchTypes: string[] = CurrentCompanySettings.defaultSearchTypes ? CurrentCompanySettings.defaultSearchTypes.split(',') : [];
    public advancedFilters: AdvancedSearchFilter[] = [];

    public resultsPerPage: number = 10;
    public fromNumber: number = 0;
    public sub: any;

    @ViewChild('title', { static: false }) title: ElementRef;

    constructor(private route: ActivatedRoute,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected secondaryNavService: SecondaryNavService,
        private searchStateService: SearchStateService,
        private typeaheadSearchService: TypeaheadSearchService) {
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

        this.sub = this.route.queryParams.subscribe(params => {
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

    private advancedFilterChanged(options) {
        this.searchStateService.setFieldFilters(options);
        this.doSearch();
    }

    private inputSearch($event) {
        this.searchText = $event.text;
        this.isExactMatch = $event.exactMatch;
        this.searchTypes = $event.types;
        this.searchStateService.setSearchCategories(this.searchTypes);
        if (this.searchText.length > 0) {
            this.doSearch();
        }
    }

    private exactMatchChance(isExactMatch) {
        this.isExactMatch = isExactMatch;
        this.doSearch();
    }

    public doSearch() {
        this.searchStateService.search(this.searchText);
    }

    public filterCheckTree(selectedNodes: CheckTreeNode[]) {
        var types = selectedNodes.filter((x) => x.type == "subCategory").map((x) => x.data);
        var categories = selectedNodes.filter((x) => x.type == "category").map((x) => x.data);
        if (types.length > 0) {
            categories = categories.concat(this.searchStateService.currentCategories.filter((x) => x.type == "category" && x.partialSelected == true).map((x) => x.data));
        }
        this.searchStateService.setAggregationFilter("d3sCategory", categories);
        this.searchStateService.setAggregationFilter("d3sAssetType", types);

        this.doSearch();
    }
};