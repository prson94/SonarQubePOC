import { Component, Output, EventEmitter, Input } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SearchService } from '../../services/search.service';
import { TypeaheadSearchService } from '../../services/typeahead-search.service';
import { SearchResults, SearchAggregation, SearchResult } from '../../models/search-result.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { Router } from '@angular/router';
import { CompanySettingsService } from '../../services/settings.service';
import { CompanySettingEnum } from '../../models/settings.model';

@Component({
    selector: 'd3s-home-search',
    template: `               
                <d3s-hero-search-input [(isExactMatch)]="isExactMatch" [(searchTypes)]="searchTypes"></d3s-hero-search-input>                
                `,
    providers: [TypeaheadSearchService],
})

export class HomeSearchComponent extends BaseComponent {
    @Output() resultsChange = new EventEmitter();
    @Input() hasResults: boolean;
    public searchResults: SearchResults;
    public categories: SearchAggregation[] = [];
    private selectedCategory: SearchAggregation;
    private searchText: string;
    isExactMatch: boolean = true;
    searchTypes: string[] = [];

    private resultsPerPage: number = 5;
    private pageNumber: number = 0;
       
    constructor(
        private searchService: SearchService,
        protected settingsService: CompanySettingsService,
        private typeaheadSearchService: TypeaheadSearchService,
        private router: Router) {
        super(settingsService);        
    }

    ngOnInit() {        
        this.searchTypes = (this.settingsService.getSettingById(CompanySettingEnum.DefaultSearchTypes).ScalarValue ?? "").split(',');
    }

    private navigateSearch() {
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_SEARCH_ROOT}?query=${this.searchText ? encodeURIComponent(this.searchText) : ''}&types=${this.searchTypes ? this.searchTypes.join(',') : ''}`);
    }
}