import { Injectable } from '@angular/core';
import { SearchFullResult, SearchModel, SearchQuery, SearchResult, SearchResults } from '../models/search-result.model';
import { HttpClient, HttpContext, HttpHeaders } from '@angular/common/http';
import { catchError, debounceTime, delay, distinctUntilChanged, map, shareReplay, switchMap, takeUntil } from 'rxjs/operators';
import { Observable, of, Subject, throwError } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';
import { SearchType, SettingsHelper } from '../models/settings.model';
import { IndexableStatus, IndexableType, IndexPartialRebuild } from "../models/search-admin.model";
import { ROUTE_INDEPENDENT_QUERY } from '../http-interceptors';
import { Table } from 'primeng/table';
import { FilterMatchMode } from 'primeng/api';
import { FeatureFlagsInitService } from './feature-flags-init.service';
import { FeatureFlags } from '../_shared/models/feature-flags';

@Injectable({
    providedIn: 'root'
})
export class SearchService extends BaseObservableService  {

	semanticUi: boolean;

	constructor(private http: HttpClient, messagesService: MessagesObservableService, featureFlagService: FeatureFlagsInitService) {
		super(messagesService);

		featureFlagService.getFlagValue(FeatureFlags.SemanticTypesUiFlag).then((flag) => {
			this.semanticUi = flag;
		});
	}

    public getEmptyResult(): SearchResults {
        const result = new SearchResults();
		result.Results = [];
        result.Aggregations = [];
        result.Matches = 0;
        return result;
    }

    serachTableLocally(table: Table, searchString: string): void {
        if(searchString.startsWith('*') && !searchString.endsWith('*')) {
            const refinedSearchString = searchString.replace(/^\*/i, '');
            table.filterGlobal(refinedSearchString, FilterMatchMode.ENDS_WITH);
        } else if (!searchString.startsWith('*') && searchString.endsWith('*')) {
            const refinedSearchString = searchString.replace(/\*$/i, '');
            table.filterGlobal(refinedSearchString, FilterMatchMode.STARTS_WITH);
        } else {
            const refinedSearchString =  searchString.replace(/(^\*|\*$)/ig, '');
            table.filterGlobal(refinedSearchString, FilterMatchMode.CONTAINS);
        }
    }

	getTypeAheadResults(term: string, size, types?: string[]): Observable<SearchResult[]> {
		term = term.substring(0, 255);

		return of().pipe(
			debounceTime(400),
			distinctUntilChanged(),
			switchMap((term) => {
				if (term === "") {
					return of(<SearchResult[]>[]);
				}
				const uri = `api/v2/search/typeahead?query=${encodeURIComponent(term)}&num=${size}&categories=${typeof types !== "undefined" ? types.join(',') : ''}`;
				return this.http.get(
					uri,
					{ context: new HttpContext().set(ROUTE_INDEPENDENT_QUERY, true) }
				).pipe(
					map((response) => <SearchResult[]>response),
					catchError((err) => this.handleError(err))
				);
			}));
	}

    getSearchResultsByQuery(query: SearchQuery): Observable<SearchResults> {
        if (typeof query.Term === "undefined" || query.Term === "") {
            //No search term, no results, no need to call endpoint
            return of(this.getEmptyResult());
		}

        return this.http
            .post('api/v2/search/results', query)
            .pipe(
                map((res) => <SearchResults>res),
                catchError((err) => {
                    let errorMessage = null;
                    if (Object.keys(err).indexOf("error") > -1) {
                        if (err.error.title === "Cannot connect to the search server") {
                            return throwError("ConnectionError");
                        }
                        errorMessage = err.error.message;
                    }
                    if (errorMessage === null || errorMessage === "") {
                        errorMessage = "An error has occurred.";
                    }
                    this.messages.showError("Search Error", errorMessage);
                    return of(this.getEmptyResult());
                })
            );
    }

    public downloadSearchExcel(query: SearchQuery): Observable<any> {
        return this.http.post(
			"api/v2/search/results",
			query, 
			{
				headers: new HttpHeaders({ 'Accept': 'application/octet-stream' }),
				responseType: 'blob' 
			}
		);
    }

    public getSearchCategories(showUsers: boolean = true, keepNotVisible: boolean = false): Observable<SearchType[]> {
        const exclude: string[] = [];
        if (!showUsers) {
            exclude.push('Group');
            exclude.push('User');
        }
        let categories: SearchType[] = SettingsHelper.getSearchTypesList().filter((t) => exclude.indexOf(t.value) === -1);
        
		if (!this.semanticUi) {
            categories = categories.filter((s) => s.value !== "SemanticType");
        }
        
        return this.getVisibleCategories().pipe(
            map((res) => categories.map((c) => {
                c.visible = res.indexOf(c.value) >= 0;
                return c;
            }).filter((c) => keepNotVisible || c.visible))
        );
    }

    //Observable for caching visible Categories
    private visibleCategories$: Observable<string[]>;
    //Subject used to control when the cache is complete
    private reload$ = new Subject<void>();

    //Public method that creates, if needed, and gets the cached Observable
    public getVisibleCategories(): Observable<string[]> {
        if (!this.visibleCategories$) {
            this.visibleCategories$ = this.requestVisibleCategories().pipe(takeUntil(this.reload$));
        }
        return this.visibleCategories$;
    }

    //Private method that calls and pipes it into a shareReplay Observable
    private requestVisibleCategories(): Observable<string[]> {
        return this.http
            .get(
                'api/v2/search/categories', 
                { context: new HttpContext().set(ROUTE_INDEPENDENT_QUERY, true) }
            ).pipe(
            map((res) => <string[]>res),
            catchError((err) => this.handleError(err)),
            shareReplay(1)
        );
    }

    public getIndexableTypes(): Observable<IndexableType[]> {
        return this.http
            .get("api/v2/search/indexableTypes")
            .pipe(
                map((res) => <IndexableType[]>res),
                catchError((err) => {
                    let errorMessage = null;
                    if (Object.keys(err).indexOf("error") > -1) {
                        errorMessage = err.error.message;
                    }
                    if (errorMessage === null || errorMessage === "") {
                        errorMessage = "An error has occurred.";
                    }
                    this.messages.showError("Search Error", errorMessage);
                    return [];
                })
            );
    }

    public getIndexableStatus(): Observable<IndexableStatus[]> {
        return this.http
            .get("api/v2/search/indexableStatus")
            .pipe(
                map((res) => <IndexableStatus[]>res),
                catchError((err) => {
                    let errorMessage = null;
                    if (Object.keys(err).indexOf("error") > -1) {
                        errorMessage = err.error.message;
                    }
                    if (errorMessage === null || errorMessage === "") {
                        errorMessage = "An error has occurred.";
                    }
                    this.messages.showError("Search Error", errorMessage);
                    return [];
                })
            );
    }

	public sendRebuildRequest(assets: IndexableStatus[]): Observable<any> {
		const requests: IndexPartialRebuild[] = [];
		assets.forEach((asset) => {
			requests.push({
				Class: asset.Class,
				AssetTypeUid: asset.AssetTypeUid
			});
		});
        return this.http
			.post(`api/v2/search/rebuild`, requests)
            .pipe(
                delay(1000),
                map((res) => res),
                catchError((err) => {
                    let errorMessage = null;
                    if (Object.keys(err).indexOf("error") > -1) {
                        errorMessage = err.error.message;
                    }
                    if (errorMessage === null || errorMessage === "") {
                        errorMessage = "An error has occurred.";
                    }
                    this.messages.showError("Search Error", errorMessage);
                    return [];
                })
            );
    }
}