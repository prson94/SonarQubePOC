import {debounceTime} from 'rxjs/operators';
import { Component, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { TypeaheadSearchService } from '../../../services/typeahead-search.service';
import { SearchResult } from '../../../models/search-result.model';
import { Router, NavigationEnd } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { SubscriptionLike as ISubscription } from 'rxjs';

declare var CompanySettings;

@Component({
    selector: 'd3s-header-typeahead-search',    
    template: ` <span #item class="header-search header-table" (keyup)="checkKey($event,ac)" >
                    <div class="header-search-input flat light">
                        <p-autoComplete #ac size="300" 
                                styleClass="global-search-typeahead" 
                                scrollHeight="400px"
                                [(ngModel)]="result" 
                                [suggestions]="results" 
                                field="Name"
                                (completeMethod)="search($event)"                              
                                placeholder="Search..."  
                                [minLength]="1"  
                                (onBlur)="clearValue()"
                                (onSelect)="selectItem(ac)">                       
                            <ng-template let-result pTemplate="item">
                                <div>                                
                                    <div class="search-typeahead-suggestion"><i *ngIf="result?.Icon" class="folder-icon fa {{result.Icon}}"></i><span *ngIf="result?.ImageUrl" class="folder-icon"><img [src]="result.ImageUrl" /></span><span>{{result.DisplayName}}</span>
                                        <span *ngIf="result?.Tags"  class="tags">
                                            <span *ngFor="let restag of result?.Tags" class="tag-item-wrapper" [innerHtml]="restag.Highlight" style="background: #BDC3C7;margin-left: 4px;border-radius: 3px;padding: 4px;text-align: center;"></span>
                                        </span>
                                    </div>
                                 </div>                            
                            </ng-template>
                        </p-autoComplete>
                    <i class="fa fa-search" (click)="openSearch()"></i>
                    </div>
                <span>`,
    providers: [TypeaheadSearchService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HeaderTypeaheadSearchComponent implements OnDestroy {
   
    public result: SearchResult;
    public searchText: string;
    public results: SearchResult[];
    private searchSub: ISubscription
    private defaultSearchOptions: string[];

    constructor(
        private router: Router,
        private typeaheadSearchService: TypeaheadSearchService,
        private ref: ChangeDetectorRef
    ) {
        this.defaultSearchOptions = CompanySettings.DefaultSearchTypes ? CompanySettings.DefaultSearchTypes.split(',') : [];
    }
    
    ngOnDestroy(): void {
        if (this.searchSub) this.searchSub.unsubscribe();
    }

    search(event) {
        this.searchText = event.query;
        this.searchSub = this.typeaheadSearchService.getResults(20, event.query, this.defaultSearchOptions).pipe(
            debounceTime(400))
            .subscribe(data => {
                this.results = data;
                this.ref.markForCheck();
            });        
    }

    openSearch() {
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_SEARCH_ROOT}?query=${this.searchText ? encodeURIComponent(this.searchText) : ''}&advanced=0&types=${this.defaultSearchOptions ? this.defaultSearchOptions.join(',') : ''}`);
    }
    
    selectItem(ac) {                
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(this.result.Url));        
        this.removeFocus(ac);
    }

    removeFocus(ac) {
        if (ac) {
            window.setTimeout(() => {
                if (ac && ac.el && ac.el.nativeElement) {
                    var inputs = ac.el.nativeElement.getElementsByClassName('ui-autocomplete-input');
                    if (inputs && inputs.length > 0) {
                        inputs[0].blur();
                    }
                }
            }, 300);
        }
    }

    checkKey(event,ac) {        
        if (event.keyCode == 13) {            
            this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_SEARCH_ROOT}?query=${event.srcElement.value ? encodeURIComponent(event.srcElement.value) : ''}&advanced=0&types=${this.defaultSearchOptions ? this.defaultSearchOptions.join(',') : ''}`);
            this.removeFocus(ac);
        }
    }   

    clearValue() {
        if (this.result) {
            this.result = undefined;
            this.ref.markForCheck();
        }
    }
}