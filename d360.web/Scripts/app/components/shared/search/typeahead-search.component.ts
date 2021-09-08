import { Component, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, Input, OnInit, SimpleChange } from '@angular/core';
import { TypeaheadSearchService } from '../../../services/typeahead-search.service';
import { SearchResult } from '../../../models/search-result.model';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { SearchSession } from '../../search/search-session';
import { SubscriptionLike as ISubscription, Subject } from 'rxjs';

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
    @Input() keepFilter: boolean = false;

    public result: SearchResult;
    public searchText: string;
    public results: SearchResult[];
    private searchSub: ISubscription
    private defaultSearchOptions: string[];
    private endSearchAllOption: SearchResult;
    private endSearchAllTypeToken: string = '__SHOWALL__';
    private options: string[];

    private typeAheadQuery$ = new Subject<string>();

    isSearchInProgress: boolean = false;

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

        this.options = !this.searchOptions ? this.defaultSearchOptions : this.searchOptions;
        this.createSubscription();
    }

    createSubscription() {
        if (this.searchSub) this.searchSub.unsubscribe();
        this.searchSub = this.typeaheadSearchService.getResults(this.typeAheadQuery$, 20, this.options)
            .subscribe(data => {
                this.results = data;
                if (this.results.length > 0) {
                    this.results.push(this.endSearchAllOption);
                }
                this.isSearchInProgress = false;
                this.ref.markForCheck();
            });
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (changes['defaultValue']) {
            this.result = new SearchResult();
            this.result.Name = this.defaultValue;
        }
        if (changes['searchOptions']) {
            this.options = this.searchOptions;
            this.createSubscription();
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
        this.isSearchInProgress = true;
        this.typeAheadQuery$.next(event.query);
    }

    openSearch() {
        if (this.result)
            this.searchText = (typeof this.result === 'string') ? this.result : this.result.Name;
        this.navigateQuery(this.searchText);
    }

    private navigateQuery(q: string) {
        let options = !this.searchOptions ? this.defaultSearchOptions : this.searchOptions;
        let url = `${SiteUrlHelpers.SITE_URL_SEARCH_ROOT}?query=${q ? encodeURIComponent(q) : ''}${(this.keepFilter) ? '&f=1' : ''}&types=${options ? options.join(',') : ''}`
        if (!this.keepFilter) {
            SearchSession.removeState(q);
        }
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
                    var inputs = ac.el.nativeElement.getElementsByClassName('p-autocomplete-input');
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
        this.typeAheadQuery$.next("");
        if (this.result) {
            if (!this.defaultValue) {
                this.result = undefined;
            }
            this.ref.markForCheck();
        }
    }
}

import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";
import { RouterModule } from '@angular/router';

import { AutoCompleteModule } from 'primeng/autocomplete';
import { TreeModule } from 'primeng/tree';
import { OverlayPanelModule } from 'primeng/overlaypanel';
import { DialogModule } from 'primeng/dialog';
import { SharedModule } from 'primeng/api';
import { AssetPathWidgetModule } from '../../search/asset-path-widget/asset-path-widget.module';



@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        //d3s
        AssetPathWidgetModule,

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

