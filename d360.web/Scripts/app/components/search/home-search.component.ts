import { Component, EventEmitter, Input, Output } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SearchService } from '../../services/search.service';
import { TypeaheadSearchService } from '../../services/typeahead-search.service';
import { SearchAggregation, SearchResults } from '../../models/search-result.model';
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
    isExactMatch: boolean = true;
    searchTypes: string[] = [];
       
    constructor(
        protected settingsService: CompanySettingsService) {
        super(settingsService);        
    }

    ngOnInit() {        
        this.searchTypes = (this.settingsService.getSettingById(CompanySettingEnum.DefaultSearchTypes).ScalarValue ?? "").split(',');
    }
}