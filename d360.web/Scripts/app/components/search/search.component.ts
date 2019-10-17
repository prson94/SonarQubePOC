import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SearchService } from '../../services/search.service';
import { TypeaheadSearchService } from '../../services/typeahead-search.service';
import { SearchResultsObject, SearchCategories, AdvancedSearchFilter } from '../../models/search-result.model';
import { CurrentCompanySettings } from '../../static/company-settings'
import { RightSidebarService } from '../../services/right-sidebar.service';
import { SettingsHelper } from '../../models/settings.model';


declare var CompanySettings;

@Component({
    selector: 'd3s-search',
    template: ` <div class="search-page-full">
                    <div #title class="title-bar search">
                        <div class="title">
                            <span class="large icon badge"><i class="fa fa-search"></i></span>
                            <h1>Search Results</h1>
                            <d3s-search-input [newSearch]="true" (search)="doSearch()" [isAdvancedMode]="showAdvanced" (advancedFiltersChange)="changeheight()" (isAdvancedModeChange)="handleAdvancedChange($event)" [(advancedFilters)]="advancedFilters" [(isExactMatch)]="isExactMatch" [(searchTypes)]="searchTypes" [hasAdvanced]="true" [(searchText)]="searchText" [style.width]="'100%'" [style.height.px]="32"></d3s-search-input>
                        </div>
                    </div>
                <d3s-search-results [from]="fromNumber" 
                    [loading]="isLoading" [itemsPerPage]="resultsPerPage"
                    [useSubscription]="true"
                    [results]="searchResults" 
                    [categories]="categories" 
                    (paginateClick)="paginate($event);" 
                    (selectedCategoryChange)="filterByCategory($event);"></d3s-search-results>
                </div>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                `,
    providers: [SearchService, TypeaheadSearchService],
})

export class SearchComponent extends BaseComponent implements OnInit {
    public searchResults: SearchResultsObject;
    public categories: SearchCategories[] = [];
    public selectedCategory: SearchCategories;
    public searchText: string;
    public isExactMatch: boolean = true;
    public searchTypes: string[] = CurrentCompanySettings.defaultSearchTypes ? CurrentCompanySettings.defaultSearchTypes.split(',') : [];
    public advancedFilters: AdvancedSearchFilter[] = [];

    public resultsPerPage: number = 10;
    public fromNumber: number = 0;
    public sub: any;
    public showAdvanced: boolean = false;

    private displayNameLookup: string[];

    @ViewChild('title') title: ElementRef;

    constructor(private route: ActivatedRoute,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected rightSidebarService: RightSidebarService,
        private searchService: SearchService,
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
            this.showAdvanced = params['advanced'] == '1';
            this.searchText = params['query'] ? params['query'] : '';
            this.isExactMatch = params['exactMatch'] ? params['exactMatch'] != '0' : (CompanySettings.SearchExactMatch && CompanySettings.SearchExactMatch == 'true');
            if (params['types'] != undefined) {
                this.searchTypes = params['types'].split(',').filter((x): x is string =>  x.length > 0);
            }
            if (this.searchText.length > 0) this.doSearch();

        });

        if (this.showAdvanced) {
            this.changeheight();
        }
    }

    handleAdvancedChange(event) {
        this.showAdvanced = event;
        this.searchResults = null;
        this.changeheight();
    }

    private changeheight() {
        window.setTimeout(() => {
            if (this.title.nativeElement) {
                let tiles = this.title.nativeElement.getElementsByClassName('tile');
                if (tiles.length > 0) {
                    let dims = this.title.nativeElement.getElementsByClassName('tile')[0].getBoundingClientRect();
                    console.log(dims);
                    this.title.nativeElement.style.height = dims.bottom + 'px';

                } else {
                    this.title.nativeElement.style.height = '67px';
                }
            }
        }, 100);
    }

    private getDisplayLookup(category:string) {
        if (this.displayNameLookup == undefined) {
            this.displayNameLookup = SettingsHelper.getSearchTypesList().reduce(function (map, obj) {
                map[obj.value] = obj.title;
                return map;
            }, []);
        }
        if (this.displayNameLookup[category] != undefined)
            return this.displayNameLookup[category];
        else
            return category;
    }

    public doSearch(filterCategory?: SearchCategories) {
        this.isLoading = true;
        this.searchService.getSearchResults(this.searchText, this.resultsPerPage, this.fromNumber, (this.showAdvanced ? undefined : this.searchTypes), filterCategory, this.isExactMatch, this.showAdvanced ? this.advancedFilters : undefined)
            .subscribe(res => {
                this.isLoading = false;
                this.searchResults = res;
                if (filterCategory == undefined) {
                    this.categories = res.Categories.map((val) => {
                        return {
                            "Name": val.Name,
                            "DisplayName": this.getDisplayLookup(val.Name),
                            "ResultCount": val.ResultCount,
                            "Categories": val.Categories
                        }
                    });
                }
            });
    }

    public filterByCategory(category) {
        this.selectedCategory = category;
        this.fromNumber = 0;
        this.doSearch(this.selectedCategory);
    }

    public paginate(event) {
        if (!event.size == undefined) {
            console.log("ERROR : MISSING ITEMS PER PAGE.");

            return;
        }

        if (event.page == undefined) {
            console.log("ERROR : MISSING PAGE NUMBER.");

            return;
        }

        if (!event.first == undefined) {
            console.log("ERROR : MISSING INDEX OF FIRST PAGE.");

            return;
        }

        this.resultsPerPage = event.size;
        
        this.fromNumber = event.first;

        this.doSearch(this.selectedCategory);
    }
};