///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Component, OnInit, Input, Output, EventEmitter, DoCheck} from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import {SelectItem} from 'primeng/primeng';
import { SearchService, TypeaheadSearchService } from '../../services/index';
import { SearchResultsObject, SearchCategories, SearchResult } from '../../models/search-result.model';

@Component({
    selector: 'd3s-search-input',
    template: `      
                <div class="search-input-container">           
                    <div class="search-input-text-container">
                        <input #search [ngModel]="searchText" (ngModelChange)="searchText=$event;searchTextChange.emit(searchText);" (keyup)="checkSearchKey($event);" type="text" id="home-search-text" placeholder="What do you want to find?" class="search-input-text" autofocus autocomplete="off" />
                    </div>
                    <div class="search-input-exact-container">
                        <div class="adv-search-btn">
                            <label><input type="checkbox" name="search-exact-chk" id="search-exact-chk" [ngModel]="isExactMatch" (ngModelChange)="isExactMatch=$event;isExactMatchChange.emit(isExactMatch);"> Exact match</label>
                        </div>
                    </div>
                    <div class="search-input-types-container">
                        <div class="search-btn">
                            <p-multiSelect [options]="searchObjectTypes" [ngModel]="searchTypes" (ngModelChange)="searchTypes=$event;searchTypesChange.emit(searchTypes);"></p-multiSelect>                        
                        </div>
                    </div>
                    <div class="search-input-adv-container">
                        <button type="button" name="action" id="home-adv-btn" class="adv-search-btn" (click)="handleAdvancedClick()">Advanced&nbsp;<i class="fa fa-caret-down"></i></button>
                    </div>
                    <div class="search-input-button-container">
                        <button type="submit" name="action" id="home-search-btn" class="search-input-btn" (click)="triggerSearch()">
                            <i class="fa fa-search"></i>
                        </button>
                    </div>
                </div>                                
                <d3s-search-autocomplete-list [searchText]="searchText" [element]="search" [autocompletions]="autocompletions"></d3s-search-autocomplete-list>
                `,
    providers: [SearchService, TypeaheadSearchService],
})

export class SearchInputComponent extends BaseComponent {
    @Input() isExactMatch: boolean = true;
    @Output() isExactMatchChange = new EventEmitter();

    @Input() searchTypes: string[] = ["Artifact", "Synonym"];
    @Output() searchTypesChange = new EventEmitter();

    @Input() searchText: string;
    @Output() searchTextChange = new EventEmitter();

    @Output() search = new EventEmitter();

    @Input() hasAdvanced: boolean = false;

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
        
    private simpleSearchID: number = 0;
    private autocompleteResultSize: number = 5;

    private autocompletions: SearchResult[] = [];

    constructor(private router: Router, private searchService: SearchService, private typeaheadSearchService: TypeaheadSearchService) {
        super();
    }
    

    private triggerSearch() {
        this.cancelAutocomplete();
        this.autocompletions = [];
        this.search.emit({
            text: this.searchText,
            exactMatch: this.isExactMatch,
            types: this.searchTypes
        });
        
    }

    private cancelAutocomplete() {
        if (this.simpleSearchID > 0) {
            window.clearTimeout(this.simpleSearchID);
            this.simpleSearchID = 0;
        }
    }
    

    private checkSearchKey(event) {
        if (event.keyCode == 13) {
            this.triggerSearch();
        }

        else if (this.searchText.length > 3) {
            this.cancelAutocomplete();

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

    private handleAdvancedClick() {
        if (this.hasAdvanced) {

        }
        else {
            this.router.navigateByUrl(`/a/search?query=${this.searchText ? encodeURIComponent(this.searchText):''}&advanced=1&types=${this.searchTypes? this.searchTypes.join(','):''}`);
        }
    }
    
};