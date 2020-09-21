import { Component, OnInit, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
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
    templateUrl: './search.component.html',
    providers: [TypeaheadSearchService],
    host: {
        '(window:resize)': 'setResultsHeight()'
    },
})

export class SearchComponent extends BaseComponent implements OnInit, AfterViewInit {
    public searchResults: SearchResultsObject;
    public categories: SearchCategories[] = [];
    public searchText: string;
    public isExactMatch: boolean = true;
    public searchTypes: string[] = CurrentCompanySettings.defaultSearchTypes ? CurrentCompanySettings.defaultSearchTypes.split(',') : [];
    public advancedFilters: AdvancedSearchFilter[] = [];

    public resultsPerPage: number = 10;
    public fromNumber: number = 0;
    public sub: any;

    newFilterOptions: any[] = [
        { field: "Name", value: 'any' },
        { field: "Description", value: 'any' },
        { field: "Tags", value: 'any' }
    ];

    @ViewChild('searchContainer', { static: false }) searchContainer: ElementRef;
    @ViewChild('title', { static: false }) title: ElementRef;

    constructor(private route: ActivatedRoute,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected secondaryNavService: SecondaryNavService,
        public searchStateService: SearchStateService,
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

    ngAfterViewInit() {
        this.setResultsHeight();
    }

    setResultsHeight() {
        window.setTimeout(() => {
            if (this.searchContainer && this.searchContainer.nativeElement) {
                this.searchContainer.nativeElement.style.height = (window.innerHeight - 125) + 'px';
            }
        }, 50);
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
        this.searchStateService.search(this.searchText, resetPage);
    }

};