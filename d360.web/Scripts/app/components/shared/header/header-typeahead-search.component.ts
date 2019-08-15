
import {debounceTime} from 'rxjs/operators';
import { Component, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { TypeaheadSearchService } from '../../../services/typeahead-search.service';
import { SearchResult } from '../../../models/search-result.model';
import { Router, NavigationEnd } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { SubscriptionLike as ISubscription } from 'rxjs';

@Component({
    selector: 'd3s-header-typeahead-search',    
    template: ` <span #item class="header-search header-table" [ngClass]="{'header-search-active':active}" (keyup)="checkKey($event)" >
                    <div class="header-search-input flat light">
                        <p-autoComplete size="300"
                                styleClass="global-search-typeahead" 
                                scrollHeight="400px"
                                [(ngModel)]="result" 
                                [suggestions]="results" 
                                field="Name"
                                (completeMethod)="search($event)"                              
                                placeholder="Search..."  
                                [minLength]="1"  
                                (onSelect)="selectItem()">                       
                            <ng-template let-result pTemplate="item">
                                <div>                                
                                   <div class="search-typeahead-suggestion"><i *ngIf="result.Icon" class="icon fa {{result.Icon}}"></i><span style="color:#999;">{{result.Type}}:</span> {{result.DisplayName}}</div>
                                </div>                            
                            </ng-template>
                        </p-autoComplete>
                    <i class="fa fa-search"></i>
                    </div>
                <span>`,
    providers: [TypeaheadSearchService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HeaderTypeaheadSearchComponent implements OnDestroy {
   
    public result: SearchResult;
    public searchText: string;
    public results: SearchResult[];
    public active: boolean = false;
    private hideHandle: number = 0;
    private searchSub: ISubscription

    constructor(
        private router: Router,
        private typeaheadSearchService: TypeaheadSearchService,
        private ref: ChangeDetectorRef
    ) { }


    ngOnDestroy(): void {
        if (this.searchSub) this.searchSub.unsubscribe();
    }

    search(event) {
        this.searchText = event.query;
        this.searchSub = this.typeaheadSearchService.getResults(20, event.query).pipe(
            debounceTime(400))
            .subscribe(data => {
                this.results = data;
                this.ref.markForCheck();
            });
        
    }

    show(item) {
        // check for any pending hides and cancel them
       if (this.hideHandle > 0) {
            window.clearTimeout(this.hideHandle);
            this.hideHandle = 0;
        }
        let panel = item.children[0].nextElementSibling;
        if (panel) {
            this.active = true;

            panel.style.zIndex = 1000;

            //panel.style.top = (item.offsetHeight - 1) + 'px'; // -1 for the border so it blends
            panel.style.right = '0px';

            //focus the input so user can just type
            // this needs to be done on timer so the elements are all visible and there.
            window.setTimeout(() => {
                var inputs = panel.getElementsByClassName("ui-autocomplete-input");                
                if (inputs && inputs.length > 0) {                    
                    inputs[0].focus();
                }
            }, 300);            
        }        
    }

    hide(item) {
        if (this.hideHandle > 0) return; //pending hide ignore new request
        //queue up a request to hide the window.
        this.hideHandle = window.setTimeout(() => {
            this.active = false;
            this.ref.markForCheck();
            },
            500);        
    }
    
    selectItem() {                
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(this.result.Url));
    }

    checkKey(event) {        
        if (event.keyCode == 13) {
            this.active = false;
            this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_SEARCH_ROOT}?query=${encodeURIComponent(event.srcElement.value)}`);
        }
    }   
}

