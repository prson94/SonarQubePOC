import { debounceTime } from 'rxjs/operators';
import { Component, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, Input, OnInit, SimpleChange } from '@angular/core';
import { TypeaheadSearchService } from '../../../services/typeahead-search.service';
import { SearchResult } from '../../../models/search-result.model';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { SubscriptionLike as ISubscription } from 'rxjs';

declare var CompanySettings;

@Component({
    selector: 'd3s-header-typeahead-search',
    templateUrl: 'typeahead-search.component.html',
    providers: [TypeaheadSearchService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class TypeaheadSearchComponent implements OnDestroy, OnInit {
    @Input() searchOptions: string[];
    @Input() autocompletePlaceholder: string = 'Search Govern...';
    @Input() additionalCssClasses: string = '';
    @Input() showBigButton: boolean = false;
    @Input() defaultValue: string;
    @Input() isExactMatch: boolean = undefined;

    public result: SearchResult;
    public searchText: string;
    public results: SearchResult[];
    private searchSub: ISubscription
    private defaultSearchOptions: string[];
    private endSearchAllOption: SearchResult;
    private endSearchAllTypeToken: string = '__SHOWALL__';

    private isSearchInProgress: boolean = false;

    constructor(
        private router: Router,
        private typeaheadSearchService: TypeaheadSearchService,
        private ref: ChangeDetectorRef
    ) {
        this.defaultSearchOptions = CompanySettings.DefaultSearchTypes ? CompanySettings.DefaultSearchTypes.split(',') : [];
        this.endSearchAllOption = new SearchResult();
        this.endSearchAllOption.Type = this.endSearchAllTypeToken;
    }

    ngOnInit() {
        if (this.defaultValue) {
            this.result = new SearchResult();
            this.result.Name = this.defaultValue;
        }
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['defaultValue']) {
            this.result = new SearchResult();
            this.result.Name = this.defaultValue;
        }
    }

    ngOnDestroy(): void {
        if (this.searchSub) this.searchSub.unsubscribe();
    }

    syncSearchText(event) {
        this.searchText = event.srcElement.value;
    }

    navigateToTag(tag: any, event:any, ac:any) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl("TAG", tag.Uid));
        this.removeFocus(ac);
        ac.hide();
        event.stopPropagation();
    }

    search(event) {
        this.searchText = event.query;
        let options = !this.searchOptions ? this.defaultSearchOptions : this.searchOptions;
        this.isSearchInProgress = true;
        this.searchSub = this.typeaheadSearchService.getResults(20, event.query, options).pipe(
            debounceTime(400))
            .subscribe(data => {
                this.results = data;
                if (this.results.length > 0) {
                    this.results.push(this.endSearchAllOption);
                }
                this.isSearchInProgress = false;
                this.ref.markForCheck();
            });
    }

    openSearch() {
        if (this.result)
            this.searchText = (typeof this.result === 'string') ? this.result : this.result.Name;
        this.navigateQuery(this.searchText);
    }

    private navigateQuery(q: string) {
        let options = !this.searchOptions ? this.defaultSearchOptions : this.searchOptions;
        let url = `${SiteUrlHelpers.SITE_URL_SEARCH_ROOT}?query=${q ? encodeURIComponent(q) : ''}&types=${options ? options.join(',') : ''}`
        if (this.isExactMatch !== undefined)
            url += '&exactMatch=' + (this.isExactMatch ? 1 : 0);
        this.router.navigateByUrl(url);
    }

    selectItem(ac) {
        if (this.result.Type == this.endSearchAllTypeToken) {
            this.result.Name = this.searchText
            this.openSearch()
        } else {
            this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(this.result.Url));
        }
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

    checkKey(event, ac) {
        if (event.keyCode == 13) {
            this.navigateQuery(event.srcElement.value);
            this.removeFocus(ac);
        }
    }

    clearValue() {
        if (this.result) {
            if (!this.defaultValue)
                this.result = undefined;
            this.ref.markForCheck();
        }
    }
}

import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";
import { RouterModule } from '@angular/router';

import { AutoCompleteModule } from 'primeng/autocomplete';
import { TreeModule } from 'primeng/tree';
import { OverlayPanelModule } from 'primeng/overlaypanel';
import { DialogModule } from 'primeng/dialog';
import { SharedModule } from 'primeng/shared';

import { PipesModule } from '../../../pipes/pipes.module';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        //d3s
        PipesModule,

        //primeng        
        AutoCompleteModule,
        OverlayPanelModule,
        SharedModule,
        TreeModule,
        DialogModule,
    ],
    declarations: [
        TypeaheadSearchComponent
    ],
    exports: [
        TypeaheadSearchComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]
})
export class TypeaheadSearchModule { }

