import { Component, OnInit, OnDestroy, Input, Output, EventEmitter, OnChanges, SimpleChange} from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import {SelectItem} from 'primeng/primeng';
import { SearchService } from '../../services/search.service';
import { TypeaheadSearchService } from '../../services/typeahead-search.service';
import { SearchResultsObject, SearchCategories, SearchResult, AdvancedSearchFilter } from '../../models/search-result.model';
import { DropdownOption } from '../../models/dropdown.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { SubscriptionLike as ISubscription } from 'rxjs';

@Component({
    selector: 'd3s-search-input',
    template: `      
                <div *ngIf="!isAdvancedMode">
                    <div class="search-input-container" >           
                        <div class="search-input-text-container">                        
                            <input #search [ngModel]="searchText" (ngModelChange)="searchText=$event;searchTextChange.emit(searchText);" (keyup)="checkSearchKey($event);" type="text" id="home-search-text" placeholder="What do you want to find?" class="search-input-text" autofocus autocomplete="off" />                        
                        </div>
                        <div class="search-input-exact-container hide-on-med-and-down">
                            <div class="adv-search-btn">
                                <label><input type="checkbox" name="search-exact-chk" id="search-exact-chk" [ngModel]="isExactMatch" (ngModelChange)="isExactMatch=$event;isExactMatchChange.emit(isExactMatch);"> Exact match</label>
                            </div>
                        </div>
                        <div class="search-input-types-container hide-on-med-and-down">
                            <div class="search-btn">
                                <p-multiSelect [options]="searchObjectTypes" [ngModel]="searchTypes" (ngModelChange)="searchTypes=$event;searchTypesChange.emit(searchTypes);"></p-multiSelect>                        
                            </div>
                        </div>
                        <div class="search-input-adv-container hide-on-med-and-down">
                            <button type="button" name="action" id="home-adv-btn" class="adv-search-btn" (click)="handleAdvancedClick()">Advanced&nbsp;<i class="fa fa-caret-down"></i></button>
                        </div>
                        <div class="search-input-button-container">
                            <button type="submit" name="action" id="home-search-btn" class="search-input-btn" (click)="triggerSearch()">
                                <i class="fa fa-search"></i>
                            </button>
                        </div>                    
                    </div>  
                    <d3s-search-autocomplete-list *ngIf="!isAdvancedMode" [searchText]="searchText" [element]="search" [autocompletions]="autocompletions"></d3s-search-autocomplete-list>            
                </div>
                <div *ngIf="isAdvancedMode" class="tile tile-detail">                             
                    <form (ngSubmit)="triggerAdvancedSearch()" #advSearchForm="ngForm">
                        <header>Advanced Search <d3s-tile-actions [hasAdd]="false" [hasClose]="true" (closeClick)="handleAdvancedClick()"></d3s-tile-actions></header>
                        <div *ngFor="let filter of advancedFilters; let last=last; let idx = index" class="row advSearchRow">
                            <div class="col s1 center-align">Field</div>
                            <div class="col s3">
                                <select [(ngModel)]="filter.field" [name]="'field'+idx" style="width:100%;" required>
                                        <option value="" disabled selected>Please Choose...</option>
                                        <option *ngFor="let p of fields" [value]="p.value">{{p.title}}</option>
                                </select>
                            </div>
                            <div class="col s3" *ngIf="filter.field != '_type'">
                                <input type="text" [(ngModel)]="filter.value" [name]="'input'+idx" style="width:100%" required placeholder="Enter a value" (keyup)="checkAdvSearchKey($event);">
                            </div>
                            <div class="col s3" *ngIf="filter.field == '_type'">
                                <select [(ngModel)]="filter.value" [name]="'inp'+idx"style="width:100%;" placeholder="Choose a type" required>
                                        <option value="" disabled selected>Please Choose...</option>
                                        <option *ngFor="let p of types" [value]="p.value">{{p.title}}</option>
                                </select>
                            </div>
                            <div class="col s1" *ngIf="filter.field != '_type'">
                                    <label><input type="checkbox" [(ngModel)]="filter.exact" [name]="'exm'+idx">Exact match</label>
                            </div>
                            <div class="col s1" *ngIf="filter.field == '_type'">&nbsp;</div>
                            <div class="col s1" *ngIf="last" (click)="addFilter()" style="cursor:pointer"><i class="fa fa-plus" aria-hidden="true" title="add filter" style="font-size:1.5em"></i></div>
                            <div class="col s1" *ngIf="!last" (click)="removeFilter(filter)"  style="cursor:pointer"><i class="fa fa-minus" aria-hidden="true" title="remove filter" style="font-size:1.5em"></i></div>
                        </div>
                        <div class="row">
                            <div class="col s1 offset-s1">
                                <button pButton [disabled]="!advSearchForm.form.valid" type="submit" label="Search" style="width:150px;"></button>                            
                            </div>
                        </div>
                    </form>
                </div>                     
                `,
    providers: [SearchService, TypeaheadSearchService],
})

export class SearchInputComponent extends BaseComponent implements OnChanges, OnDestroy {
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
    private searchSub: ISubscription;

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
        { title: "Glossary", value: "Artifact" },
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
        { value: "Artifact", label: "Glossary" },
        { value: "Group", label: "Group" },
        { value: "Taxonomy", label: "Model" },
        { value: "Reference", label: "Reference" },
        { value: "Resource", label: "User" },
        { value: "Synonym", label: "Grammatic Type" },
        { value: "Rule", label: "Data Quality" },
    ];
        
    private simpleSearchID: number = 0;
    private autocompleteResultSize: number = 20;

    private autocompletions: SearchResult[] = [];

    constructor(private router: Router, private searchService: SearchService, private typeaheadSearchService: TypeaheadSearchService) {
        super();
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

            this.simpleSearchID = window.setTimeout(() => this.doAutocompleteSearch(), 1000);
        }
    }

    private doAutocompleteSearch() {
        if (!this.searchText || this.searchText.length == 0) return;
        this.searchSub = this.typeaheadSearchService.getResults(this.autocompleteResultSize, this.searchText, this.searchTypes)
            .subscribe(res => {
                this.autocompletions = res;
            });
    }

    private removeFilter(filter) {
        let index = this.advancedFilters.findIndex(x => x == filter);

        if (index >= 0 && index < this.advancedFilters.length) {
            this.advancedFilters.splice(index,1);
            this.advancedFiltersChange.emit(this.advancedFilters);
        }
    }

    private addFilter() {
        this.advancedFilters.push(new AdvancedSearchFilter());
        this.advancedFiltersChange.emit(this.advancedFilters);
    }

    private handleAdvancedClick() {
        if (this.hasAdvanced) {
            this.isAdvancedMode = !this.isAdvancedMode;
                        
            this.isAdvancedModeChange.emit(this.isAdvancedMode);
        }
        else {
            this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_SEARCH_ROOT}?query=${this.searchText ? encodeURIComponent(this.searchText) : ''}&advanced=1&types=${this.searchTypes ? this.searchTypes.join(',') : ''}`);
        }
    }
    
};