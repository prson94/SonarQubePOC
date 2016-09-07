///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Component, OnInit} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SearchService, TypeaheadSearchService } from '../../services/index';
import { SearchResultsObject, SearchCategories, SearchResult } from '../../models/search-result.model';
import {SelectItem} from 'primeng/primeng';

@Component({
    selector: 'd3s-home-search',
    template: `      
                <div class="search-input-container">           
                    <div class="search-input-text-container">
                        <input #search [(ngModel)]="searchText" (keyup)="checkSearchKey($event);" type="text" id="home-search-text" placeholder="What do you want to find?" class="search-input-text" autofocus autocomplete="off" />
                    </div>
                    <div class="search-input-exact-container">
                        <div class="adv-search-btn">
                            <label><input type="checkbox" name="search-exact-chk" id="search-exact-chk" [(ngModel)]="isExactMatch"> Exact match</label>
                        </div>
                    </div>
                    <div class="search-input-types-container">
                        <div class="search-btn">
                            <p-multiSelect [options]="searchObjectTypes" [(ngModel)]="searchTypes"></p-multiSelect>                        
                        </div>
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
                <d3s-search-autocomplete-list [searchText]="searchText" [element]="search" *ngIf="autocompletions.length > 0" [autocompletions]="autocompletions"></d3s-search-autocomplete-list>
                <d3s-search-results [itemsPerPage]="resultsPerPage" [results]="searchResults" [categories]="categories" (paginateClick)="paginate($event);" (categoryClick)="filterByCategory($event);"></d3s-search-results>

                `,
    providers: [SearchService, TypeaheadSearchService],
})

export class HomeSearchComponent extends BaseComponent implements OnInit {
    private searchResults: SearchResultsObject;
    private categories: SearchCategories[] = [];
    private selectedCategory: SearchCategories;
    private searchText: string;
    private resultsPerPage: number = 5;
    private pageNumber: number = 0;
    private searchTypes: string[] = ["Artifact", "Synonym"];
    private simpleSearchID: number = 0;
    private autocompleteResultSize: number = 5;

    private autocompletions: SearchResult[] = [];

    private searchObjectTypes: SelectItem[] = [        
        { value: "Attribute", label: "Attribute" },
        { value: "FusionAttributes", label: "Fusion" },
        { value: "FusionType", label: "Fusion Type" },
        { value: "Artifact", label: "Glossary" },
        { value: "Group", label: "Group" },
        { value: "Taxonomy", label: "Model" },
        { value: "Domain", label: "Reference" },
        { value: "Users", label: "User" },
        { value: "Synonym", label: "Synonym" },
    ];

    private isExactMatch: boolean = true;
    

    constructor(private searchService: SearchService, private typeaheadSearchService: TypeaheadSearchService) {
        super();
    }

    ngOnInit() {
        
    }
        
    private doSearch(filterCategory?: SearchCategories) {
        this.searchService.getSearchResults(this.searchText, this.resultsPerPage, this.pageNumber, this.searchTypes, filterCategory, this.isExactMatch)
            .then(res => {
                this.autocompletions = [];
                this.searchResults = res;
                if (filterCategory == undefined) this.categories = res.Categories;
            });
    }

    private checkSearchKey(event) {                
        if (event.keyCode == 13) this.doSearch();
        else if (this.searchText.length > 3) {
            if (this.simpleSearchID > 0) {
                window.clearTimeout(this.simpleSearchID);
                this.simpleSearchID = 0;
            }

            this.simpleSearchID = window.setTimeout(() => this.doAutocompleteSearch(), 1000);
        }
    }

    private doAutocompleteSearch() {
        if (!this.searchText || this.searchText.length == 0) return;
        this.typeaheadSearchService.getResults(this.autocompleteResultSize, this.searchText, this.searchTypes)
            .then(res => {
                this.autocompletions = res;
            });
    }

    private filterByCategory(event) {        
        this.selectedCategory = event.category;
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
        
        this.pageNumber = event.first == 0 ? 0 : (event.first / this.resultsPerPage);

        this.doSearch(this.selectedCategory);
    }
};