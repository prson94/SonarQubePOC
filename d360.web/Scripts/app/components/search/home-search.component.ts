import { Component, Output, EventEmitter, Input } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SearchService } from '../../services/search.service';
import { TypeaheadSearchService } from '../../services/typeahead-search.service';
import { SearchResultsObject, SearchCategories, SearchResult } from '../../models/search-result.model';
import { CurrentCompanySettings } from '../../static/company-settings'
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { Router } from '@angular/router';

declare var CompanySettings;

@Component({
    selector: 'd3s-home-search',
    template: `               
                <d3s-hero-search-input [(isExactMatch)]="isExactMatch" [(searchTypes)]="searchTypes"></d3s-hero-search-input>                
                `,
    providers: [SearchService, TypeaheadSearchService],
})

export class HomeSearchComponent extends BaseComponent {
    @Output() resultsChange = new EventEmitter();
    @Input() hasResults: boolean;
    private searchResults: SearchResultsObject;
    private categories: SearchCategories[] = [];
    private selectedCategory: SearchCategories;
    private searchText: string;
    private isExactMatch: boolean = true;
    private searchTypes: string[] = CurrentCompanySettings.defaultSearchTypes ? CurrentCompanySettings.defaultSearchTypes.split(',') : [];

    private resultsPerPage: number = 5;
    private pageNumber: number = 0;
       
    constructor(private searchService: SearchService, private typeaheadSearchService: TypeaheadSearchService, private router: Router) {
        super();        
    }

    ngOnInit() {
        this.isExactMatch = (CompanySettings.SearchExactMatch && CompanySettings.SearchExactMatch == 'true');
    }

    private navigateSearch() {
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_SEARCH_ROOT}?query=${this.searchText ? encodeURIComponent(this.searchText) : ''}&advanced=0&types=${this.searchTypes ? this.searchTypes.join(',') : ''}`);
    }
};