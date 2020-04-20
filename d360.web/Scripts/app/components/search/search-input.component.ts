import { Component, OnDestroy, Input, Output, EventEmitter } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { SearchService } from '../../services/search.service';
import { TypeaheadSearchService } from '../../services/typeahead-search.service';
import { SearchResult, AdvancedSearchFilter } from '../../models/search-result.model';
import { SubscriptionLike as ISubscription } from 'rxjs';

declare var CompanySettings;
@Component({
    selector: 'd3s-search-input',
    template: `<div class="titlebar-search">           
                    <div class="field grow mr10">
                        <d3s-header-typeahead-search 
                                  [additionalCssClasses]="'gov-search'" 
                                  [autocompletePlaceholder]="'What are you looking for?'"
                                  [searchOptions]="searchTypes"
                                  [defaultValue]="searchText"
                                  [isExactMatch]="isExactMatch"
                                  [keepFilter]="true">
                        </d3s-header-typeahead-search>
                    </div>
                    <!--label class="checkbox mr10"><input type="checkbox" [ngModel]="isExactMatch" (ngModelChange)="isExactMatch=$event;isExactMatchChange.emit(isExactMatch);"><span>Match exact phrase</span></label-->
                </div>
              `,
    providers: [SearchService, TypeaheadSearchService],
})

export class SearchInputComponent extends BaseComponent implements OnDestroy {
    @Input() isExactMatch: boolean = true;
    @Output() isExactMatchChange = new EventEmitter();

    @Input() searchTypes: string[] = ["BusinessAsset", "Synonym"];
    @Output() searchTypesChange = new EventEmitter();

    @Input() searchText: string;
    @Output() searchTextChange = new EventEmitter();

    @Output() search = new EventEmitter();

    @Input() advancedFilters: AdvancedSearchFilter[] = [];
    @Output() advancedFiltersChange = new EventEmitter();

    @Input() newSearch: boolean = false;
    private searchSub: ISubscription;
    private autocompleteLoading: boolean = false;

    private simpleSearchID: number = 0;
    private autocompleteResultSize: number = 20;

    private autocompletions: SearchResult[] = [];
    private autocompleteWidth: number = 0;

    constructor(private router: Router, private searchService: SearchService, private typeaheadSearchService: TypeaheadSearchService) {
        super();
    }

    ngOnDestroy(): void {
        if (this.searchSub) this.searchSub.unsubscribe();
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
            this.autocompleteLoading = false;
            window.clearTimeout(this.simpleSearchID);
            this.simpleSearchID = 0;
        }
    }

    private checkSearchKey(event) {
        if (event.keyCode == 13) {
            this.triggerSearch();
        }

        else if (this.searchText && this.searchText.length >= 3) {
            this.cancelAutocomplete();
            this.autocompleteLoading = true;
            this.simpleSearchID = window.setTimeout(() => this.doAutocompleteSearch(), 1000);
        }
    }

    private doAutocompleteSearch() {
        if (!this.searchText || this.searchText.length == 0) return;
        this.searchSub = this.typeaheadSearchService.getResults(this.autocompleteResultSize, this.searchText, this.searchTypes)
            .subscribe(res => {
                this.autocompletions = res;
                this.autocompleteLoading = false;
            });
    }
    
};