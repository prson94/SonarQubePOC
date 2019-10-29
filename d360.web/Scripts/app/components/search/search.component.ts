import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SearchService } from '../../services/search.service';
import { TypeaheadSearchService } from '../../services/typeahead-search.service';
import { SearchStateService } from './search-state.service';
import { SearchResultsObject, SearchCategories, AdvancedSearchFilter, SearchAggregationFilter } from '../../models/search-result.model';
import { CurrentCompanySettings } from '../../static/company-settings'
import { RightSidebarService } from '../../services/right-sidebar.service';
import { CheckTreeNode } from '../shared/small-widgets/check-tree/checktreenode';

declare var CompanySettings;

@Component({
    selector: 'd3s-search',
    template: ` <div class="search-page-full">
                    <div #title class="title-bar search">
                        <div class="title">
                            <span class="large icon badge"><i class="fa fa-search"></i></span>
                            <h1>Search Results</h1>
                            <d3s-search-input
                                (search)="inputSearch($event)"
                                [(isExactMatch)]="isExactMatch"
                                [(searchTypes)]="searchTypes"
                                [(searchText)]="searchText"
                                [style.width]="'100%'"
                                [style.height.px]="32"></d3s-search-input>
                        </div>
                    </div>
                <d3s-search-results [from]="fromNumber" 
                    [loading]="isLoading" [itemsPerPage]="resultsPerPage"
                    [results]="searchResults" 
                    [categories]="categories"
                    [filterTree]="filterTree"
                    (selectedCategoryChange)="filterCheckTree($event);"
                    (advFilterChanged)="searchFilterChanged($event);">
                </d3s-search-results>
                </div>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                `,
    providers: [SearchService, TypeaheadSearchService],
})

export class SearchComponent extends BaseComponent implements OnInit {
    public searchResults: SearchResultsObject;
    public categories: SearchCategories[] = [];
    public filterTree: CheckTreeNode[];
    public selectedCategory: SearchCategories;
    public searchText: string;
    public isExactMatch: boolean = true;
    public searchTypes: string[] = CurrentCompanySettings.defaultSearchTypes ? CurrentCompanySettings.defaultSearchTypes.split(',') : [];
    public advancedFilters: AdvancedSearchFilter[] = [];

    public resultsPerPage: number = 10;
    public fromNumber: number = 0;
    public sub: any;

    private displayNameLookup: string[];

    @ViewChild('title') title: ElementRef;

    constructor(private route: ActivatedRoute,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected rightSidebarService: RightSidebarService,
        private searchService: SearchService,
        private searchStateService: SearchStateService,
        private typeaheadSearchService: TypeaheadSearchService) {
        super();
        this.rightSidebarService = rightSidebarService;
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Search Results');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Search Results'));

        this.rightSidebarService.clearItems();
        this.rightSidebarService.clearButtons();
        this.rightSidebarService.clearCurrentObject();
        this.rightSidebarService.setCurrentArea('Search Results', 'fa-search', null);
        this.rightSidebarService.showHeader(false);

        this.sub = this.route.queryParams.subscribe(params => {
            this.searchText = params['query'] ? params['query'] : '';
            this.isExactMatch = params['exactMatch'] ? params['exactMatch'] != '0' : (CompanySettings.SearchExactMatch && CompanySettings.SearchExactMatch == 'true');
            if (params['types'] != undefined) {
                this.searchTypes = params['types'].split(',').filter((x): x is string => x.length > 0);
            }
            this.searchStateService.setSearchCategories(this.searchTypes);
            this.searchStateService.setFieldFilters(this.advancedFilters);
            if (this.searchText.length > 0) {
                this.doSearch();
            }
        });
    }

    private searchFilterChanged(options) {
        this.advancedFilters = options;
        this.searchStateService.setFieldFilters(this.advancedFilters);
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

    public doSearch() {
        this.searchStateService.search(this.isExactMatch ? `'${this.searchText}'` : this.searchText);
    }

    public filterCheckTree(selectedNodes: CheckTreeNode[]) {
        this.searchStateService.setAggregationFilter("d3sGroup", selectedNodes.filter((x) => x.type == "category").map((x) => x.data));
        this.searchStateService.setAggregationFilter("Type", selectedNodes.filter((x) => x.type == "subCategory").map((x) => x.data));

        this.doSearch();
    }
};