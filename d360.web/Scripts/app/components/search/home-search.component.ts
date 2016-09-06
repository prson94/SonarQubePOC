///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Component, OnInit} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SearchService } from '../../services/index';
import { SearchResultsObject, SearchCategories } from '../../models/search-result.model';

@Component({
    selector: 'd3s-home-search',
    template: `      
                <div class="search-input-container">           
                    <div class="search-input-text-container">
                        <input [(ngModel)]="searchText" (keyup)="checkSearchKey($event);" type="text" id="home-search-text" placeholder="What do you want to find?" class="search-input-text" autofocus autocomplete="off" />
                    </div>
                    <div class="search-input-exact-container">
                        <div class="adv-search-btn">
                            <label><input type="checkbox" name="search-exact-chk" id="search-exact-chk" [(ngModel)]="isExactMatch"> Exact match</label>
                        </div>
                    </div>
                    <div class="search-input-types-container">
                        <div id="SearchTypesDropdown" class="search-btn"></div>
                    </div>
                    <div class="search-input-adv-container">
                        <button type="button" name="action" id="home-adv-btn" class="adv-search-btn" [routerLink]="'/a/search'">Advanced&nbsp;<i class="fa fa-caret-down"></i></button>
                    </div>
                    <div class="search-input-button-container">
                        <button type="submit" name="action" id="home-search-btn" class="search-input-btn" (click)="doSearch()">
                            <i class="fa fa-search"></i>
                        </button>
                    </div>
                </div>
                <d3s-search-results [itemsPerPage]="resultsPerPage" [results]="searchResults" [categories]="categories" (paginateClick)="paginate($event);" (categoryClick)="filterByCategory($event);"></d3s-search-results>

                `,
    providers: [SearchService],
})

export class HomeSearchComponent extends BaseComponent implements OnInit {
    private searchResults: SearchResultsObject;
    private categories: SearchCategories[] = [];
    private selectedCategory: SearchCategories;
    private searchText: string;
    private resultsPerPage: number = 5;
    private pageNumber: number = 0;
    private searchTypes: string[] = ["Artifact", "Synonym"];

    private isExactMatch: boolean = true;

    
    constructor(private searchService: SearchService) {
        super();
    }

    ngOnInit() {
        
    }

    private doSearch(filterCategory?: SearchCategories) {
        this.searchService.getSearchResults(this.searchText, this.resultsPerPage, this.pageNumber, filterCategory, this.isExactMatch)
            .then(res => {
                this.searchResults = res;
                if (filterCategory == undefined) this.categories = res.Categories;
            });
    }

    private checkSearchKey(event) {        
        if (event.keyCode == 13) this.doSearch();
    }

    private filterByCategory(event) {
        this.selectedCategory = event.category;
        this.doSearch(this.selectedCategory);
    }

    private paginate(event) {
        //this.paginateClick.emit({page: data.page, size: data.rows, first: data.first});

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
        console.log(event);
        this.pageNumber = event.first == 0 ? 0 : (event.first / this.resultsPerPage);

        this.doSearch(this.selectedCategory);
    }
};