import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    Input,
    OnDestroy,
    OnInit,
	SimpleChange
} from '@angular/core';
import { Router } from '@angular/router';
import { Subject, SubscriptionLike as ISubscription } from 'rxjs';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { CoreModule } from '../../components/shared/core.module';
import { DataCyModule } from '../../directives/ig-data-cy.directive';
import { SearchResult } from '../../models/search-result.model';
import { CompanySettingEnum } from '../../models/settings.model';
import { AssetPath } from '../../pages/search/components/asset-path';
import { AuthenticationService } from '../../services/authentication.service';
import { SearchService } from '../../services/search.service';
import { CompanySettingsService } from '../../services/settings.service';
import { TypeaheadSearchService } from '../../services/typeahead-search.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { FormsModule } from '@angular/forms';
import { PipesModule } from '../../pipes/pipes.module';

@Component({
    selector: 'typeahead-search',
    templateUrl: 'typeahead-search.html',
	changeDetection: ChangeDetectionStrategy.OnPush,
	standalone: true,
	imports: [AutoCompleteModule, AssetPath, CoreModule, DataCyModule, FormsModule, PipesModule]
})
export class TypeaheadSearch implements OnDestroy, OnInit {
    @Input() searchOptions: string[];
    @Input() autocompletePlaceholder: string = $localize`Search Govern...`;
    @Input() additionalCssClasses: string = '';
    @Input() showBigButton: boolean = false;
    @Input() defaultValue: string;
    @Input() isExactMatch: boolean = undefined;
    @Input() keepFilter: boolean = false;

    public result: SearchResult;
    public searchText: string;
    public results: SearchResult[];
    private searchSub: ISubscription;
    private defaultSearchOptions: string[];
    private availableSearchOptions: string[];
    private endSearchAllOption: SearchResult;
    private endSearchAllTypeToken: string = '__SHOWALL__';
    private options: string[];
    private clearTimer: ReturnType<typeof setTimeout> = null;

    private typeAheadQuery$ = new Subject<string>();

    isSearchInProgress: boolean = false;

    constructor(
        private router: Router,
        settingsService: CompanySettingsService,
        private searchService: SearchService,
        private authenticationService: AuthenticationService,
        private typeaheadSearchService: TypeaheadSearchService,
        private ref: ChangeDetectorRef
    ) {
        this.defaultSearchOptions = (settingsService.getSettingById(CompanySettingEnum.DefaultSearchTypes).ScalarValue ?? "").split(',');
        this.endSearchAllOption = new SearchResult();
        this.endSearchAllOption.Type = this.endSearchAllTypeToken;
    }

    ngOnInit() {
        if (this.defaultValue) {
            this.result = new SearchResult();
            this.result.Name = this.defaultValue;
        }

        this.authenticationService.checkCurrentUserAdmin().subscribe((isAdmin) => {
            this.searchService.getSearchCategories(isAdmin, false).subscribe((res) => {
                this.availableSearchOptions = res.map((c) => c.value);
                this.setOptions(!this.searchOptions ? this.defaultSearchOptions : this.searchOptions);
            });
        });
    }

    createSubscription() {
        if (this.searchSub) {this.searchSub.unsubscribe();}
        this.searchSub = this.typeaheadSearchService.getResults(this.typeAheadQuery$, 20, this.options)
			.subscribe((data) => {
				const _results = data;
                if (_results.length > 0) {
                    _results.push(this.endSearchAllOption);
				}
				this.results = [..._results];
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
            this.setOptions(this.searchOptions);
        }
    }

    ngOnDestroy(): void {
        clearTimeout(this.clearTimer);
        if (this.searchSub) {this.searchSub.unsubscribe();}
    }

    private setOptions(options: string[]) {
        this.options = this.availableSearchOptions?.length ? options.filter((o) => this.availableSearchOptions.indexOf(o) !== -1) : options;
        this.createSubscription();
    }

    syncSearchText(event) {
        this.searchText = event.srcElement.value;
    }

	navigateToTag(tag: any, event: any, ac: any) {
		this.router.navigateByUrl(SiteUrlHelpers.federateUrl(SiteUrlHelpers.getObjectUrl("TAG", tag.Uid)));
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
            {this.searchText = (typeof this.result === 'string') ? this.result : this.result.Name;}
        this.navigateQuery(this.searchText);
    }

    private navigateQuery(q: string) {
        const url = `${SiteUrlHelpers.SITE_URL_SEARCH_ROOT}?query=${q ? encodeURIComponent(q) : ''}${(this.keepFilter) ? '&f=1' : ''}&types=${this.options ? this.options.join(',') : ''}`;
  //      if (!this.keepFilter) {
  //          SearchSession.removeState(q);
		//}
		this.router.navigateByUrl(SiteUrlHelpers.federateUrl(url));
    }

    selectItem(ac) {
        if (this.result.Type === this.endSearchAllTypeToken) {
            this.result.Name = this.searchText;
            this.openSearch();
		} else {
			this.router.navigateByUrl(SiteUrlHelpers.federateUrl(SiteUrlHelpers.convertClassicUrl(this.result.Url)));
        }
        this.removeFocus(ac);
    }

    removeFocus(ac) {
        if (ac) {
            window.setTimeout(() => {
                if (ac && ac.el && ac.el.nativeElement) {
                    const inputs = ac.el.nativeElement.getElementsByClassName('p-autocomplete-input');
                    if (inputs && inputs.length > 0) {
                        inputs[0].blur();
                    }
                }
            }, 300);
        }
    }

    checkKey(event, ac) {
        if (event.keyCode === 13) {
            this.navigateQuery(event.srcElement.value);
            this.removeFocus(ac);
        }
    }

    blurHandler() {
        if (this.showBigButton) {
            this.clearTimer = setTimeout(() => { this.clearValue(); }, 20000);
        } else {
            this.clearValue();
        }
    }

    focusHandler() {
        clearTimeout(this.clearTimer);
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

	get searchInProgressCss() {
		return this.isSearchInProgress ? "fa-spinner fa-spin" : "fa-search";
	}

    public showType(result: SearchResult): boolean {
        if (result.Group === "Semantic Type") {
            return false;
        }
        return (typeof result.Type !== "undefined");
    }
}

