import { Component, OnDestroy, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { SelectItem } from 'primeng/api';
import { SearchService } from '../../services/search.service';
import { TypeaheadSearchService } from '../../services/typeahead-search.service';
import { SearchResult } from '../../models/search-result.model';
import { DropdownOption } from '../../models/dropdown.model';
import { SubscriptionLike as ISubscription } from 'rxjs';
import { StringConstants } from '../../static/string-constants';

declare var CompanySettings;
@Component({
    selector: 'd3s-hero-search-input',
    template: ` <div class="hero-search-input">           
                        <p-multiSelect [options]="searchObjectTypes"    
                            [ngModel]="searchTypes" 
                            [filter]="false"
                            [showTransitionOptions]="'0ms ease-out'"
                            [hideTransitionOptions]="'0ms ease-in'"
                            [filterPlaceHolder]="'select all'"
                            [scrollHeight]="'400px'"
                            [baseZIndex]="2"
                            [defaultLabel]="'Search All Categories'"
                            [maxSelectedLabels]="1"
                            (ngModelChange)="searchTypes=$event;searchTypesChange.emit(searchTypes);">
                        </p-multiSelect>                        
                        <div class="field mr10">
                 <d3s-search-autocomplete-list [searchText]="searchText" [setwidth]="autocompleteWidth" [autocompletions]="autocompletions" [setTop]="25"></d3s-search-autocomplete-list> 
                            <input #searchip (keydown.enter)="triggerSearch()" 
                                [ngModel]="searchText" 
                                (keyup)="checkSearchKey($event);" 
                                (ngModelChange)="searchText=$event;searchTextChange.emit(searchText);autocompleteWidth=searchip.offsetWidth;" 
                                autofocus autocomplete="off"    
                                type="text" 
                                placeholder="What are you looking for?">
                                <span *ngIf="!autocompleteLoading" class="icon-holder"><i (click)="triggerSearch()" class="fa fa-search"></i></span>
                                <span *ngIf="autocompleteLoading" class="icon-holder"><i class="fa fa-spinner fa-spin"></i></span>
                        </div>
                 </div>  
                `,
    providers: [SearchService, TypeaheadSearchService],
})

export class HeroSearchInputComponent extends BaseComponent implements OnDestroy, OnInit {
    @Input() isExactMatch: boolean = true;
    @Output() isExactMatchChange = new EventEmitter();

    @Input() searchTypes: string[] = ["Artifact", "Synonym"];
    @Output() searchTypesChange = new EventEmitter();

    @Output() resultsChange = new EventEmitter();
    @Input() hasResults: boolean;

    @Input() searchText: string;
    @Output() searchTextChange = new EventEmitter();

    @Output() search = new EventEmitter();

    private searchSub: ISubscription;
    private autocompleteLoading: boolean = false;

    constructor(private router: Router, private searchService: SearchService, private typeaheadSearchService: TypeaheadSearchService) {
        super();
    }

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
        { title: StringConstants.AssetTypeClass_Business, value: "Artifact" },
        { title: StringConstants.AssetTypeClass_Technical, value: "Artifact" },
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
        { value: "Artifact", label: StringConstants.AssetTypeClass_Business },
        { value: "Artifact", label: StringConstants.AssetTypeClass_Technical },
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


    ngOnDestroy(): void {
        if (this.searchSub) this.searchSub.unsubscribe();
    }

    ngOnInit() {
        if (CompanySettings && CompanySettings.FusionEnabled == 'false') {
            this.searchObjectTypes = this.searchObjectTypes.filter(x => x.value != 'FusionAttributes' && x.value != 'FusionType');
            this.types = this.types.filter(x => x.value != 'FusionAttributes' && x.value != 'FusionType');
        }
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
