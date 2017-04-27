import { Component } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SearchService } from '../../services/search.service';
import { TypeaheadSearchService } from '../../services/typeahead-search.service';
import { SearchResultsObject, SearchCategories, SearchResult } from '../../models/search-result.model';
import { CurrentCompanySettings } from '../../static/company-settings'

@Component({
    selector: 'd3s-home-search',
    template: `               
                <d3s-search-input (search)="doSearch()" [(isExactMatch)]="isExactMatch" [(searchTypes)]="searchTypes" [(searchText)]="searchText"></d3s-search-input>                
                <d3s-search-results [itemsPerPage]="resultsPerPage" [results]="searchResults" [categories]="categories" (paginateClick)="paginate($event);" (selectedCategoryChange)="filterByCategory($event);"></d3s-search-results>
                `,
    providers: [SearchService, TypeaheadSearchService],
})

export class HomeSearchComponent extends BaseComponent {
    private searchResults: SearchResultsObject;
    private categories: SearchCategories[] = [];
    private selectedCategory: SearchCategories;
    private searchText: string;
    private isExactMatch: boolean = true;
    private searchTypes: string[] = CurrentCompanySettings.defaultSearchTypes ? CurrentCompanySettings.defaultSearchTypes.split(',') : [];

    private resultsPerPage: number = 5;
    private pageNumber: number = 0;
       
    constructor(private searchService: SearchService, private typeaheadSearchService: TypeaheadSearchService) {
        super();        
    }
    
    private doSearch(filterCategory?: SearchCategories) {        
        this.searchService.getSearchResults(this.searchText, this.resultsPerPage, this.pageNumber, this.searchTypes, filterCategory, this.isExactMatch)
            .then(res => {                
                this.searchResults = res;
                if (filterCategory == undefined) this.categories = res.Categories;
            });
    }
       
    private filterByCategory(category) {        
        this.selectedCategory = category;
        this.doSearch(this.selectedCategory);
    }

    private paginate(event) {        
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
        
        this.pageNumber = event.first;

        this.doSearch(this.selectedCategory);
    }
};