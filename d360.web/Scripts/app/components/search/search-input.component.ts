import { Component, OnInit, OnDestroy, Input, Output, EventEmitter, OnChanges, SimpleChange, ElementRef, ViewChild} from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { SelectItem } from 'primeng/api';
import { SearchService } from '../../services/search.service';
import { TypeaheadSearchService } from '../../services/typeahead-search.service';
import { SearchResultsObject, SearchCategories, SearchResult, AdvancedSearchFilter } from '../../models/search-result.model';
import { DropdownOption } from '../../models/dropdown.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { SubscriptionLike as ISubscription } from 'rxjs';

declare var CompanySettings;
@Component({
    selector: 'd3s-search-input',
    template: ` <div *ngIf="newSearch && !isAdvancedMode"
                class="titlebar-search">           
                        <div class="field grow mr10"><input #searchip (keydown.enter)="triggerSearch()" [ngModel]="searchText" (keyup)="checkSearchKey($event);" (ngModelChange)="searchText=$event;searchTextChange.emit(searchText);autocompleteWidth=searchip.offsetWidth;" autofocus autocomplete="off" type="text" placeholder="Please enter search terms"><i *ngIf="!autocompleteLoading" (click)="triggerSearch()" class="fa fa-search"></i><i *ngIf="autocompleteLoading" class="fa fa-spinner fa-spin"></i></div>
                        <label class="checkbox mr10"><input type="checkbox" [ngModel]="isExactMatch" (ngModelChange)="isExactMatch=$event;isExactMatchChange.emit(isExactMatch);"><span>Match Whole Words</span></label>
                        <p-multiSelect [options]="searchObjectTypes" [ngModel]="searchTypes" (ngModelChange)="searchTypes=$event;searchTypesChange.emit(searchTypes);"></p-multiSelect>
                </div>      
                <div *ngIf="!isAdvancedMode">
                    <div *ngIf="!newSearch" class="search-input-container">           
                        <div class="search-input-text-container">                        
                            <input #searchip [ngModel]="searchText" (ngModelChange)="searchText=$event;searchTextChange.emit(searchText);autocompleteWidth=searchip.offsetWidth;" (keyup)="checkSearchKey($event);" type="text" id="home-search-text" placeholder="What do you want to find?" class="search-input-text" autofocus autocomplete="off" />                        
                        </div>
                        <div class="search-input-exact-container hide-on-med-and-down">
                            <div class="adv-search-btn">
                                <label><input type="checkbox" name="search-exact-chk" id="search-exact-chk" [ngModel]="isExactMatch" (ngModelChange)="isExactMatch=$event;isExactMatchChange.emit(isExactMatch);"> Match Whole Words</label>
                            </div>
                        </div>
                        <div class="search-input-types-container hide-on-med-and-down">
                            <div class="search-btn">
                                <p-multiSelect [options]="searchObjectTypes" [ngModel]="searchTypes" (ngModelChange)="searchTypes=$event;searchTypesChange.emit(searchTypes);"></p-multiSelect>                        
                            </div>
                        </div>
                        <div class="search-input-button-container">
                            <button type="submit" name="action" id="home-search-btn" class="search-input-btn" (click)="triggerSearch()">
                                <i class="fa fa-search"></i>
                            </button>
                        </div>                    
                    </div>   
                    <d3s-search-autocomplete-list [searchText]="searchText" [setwidth]="autocompleteWidth" [autocompletions]="autocompletions"></d3s-search-autocomplete-list>            
                </div>
              `,
    providers: [SearchService, TypeaheadSearchService],
})

export class SearchInputComponent extends BaseComponent implements OnChanges, OnDestroy, OnInit {
    @Input() isExactMatch: boolean = true;
    @Output() isExactMatchChange = new EventEmitter();

    @Input() searchTypes: string[] = ["Artifact", "Synonym"];
    @Output() searchTypesChange = new EventEmitter();

    @Input() searchText: string;
    @Output() searchTextChange = new EventEmitter();

    @Output() search = new EventEmitter();

    @Input() hasAdvanced: boolean = false;

    @Input() isAdvancedMode: boolean = false;
    @Output() isAdvancedModeChange = new EventEmitter();

    @Input() advancedFilters: AdvancedSearchFilter[] = [];
    @Output() advancedFiltersChange = new EventEmitter();

    @Input() newSearch: boolean = false;
    private searchSub: ISubscription;
    private autocompleteLoading: boolean = false;

    private fields: DropdownOption[] = [
        { title: "Category", value: "Type" },
        { title: "Description", value: "Description" },
        { title: "Name", value: "Name" },
        { title: "Type", value: "_type" },
    ];

    private types: DropdownOption[] = [
        { title: "Attribute", value: "Attribute" },
        { title: "Fusion", value: "FusionAttributes" },
        { title: "Fusion Type", value: "FusionType" },
        { title: "Business", value: "Artifact" },
        { title: "Technical", value: "Artifact" },
        { title: "Group", value: "Group" },
        { title: "Model", value: "Taxonomy" },
        { title: "Reference", value: "Reference" },
        { title: "User", value: "Resource" },
        { title: "Grammatic Type", value: "Synonym" },
        { title: "Data Quality", value: "Rule" },
    ];

    private searchObjectTypes: SelectItem[] = [
        { value: "Attribute", label: "Attribute" },
        { value: "FusionAttributes", label: "Fusion" },
        { value: "FusionType", label: "Fusion Type" },
        { value: "Artifact", label: "Business" },
        { value: "Artifact", label: "Technical" },
        { value: "Group", label: "Group" },
        { value: "Taxonomy", label: "Model" },
        { value: "Policy", label: "Policy" },
        { value: "Reference", label: "Reference" },
        { value: "Resource", label: "User" },
        { value: "Synonym", label: "Grammatic Type" },
        { value: "Rule", label: "Data Quality" },
    ];
        
    private simpleSearchID: number = 0;
    private autocompleteResultSize: number = 20;

    private autocompletions: SearchResult[] = [];
    private autocompleteWidth: number = 0;

    constructor(private router: Router, private searchService: SearchService, private typeaheadSearchService: TypeaheadSearchService) {
        super();
    }

    ngOnInit() {
        if (CompanySettings && CompanySettings.FusionEnabled == 'false') {
            this.searchObjectTypes = this.searchObjectTypes.filter(x => x.value != 'FusionAttributes' && x.value != 'FusionType');
            this.types = this.types.filter(x => x.value != 'FusionAttributes' && x.value != 'FusionType');
        }
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.isAdvancedMode && this.advancedFilters.length == 0)
            this.advancedFilters.push(new AdvancedSearchFilter("Name", this.searchText) );
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

    private triggerAdvancedSearch() {
        this.cancelAutocomplete();
        this.autocompletions = [];
        this.search.emit({
            adv: this.advancedFilters
        });        
    }

    private cancelAutocomplete() {
        if (this.simpleSearchID > 0) {
            this.autocompleteLoading = false;
            window.clearTimeout(this.simpleSearchID);
            this.simpleSearchID = 0;
        }
    }

    private checkAdvSearchKey(event) {
        if (event.keyCode == 13) {
            this.triggerAdvancedSearch();
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